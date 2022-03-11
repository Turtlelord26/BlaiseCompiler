using System;
using Blaise2.Ast;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Emitters
{
    public partial class CilEmitter
    {
        private int labelNum = 0;

        private int anonymousVarNum = 0;

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

        private string MakeLabel() => $"Label{labelNum++}";

        private string MakeAnonymousVar() => $"___AnonVar_{anonymousVarNum++}";

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
            And => @"
    and",
            Or => @"
    or",
            Not => @"
    ldc.i4.0
    ceq",
            _ => throw new InvalidOperationException($"Invalid Binary Operator {op}")
        };

        private string TypeConvert(BlaiseType currentType, BlaiseType targetType, AbstractAstNode opContainer)
        {
            return targetType.BasicType switch
            {
                CHAR => @"
    conv.u2",
                INTEGER => @"
    conv.i4",
                REAL => @"
    conv.r8",
                STRING => PromoteToString(currentType.BasicType, targetType.BasicType, opContainer),
                _ => throw new InvalidOperationException($"Cannot convert {currentType} to {targetType}")
            };
        }

        private string PromoteToString(BlaiseTypeEnum current, BlaiseTypeEnum target, AbstractAstNode container)
        {
            var systemType = current switch
            {
                CHAR => "Char",
                INTEGER => "Int32",
                REAL => "Double",
                _ => throw new InvalidOperationException($"Cannot convert {current} to {target}")
            };
            var identifier = MakeAnonymousVar();
            var decl = new VarDeclNode()
            {
                Identifier = identifier,
                BlaiseType = new() { BasicType = current }
            };
            switch (GetContainingFunctionOrProgram(container))
            {
                case FunctionNode func:
                    func.VarDecls.Add(decl);
                    break;
                case ProgramNode:
                    MainLocals.Add(decl);
                    break;
            }
            return @$"
    stloc {identifier}
    ldloca {identifier}
    call instance string [System.Private.CoreLib]System.{systemType}::ToString()";
        }

        private static ProgramNode GetContainingFunctionOrProgram(AbstractAstNode climber) => climber switch
        {
            ProgramNode prog => prog,
            null => throw new InvalidOperationException($"Fatal error: emitter could not resolve containing program while type converting"),
            _ => GetContainingFunctionOrProgram(climber.Parent)
        };
    }
}