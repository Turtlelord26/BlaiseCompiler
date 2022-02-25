using System;
using System.Collections.Generic;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class TypeResolver
    {
        public static BlaiseType FindType(BinaryOpNode node)
        {
            if (node.ExprType is not null)
            {
                return node.ExprType;
            }
            var leftType = FindType((dynamic)node.Left);
            var rightType = FindType((dynamic)node.Right);
            var thisType = PromoteBinaryOp(leftType, rightType, node.Operator);
            node.ExprType = thisType;
            return thisType;
        }

        public static BlaiseType FindType(BooleanOpNode node)
        {
            var leftType = FindType((dynamic)node.Left);
            var rightType = FindType((dynamic)node.Right);
            return ValidateBooleanOp(leftType, rightType);
        }

        public static BlaiseType ResolveType(AbstractAstNode node) => FindType((dynamic)node);

        private static BlaiseType FindType(FunctionCallNode node) => node.CallTarget.ReturnType;

        /*private static BlaiseType FindType(LogicalOpNode node) => new()
        {
            BasicType = BOOLEAN
        };

        private static BlaiseType FindType(NotOpNode node) => new()
        {
            BasicType = BOOLEAN
        };*/

        private static BlaiseType FindType(VarRefNode node) => node.VarInfo?.VarDecl.BlaiseType ?? BlaiseType.ErrorType;

        private static BlaiseType FindType(IntegerNode node) => new()
        {
            BasicType = INTEGER
        };

        private static BlaiseType FindType(RealNode node) => new()
        {
            BasicType = REAL
        };

        private static BlaiseType FindType(BooleanNode node) => new()
        {
            BasicType = BOOLEAN
        };

        private static BlaiseType FindType(CharNode node) => new()
        {
            BasicType = CHAR
        };

        private static BlaiseType FindType(StringNode node) => new()
        {
            BasicType = STRING
        };

        private static BlaiseType FindType(AbstractAstNode node) =>
            throw new InvalidOperationException($"Unexpected node of type {node.GetType()} found while resolving expression types.");

        private static BlaiseType PromoteBinaryOp(BlaiseType left, BlaiseType right, BlaiseOperator op)
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
                                              | left.BasicType == REAL)
                || op == Add & (right.BasicType == STRING & (left.BasicType == CHAR
                                                            | left.BasicType == INTEGER
                                                            | left.BasicType == REAL
                                                            | left.BasicType == STRING)))
            {
                return right.DeepCopy();
            }
            if (right.IsExtended | left.IsExtended)
            {
                throw new NotImplementedException("Type promotion support for extended types not yet implemented.");
            }
            return BlaiseType.ErrorType;
        }

        private static BlaiseType ValidateBooleanOp(BlaiseType left, BlaiseType right)
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