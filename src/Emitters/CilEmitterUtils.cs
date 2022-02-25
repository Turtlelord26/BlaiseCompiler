using System;
using Blaise2.Ast;
using static Blaise2.Ast.BlaiseOperator;

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
            Pow => throw new NotImplementedException("Exponentiation unimplemented."),
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
    }
}