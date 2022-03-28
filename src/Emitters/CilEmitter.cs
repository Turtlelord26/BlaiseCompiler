using System;
using System.Collections.Generic;
using System.Linq;
using Blaise2.Ast;
using Blaise2.Emitters.EmitterSubcomponents;
using static Blaise2.Ast.BlaiseTypeEnum;
using static Blaise2.Ast.LoopType;
using static Blaise2.Ast.VarType;

namespace Blaise2.Emitters
{
    public partial class CilEmitter
    {
        private const string nopDelimiter = @"
    nop";
        private string ProgramName;
        public List<VarDeclNode> MainLocals { get; init; } = new();

        public string EmitCil(ProgramNode program) => EmitProgram(program);

        private string EmitStat(AbstractAstNode node) => node switch
        {
            AssignmentNode assign => EmitAssignment(assign),
            WriteNode write => EmitWrite(write),
            FunctionCallNode call => EmitCall(call),
            IfNode ifStat => EmitIf(ifStat),
            ForLoopNode forLoop => EmitForLoop(forLoop),
            LoopNode loop => EmitLoop(loop),
            SwitchNode switchStat => EmitSwitch(switchStat),
            ReturnNode ret => EmitReturn(ret),
            BlockNode block => EmitBlock(block),
            AbstractAstNode aan when aan.IsEmpty() => String.Empty,
            _ => throw new InvalidOperationException($"Unrecognized node {node.GetType()} encountered while emitting.")
        };

        private string EmitExpression(AbstractTypedAstNode node) => node switch
        {
            LogicalOpNode logop => EmitLogOp(logop),
            BooleanOpNode boolop => EmitBooleanOp(boolop),
            BinaryOpNode binop => EmitBinaryOp(binop),
            NotOpNode notop => EmitNotOp(notop),
            FunctionCallNode call => EmitCall(call),
            VarRefNode varref => EmitVarRef(varref),
            BooleanNode booln => EmitBoolean(booln),
            CharNode charn => EmitChar(charn),
            IntegerNode intn => EmitInt(intn),
            RealNode real => EmitReal(real),
            StringNode stringn => EmitString(stringn),
            AbstractTypedAstNode atan when atan.IsEmpty() => String.Empty,
            _ => throw new InvalidOperationException($"Unrecognized node {node.GetType()} encountered while emitting.")
        };

        private string EmitProgram(ProgramNode node)
        {
            ProgramName = node.Identifier;
            var body = EmitStat(node.Stat);
            var fields = node.VarDecls.Aggregate("", (cil, v) => cil += $@"
    .field public {v.BlaiseType.ToCilType()} {v.Identifier}");
            var methods = node.Procedures.Concat(node.Functions).Aggregate("\n", (cil, func) => cil += EmitFunction(func));
            var localIndex = 1;
            var mainLocals = MainLocals.Aggregate("", (cil, local) => cil += $",\n\t\t[{localIndex++}] {local.BlaiseType.ToCilType()} {local.Identifier}");
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

        private string EmitFunction(FunctionNode node)
        {
            var body = EmitStat(node.Stat);
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
                throw new NotImplementedException("Can't emit nested functions yet");
            }
            return output;
        }

        private string EmitBlock(BlockNode node) => string.Join(nopDelimiter, node.Stats.Select(stat => EmitStat(stat)));

        private string EmitWrite(WriteNode node)
        {
            var exprType = node.Expression.GetExprType().ToCilType();
            var method = node.WriteNewline ? "WriteLine" : "Write";
            return @$"
    {EmitExpression(node.Expression)}
    call void [System.Console]System.Console::{method}({exprType})";
        }

        private string EmitAssignment(AssignmentNode node)
        {
            var info = node.VarInfo ?? throw new InvalidOperationException($"Couldn't resolve variable {node.Identifier}.");
            var varType = info.VarDecl.BlaiseType;
            var exprType = node.Expression.GetExprType();
            var typeConversion = varType.Equals(exprType) ? ""
                                                          : TypeConvert(exprType, varType, node);
            return info.VarType switch
            {
                Global => @$"
    ldloc.0{EmitExpression(node.Expression)}
    {typeConversion}
    stfld {varType.ToCilType()} {ProgramName}::{info.VarDecl.Identifier}",
                Local => @$"
    {EmitExpression(node.Expression)}
    {typeConversion}
    stloc {info.VarDecl.Identifier}",
                Argument => @$"
    {EmitExpression(node.Expression)}
    {typeConversion}
    starg.s {info.VarDecl.Identifier}",
                _ => throw new InvalidOperationException($"Invalid VarType {info.VarType}")
            };
        }

        private string EmitIf(IfNode node)
        {
            var thenLabel = MakeLabel();
            var endLabel = MakeLabel();
            return @$"
    {EmitExpression(node.Condition)}
    brtrue.s {thenLabel}
    {EmitStat(node.ElseStat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, node.ElseStat)}
    {thenLabel}: nop
    {EmitStat(node.ThenStat)}
    {EmitBranchToEndLabelUnlessStatReturns(endLabel, node.ThenStat)}
    {EmitLabelUnlessStatReturns(endLabel, node.ThenStat)}";
        }

        private string EmitLoop(LoopNode node)
        {
            var bodyLabel = MakeLabel();
            var endLabel = MakeLabel();
            var brEnd = node.LoopType == Until ? "brtrue" : "brfalse";
            var brBody = node.LoopType == Until ? "brfalse" : "brtrue";
            var conditionCil = EmitExpression(node.Condition);
            return @$"
    {conditionCil}
    {brEnd}.s {endLabel}
    {bodyLabel}: nop
    {EmitStat(node.Body)}
    {conditionCil}
    {brBody}.s {bodyLabel}
    {endLabel}: nop";
        }

        private string EmitForLoop(ForLoopNode node) => EmitAssignment(node.Assignment) + EmitLoop(node);

        private string EmitSwitch(SwitchNode node) => node.Input.GetExprType() switch
        {
            { BasicType: CHAR or INTEGER or REAL } => EmitNumericSwitch(node),
            { BasicType: STRING } => EmitStringSwitch(node),
            BlaiseType bt => throw new InvalidOperationException($"Encountered invalid switch input type {bt} while emitting.")
        };

        private string EmitCall(FunctionCallNode node)
        {
            var callTarget = node.CallTarget;
            var paramTypes = string.Join(", ", callTarget.Params.Select(p => p.BlaiseType.ToCilType()));
            var argExprs = string.Join('\n', node.Arguments.Select(arg => EmitExpression(arg)));
            var returnType = callTarget.IsFunction ? callTarget.ReturnType.ToCilType() : "void";
            return @$"
    ldloc.0
    {argExprs}
    call instance {returnType} {ProgramName}::{callTarget.Identifier}({paramTypes})";
        }

        private string EmitReturn(ReturnNode node) => @$"{EmitExpression(node.Expression)}
    ret";

        private string EmitInt(IntegerNode node) => @$"
    ldc.i4.s {node.IntValue}";

        private string EmitReal(RealNode node) => @$"
    ldc.r8 {node.RealValue}";

        private string EmitBoolean(BooleanNode node) => @$"
    ldc.i4.{(node.BoolValue ? 1 : 0)}";

        private string EmitChar(CharNode node) => @$"
    ldc.i4.s {(int)node.CharValue}";

        private string EmitString(StringNode node) => @$"
    ldstr ""{node.StringValue}""";

        private string EmitVarRef(VarRefNode node)
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

        private string EmitBinaryOp(BinaryOpNode node)
        {
            var output = EmitExpression(node.Left);
            if (!node.LeftType.Equals(node.ExprType))
            {
                output += TypeConvert(node.LeftType, node.ExprType, node);
            }
            output += EmitExpression(node.Right);
            if (!node.RightType.Equals(node.ExprType))
            {
                output += TypeConvert(node.RightType, node.ExprType, node);
            }
            return output + ToCilOperator(node.Operator, node.ExprType);
        }

        private string EmitBooleanOp(BooleanOpNode node)
        {
            var leftBasicType = node.LeftType.BasicType;
            var rightBasicType = node.RightType.BasicType;
            var output = EmitExpression(node.Left);
            if (leftBasicType < rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.LeftType, node.RightType, node);
            }
            output += EmitExpression(node.Right);
            if (leftBasicType > rightBasicType & !node.LeftType.Equals(node.RightType))
            {
                output += TypeConvert(node.RightType, node.LeftType, node);
            }
            return output + ToCilOperator(node.Operator, node.GetExprType());
        }

        private string EmitLogOp(LogicalOpNode node)
        {
            return $"{EmitExpression(node.Left)}{EmitExpression(node.Right)}{ToCilOperator(node.Operator, node.GetExprType())}";
        }

        private string EmitNotOp(NotOpNode node)
        {
            return @$"{EmitExpression(node.Expression)}{ToCilOperator(BlaiseOperator.Not, node.GetExprType())}";
        }
    }
}