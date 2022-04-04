using System;
using System.Collections.Generic;
using static Blaise2.Ast.ConstType;

namespace Blaise2.Ast
{
    public class AstConstantComparer : IComparer<AstConstant>
    {
        public int Compare(AstConstant a, AstConstant b) => (a.ConstType, b.ConstType) switch
        {
            (CharConst, CharConst) => a.CharValue.CompareTo(b.CharValue),
            (IntConst, CharConst) => a.IntValue.CompareTo(b.CharValue),
            (RealConst, CharConst) => a.RealValue.CompareTo(b.CharValue),
            (StringConst, CharConst) => a.StringValue.CompareTo(b.CharValue.ToString()),
            (CharConst, IntConst) => a.CharValue.CompareTo(b.IntValue),
            (IntConst, IntConst) => a.IntValue.CompareTo(b.IntValue),
            (RealConst, IntConst) => a.RealValue.CompareTo(b.IntValue),
            (StringConst, IntConst) => a.StringValue.CompareTo(b.IntValue.ToString()),
            (CharConst, RealConst) => a.CharValue.CompareTo(b.RealValue),
            (IntConst, RealConst) => a.IntValue.CompareTo(b.RealValue),
            (RealConst, RealConst) => a.RealValue.CompareTo(b.RealValue),
            (StringConst, RealConst) => a.StringValue.CompareTo(b.RealValue.ToString()),
            (CharConst, StringConst) => a.CharValue.ToString().CompareTo(b.StringValue),
            (IntConst, StringConst) => a.IntValue.ToString().CompareTo(b.StringValue),
            (RealConst, StringConst) => a.RealValue.ToString().CompareTo(b.StringValue),
            (StringConst, StringConst) => a.StringValue.CompareTo(b.StringValue),
            (BoolConst, _)
            or (_, BoolConst) => throw new InvalidOperationException($"Cannot compare bools to other constants"),
            _ => throw new InvalidOperationException($"Encountered unrecognized AstConstant contents.")
        };
    }
}