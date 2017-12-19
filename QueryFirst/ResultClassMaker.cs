﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TinyIoC;

namespace QueryFirst
{
    public interface IResultClassMaker
    {
        string Usings();
        string StartClass(CodeGenerationContext ctx);
        string MakeProperty(ResultFieldDetails fld);
        string CloseClass();
    }
    public class ResultClassMaker : IResultClassMaker
    {
        public virtual string Usings() { return ""; }

        private string nl = Environment.NewLine;
        public virtual string StartClass(CodeGenerationContext ctx)
        {
            return string.Format("public partial class {0} {{" + nl, ctx.ResultClassName);
        }
        public virtual string MakeProperty(ResultFieldDetails fld)
        {
            StringBuilder code = new StringBuilder();
            code.AppendLine($"protected {fld.TypeCsShort} _{fld.CSColumnName}; //({fld.TypeDb} {(fld.AllowDBNull ? "null" : "not null")})");
            code.AppendLine($"public {fld.TypeCsShort} {fld.CSColumnName}{{\nget{{return _{fld.CSColumnName};}}\nset{{_{fld.CSColumnName} = value;}}\n}}");
            return code.ToString();
        }

        public virtual string CloseClass()
        {
            return "}" + nl;
        }
    }

	public class ParameterClassMaker : IResultClassMaker
	{
		public virtual string Usings() { return ""; }

		private string nl = Environment.NewLine;
		public virtual string StartClass(CodeGenerationContext ctx)
		{
			return string.Format("public partial class {0} {{" + nl, ctx.ParametersClassName);
		}
		public virtual string MakeProperty(ResultFieldDetails fld)
		{
			StringBuilder code = new StringBuilder();
			code.AppendLine($"protected {fld.TypeCsShort} _{fld.CSColumnName}; //({fld.TypeDb} {(fld.AllowDBNull ? "null" : "not null")})");
			code.AppendLine($"public {fld.TypeCsShort} {fld.CSColumnName}{{\nget{{return _{fld.CSColumnName};}}\nset{{_{fld.CSColumnName} = value;}}\n}}");
			return code.ToString();
		}

		public virtual string CloseClass()
		{
			return "}" + nl;
		}
	}
}
