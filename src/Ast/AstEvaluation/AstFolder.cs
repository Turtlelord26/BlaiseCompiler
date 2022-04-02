using System;
using System.Linq;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class AstFolder : AbstractAstVisitor<AbstractAstNode>
    {
        public override AbstractAstNode VisitProgram(ProgramNode node)
        {
            var procedures = node.Procedures;
            var functions = node.Functions;
            node.Procedures = node.Procedures.Select(proc => (FunctionNode)VisitProgram(proc)).ToList();
            node.Functions = node.Functions.Select(func => (FunctionNode)VisitProgram(func)).ToList();
            node.Stat = VisitStatement(node.Stat);
            return node;
        }

        public override AbstractAstNode VisitVarDecl(VarDeclNode node) => node;

        public override AbstractAstNode VisitFunction(FunctionNode node) => VisitProgram(node);

        public override AbstractAstNode VisitBlock(BlockNode node)
        {
            node.Stats = node.Stats.Select(stat => VisitStatement(stat)).ToList();
            return node;
        }

        public override AbstractAstNode VisitWrite(WriteNode node)
        {
            node.Expression = VisitExpression(node.Expression);
            return node;
        }

        public override AbstractAstNode VisitAssignment(AssignmentNode node)
        {
            node.Expression = VisitExpression(node.Expression);
            return node;
        }

        public override AbstractAstNode VisitIf(IfNode node)
        {
            node.Condition = VisitExpression(node.Condition);
            node.ThenStat = VisitStatement(node.ThenStat);
            node.ElseStat = VisitStatement(node.ElseStat);
            return node.Condition switch
            {
                BooleanNode condition when condition.BoolValue => node.ThenStat,
                BooleanNode => node.ElseStat,
                _ => node
            };
        }

        public override AbstractAstNode VisitLoop(LoopNode node)
        {
            node.Condition = VisitExpression(node.Condition);
            node.Body = VisitStatement(node.Body);
            return node.Condition switch
            {
                BooleanNode condition when condition.BoolValue => node.Body,
                BooleanNode => AbstractAstNode.Empty,
                _ => node
            };
        }

        public override AbstractAstNode VisitForLoop(ForLoopNode node)
        {
            node.Assignment = (AssignmentNode)VisitAssignment(node.Assignment);
            node = (ForLoopNode)VisitLoop(node);
            var initial = node.Assignment.Expression;
            var condition = (BooleanOpNode)node.Condition;
            var limit = condition.Right;
            if (initial is IConstantNode & limit is IConstantNode)
            {
                var initialValue = ((IConstantNode)initial).GetConstant();
                var limitValue = ((IConstantNode)limit).GetConstant();
                var comparer = new AstConstantComparer();
                return condition.Operator switch
                {
                    Gt when comparer.Compare(initialValue, limitValue) <= 0 => node.Assignment,
                    Lt when comparer.Compare(initialValue, limitValue) >= 0 => node.Assignment,
                    _ => node
                };
            }
            return node;
        }

        public override AbstractAstNode VisitSwitch(SwitchNode node)
        {
            node.Input = VisitExpression(node.Input);
            node.Cases.ForEach(swCase => VisitStatement(swCase.Stat));
            if (node.Input is IConstantNode)
            {
                var comparer = new AstConstantComparer();
                var value = ((IConstantNode)node.Input).GetConstant();
                var matchingCase = node.Cases.Where(c => comparer.Compare((c.Case as IConstantNode).GetConstant(), value) == 0).FirstOrDefault();
                if (matchingCase is not null)
                {
                    return matchingCase.Stat;
                }
            }
            return node;
        }

        public override AbstractAstNode VisitReturn(ReturnNode node)
        {
            node.Expression = VisitExpression(node.Expression);
            return node;
        }

        public override AbstractAstNode VisitCall(FunctionCallNode node)
        {
            node.Arguments = node.Arguments.Select(arg => VisitExpression(arg)).ToList();
            return node;
        }

        public override AbstractAstNode VisitBinaryOperator(BinaryOpNode node)
        {
            node.Left = VisitExpression(node.Left);
            node.Right = VisitExpression(node.Right);
            return FoldBinaryValues(node);
        }

        public override AbstractAstNode VisitBooleanOperator(BooleanOpNode node) => VisitBinaryOperator(node);

        public override AbstractAstNode VisitLogicalOperator(LogicalOpNode node) => VisitBinaryOperator(node);

        public override AbstractAstNode VisitNotOperator(NotOpNode node)
        {
            node.Expression = VisitExpression(node.Expression);
            if (node.Expression is BooleanNode)
            {
                var boolnode = (BooleanNode)node.Expression;
                boolnode.BoolValue = !boolnode.BoolValue;
                return boolnode;
            }
            return node;
        }

        public new AbstractTypedAstNode VisitExpression(AbstractTypedAstNode node) => (AbstractTypedAstNode)base.VisitExpression(node);

        public override AbstractAstNode VisitVarRef(VarRefNode node) => node;

        public override AbstractAstNode VisitBoolean(BooleanNode node) => node;

        public override AbstractAstNode VisitChar(CharNode node) => node;

        public override AbstractAstNode VisitInteger(IntegerNode node) => node;

        public override AbstractAstNode VisitReal(RealNode node) => node;

        public override AbstractAstNode VisitString(StringNode node) => node;

        public override AbstractAstNode VisitEmpty(AbstractAstNode node) => node;

        public AbstractAstNode FoldBinaryValues(BinaryOpNode node)
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
    }
}