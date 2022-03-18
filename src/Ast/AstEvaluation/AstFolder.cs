using System;
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
                    var astConstant = icn.GetConstant();
                    var initValue = icn.GetConstant();
                    var op = condition.Operator;
                    var limit = condition.Right;
                    if (limit is not IConstantNode)
                    {
                        goto default;
                    }
                    var constLimit = (limit as IConstantNode).GetConstant();
                    var comparer = new AstConstantComparer();
                    if (op is Gt & comparer.Compare(initValue, constLimit) <= 0)
                    {
                        return node.Assignment;
                    }
                    else if (op is Lt & comparer.Compare(initValue, constLimit) >= 0)
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
                    var comparer = new AstConstantComparer();
                    var value = icn.GetConstant();
                    var matchingCase = node.Cases.Where(c => comparer.Compare((c.Case as IConstantNode).GetConstant(), value) == 0).FirstOrDefault();
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
            var leftValue = (node.Left as IConstantNode).GetConstant();
            var rightValue = (node.Right as IConstantNode).GetConstant();
            return (node.Operator, node.GetExprType().BasicType) switch
            {
                (Pow, CHAR) => new CharNode() { CharValue = (char)Math.Pow(leftValue.GetValueAsReal(), rightValue.GetValueAsReal()) },
                (Add, CHAR) => new CharNode() { CharValue = (char)(leftValue.GetValueAsChar() + rightValue.GetValueAsChar()) },
                (Sub, CHAR) => new CharNode() { CharValue = (char)(leftValue.GetValueAsChar() - rightValue.GetValueAsChar()) },
                (Mul, CHAR) => new CharNode() { CharValue = (char)(leftValue.GetValueAsChar() * rightValue.GetValueAsChar()) },
                (Div, CHAR) => new CharNode() { CharValue = (char)(leftValue.GetValueAsChar() / rightValue.GetValueAsChar()) },
                (Pow, INTEGER) => new IntegerNode() { IntValue = (int)Math.Pow(leftValue.GetValueAsReal(), rightValue.GetValueAsReal()) },
                (Add, INTEGER) => new IntegerNode() { IntValue = leftValue.GetValueAsInt() + rightValue.GetValueAsInt() },
                (Sub, INTEGER) => new IntegerNode() { IntValue = leftValue.GetValueAsInt() - rightValue.GetValueAsInt() },
                (Mul, INTEGER) => new IntegerNode() { IntValue = leftValue.GetValueAsInt() * rightValue.GetValueAsInt() },
                (Div, INTEGER) => new IntegerNode() { IntValue = leftValue.GetValueAsInt() / rightValue.GetValueAsInt() },
                (Pow, REAL) => new RealNode() { RealValue = Math.Pow(leftValue.GetValueAsReal(), rightValue.GetValueAsReal()) },
                (Add, REAL) => new RealNode() { RealValue = leftValue.GetValueAsReal() + rightValue.GetValueAsReal() },
                (Sub, REAL) => new RealNode() { RealValue = leftValue.GetValueAsReal() - rightValue.GetValueAsReal() },
                (Mul, REAL) => new RealNode() { RealValue = leftValue.GetValueAsReal() * rightValue.GetValueAsReal() },
                (Div, REAL) => new RealNode() { RealValue = leftValue.GetValueAsReal() / rightValue.GetValueAsReal() },
                (Add, STRING) => new StringNode() { StringValue = leftValue.GetValueAsString() + rightValue.GetValueAsString() },
                (Gt, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsReal() > rightValue.GetValueAsReal() },
                (Lt, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsReal() < rightValue.GetValueAsReal() },
                (Eq, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsReal() == rightValue.GetValueAsReal() },
                (Gte, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsReal() >= rightValue.GetValueAsReal() },
                (Lte, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsReal() <= rightValue.GetValueAsReal() },
                (Ne, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsReal() != rightValue.GetValueAsReal() },
                (And, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsBool() & rightValue.GetValueAsBool() },
                (Or, _) => new BooleanNode() { BoolValue = leftValue.GetValueAsBool() | rightValue.GetValueAsBool() },
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