using System;
using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;
using static Blaise2.Ast.BlaiseTypeEnum;
using static Blaise2.Ast.LoopType;
using static Blaise2.Ast.VarType;

namespace Blaise2.Emitters
{
    public partial class CilEmitter : AbstractAstVisitor<string>
    {
        private const string nopDelimiter = @"
    nop";
        private string ProgramName;
        public List<VarDeclNode> MainLocals { get; init; } = new();

        public override string VisitProgram(ProgramNode node)
        {
            ProgramName = node.Identifier;
            var body = VisitStatement(node.Stat);
            var fields = node.VarDecls.Aggregate(string.Empty, (cil, v) => cil += VisitVarDecl(v));
            var methods = node.Procedures.Concat(node.Functions).Aggregate("\n", (cil, func) => cil += VisitFunction(func));
            var localIndex = 1;
            var mainLocals = MainLocals.Aggregate(string.Empty, (cil, local) => cil += $",\n        [{localIndex++}] {local.BlaiseType.ToCilType()} {local.Identifier}");
            return @$"{Preamble()}
.class public auto ansi beforefieldinit {node.Identifier}
    extends [System.Private.CoreLib]System.Object
{{{fields}

    // Methods
    
    {methods}

  .method public hidebysig static void main() cil managed
  {{
    .entrypoint
    .locals init (
        [0] class {ProgramName}{mainLocals}
    )

    nop
    newobj instance void {ProgramName}::.ctor()
    stloc.0
    {body}
    nop
    ret
  }} // end of main
  {Postamble()}
";
        }

        public override string VisitVarDecl(VarDeclNode node) => $@"
    .field public {node.BlaiseType.ToCilType()} {node.Identifier}";

        public override string VisitFunction(FunctionNode node)
        {
            var body = VisitStatement(node.Stat);
            var isFunction = node.IsFunction;
            var returnType = isFunction ? node.ReturnType.ToCilType() : "void";
            var paramsList = string.Join(",\n\t\t", node.Params.Select(p => $"{p.BlaiseType.ToCilType()} {p.Identifier}"));
            var index = 1;
            var locals = string.Join("", node.VarDecls.Select(v => $",\n\t\t[{index++}] {v.BlaiseType.ToCilType()} {v.Identifier}"));
            var output = @$"
  .method public hidebysig 
    instance {returnType} {node.Identifier} (
        {paramsList}
    ) cil managed
  {{
    .locals init (
        [0] class {ProgramName}{locals}
    )
    ldarg.0
    stloc.0
    {body}
  }}";
            //THIS IS WHERE INNER FUNCS AND PROCS WOULD GO
            if (node.Functions.Count() > 0 | node.Procedures.Count() > 0)
            {
                throw new NotImplementedException("Can't Visit nested functions yet");
            }
            return output;
        }

        public override string VisitBlock(BlockNode node) => string.Join(nopDelimiter, node.Stats.Select(stat => VisitStatement(stat)));

        public override string VisitWrite(WriteNode node)
        {
            var exprType = node.Expression.GetExprType().ToCilType();
            var method = node.WriteNewline ? "WriteLine" : "Write";
            return @$"
    {VisitExpression(node.Expression)}
    call void [System.Console]System.Console::{method}({exprType})";
        }

        public override string VisitAssignment(AssignmentNode node)
        {
            var info = node.VarInfo ?? throw new InvalidOperationException($"Couldn't resolve variable {node.Identifier}.");
            var varType = info.VarDecl.BlaiseType;
            var exprType = node.Expression.GetExprType();
            var typeConversion = varType.Equals(exprType) ? string.Empty
                                                          : TypeConvert(exprType, varType, node);
            return info.VarType switch
            {
                Global => @$"
    ldloc.0{VisitExpression(node.Expression)}{typeConversion}
    stfld {varType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    {VisitExpression(node.Expression)}{typeConversion}
    stloc {info.VarDecl.Identifier}",
                Argument => @$"
    {VisitExpression(node.Expression)}{typeConversion}
    starg.s {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public override string VisitIf(IfNode node)
        {
            var thenLabel = MakeLabel();
            var endLabel = MakeLabel();
            return @$"
    {VisitExpression(node.Condition)}
    brtrue.s {thenLabel}
    {VisitStatement(node.ElseStat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, node.ElseStat)}
    {thenLabel}: nop
    {VisitStatement(node.ThenStat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, node.ThenStat)}
    {EmitLabelUnlessStatReturns(endLabel, node.ThenStat)}";
        }

        public override string VisitLoop(LoopNode node)
        {
            var bodyLabel = MakeLabel();
            var endLabel = MakeLabel();
            var brEnd = node.LoopType == Until ? "brtrue" : "brfalse";
            var brBody = node.LoopType == Until ? "brfalse" : "brtrue";
            var conditionCil = VisitExpression(node.Condition);
            return @$"
    {conditionCil}
    {brEnd}.s {endLabel}
    {bodyLabel}: nop
    {VisitStatement(node.Body)}
    {conditionCil}
    {brBody}.s {bodyLabel}
    {endLabel}: nop";
        }

        public override string VisitForLoop(ForLoopNode node) => VisitAssignment(node.Assignment) + VisitLoop(node);

        public override string VisitSwitch(SwitchNode node) => node.Input.GetExprType() switch
        {
            { BasicType: CHAR or INTEGER or REAL } => EmitNumericSwitch(node),
            { BasicType: STRING } => EmitStringSwitch(node),
            BlaiseType bt => throw new InvalidOperationException($"Encountered invalid switch input type {bt} while Visitting.")
        };

        public override string VisitCall(FunctionCallNode node)
        {
            var callTarget = node.CallTarget;
            var paramTypes = string.Join(", ", callTarget.Params.Select(p => p.BlaiseType.ToCilType()));
            var argExprs = string.Join('\n', node.Arguments.Select(arg => VisitExpression(arg)));
            var returnType = callTarget.IsFunction ? callTarget.ReturnType.ToCilType() : "void";
            return @$"
    ldloc.0
    {argExprs}
    call instance {returnType} {ProgramName}::{callTarget.Identifier}({paramTypes})";
        }

        public override string VisitReturn(ReturnNode node) => @$"{VisitExpression(node.Expression)}
    ret";

        public override string VisitInteger(IntegerNode node) => @$"
    ldc.i4.s {node.IntValue}";

        public override string VisitReal(RealNode node) => @$"
    ldc.r8 {node.RealValue}";

        public override string VisitBoolean(BooleanNode node) => @$"
    ldc.i4.{(node.BoolValue ? 1 : 0)}";

        public override string VisitChar(CharNode node) => @$"
    ldc.i4.s {(int)node.CharValue}";

        public override string VisitString(StringNode node) => @$"
    ldstr ""{node.StringValue}""";

        public override string VisitVarRef(VarRefNode node)
        {
            var info = node.VarInfo ?? throw new InvalidOperationException($"Couldn't resolve variable {node.Identifier}.");
            return info.VarType switch
            {
                Global => @$"
    ldloc.0
    ldfld {info.VarDecl.BlaiseType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    ldloc {info.VarDecl.Identifier}",
                Argument => @$"
    ldarg {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        public override string VisitBinaryOperator(BinaryOpNode node)
        {
            var output = VisitExpression(node.Left);
            if (!node.LeftType.Equals(node.ExprType))
            {
                output += TypeConvert(node.LeftType, node.ExprType, node);
            }
            output += VisitExpression(node.Right);
            if (!node.RightType.Equals(node.ExprType))
            {
                output += TypeConvert(node.RightType, node.ExprType, node);
            }
            return output + ToCilOperator(node.Operator, node.ExprType);
        }

        public override string VisitBooleanOperator(BooleanOpNode node)
        {
            var leftBasicType = node.LeftType.BasicType;
            var rightBasicType = node.RightType.BasicType;
            var output = VisitExpression(node.Left);
            if (leftBasicType < rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.LeftType, node.RightType, node);
            }
            output += VisitExpression(node.Right);
            if (leftBasicType > rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.RightType, node.LeftType, node);
            }
            return output + ToCilOperator(node.Operator, node.GetExprType());
        }

        public override string VisitLogicalOperator(LogicalOpNode node)
        {
            return $"{VisitExpression(node.Left)}{VisitExpression(node.Right)}{ToCilOperator(node.Operator, node.GetExprType())}";
        }

        public override string VisitNotOperator(NotOpNode node)
        {
            return @$"{VisitExpression(node.Expression)}{ToCilOperator(BlaiseOperator.Not, node.GetExprType())}";
        }

        public override string VisitEmpty(AbstractAstNode node) => string.Empty;
    }
}