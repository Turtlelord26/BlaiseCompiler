using System;
using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class AstEvaluator
    {
        public static bool EvaluateAst(AbstractAstNode node) => Evaluate((dynamic)node);

        public static List<string> Errors { get; private set; } = new();

        private static bool Evaluate(ProgramNode node) => ContainsNoDuplicateVariables(node.VarDecls)
                                                        & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                                                        & node.Procedures.Aggregate(true, (valid, next) => valid & Evaluate(next))
                                                        & node.Functions.Aggregate(true, (valid, next) => valid & Evaluate(next))
                                                        & Evaluate((dynamic)node.Stat);

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
            if ((node.Condition as ITypedNode).GetExprType().BasicType != BOOLEAN)
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
            if (TypeResolver.ResolveType(node).IsValid())
            {
                //Collapse tree if possible
                return valid;
            }
            else
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {(node.Left).GetExprType()}, {(node.Right).GetExprType()}");
                return false;
            }

        }

        private static bool Evaluate(BooleanOpNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            if (TypeResolver.ResolveType(node).IsValid())
            {
                //Collapse tree if possible
                return valid;
            }
            else
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {(node.Left).GetExprType()}, {(node.Right).GetExprType()}");
                return false;
            }
        }

        /*private static bool Evaluate(LogicalOperatorNode node)
        {
            var valid = Evaluate((dynamic)node.Left) & Evaluate((dynamic)node.Right);
            var leftType = ResolveType(node.Left);
            var rightType = ResolveType(node.Left);
            valid = leftType.BasicType is BOOLEAN
                  & rightType.BasicType is BOOLEAN;
            //Collapse tree if possible
            return valid;
        }

        private static bool Evaluate(NotOperatorNode node) => Evaluate(node.Expr)
                                                            & ResolveType(node.Expr).BasicType == BOOLEAN;
            //Collapse tree if possible*/

        private static bool Evaluate(FunctionCallNode node)
        {
            var valid = node.Arguments.Aggregate(true, (valid, next) => Evaluate((dynamic)next));
            node.CallTarget = ReferenceResolver.FindFunction(node, node.Identifier, node.IsFunction);
            if (node.CallTarget.IsEmpty())
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

        private static bool ContainsNoDuplicateFunctionSignatures(IEnumerable<FunctionNode> functions)
        {
            var valid = true;
            var seen = new HashSet<BlaiseSignature>();
            foreach (var node in functions)
            {
                var sig = GetFunctionSignature(node);
                if (!seen.Add(sig))
                {
                    Errors.Append($"Duplicate Function {sig} detected in {node.Parent.GetType()} {(node.Parent as ProgramNode).Identifier}");
                    valid = false;
                }
            }
            return valid;
        }

        private static bool ContainsNoDuplicateVariables(IEnumerable<VarDeclNode> varDecls)
        {
            var valid = true;
            var seen = new HashSet<string>();
            foreach (var node in varDecls)
            {
                if (!seen.Add(node.Identifier))
                {
                    Errors.Append($"Duplicate variable {node.Identifier} detected in {node.Parent.GetType()} {(node.Parent as ProgramNode).Identifier}");
                    valid = false;
                }
            }
            return valid;
        }

        private static BlaiseSignature GetFunctionSignature(FunctionNode function) => new BlaiseSignature()
        {
            Identifier = function.Identifier,
            ReturnType = function.ReturnType,
            Parameters = function.Params.Select(p => (p as VarDeclNode).BlaiseType).ToList()
        };

        private static AbstractAstNode GetContainingFunction(AbstractAstNode climber)
        {
            while (climber is not null && climber is not FunctionNode)
            {
                climber = climber.Parent;
            }
            return climber is not null ? climber : AbstractAstNode.Empty;
        }

        private static bool CallSignatureMatchesFunctionSignature(FunctionCallNode node)
        {
            if (ReferenceResolver.SignaturesMatch(node, node.CallTarget))
            {
                return true;
            }
            var callArgTypes = node.Arguments.Select(a => a.GetExprType()).ToList();
            var funcParamTypes = node.CallTarget.Params.Select(p => p.BlaiseType).ToList();
            Errors.Append($"Call to {node.CallTarget.Identifier} expected argument types {funcParamTypes} but got {callArgTypes}.");
            return false;
        }
    }
}