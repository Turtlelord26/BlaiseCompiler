using System;
using System.IO;
using Blaise2.Ast;

namespace Blaise2
{
    public class DotRenderer
    {
        private int ctr = 0;

        private readonly StreamWriter outStream;

        public DotRenderer(string filename)
        {
            outStream = new StreamWriter(filename);
        }

        public void Close() => outStream.Close();

        public void Visualize(AbstractAstNode root)
        {
            outStream.WriteLine("digraph Expr {");
            Treewalk((dynamic)root, "");
            outStream.WriteLine("}");
        }

        private void Treewalk(ProgramNode node, string parent)
        {
            var dot = MakeDotNode();
            outStream.WriteLine($"  {dot} [shape=\"rect\" label=\"Program\n{node.Identifier}\"]");
            node.VarDecls.ForEach(decl => Treewalk((dynamic)decl, dot));
            node.Procedures.ForEach(proc => Treewalk((dynamic)proc, dot));
            node.Functions.ForEach(func => Treewalk((dynamic)func, dot));
            Treewalk((dynamic)node.Stat, dot);
        }

        private void Treewalk(VarDeclNode node, string parent)
        {
            var label = $"VarDecl\n{node.Identifier} : {node.BlaiseType.ToString()}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(BlockNode node, string parent)
        {
            var label = "Block";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            node.Stats.ForEach(stat => Treewalk((dynamic)stat, dot));
        }

        private void Treewalk(WriteNode node, string parent)
        {
            var label = node.WriteNewline ? "WriteLn" : "Write";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Expression, dot);
        }

        private void Treewalk(AssignmentNode node, string parent)
        {
            var label = $"Assignment\n{node.Identifier}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Expression, dot);
        }

        private void Treewalk(FunctionNode node, string parent)
        {
            var label = node.IsFunction ? $"Function\n{node.Identifier}\n{node.ReturnType.ToString()}"
                                        : $"Procedure\n{node.Identifier}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            node.Params.ForEach(param => Treewalk((dynamic)param, dot));
            node.VarDecls.ForEach(decl => Treewalk((dynamic)decl, dot));
            node.Procedures.ForEach(proc => Treewalk((dynamic)proc, dot));
            node.Functions.ForEach(func => Treewalk((dynamic)func, dot));
            Treewalk((dynamic)node.Stat, dot);
        }

        private void Treewalk(BinaryOpNode node, string parent)
        {
            var label = $"BinaryOperator\n{node.Operator}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Left, dot);
            Treewalk((dynamic)node.Right, dot);
        }

        private void Treewalk(BooleanOpNode node, string parent)
        {
            var label = $"BooleanOperator\n{node.Operator}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Left, dot);
            Treewalk((dynamic)node.Right, dot);
        }

        private void Treewalk(LogicalOpNode node, string parent)
        {
            var label = $"LogicalOperator\n{node.Operator}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Left, dot);
            Treewalk((dynamic)node.Right, dot);
        }

        private void Treewalk(NotOpNode node, string parent)
        {
            var label = "Not";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Expression, dot);
        }

        private void Treewalk(FunctionCallNode node, string parent)
        {
            var label = node.IsFunction ? $"Function Call\n{node.Identifier}"
                                       : $"Procedure Call\n{node.Identifier}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            node.Arguments.ForEach(arg => Treewalk((dynamic)arg, dot));
        }

        private void Treewalk(IfNode node, string parent)
        {
            var label = "If Statement";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Condition, dot);
            Treewalk((dynamic)node.ThenStat, dot);
            Treewalk((dynamic)node.ElseStat, dot);
        }

        private void Treewalk(LoopNode node, string parent)
        {
            var label = $"Loop\n{node.LoopType}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Condition, dot);
            Treewalk((dynamic)node.Body, dot);
        }

        private void Treewalk(ForLoopNode node, string parent)
        {
            var label = $"Loop\n{node.LoopType}";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Assignment, dot);
            Treewalk((dynamic)node.Condition, dot);
            Treewalk((dynamic)node.Body, dot);
        }

        private void Treewalk(SwitchNode node, string parent)
        {
            var label = "Switch";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Input, dot);
            node.Cases.ForEach(c => Treewalk((dynamic)c, dot));
            Treewalk((dynamic)node.Default, dot);
        }

        private void Treewalk(SwitchCaseNode node, string parent)
        {
            var label = "Case";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Case, dot);
            Treewalk((dynamic)node.Stat, dot);
        }

        private void Treewalk(ReturnNode node, string parent)
        {
            var label = "Return";
            var dot = WriteNewNodeAndParentEdge(parent, label);
            Treewalk((dynamic)node.Expression, dot);
        }

        private void Treewalk(IntegerNode node, string parent)
        {
            var label = $"Int\n{node.IntValue}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(RealNode node, string parent)
        {
            var label = $"Real\n{node.RealValue}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(BooleanNode node, string parent)
        {
            var label = $"Boolean\n{node.BoolValue}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(VarRefNode node, string parent)
        {
            var label = $"Identifier\n{node.Identifier}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(CharNode node, string parent)
        {
            var label = $"Char\n{node.CharValue}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(StringNode node, string parent)
        {
            var label = $"String\n{node.StringValue}";
            var dot = WriteNewNodeAndParentEdge(parent, label, true);
        }

        private void Treewalk(AbstractAstNode node, string parent)
        {
            if (node.IsEmpty())
            {
                return;
            }
            throw new InvalidOperationException($"Unrecognized tree node of type {node.GetType()}");
        }

        private string WriteNewNodeAndParentEdge(string parent, string label, bool terminal = false)
        {
            var dot = MakeDotNode();
            var shape = DetermineShape(terminal);
            PrintNode(dot, shape, label);
            PrintEdge(parent, dot);
            return dot;
        }

        private string MakeDotNode() => $"node{ctr++}";

        private string DetermineShape(bool terminal) => terminal ? "" : "shape=\"rect\" ";

        private void PrintNode(string dot, string shape, string label) => outStream.WriteLine($"  {dot} [{shape}label=\"{label}\"]");

        private void PrintEdge(string parent, string dot) => outStream.WriteLine($"  {parent} -> {dot};");
    }
}
