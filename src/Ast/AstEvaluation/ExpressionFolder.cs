using System;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class ExpressionFolder
    {
        public static void Visit(AbstractTypedAstNode node)
        {
            switch (node)
            {
                case BinaryOpNode bin:
                    Visit(bin.Left);
                    Visit(bin.Right);
                    var folded = FoldBinary(bin);
                    if (!folded.IsEmpty())
                    {
                        Hoist(bin, folded);
                    }
                    return;
                case NotOpNode not:
                    Visit(not.Expression);
                    if (not.Expression is BooleanNode)
                    {
                        var boolnode = (BooleanNode)not.Expression;
                        boolnode.BoolValue = !boolnode.BoolValue;
                        Hoist(not, boolnode);
                    }
                    return;
                case FunctionCallNode
                    or IntegerNode
                    or RealNode
                    or VarRefNode
                    or BooleanNode
                    or CharNode
                    or StringNode:
                    return;
                default:
                    throw new InvalidOperationException("Invalid AbstractTypedAstNode detected during Expression Evaluation");
            }
        }

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

        private static void Hoist(AbstractTypedAstNode oldParent, AbstractTypedAstNode child)
        {
            var newParent = oldParent.Parent;
            switch (newParent)
            {
                case WriteNode write when write.Expression == oldParent:
                    write.Expression = child;
                    break;
                case AssignmentNode assignment when assignment.Expression == oldParent:
                    assignment.Expression = child;
                    break;
                case IfNode ifn when ifn.Condition == oldParent:
                    ifn.Condition = child;
                    break;
                case LoopNode loop when loop.Condition == oldParent:
                    loop.Condition = child;
                    break;
                case SwitchNode switchn when switchn.Input == oldParent:
                    switchn.Input = child;
                    break;
                case SwitchCaseNode switchCase when switchCase.Case == oldParent:
                    switchCase.Case = child;
                    break;
                case ReturnNode ret when ret.Expression == oldParent:
                    ret.Expression = child;
                    break;
                case BinaryOpNode bin when bin.Left == oldParent:
                    bin.Left = child;
                    break;
                case BinaryOpNode bin when bin.Right == oldParent:
                    bin.Right = child;
                    break;
                case NotOpNode not when not.Expression == oldParent:
                    not.Expression = child;
                    break;
                case FunctionCallNode call when call.Arguments.Contains(oldParent):
                    var argIndex = call.Arguments.FindIndex(a => a == oldParent);
                    call.Arguments[argIndex] = child;
                    break;
                default:
                    throw new InvalidOperationException($"Hoist error: old parent {oldParent.Type} not a child of new parent {newParent.Type}, or new parent node type not recognized.");
            }
            child.Parent = newParent;
        }
    }
}