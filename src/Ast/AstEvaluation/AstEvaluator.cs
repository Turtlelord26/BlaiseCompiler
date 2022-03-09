using System;
using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public partial class AstEvaluator
    {
        public static List<string> Errors { get; private set; } = new();

        public static bool EvaluateAst(AbstractAstNode node) => Evaluate((dynamic)node);

        private static bool Evaluate(ProgramNode node)
        {
            var valid = node.VarDecls.Aggregate(true, (valid, next) => valid & Evaluate(next))
                        & ContainsNoDuplicateVariables(node.VarDecls)
                        & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                        & node.Procedures.Aggregate(true, (valid, next) => valid & Evaluate(next))
                        & node.Functions.Aggregate(true, (valid, next) => valid & Evaluate(next))
                        & Evaluate((dynamic)node.Stat);
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a program identifier.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(VarDeclNode node)
        {
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a variable identifier.");
                return false;
            }
            return true;
        }

        private static bool Evaluate(BlockNode node) => node.Stats.Aggregate(true, (valid, next) => valid & Evaluate((dynamic)next));

        private static bool Evaluate(WriteNode node) => Evaluate((dynamic)node.Expression);

        private static bool Evaluate(AssignmentNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            var valid = node.VarInfo is not null
                 & Evaluate((dynamic)node.Expression);
            var varType = node.VarInfo?.VarDecl.BlaiseType;
            var exprType = TypeResolver.ResolveType((dynamic)node.Expression);
            if (TypeResolver.IsAllowedAssignment(exprType, varType))
            {
                return valid;
            }
            Errors.Append($"Cannot implicitly convert a value of type {exprType} to {varType}.");
            return false;
        }

        private static bool Evaluate(FunctionNode node)
        {
            var valid = node.IsFunction && node.ReturnType is not null
                      | !node.IsFunction;
            var varDecls = node.VarDecls.Concat(node.Params).ToList();
            valid = valid
                & ContainsNoDuplicateVariables(varDecls)
                & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                & node.Procedures.Aggregate(true, (valid, next) => valid & Evaluate(next))
                & node.Functions.Aggregate(true, (valid, next) => valid & Evaluate(next))
                & Evaluate((dynamic)node.Stat);
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a function identifier.");
                valid = false;
            }
            if (FunctionReturnEvaluator.Visit(node))
            {
                return valid;
            }
            Errors.Append($"{node.Identifier}: Not all code paths return{(node.IsFunction ? " a value" : "")}.");
            return false;
        }

        private static bool Evaluate(IfNode node)
        {
            var valid = Evaluate((dynamic)node.Condition)
                    & Evaluate((dynamic)node.ThenStat)
                    & Evaluate((dynamic)node.ElseStat);
            if (TypeResolver.ResolveType((dynamic)node.Condition).BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve if condition to a bool.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(LoopNode node)
        {
            var valid = Evaluate((dynamic)node.Condition)
                      & Evaluate((dynamic)node.Body);
            if ((node.Condition as AbstractTypedAstNode).GetExprType().BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve loop condition to a bool.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(ForLoopNode node) => Evaluate((LoopNode)node)
                                                        & Evaluate(node.Assignment)
                                                        & Evaluate(node.Iteration);

        private static bool Evaluate(SwitchNode node)
        {
            var inType = TypeResolver.ResolveType((dynamic)node.Input);
            var valid = node.Cases.Aggregate(true, (valid, next) => valid & Evaluate(next))
                    & Evaluate((dynamic)node.Default);
            if (!TypeResolver.IsValidSwitchInput(inType))
            {
                Errors.Append($"{inType} is not a valid case statement input type.");
                return false;
            };
            var typesMatch = node.Cases.Aggregate(true, (valid, next) => valid & TypeResolver.ResolveType((dynamic)next.Case).Equals(inType));
            if (!typesMatch)
            {
                Errors.Append($"Case alternative type mismatch, expected {inType}.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(SwitchCaseNode node)
        {
            return Evaluate((dynamic)node.Case)
                & Evaluate((dynamic)node.Stat);
        }

        private static bool Evaluate(ReturnNode node)
        {
            var valid = Evaluate((dynamic)node.Expression);
            var containingFunction = GetContainingFunction(node.Parent);
            if (containingFunction is not FunctionNode)
            {
                Errors.Append($"Return statement cannot resolve containing function.");
                return false;
            }
            var func = (FunctionNode)containingFunction;
            var exprType = node.Expression.GetExprType();
            if (!exprType.IsValid())
            {
                Errors.Append($"Cannot resolve type of return expression in {((AbstractAstNode)node.Expression).Type}.");
            }
            if (func.IsFunction & exprType.Equals(func.ReturnType)
                || !func.IsFunction & ((AbstractAstNode)node.Expression).IsEmpty())
            {
                return valid;
            }
            else
            {
                Errors.Append($"Return expression type does not match function return type. Expected ({(func.IsFunction ? func.ReturnType : "void")}) but got ({exprType})");
                return false;
            }
        }

        private static bool Evaluate(BinaryOpNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            if (!TypeResolver.ResolveType(node).IsValid())
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {(node.Left).GetExprType()}, {(node.Right).GetExprType()}");
                return false;
            }
            ExpressionFolder.Visit(node);
            return valid;
        }

        private static bool Evaluate(BooleanOpNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            if (!TypeResolver.ResolveType(node).IsValid())
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {(node.Left).GetExprType()}, {(node.Right).GetExprType()}");
                return false;
            }
            ExpressionFolder.Visit(node);
            return valid;
        }

        private static bool Evaluate(LogicalOpNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            if (!TypeResolver.ResolveType(node).IsValid()
                | node.LeftType.BasicType is not BOOLEAN
                | node.RightType.BasicType is not BOOLEAN)
            {
                Errors.Append($"Could not resolve LogicalOperatorNode operands to booleans. Got {node.LeftType} {node.Operator} {node.RightType}.");
                valid = false;
            }
            ExpressionFolder.Visit(node);
            return valid;
        }

        private static bool Evaluate(NotOpNode node)
        {
            var valid = Evaluate((dynamic)node.Expression);
            if (TypeResolver.ResolveType((dynamic)node.Expression).BasicType != BOOLEAN)
            {
                Errors.Append($"Could not resolve Not operand to a boolean. Got {node.Expression.GetExprType()}.");
                return false;
            }
            ExpressionFolder.Visit(node);
            return valid;
        }

        private static bool Evaluate(FunctionCallNode node)
        {
            var valid = node.Arguments.Aggregate(true, (valid, next) => Evaluate((dynamic)next));
            node.CallTarget = ReferenceResolver.FindFunction(node, node.Identifier, node.IsFunction);
            if (node.CallTarget is null)
            {
                Errors.Append("Can't resolve target of function call.");
                valid = false;
            }
            else if (!CallSignatureMatchesFunctionSignature(node))
            {
                Errors.Append($"Argument mismatch. Expected {node.CallTarget.Params.Select(p => p.BlaiseType)} but got {node.Arguments.Select(a => a.GetExprType())}");
                valid = false;
            }
            return valid;
        }

        private static bool Evaluate(IntegerNode node) => true;

        private static bool Evaluate(RealNode node) => true;

        private static bool Evaluate(BooleanNode node) => true;

        private static bool Evaluate(VarRefNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            if (node.VarInfo is null)
            {
                Errors.Append($"Cannot resolve variable {node.Identifier}");
                return false;
            }
            return true;
        }

        private static bool Evaluate(CharNode node) => true;

        private static bool Evaluate(StringNode node) => true;

        private static bool Evaluate(AbstractAstNode node) => node.IsEmpty() ? true
            : throw new InvalidOperationException($"Unrecognized node type {node.GetType()}");
    }
}