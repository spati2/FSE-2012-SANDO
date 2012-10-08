﻿using System;
using System.Collections.Generic;
using System.IO;
using Sando.Core.Extensions.Logging;
using Sando.ExtensionContracts.ParserContracts;
using Sando.ExtensionContracts.ProgramElementContracts;

namespace Sando.Parser
{
    public class TextFileParser: IParser 
    {
        private static readonly int SnippetSize = 5;
		private static readonly int SnippetLinesAbove = 0;

        public List<ProgramElement> Parse(string filename)
        {
            if(File.Exists(filename)&&GetSizeInMb(filename)>15)
            {
                return new List<ProgramElement>();
            }
            var list = new List<ProgramElement>(); 
            try
            {
                // Create an instance of StreamReader to read from a file.
                // The using statement also closes the StreamReader.
                using (var sr = new StreamReader(filename))
                {
                    String line;
                	int linenum = 0;
                    // Read and display lines from the file until the end of
                    // the file is reached.
                    while ((line = sr.ReadLine()) != null)
                    {
                    	linenum++;
						if (String.IsNullOrWhiteSpace(line)) continue;
						//var name = Regex.Replace(line, @"(\w+)\W+", "$1 ");
						var name = line.TrimStart(' ', '\n', '\r', '\t');
						name = name.TrimEnd(' ');
                    	var snippet = SrcMLParsingUtils.RetrieveSnippet(name, SnippetSize); 
                    	var element = new TextLineElement(name, linenum, filename, snippet, line);
                    	list.Add(element);
                    }
    
                }
            }
            catch (Exception e)
            {
                FileLogger.DefaultLogger.Error(ExceptionFormatter.CreateMessage(e, "The file could not be read:"));
            }
            return list;
        }

        private float GetSizeInMb(string filename)
        {
            float sizeInMb = (new FileInfo(filename).Length/1024f)/1024f;
            return sizeInMb;
        }
    }
}
