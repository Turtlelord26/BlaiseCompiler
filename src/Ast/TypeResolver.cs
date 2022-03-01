using System;
using System.Collections.Generic;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class TypeResolver
    {
        public static BlaiseType ResolveType(BinaryOpNode node)
        {
            if (node.ExprType is not null)
            {
                return node.ExprType;
            }
            node.LeftType = ResolveType((dynamic)node.Left);
            node.RightType = ResolveType((dynamic)node.Right);
            var thisType = PromoteBinaryOp(node.LeftType, node.RightType, node.Operator);
            node.ExprType = thisType;
            return thisType;
        }

        public static BlaiseType ResolveType(BooleanOpNode node)
        {
            node.LeftType = ResolveType((dynamic)node.Left);
            node.RightType = ResolveType((dynamic)node.Right);
            return ValidateBooleanOp(node.LeftType, node.RightType);
        }

        public static BlaiseType ResolveType(FunctionCallNode node) => node.CallTarget.ReturnType;

        /*public static BlaiseType ResolveType(LogicalOpNode node) => new()
        {
            BasicType = BOOLEAN
        };

        public static BlaiseType ResolveType(NotOpNode node) => new()
        {
            BasicType = BOOLEAN
        };*/

        public static BlaiseType ResolveType(VarRefNode node) => node.VarInfo?.VarDecl.BlaiseType ?? BlaiseType.ErrorType;

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

        public static BlaiseType ResolveType(AbstractAstNode node) =>
            throw new InvalidOperationException($"Unexpected node of type {node.GetType()} found while resolving expression types.");

        public static bool IsAllowedAssignment(BlaiseType exprType, BlaiseType varType)
        {
            return exprType.Equals(varType)
                | exprType.BasicType == (INTEGER | REAL | CHAR) & varType.BasicType == (INTEGER | REAL | CHAR);
        }

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
                if (op == Pow)
                {
                    return new()
                    {
                        BasicType = REAL
                    };
                }
                else
                {
                    return right.DeepCopy();
                }
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