using System;
using System.Collections.Generic;
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

        private static string ToCilOperator(BlaiseOperator op, BlaiseType operandType) => (op, operandType.BasicType) switch
        {
            (Pow, _) => @"
    call float64 [System.Private.CoreLib]System.Math::Pow(float64, float64)",
            (Mul, _) => @"
    mul",
            (Div, _) => @"
    div",
            (Add, STRING) => @"
    add",
            (Add, _) => @"
    add",
            (Sub, _) => @"
    sub",
            (Gt, _) => @"
    cgt",
            (Lt, _) => @"
    clt",
            (Eq, STRING) => @"
    call bool [System.Private.CoreLib]System.String::op_Equality(string, string)",
            (Eq, _) => @"
    ceq",
            (Ne, STRING) => @"
    call bool [System.Private.CoreLib]System.String::op_Equality(string, string)
    ldc.i4.0
    ceq",
            (Ne, _) => @"
    ceq
    ldc.i4.0
    ceq",
            (Gte, _) => @"
    clt
    ldc.i4.0
    ceq",
            (Lte, _) => @"
    cgt
    ldc.i4.0
    ceq",
            (And, _) => @"
    and",
            (Or, _) => @"
    or",
            (Not, _) => @"
    ldc.i4.0
    ceq",
            _ => throw new InvalidOperationException($"Invalid Binary Operator {op}")
        };

        private string MakeLabel() => LabelFactory.MakeLabel();

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
                STRING => PromoteToString(currentType, targetType, opContainer),
                _ => throw new InvalidOperationException($"Cannot convert {currentType} to {targetType}")
            };
        }

        private string PromoteToString(BlaiseType current, BlaiseType target, AbstractAstNode container)
        {
            var systemType = current.BasicType switch
            {
                CHAR => "Char",
                INTEGER => "Int32",
                REAL => "Double",
                _ => throw new InvalidOperationException($"Cannot convert {current} to {target}")
            };
            var identifier = MakeAndInjectLocalVar(current, container);
            return @$"
    stloc {identifier}
    ldloca {identifier}
    call instance string [System.Private.CoreLib]System.{systemType}::ToString()";
        }

        private string MakeAndInjectLocalVar(BlaiseType varType, AbstractAstNode parent)
        {
            var decl = VarFactory.MakeLocalVar(varType);
            InjectLocalVariableIntoContainer(decl, parent);
            return decl.Identifier;
        }

        private void InjectLocalVariableIntoContainer(VarDeclNode decl, AbstractAstNode parent)
        {
            switch (GetContainingFunctionOrProgram(parent))
            {
                case FunctionNode func:
                    func.VarDecls.Add(decl);
                    return;
                case ProgramNode:
                    MainLocals.Add(decl);
                    return;
            }
        }

        private static ProgramNode GetContainingFunctionOrProgram(AbstractAstNode climber) => climber switch
        {
            ProgramNode prog => prog,
            null => throw new InvalidOperationException($"Fatal error: emitter could not resolve containing program while type converting"),
            _ => GetContainingFunctionOrProgram(climber.Parent)
        };

        private static string EmitBranchToEndLabelUnlessStatReturns(string endLabel, AbstractAstNode stat) =>
            FunctionReturnEvaluator.Visit(stat) ? ""
                                                : $"br.s {endLabel}";

        private static string EmitLabelUnlessStatReturns(string label, AbstractAstNode stat) =>
            FunctionReturnEvaluator.Visit(stat) ? ""
                                                : $"{label}: nop";
    }

    class SwitchCaseNodeComparer : IComparer<SwitchCaseNode>
    {
        private BlaiseType CaseType;

        public SwitchCaseNodeComparer(BlaiseType caseType) => CaseType = caseType;

        public int Compare(SwitchCaseNode a, SwitchCaseNode b)
        {
            return CaseType.BasicType switch
            {
                CHAR => (a.Case as IConstantNode).GetConstant().GetValueAsChar()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsChar()),
                INTEGER => (a.Case as IConstantNode).GetConstant().GetValueAsInt()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsInt()),
                REAL => (a.Case as IConstantNode).GetConstant().GetValueAsReal()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsReal()),
                STRING => (a.Case as IConstantNode).GetConstant().GetValueAsString()
                    .CompareTo((b.Case as IConstantNode).GetConstant().GetValueAsString()),
                _ => throw new InvalidOperationException($"Invalid switch case type {CaseType} assigned to Case comparer.")
            };
        }
    }
}