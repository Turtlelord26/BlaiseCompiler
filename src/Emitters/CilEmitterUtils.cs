using System;
using Blaise2.Ast;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Emitters
{
    public partial class CilEmitter
    {
        private static string Preamble()
        {
            return @"
// Preamble
.assembly _ { }
.assembly extern System.Collections {}
.assembly extern System.Console {}
.assembly extern System.Private.CoreLib {}
";
        }

        private static string Postamble()
        {
            return @"
  .method public hidebysig specialname rtspecialname instance void .ctor () cil managed 
  {
    ldarg.0
    call instance void [System.Private.CoreLib]System.Object::.ctor()
    ret
  }

} // end of class
";
        }

        private static string ToCilOperator(BlaiseOperator op) => op switch
        {
            Pow => @"
    call float64 [System.Private.CoreLib]System.Math::Pow(float64, float64)",
            Mul => @"
    mul",
            Div => @"
    div",
            Add => @"
    add",
            Sub => @"
    sub",
            Gt => @"
    cgt",
            Lt => @"
    clt",
            Eq => @"
    ceq",
            Ne => @"
    ceq
    ldc.i4.0
    ceq",
            Gte => @"
    clt
    ldc.i4.0
    ceq",
            Lte => @"
    cgt
    ldc.i4.0
    ceq",
            _ => throw new InvalidOperationException($"Invalid Binary Operator {op}")
        };

        private static string TypeConvert(BlaiseType currentType, BlaiseType targetType)
        {
            var current = currentType.BasicType;
            var target = targetType.BasicType;
            if (target == REAL & current == CHAR
                                | current == INTEGER)
            {
                return @"
    conv.r8";
            }
            if (target == STRING)
            {
                if (current == CHAR | current == INTEGER)
                {
                    throw new NotImplementedException($"convert {current} to {target}");
                }
                if (current == REAL)
                {
                    throw new NotImplementedException($"convert {current} to {target}");
                }
            }
            throw new InvalidOperationException($"Cannot convert {current} to {target}");
        }
    }
}