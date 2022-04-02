using System.Linq;
using Blaise2.Ast;

namespace Blaise2.Visualizations
{
    public class AstStringTree : AbstractAstVisitor<string>
    {
        public override string VisitProgram(ProgramNode node) =>
            "(program"
            + $" (vars {string.Join(" ", node.VarDecls.Select(v => VisitVarDecl(v)))})"
            + $" (procedures {string.Join(" ", node.Procedures.Select(v => VisitFunction(v)))})"
            + $" (functions {string.Join(" ", node.Functions.Select(v => VisitFunction(v)))})"
            + $" {VisitStatement(node.Stat)})";

        public override string VisitVarDecl(VarDeclNode node) => $"({node.Identifier}: {node.BlaiseType})";

        public override string VisitFunction(FunctionNode node) => node.IsFunction
            ? $"(function {node.ReturnType} {node.Identifier}" + VisitStatement(node.Stat) + ")"
            : $"(procedure {node.Identifier}" + VisitStatement(node.Stat) + ")";

        public override string VisitBlock(BlockNode node) =>
            "(block " + string.Join(" ", node.Stats.Select(s => VisitStatement(s))) + ")";

        public override string VisitWrite(WriteNode node) =>
            $"({(node.WriteNewline ? "writeln" : "write")} " + VisitExpression(node.Expression) + ")";

        public override string VisitAssignment(AssignmentNode node) =>
            $"(assign {node.Identifier} " + VisitExpression(node.Expression) + ")";

        public override string VisitReturn(ReturnNode node) =>
            $"(return {VisitExpression(node.Expression)})";

        public override string VisitCall(FunctionCallNode node) =>
            $"(invoke {node.Identifier} {string.Join(" ", node.Arguments.Select(e => VisitExpression(e)))})";

        public override string VisitIf(IfNode node) =>
            $"(if {VisitExpression(node.Condition)} {VisitStatement(node.ThenStat)} {VisitStatement(node.ElseStat)})";

        public override string VisitLoop(LoopNode node) =>
            $"({node.LoopType} {VisitExpression(node.Condition)} {VisitStatement(node.Body)})";

        public override string VisitForLoop(ForLoopNode node) =>
            $"(forloop {VisitAssignment(node.Assignment)} {VisitExpression(node.Condition)} {VisitStatement(node.Body)})";

        public override string VisitSwitch(SwitchNode node) =>
            $"(switch {VisitExpression(node.Input)} {string.Join(' ', node.Cases.Select(c => VisitSwitchCase(c)))})";

        public string VisitSwitchCase(SwitchCaseNode node) =>
            $"(case {VisitExpression(node.Case)} {VisitStatement(node.Stat)})";

        public override string VisitInteger(IntegerNode node) => node.IntValue.ToString();

        public override string VisitReal(RealNode node) => node.RealValue.ToString();

        public override string VisitBoolean(BooleanNode node) => node.BoolValue.ToString();

        public override string VisitChar(CharNode node) => node.CharValue.ToString();

        public override string VisitString(StringNode node) => node.StringValue;

        public override string VisitVarRef(VarRefNode node) => $"(var {node.Identifier})";

        public override string VisitBinaryOperator(BinaryOpNode node) =>
            $"(binop {node.Operator} {VisitExpression(node.Left)} {VisitExpression(node.Right)})";

        public override string VisitBooleanOperator(BooleanOpNode node) =>
            $"(binop {node.Operator} {VisitExpression(node.Left)} {VisitExpression(node.Right)})";

        public override string VisitLogicalOperator(LogicalOpNode node) =>
            $"(binop {node.Operator} {VisitExpression(node.Left)} {VisitExpression(node.Right)})";

        public override string VisitNotOperator(NotOpNode node) =>
            $"(not {VisitExpression(node.Expression)})";

        public override string VisitEmpty(AbstractAstNode node) => string.Empty;
    }
}