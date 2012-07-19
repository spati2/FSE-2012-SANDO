﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Sando.Core.Extensions;
using Sando.ExtensionContracts.ProgramElementContracts;

namespace Sando.Parser
{
	public static class SrcMLParsingUtils
	{
		private static readonly XNamespace SourceNamespace = "http://www.sdml.info/srcML/src";
		private static readonly XNamespace PositionNamespace = "http://www.sdml.info/srcML/position";

		public static void ParseFields(List<ProgramElement> programElements, XElement elements, string fileName, int snippetSize)
		{
			IEnumerable<XElement> fields =
				from el in elements.Descendants(SourceNamespace + "class")
				select el.Element(SourceNamespace + "block");

			fields =
				from el in fields.Elements(SourceNamespace + "decl_stmt").Elements(SourceNamespace + "decl")
				where el.Element(SourceNamespace + "name") != null &&
						el.Element(SourceNamespace + "type") != null &&
						(
							(el.Element(SourceNamespace + "init") != null && el.Elements().Count() == 3) ||
							el.Elements().Count() == 2
						)
				select el;

			foreach(XElement field in fields)
			{
				string name;
				int definitionLineNumber;
				SrcMLParsingUtils.ParseNameAndLineNumber(field, out name, out definitionLineNumber);

				ClassElement classElement = RetrieveClassElement(field, programElements);
				Guid classId = classElement != null ? classElement.Id : Guid.Empty;
				string className = classElement != null ? classElement.Name : String.Empty;

				//parse access level and type
				XElement accessElement = field.Element(SourceNamespace + "type").Element(SourceNamespace + "specifier");
				AccessLevel accessLevel = accessElement != null ? StrToAccessLevel(accessElement.Value) : AccessLevel.Internal;

				IEnumerable<XElement> types = field.Element(SourceNamespace + "type").Elements(SourceNamespace + "name");
				string fieldType = String.Empty;
				foreach(XElement type in types)
				{
					fieldType += type.Value + " ";
				}
				fieldType = fieldType.TrimEnd();

				string initialValue = String.Empty;
				XElement initialValueElement = field.Element(SourceNamespace + "init");
				if(initialValueElement != null)
					initialValue = initialValueElement.Element(SourceNamespace + "expr").Value;

				string fullFilePath = System.IO.Path.GetFullPath(fileName);
				string snippet = RetrieveSnippet(fileName, definitionLineNumber, snippetSize);

				programElements.Add(new FieldElement(name, definitionLineNumber, fullFilePath, snippet, accessLevel, fieldType, classId, className, String.Empty, initialValue));
			}
		}

		public static void ParseComments(List<ProgramElement> programElements, XElement elements, string fileName, int snippetSize)
		{
			IEnumerable<XElement> comments =
				from el in elements.Descendants(SourceNamespace + "comment")
				select el;

            List<List<XElement>> commentGroups = new List<List<XElement>>();
            List<XElement> lastGroup = null;
		    foreach (var aComment in comments)
		    {
                  if(lastGroup!=null)
                  {
                      int line = Int32.Parse(lastGroup.Last().Attribute(PositionNamespace + "line").Value);
                      int linenext = Int32.Parse(aComment.Attribute(PositionNamespace + "line").Value);
                      if(line+1==linenext)
                      {
                          lastGroup.Add(aComment);
                      }else
                      {
                          var xElements = new List<XElement>();
                          xElements.Add(aComment);
                          commentGroups.Add(xElements);
                          lastGroup = xElements;
                      }
                  }
                  else
                  {
                      var xElements = new List<XElement>();
                      xElements.Add(aComment);
                      commentGroups.Add(xElements);
                      lastGroup = xElements;
                  }
		    }


            foreach (var oneGroup in commentGroups)
            {
                var comment = oneGroup.First();
                var commentText = GetCommentText(oneGroup);
                int commentLine = Int32.Parse(comment.Attribute(PositionNamespace + "line").Value);				
				if(String.IsNullOrWhiteSpace(commentText)) continue;

				//comment name doesn't contain non-word characters and is compact-er than its body
				var commentName = Regex.Replace(commentText, @"(\w+)\W+", "$1 ");
            	commentName = commentName.TrimStart('*', ' ', '\n', '\r');
				if(String.IsNullOrWhiteSpace(commentName)) commentName = commentText;

				//comments above method or class
				XElement belowComment = (comment.NextNode is XElement) ? (XElement)comment.NextNode : null;
				while(belowComment != null &&
						belowComment.Name == (SourceNamespace + "comment"))
				{
					//proceed down the list of comments until I get to some other program element
					belowComment = (belowComment.NextNode is XElement) ? (XElement)belowComment.NextNode : null;
				}

				if(belowComment != null &&
					(belowComment.Name == (SourceNamespace + "function") ||
						belowComment.Name == (SourceNamespace + "constructor")))
				{
					XElement name = belowComment.Element(SourceNamespace + "name");
					if(name != null)
					{
						string methodName = name.Value;
						var methodElement = (MethodElement)programElements.Find(element => element is MethodElement && ((MethodElement)element).Name == methodName);
						if(methodElement != null)
						{
							programElements.Add(new DocCommentElement(commentName, commentLine, methodElement.FullFilePath,
																		RetrieveSnippet(methodElement.FullFilePath, commentLine, snippetSize),
																		commentText, methodElement.Id));
						    methodElement.Body += " "+commentText.Replace("\r\n","");
							continue;
						}
					}
				}
				else if(belowComment != null &&
							belowComment.Name == (SourceNamespace + "class"))
				{
					XElement name = belowComment.Element(SourceNamespace + "name");
					if(name != null)
					{
						string className = name.Value;
						var classElement = (ClassElement)programElements.Find(element => element is ClassElement && ((ClassElement)element).Name == className);
						if(classElement != null)
						{
							programElements.Add(new DocCommentElement(commentName, commentLine, classElement.FullFilePath,
																		RetrieveSnippet(classElement.FullFilePath, commentLine, snippetSize),
																		commentText, classElement.Id));                            
							continue;
						}
					}
				}

				//comments inside a method or class
				MethodElement methodEl = RetrieveMethodElement(comment, programElements);
				if(methodEl != null)
				{
					programElements.Add(new DocCommentElement(commentName, commentLine, methodEl.FullFilePath,
																RetrieveSnippet(methodEl.FullFilePath, commentLine, snippetSize),
																commentText, methodEl.Id));
					continue;
				}
				ClassElement classEl = RetrieveClassElement(comment, programElements);
				if(classEl != null)
				{
					programElements.Add(new DocCommentElement(commentName, commentLine, classEl.FullFilePath,
																RetrieveSnippet(classEl.FullFilePath, commentLine, snippetSize),
																commentText, classEl.Id));
					continue;
				}

				//comments is not associated with another element, so it's a plain CommentElement
				programElements.Add(new CommentElement(commentName, commentLine, fileName, RetrieveSnippet(fileName, commentLine, snippetSize), commentText));
			}
		}

	    private static string GetCommentText(List<XElement> comments)
	    {
            StringBuilder builder = new StringBuilder();
	        Boolean first = true;
	        foreach (var comment in comments)
	        {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(Environment.NewLine + " ");
                }
	            var commentText = comment.Value;
                if (commentText.StartsWith("/")) commentText = commentText.TrimStart('/');
                builder.Append(commentText.TrimStart());

	        }	        
	        return builder.ToString();
	    }

	    public static string ParseBody(XElement function)
		{
			string body = String.Empty;
			XElement block = function.Element(SourceNamespace + "block");
			if(block != null)
			{

				IEnumerable<XElement> comments =
					from el in block.Descendants(SourceNamespace + "comment")
					select el;
				foreach(XElement elem in comments)
				{
					body += String.Join(" ", elem.Value) + " ";
				}

				//Expressions should also include all names, but we need to verify this...
				IEnumerable<XElement> expressions =
						from el in block.Descendants(SourceNamespace + "expr")
						select el;
				foreach(XElement elem in expressions)
				{
					body += String.Join(" ", elem.Value) + " ";
				}
                //need to also add a names from declarations
                IEnumerable<XElement> declarations =
                    from el in block.Descendants(SourceNamespace + "decl")
                    select el;
			    foreach (var declaration in declarations)
			    {
                    var declNames = from el in declaration.Descendants(SourceNamespace + "name")
                                    where (el.Parent.Name.LocalName.Equals("type")||
                                    el.Parent.Name.LocalName.Equals("decl"))
                                    select el;
                    foreach (XElement elem in declNames)
                    {
                        body += String.Join(" ", elem.Value) + " ";
                    }			        
			    }
				body = body.TrimEnd();
			}
			body = Regex.Replace(body, "\\W", " ");
			return body;
		}

		public static void ParseNameAndLineNumber(XElement target, out string name, out int definitionLineNumber)
		{
			XElement nameElement;
			nameElement = target.Element(SourceNamespace + "name");
			if(nameElement == null && 
				target.Element(SourceNamespace + "super") != null) 
			{
				//case of anonymous inner class, should have a super
				nameElement = target.Element(SourceNamespace + "super");
				nameElement = nameElement.Element(SourceNamespace + "name");
				name = nameElement.Value;
			}
			else if(nameElement == null)
			{
				//case of there is no resemblance of a name available
				name = ProgramElement.UndefinedName;
				nameElement = target; //still try to parse line number
			}
			else
			{
				//normal case
				name = nameElement.Value;
			}

			////try to get line number
			if(nameElement.Attribute(PositionNamespace + "line") != null)
			{
				definitionLineNumber = Int32.Parse(nameElement.Attribute(PositionNamespace + "line").Value);
			}
			else
			{
				//i can't find the line number
				definitionLineNumber = 0;
			}
		}

		public static ClassElement RetrieveClassElement(XElement field, List<ProgramElement> programElements)
		{
			IEnumerable<XElement> ownerClasses =
				from el in field.Ancestors(SourceNamespace + "class")
				select el;
			if(ownerClasses.Count() > 0)
			{
				//this ignores the possibility that a field may be part of an inner class
				XElement name = ownerClasses.First().Element(SourceNamespace + "name");
				string ownerClassName = name.Value;
				//now find the ClassElement object corresponding to ownerClassName, since those should have been gen'd by now
				ProgramElement ownerClass = programElements.Find(element => element is ClassElement && ((ClassElement)element).Name == ownerClassName);
				return ownerClass as ClassElement;
			}
			else
			{
				//field is not contained by a class
				return null;
			}
		}

		public static MethodElement RetrieveMethodElement(XElement field, List<ProgramElement> programElements)
		{
			IEnumerable<XElement> ownerMethods =
				from el in field.Ancestors(SourceNamespace + "function")
				select el;
			ownerMethods.Union(from el in field.Ancestors(SourceNamespace + "constructor") select el);

			if(ownerMethods.Count() > 0)
			{
				XElement name = ownerMethods.First().Element(SourceNamespace + "name");
				string ownerMethodName = name.Value;
				//now find the MethodElement object corresponding to ownerMethodName, since those should have been gen'd by now
                ProgramElement ownerMethod = programElements.Find(element => element is MethodElement && ((MethodElement)element).Name == ownerMethodName);
				return ownerMethod as MethodElement;
			}
			else
			{
				//field is not contained by a method
				return null;
			}
		}

		public static string RetrieveSnippet(string filename, int lineNum, int snippetNumLines, int linesAbove = 0)
		{
			int startLine = lineNum - linesAbove - 1;
			string snip = String.Empty;
			using(var reader = new StreamReader(filename))
			{
				try
				{
					//discard lines preceeding snippet
					string line = null;
					for (int i = 0; i < startLine; ++i)
					{
						line = reader.ReadLine();
					}

					for (int i = 0; i < snippetNumLines; ++i)
					{
						line = reader.ReadLine();
						snip += line + Environment.NewLine;
					}
				}
				catch(IOException)
				{
					return snip;
				}
			}
			return snip;
		}

		public static AccessLevel StrToAccessLevel(string level)
		{
			return (AccessLevel)Enum.Parse(typeof(AccessLevel), level, true);
		}
	}
}
