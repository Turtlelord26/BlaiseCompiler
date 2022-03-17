using System;
using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class AstFolder
    {
        public static void FoldAst(ProgramNode Ast) => FoldProgram(Ast);

        private static ProgramNode FoldProgram(ProgramNode node)
        {
            var procedures = node.Procedures;
            var functions = node.Functions;
            node.Procedures = node.Procedures.Select(proc => (FunctionNode)FoldProgram(proc)).ToList();
            node.Functions = node.Functions.Select(func => (FunctionNode)FoldProgram(func)).ToList();
            node.Stat = FoldStat(node.Stat);
            return node;
        }

        private static BlockNode FoldBlock(BlockNode node)
        {
            node.Stats = node.Stats.Select(stat => FoldStat(stat)).ToList();
            return node;
        }

        private static WriteNode FoldWrite(WriteNode node)
        {
            node.Expression = FoldExpression(node.Expression);
            return node;
        }

        private static AssignmentNode FoldAssignment(AssignmentNode node)
        {
            node.Expression = FoldExpression(node.Expression);
            return node;
        }

        private static AbstractAstNode FoldIf(IfNode node)
        {
            node.Condition = FoldExpression(node.Condition);
            node.ThenStat = FoldStat(node.ThenStat);
            node.ElseStat = FoldStat(node.ElseStat);
            return node.Condition switch
            {
                BooleanNode constant => constant.BoolValue ? node.ThenStat
                                                           : node.ElseStat,
                _ => node
            };
        }

        private static AbstractAstNode FoldLoop(LoopNode node)
        {
            node.Condition = FoldExpression(node.Condition);
            node.Body = FoldStat(node.Body);
            return node.Condition switch
            {
                BooleanNode constant => constant.BoolValue ? node.Body
                                                           : AbstractAstNode.Empty,
                _ => node
            };
        }

        private static AbstractAstNode FoldForLoop(ForLoopNode node)
        {
            node.Assignment = FoldAssignment(node.Assignment);
            node = (ForLoopNode)FoldLoop(node as LoopNode);
            var initExpr = node.Assignment.Expression;
            var condition = node.Condition as BooleanOpNode;
            switch (initExpr)
            {
                case IConstantNode icn:
                    dynamic initValue = icn.GetValue();
                    var op = condition.Operator;
                    var limit = condition.Right;
                    if (limit is not IConstantNode)
                    {
                        goto default;
                    }
                    var constLimit = (limit as IConstantNode).GetValue();
                    if (op is Gt & initValue <= constLimit)
                    {
                        return node.Assignment;
                    }
                    else if (op is Lt & initValue >= constLimit)
                    {
                        return node.Assignment;
                    }
                    goto default;
                default:
                    return node;
            }
        }

        private static AbstractAstNode FoldSwitch(SwitchNode node)
        {
            node.Input = FoldExpression(node.Input);
            foreach (var swCase in node.Cases)
            {
                swCase.Stat = FoldStat(swCase.Stat);
            }
            switch (node.Input)
            {
                case IConstantNode icn:
                    dynamic value = icn.GetValue();
                    var matchingCase = node.Cases.Where(c => (c.Case as IConstantNode).GetValue() == value).FirstOrDefault();
                    if (matchingCase is not null)
                    {
                        return matchingCase.Stat;
                    }
                    goto default;
                default:
                    return node;
            }
        }

        private static ReturnNode FoldReturn(ReturnNode node)
        {
            node.Expression = FoldExpression(node.Expression);
            return node;
        }

        private static FunctionCallNode FoldCall(FunctionCallNode node)
        {
            node.Arguments = node.Arguments.Select(arg => FoldExpression(arg)).ToList();
            return node;
        }

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

        private static AbstractAstNode FoldStat(AbstractAstNode node) => node switch
        {
            ProgramNode prog => FoldProgram(prog),
            BlockNode block => FoldBlock(block),
            WriteNode write => FoldWrite(write),
            AssignmentNode assign => FoldAssignment(assign),
            IfNode ifn => FoldIf(ifn),
            ForLoopNode forl => FoldForLoop(forl),
            LoopNode loop => FoldLoop(loop),
            SwitchNode switcher => FoldSwitch(switcher),
            ReturnNode ret => FoldReturn(ret),
            FunctionCallNode call => FoldCall(call),
            AbstractAstNode aan when aan.IsEmpty() => aan,
            _ => throw new InvalidOperationException($"Unexpected node type {node.GetType()} encountered during statement folding.")
        };

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
            _ => throw new InvalidOperationException($"Invalid node type {node.GetType()} detected during expression folding")
        };
    }
}