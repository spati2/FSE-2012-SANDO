﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sando.Core;

namespace Sando.Parser.UnitTests
{
	[TestFixture]
	public class InheritanceParserTest
	{
		private static string CurrentDirectory;
		private static SrcMLGenerator Generator;

		[SetUp]
		public static void Init()
		{
			//set up generator
			CurrentDirectory = Environment.CurrentDirectory;
			Generator = new SrcMLGenerator();
			Generator.SetSrcMLLocation(CurrentDirectory + "\\..\\..\\..\\..\\LIBS\\srcML-Win");
		}

		[Test]
		public void ParseProperties()
		{
			var parser = new SrcMLParser();
			var elements = parser.Parse("..\\..\\TestFiles\\ShortInheritance.txt");
			bool seenClass = false;
			int countProperties = 0;
			foreach (var programElement in elements)
			{
				if(programElement.ProgramElementType == ProgramElementType.Class)
					seenClass = true;
				if(programElement.ProgramElementType == ProgramElementType.Property)
					countProperties++;
			}
			Assert.IsTrue(seenClass);
			Assert.IsTrue(countProperties==6);
		}

		[Test]
		public void ParseMultipleParents()
		{
			var parser = new SrcMLParser();
			var elements = parser.Parse("..\\..\\TestFiles\\MultiParentTest.txt");
			bool seenClass = false;
			foreach(var programElement in elements)
			{
				if(programElement.ProgramElementType == ProgramElementType.Class)
				{
					seenClass = true;
					var classElement = programElement as ClassElement;
					int numParents = 0;
					//TODO - not sure how we will be able to determine which are interfaces and which are classes
					//might have to just put all but the first one in interfaces?
					if(classElement.ImplementedInterfaces != null)
					{
						numParents += classElement.ImplementedInterfaces.Split(' ').Count();
					}
					if(classElement.ExtendedClasses != null)
					{
						numParents += classElement.ExtendedClasses.Split(' ').Count();
					}
					Assert.IsTrue(numParents==4);
				}
			}
			Assert.IsTrue(seenClass);			
		}

		[Test]
		public void ClassInheritanceTest()
		{
			bool seenClass = false;
			var parser = new SrcMLParser();
			var elements = parser.Parse("..\\..\\TestFiles\\InheritanceCSharpFile.txt");
			Assert.IsNotNull(elements);
			Assert.IsTrue(elements.Length > 0);
			foreach(ProgramElement pe in elements)
			{
				if(pe is ClassElement)
				{
					ClassElement classElem = (ClassElement)pe;
					if(classElem.Name == "IndexerException")
					{
						seenClass = true;
						Assert.AreEqual(classElem.DefinitionLineNumber, 8);
						Assert.AreEqual(classElem.AccessLevel, AccessLevel.Public);
						Assert.AreEqual(classElem.Namespace, "Sando Indexer Exceptions");
						//TODO - make this not dependent upon your path...
						//Assert.AreEqual(classElem.FullFilePath, "C:\\Users\\kosta\\sando\\Parser\\Parser.UnitTests\\TestFiles\\InheritanceCSharpFile.txt");
						Assert.AreEqual(classElem.ImplementedInterfaces, "SandoException");
					}
				}
			}
			Assert.IsTrue(seenClass);
		}

	}
}