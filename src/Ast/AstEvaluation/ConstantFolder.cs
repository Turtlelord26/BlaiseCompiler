using System;
using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class ConstantFolder
    {
        public static void FoldConstants(ProgramNode Ast) => FoldProgram(Ast);

        private static void FoldStat(AbstractAstNode node)
        {
            switch (node)
            {
                case ProgramNode prog: FoldProgram(prog); return;
                case BlockNode block: FoldBlock(block); return;
                case WriteNode write: FoldWrite(write); return;
                case AssignmentNode assign: FoldAssignment(assign); return;
                case IfNode ifn: FoldIf(ifn); return;
                case ForLoopNode forl: FoldForLoop(forl); return;
                case LoopNode loop: FoldLoop(loop); return;
                case SwitchNode switcher: FoldSwitch(switcher); return;
                case ReturnNode ret: FoldReturn(ret); return;
                case FunctionCallNode call: FoldCall(call); return;
                case AbstractAstNode aan when aan.IsEmpty(): return;
                default: throw new InvalidOperationException($"Unexpected node type {node.GetType()} encountered during constant folding.");
            }
        }

        private static AbstractTypedAstNode FoldExpression(AbstractTypedAstNode node) => node switch
        {
            BinaryOpNode bin => FoldBinaryOp(bin),
            NotOpNode not => FoldNotOp(not),
            FunctionCallNode
            or IntegerNode
            or RealNode
            or VarRefNode
            or BooleanNode
            or CharNode
            or StringNode => node,
            AbstractTypedAstNode atan when atan.IsEmpty() => (AbstractTypedAstNode)AbstractAstNode.Empty,
            _ => throw new InvalidOperationException($"Invalid node type {node.GetType()} detected during Expression Evaluation")
        };

        private static AbstractTypedAstNode FoldBinaryOp(BinaryOpNode node)
        {
            node.Left = FoldExpression(node.Left);
            node.Right = FoldExpression(node.Right);
            return FoldBinaryValues(node);
        }

        private static AbstractTypedAstNode FoldNotOp(NotOpNode node)
        {
            node.Expression = FoldExpression(node.Expression);
            if (node.Expression is BooleanNode)
            {
                var boolnode = (BooleanNode)node.Expression;
                boolnode.BoolValue = !boolnode.BoolValue;
                return boolnode;
            }
            return node;
        }

        private static AbstractTypedAstNode FoldBinaryValues(BinaryOpNode node)
        {
            if (node.Left is not IConstantNode | node.Right is not IConstantNode)
            {
                return node;
            }
            dynamic leftValue = (node.Left as IConstantNode).GetValue();
            dynamic rightValue = (node.Right as IConstantNode).GetValue();
            return (node.Operator, node.GetExprType().BasicType) switch
            {
                (Pow, CHAR) => new CharNode() { CharValue = (char)Math.Pow((double)leftValue, (double)rightValue) },
                (Add, CHAR) => new CharNode() { CharValue = leftValue + rightValue },
                (Sub, CHAR) => new CharNode() { CharValue = leftValue - rightValue },
                (Mul, CHAR) => new CharNode() { CharValue = leftValue * rightValue },
                (Div, CHAR) => new CharNode() { CharValue = leftValue / rightValue },
                (Pow, INTEGER) => new IntegerNode() { IntValue = (int)Math.Pow((double)leftValue, (double)rightValue) },
                (Add, INTEGER) => new IntegerNode() { IntValue = leftValue + rightValue },
                (Sub, INTEGER) => new IntegerNode() { IntValue = leftValue - rightValue },
                (Mul, INTEGER) => new IntegerNode() { IntValue = leftValue * rightValue },
                (Div, INTEGER) => new IntegerNode() { IntValue = leftValue / rightValue },
                (Pow, REAL) => new RealNode() { RealValue = Math.Pow((double)leftValue, (double)rightValue) },
                (Add, REAL) => new RealNode() { RealValue = leftValue + rightValue },
                (Sub, REAL) => new RealNode() { RealValue = leftValue - rightValue },
                (Mul, REAL) => new RealNode() { RealValue = leftValue * rightValue },
                (Div, REAL) => new RealNode() { RealValue = leftValue / rightValue },
                (Add, STRING) => new StringNode() { StringValue = leftValue + rightValue },
                (Gt, _) => new BooleanNode() { BoolValue = leftValue > rightValue },
                (Lt, _) => new BooleanNode() { BoolValue = leftValue < rightValue },
                (Eq, _) => new BooleanNode() { BoolValue = leftValue == rightValue },
                (Gte, _) => new BooleanNode() { BoolValue = leftValue >= rightValue },
                (Lte, _) => new BooleanNode() { BoolValue = leftValue <= rightValue },
                (Ne, _) => new BooleanNode() { BoolValue = leftValue != rightValue },
                (And, _) => new BooleanNode() { BoolValue = leftValue & rightValue },
                (Or, _) => new BooleanNode() { BoolValue = leftValue | rightValue },
                _ => throw new InvalidOperationException($"Detected invalid operator {node.Operator} in Binary node.")
            };
        }

        private static void FoldProgram(ProgramNode node)
        {
            foreach (var routine in node.Procedures.Concat(node.Functions))
            {
                FoldProgram(routine);
            }
            FoldStat(node.Stat);
        }

        private static void FoldBlock(BlockNode node) => node.Stats.ForEach(stat => FoldStat(stat));

        private static void FoldWrite(WriteNode node) => node.Expression = FoldExpression(node.Expression);

        private static void FoldAssignment(AssignmentNode node) => node.Expression = FoldExpression(node.Expression);

        private static void FoldIf(IfNode node)
        {
            node.Condition = FoldExpression(node.Condition);
            FoldStat(node.ThenStat);
            FoldStat(node.ElseStat);
        }

        private static void FoldLoop(LoopNode node)
        {
            node.Condition = FoldExpression(node.Condition);
            FoldStat(node.Body);
        }

        private static void FoldForLoop(ForLoopNode node)
        {
            FoldAssignment(node.Assignment);
            FoldLoop(node as LoopNode);
        }

        private static void FoldSwitch(SwitchNode node)
        {
            node.Input = FoldExpression(node.Input);
            foreach (var swc in node.Cases)
            {
                FoldStat(swc.Stat);
            }
        }

        private static void FoldReturn(ReturnNode node) => node.Expression = FoldExpression(node.Expression);

        private static void FoldCall(FunctionCallNode node)
        {
            //Cannot use a foreach loop or linq function here, because we are potentially modifying the underlying collection.
            var args = node.Arguments;
            for (int i = 0; i < args.Count; i++)
            {
                args[i] = FoldExpression(args[i]);
            }
        }
    }
}