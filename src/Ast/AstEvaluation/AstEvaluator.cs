using System;
using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public partial class AstEvaluator
    {
        public static List<string> Errors { get; private set; } = new();

        public static bool EvaluateAst(ProgramNode node) => Evaluate(node);

        private static bool Evaluate(ProgramNode node)
        {
            var valid = node.VarDecls.Aggregate(true, (valid, decl) => valid & Evaluate(decl))
                        & ContainsNoDuplicateVariables(node.VarDecls)
                        & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                        & node.Procedures.Aggregate(true, (valid, proc) => valid & Evaluate(proc))
                        & node.Functions.Aggregate(true, (valid, func) => valid & Evaluate(func))
                        & EvaluateStat(node.Stat);
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

        private static bool Evaluate(BlockNode node) => node.Stats.Aggregate(true, (valid, stat) => valid & EvaluateStat(stat));

        private static bool Evaluate(WriteNode node) => EvaluateExpression(node.Expression);

        private static bool Evaluate(AssignmentNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            var valid = node.VarInfo is not null
                 & EvaluateExpression(node.Expression);
            var varType = node.VarInfo?.VarDecl.BlaiseType;
            if (varType is null)
            {
                Errors.Append($"Cannot resolve variable {node.Identifier}.");
                return false;
            }
            var exprType = TypeResolver.ResolveType(node.Expression);
            if (varType.Equals(exprType))
            {
                return valid;
            }
            if (IsConstantNode(node.Expression))
            {
                var promoted = PromoteAssignmentExpressionOrEmpty(node.Expression, varType.BasicType);
                if (!promoted.IsEmpty())
                {
                    node.Expression = promoted;
                    return valid;
                }
            }
            else if (IsAllowedPromotion(exprType, varType))
            {
                return valid;
            }
            Errors.Append($"Cannot assign a value of type {exprType} to {varType}.");
            return false;
        }

        private static bool Evaluate(FunctionNode node)
        {
            var valid = (node.IsFunction && node.ReturnType is not null)
                      | !node.IsFunction;
            var varDecls = node.VarDecls.Concat(node.Params).ToList();
            valid = valid
                & ContainsNoDuplicateVariables(varDecls)
                & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                & node.Procedures.Aggregate(true, (valid, proc) => valid & Evaluate(proc))
                & node.Functions.Aggregate(true, (valid, func) => valid & Evaluate(func))
                & EvaluateStat(node.Stat);
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a function identifier.");
                valid = false;
            }
            if (FunctionReturnEvaluator.Visit(node))
            {
                return valid;
            }
            else if (!node.IsFunction)
            {
                var retNode = Build<ReturnNode>(n => n.Expression = (AbstractTypedAstNode)AbstractAstNode.Empty).WithParent(node);
                switch (node.Stat)
                {
                    case BlockNode block:
                        block.Stats.Add(retNode);
                        return valid;
                    case AbstractAstNode when IsStatButNotBlockOrReturn(node.Stat):
                        node.Stat = Build<BlockNode>(n => n.Stats = new List<AbstractAstNode>() { node.Stat, retNode }).WithParent(node);
                        return valid;
                }
            }
            Errors.Append($"{node.Identifier}: Not all code paths return{(node.IsFunction ? " a value" : "")}.");
            return false;
        }

        private static bool Evaluate(IfNode node)
        {
            var valid = EvaluateExpression(node.Condition)
                    & EvaluateStat(node.ThenStat)
                    & EvaluateStat(node.ElseStat);
            if (TypeResolver.ResolveType(node.Condition).BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve if condition to a bool.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(LoopNode node)
        {
            var valid = EvaluateExpression(node.Condition)
                      & EvaluateStat(node.Body);
            if ((node.Condition as AbstractTypedAstNode).GetExprType().BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve loop condition to a bool.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(ForLoopNode node)
        {
            var valid = Evaluate((LoopNode)node)
                      & Evaluate(node.Assignment);
            var iterType = node.Assignment.VarInfo.VarDecl.BlaiseType.BasicType;
            if (iterType is not INTEGER)
            {
                Errors.Append($"For loop iterator must be of type integer, but is {iterType}.");
                valid = false;
            }
            var limitType = (node.Condition as BooleanOpNode).Right.GetExprType().BasicType;
            if (limitType is not INTEGER)
            {
                Errors.Append($"For loop limit must be of type integer, but is {limitType}.");
                valid = false;
            }
            return valid;
        }

        private static bool Evaluate(SwitchNode node)
        {
            var valid = EvaluateExpression(node.Input);
            var inType = TypeResolver.ResolveType(node.Input);
            valid = inType.IsValid()
                    & node.Cases.Aggregate(true, (valid, caseNode) => valid & Evaluate(caseNode));
            if (!node.Default.IsEmpty())
            {
                valid = valid & EvaluateStat(node.Default);
            }
            if (!IsValidSwitchInput(inType))
            {
                Errors.Append($"{inType} is not a valid case statement input type.");
                return false;
            };
            var typesMatch = node.Cases.Aggregate(true, (valid, caseNode) => valid & TypeResolver.ResolveType(caseNode.Case).Equals(inType));
            if (!typesMatch)
            {
                Errors.Append($"Case alternative type mismatch, expected {inType}.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(SwitchCaseNode node)
        {
            return EvaluateExpression(node.Case)
                & EvaluateStat(node.Stat);
        }

        private static bool Evaluate(ReturnNode node)
        {
            var valid = EvaluateExpression(node.Expression);
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
            var valid = EvaluateExpression(node.Left) & EvaluateExpression(node.Right);
            if (!TypeResolver.ResolveType(node).IsValid())
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {node.Left.GetExprType()}, {node.Right.GetExprType()}");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(BooleanOpNode node)
        {
            var valid = EvaluateExpression(node.Left) & EvaluateExpression(node.Right);
            if (!TypeResolver.ResolveType(node).IsValid())
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {node.Left.GetExprType()}, {node.Right.GetExprType()}");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(LogicalOpNode node)
        {
            var valid = EvaluateExpression(node.Left) & EvaluateExpression(node.Right);
            if (!TypeResolver.ResolveType(node).IsValid()
                | node.LeftType.BasicType is not BOOLEAN
                | node.RightType.BasicType is not BOOLEAN)
            {
                Errors.Append($"Could not resolve LogicalOperatorNode operands to booleans. Got {node.LeftType} {node.Operator} {node.RightType}.");
                valid = false;
            }
            return valid;
        }

        private static bool Evaluate(NotOpNode node)
        {
            var valid = EvaluateExpression(node.Expression);
            if (TypeResolver.ResolveType(node.Expression).BasicType != BOOLEAN)
            {
                Errors.Append($"Could not resolve Not operand to a boolean. Got {node.Expression.GetExprType()}.");
                return false;
            }
            return valid;
        }

        private static bool Evaluate(FunctionCallNode node)
        {
            var valid = node.Arguments.Aggregate(true, (valid, arg) => valid = valid & EvaluateExpression(arg));
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

        private static bool EvaluateStat(AbstractAstNode stat) => stat switch
        {
            BlockNode block => Evaluate(block),
            WriteNode write => Evaluate(write),
            AssignmentNode assign => Evaluate(assign),
            IfNode ifn => Evaluate(ifn),
            ForLoopNode forl => Evaluate(forl),
            LoopNode loop => Evaluate(loop),
            SwitchNode switcher => Evaluate(switcher),
            ReturnNode ret => Evaluate(ret),
            FunctionCallNode call => Evaluate(call),
            AbstractAstNode aan when aan.IsEmpty() => true,
            _ => throw new InvalidOperationException($"Unexpected node type {stat.GetType()} encountered during Ast evaluation.")
        };

        private static bool EvaluateExpression(AbstractTypedAstNode expr) => expr switch
        {
            LogicalOpNode logop => Evaluate(logop),
            BooleanOpNode boolop => Evaluate(boolop),
            BinaryOpNode binop => Evaluate(binop),
            NotOpNode notop => Evaluate(notop),
            FunctionCallNode call => Evaluate(call),
            VarRefNode varref => Evaluate(varref),
            IntegerNode => true,
            RealNode => true,
            BooleanNode => true,
            CharNode => true,
            StringNode => true,
            AbstractTypedAstNode atan when atan.IsEmpty() => true,
            _ => throw new InvalidOperationException($"Invalid node type {expr.GetType()} detected during Ast Evaluation")
        };
    }
}