﻿using System;
using EnvDTE;
using EnvDTE80;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Core.Extensions.Logging;

namespace Sando.UI.View
{
		public static class FileOpener
    	{
			private static DTE2 _dte;

            public static void OpenItem(CodeSearchResult result, string text)
    		{
    			if(result != null)
    			{
					OpenFile(result.Element.FullFilePath, result.Element.DefinitionLineNumber, text);
    			}
    		}

    		public static void OpenFile(string filePath, int lineNumber, string text)
    		{
    			InitDte2();
    			_dte.ItemOperations.OpenFile(filePath, Constants.vsViewKindTextView);
    			try
    			{
    				var selection = (TextSelection) _dte.ActiveDocument.Selection;                    
    				selection.GotoLine(lineNumber);

                    if (IsLiteralSearchString(text))
                        FocusOnLiteralString(text);
                    else
                        HighlightTerms(text);
    			}
    			catch (Exception e)
    			{
                    FileLogger.DefaultLogger.Error(e);
    				//ignore, we don't want this feature ever causing a crash
    			}
    		}

            private static void HighlightTerms(string text)
            {
                var terms = text.Split(' ');
                foreach (var term in terms)
                {
                    var objSel = (TextSelection)(_dte.ActiveDocument.Selection);
                    TextRanges textRanges = null;
                    objSel.FindPattern(term, 0, ref textRanges);
                    {
                        long lStartLine = objSel.TopPoint.Line;
                        long lStartColumn = objSel.TopPoint.LineCharOffset;                        
                        objSel.SwapAnchor();
                        objSel.MoveToLineAndOffset(System.Convert.ToInt32
                                (lStartLine), System.Convert.ToInt32(lStartColumn+term.Length), true);                                                
                    }

                }
            }

            private static bool IsLiteralSearchString(string text)
            {
                return text.Contains("\"");
            }

            private static void FocusOnLiteralString(string text) 
            {
                const char chars = '"';
                text = text.TrimStart(chars);
                text = text.TrimEnd(chars);
                var objSel = (TextSelection)(_dte.ActiveDocument.Selection);
                TextRanges textRanges = null;
                objSel.FindPattern(text, 0, ref textRanges);
                {
                    objSel.SelectLine();
                }                
            }    	

    		private static void InitDte2()
    		{
    			if (_dte == null)
    			{
    				_dte = ServiceLocator.Resolve<DTE2>();
    			}
    		}
    	}

}
