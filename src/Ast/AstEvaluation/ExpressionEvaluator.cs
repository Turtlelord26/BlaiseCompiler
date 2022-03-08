using System;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class ExpressionEvaluator
    {
        public static void Visit(BinaryOpNode node)
        {
            Visit((dynamic)node.Left);
            Visit((dynamic)node.Right);
            var folded = FoldBinary(node);
            if (!folded.IsEmpty())
            {
                Hoist(node, (dynamic)node.Parent, folded);
            }
        }

        public static void Visit(NotOpNode node)
        {
            Visit((dynamic)node.Expression);
            if (node.Expression is BooleanNode)
            {
                var boolnode = (BooleanNode)node.Expression;
                boolnode.BoolValue = !boolnode.BoolValue;
                Hoist(node, (dynamic)node.Parent, boolnode);
            }
        }

        public static void Visit(FunctionCallNode node) { }

        public static void Visit(IntegerNode node) { }

        public static void Visit(RealNode node) { }

        public static void Visit(VarRefNode node) { }

        public static void Visit(BooleanNode node) { }

        public static void Visit(CharNode node) { }

        public static void Visit(StringNode node) { }

        public static void Visit(AbstractTypedAstNode node) => throw new InvalidOperationException("Invalid AbstractTypedAstNode detected during Expression Evaluation");

        private static AbstractTypedAstNode FoldBinary(BinaryOpNode node)
        {
            if (node.Left is not IConstantNode | node.Right is not IConstantNode)
            {
                return (AbstractTypedAstNode)AbstractAstNode.Empty;
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

        private static void Hoist(AbstractAstNode oldParent, WriteNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Expression)
            {
                newParent.Expression = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, AssignmentNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Expression)
            {
                newParent.Expression = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, IfNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Condition)
            {
                newParent.Condition = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, LoopNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Condition)
            {
                newParent.Condition = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, SwitchNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Input)
            {
                newParent.Input = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, SwitchCaseNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Case)
            {
                newParent.Case = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, ReturnNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Expression)
            {
                newParent.Expression = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, BinaryOpNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Left)
            {
                newParent.Left = child;
                child.Parent = newParent;
            }
            else if (oldParent == newParent.Right)
            {
                newParent.Right = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, NotOpNode newParent, AbstractTypedAstNode child)
        {
            if (oldParent == newParent.Expression)
            {
                newParent.Expression = child;
                child.Parent = newParent;
            }
            else
            {
                throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
            }
        }

        private static void Hoist(AbstractAstNode oldParent, FunctionCallNode newParent, AbstractTypedAstNode child)
        {
            for (int i = 0; i < newParent.Arguments.Count; i++)
            {
                if (oldParent == newParent.Arguments[i])
                {
                    newParent.Arguments[i] = child;
                    child.Parent = newParent;
                    return;
                }
            }
            throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}.");
        }

        private static void Hoist(AbstractAstNode oldParent, AbstractAstNode newParent, AbstractTypedAstNode child) => throw new InvalidOperationException("Invalid Expression Hoist");
    }
}