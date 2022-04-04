using System;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class TypeResolver
    {
        public static BlaiseType ResolveType(AbstractTypedAstNode expressionNode) => expressionNode switch
        {
            LogicalOpNode node => ResolveType(node),
            BooleanOpNode node => ResolveType(node),
            BinaryOpNode node => ResolveType(node),
            NotOpNode node => ResolveType(node),
            FunctionCallNode node => ResolveType(node),
            VarRefNode node => ResolveType(node),
            BooleanNode node => ResolveType(node),
            CharNode node => ResolveType(node),
            IntegerNode node => ResolveType(node),
            RealNode node => ResolveType(node),
            StringNode node => ResolveType(node),
            _ => throw new InvalidOperationException($"Unexpected node of type {expressionNode.GetType()} found while resolving expression types.")
        };

        public static BlaiseType ResolveType(BinaryOpNode node)
        {
            if (node.ExprType is null)
            {
                node.LeftType = ResolveType(node.Left);
                node.RightType = ResolveType(node.Right);
                node.ExprType = GetPromotedBinaryOpType(node.LeftType, node.RightType, node.Operator);
            }
            return node.ExprType;
        }

        public static BlaiseType ResolveType(BooleanOpNode node)
        {
            if (node.ExprType is null)
            {
                node.LeftType = ResolveType(node.Left);
                node.RightType = ResolveType(node.Right);
                node.ExprType = ValidateBooleanTypeOrErrorType(node.LeftType, node.RightType);
            }
            return node.ExprType;
        }

        public static BlaiseType ResolveType(LogicalOpNode node)
        {
            if (node.ExprType is null)
            {
                node.LeftType = ResolveType(node.Left);
                node.RightType = ResolveType(node.Right);
                node.ExprType = new()
                {
                    BasicType = BOOLEAN
                };
            }
            return node.ExprType;
        }

        public static BlaiseType ResolveType(NotOpNode node) => new()
        {
            BasicType = BOOLEAN
        };

        public static BlaiseType ResolveType(FunctionCallNode node) => node.CallTarget.ReturnType;

        public static BlaiseType ResolveType(VarRefNode node) => node.VarInfo?.VarDecl.BlaiseType
                                                                 ?? BlaiseType.ErrorType;

        public static BlaiseType ResolveType(IntegerNode node) => new()
        {
            BasicType = INTEGER
        };

        public static BlaiseType ResolveType(RealNode node) => new()
        {
            BasicType = REAL
        };

        public static BlaiseType ResolveType(BooleanNode node) => new()
        {
            BasicType = BOOLEAN
        };

        public static BlaiseType ResolveType(CharNode node) => new()
        {
            BasicType = CHAR
        };

        public static BlaiseType ResolveType(StringNode node) => new()
        {
            BasicType = STRING
        };

        private static BlaiseType GetPromotedBinaryOpType(BlaiseType left, BlaiseType right, BlaiseOperator op)
        {
            if (left.BasicType > right.BasicType)
            {
                var temp = right;
                right = left;
                left = temp;
            }
            // Now right must be at least as complex as left
            return (op, right.BasicType, left.BasicType) switch
            {
                (_, CHAR, CHAR)
                or (_, INTEGER, CHAR or INTEGER)
                or (_, REAL, CHAR or INTEGER or REAL)
                or (Add or Eq or Ne, STRING, CHAR or INTEGER or REAL or STRING) => right.DeepCopy(),
                (_, _, ARRAY or SET) => throw new NotImplementedException("Type promotion support for extended types not yet implemented."),
                _ => BlaiseType.ErrorType
            };
        }

        private static BlaiseType ValidateBooleanTypeOrErrorType(BlaiseType left, BlaiseType right)
        {
            if (left.BasicType > right.BasicType)
            {
                var temp = right;
                right = left;
                left = temp;
            }
            // Now right must be at least as complex as left
            if (right.BasicType == CHAR & left.BasicType == CHAR
                || right.BasicType == INTEGER & (left.BasicType == CHAR
                                              | left.BasicType == INTEGER)
                || right.BasicType == REAL & (left.BasicType == CHAR
                                              | left.BasicType == INTEGER
                                              | left.BasicType == REAL))
            {
                return new()
                {
                    BasicType = BOOLEAN
                };
            }
            return BlaiseType.ErrorType;
        }
    }
}