using System.Linq;
using Blaise2.Ast;

namespace Blaise2.Visualizations
{
    public class AstStringTree : AstVisitorBase<string>
    {
        public override string Visit(ProgramNode node)
        {
            var s = "(program";

            if (node.VarDecls.Count > 0)
            {
                s += " (vars " + string.Join(" ", node.VarDecls.Select(v => Visit((dynamic)v))) + ")";
            }

            if (node.Procedures.Count > 0)
            {
                s += " (procedures " + string.Join(" ", node.Procedures.Select(v => Visit((dynamic)v))) + ")";
            }

            if (node.Functions.Count > 0)
            {
                s += " (functions " + string.Join(" ", node.Functions.Select(v => Visit((dynamic)v))) + ")";
            }

            s += " " + Visit((dynamic)node.Stat);

            s += ")";
            return s;
        }

        public override string Visit(VarDeclNode node)
        {
            return $"({node.Identifier}: {node.BlaiseType})";
        }

        public override string Visit(ProcedureNode node)
        {
            return $"(procedure {node.Identifier}" + Visit((dynamic)node.Stat) + ")";
        }

        public override string Visit(FunctionNode node)
        {
            return $"(function {node.ReturnType} {node.Identifier}" + Visit((dynamic)node.Stat) + ")";
        }

        public override string Visit(BlockNode node)
        {
            return "(block " + string.Join(" ", node.Stats.Select(s => Visit((dynamic)s))) + ")";
        }

        public override string Visit(WriteNode node)
        {
            var cmd = node.WriteNewline ? "writeln" : "write";
            return $"({cmd} " + Visit((dynamic)node.Expression) + ")";
        }

        public override string Visit(AssignmentNode node)
        {
            return $"(assign {node.Identifier} " + Visit((dynamic)node.Expression) + ")";
        }

        public override string Visit(FunctionCallNode node)
        {
            return $"(invoke {node.Identifier} " + string.Join(" ", node.ArgumentExpressions.Select(e => Visit((dynamic)e))) + ")";
        }

        public override string Visit(IntegerNode node)
        {
            return node.IntValue.ToString();
        }

        public override string Visit(RealNode node)
        {
            return node.RealValue.ToString();
        }

        public override string Visit(StringNode node)
        {
            return $"\"{node.StringValue}\"";
        }

        public override string Visit(VarRefNode node)
        {
            return $"(var {node.Identifier})";
        }

        public override string Visit(BinaryOpNode node)
        {
            return $"(binop {node.Op} {Visit((dynamic)node.Lhs)} {Visit((dynamic)node.Rhs)}";
        }

        public override string Visit(BooleanOpNode node)
        {
            return $"(binop {node.Op} {Visit((dynamic)node.Lhs)} {Visit((dynamic)node.Rhs)}";
        }
    }
}