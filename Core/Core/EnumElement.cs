﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sando.Core
{
	public class EnumElement : ProgramElement
	{
		public virtual string Name { get; set; }
		public virtual AccessLevel AccessLevel { get; set; }
		public virtual int DefinitionLineNumber { get; set; }
		public virtual string Namespace { get; set; }
		public virtual string Values { get; set; }
		public virtual string FileName { get; set; }
		public virtual string FullFilePath { get; set; }
	}
}