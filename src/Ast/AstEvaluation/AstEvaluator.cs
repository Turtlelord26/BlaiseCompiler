using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public partial class AstEvaluator : AbstractAstVisitor<bool>
    {
        public List<string> Errors { get; init; } = new();

        public override bool VisitProgram(ProgramNode node)
        {
            var valid = node.VarDecls.Aggregate(true, (valid, decl) => valid & VisitVarDecl(decl))
                        & ContainsNoDuplicateVariables(node.VarDecls)
                        & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                        & node.Procedures.Aggregate(true, (valid, proc) => valid & VisitFunction(proc))
                        & node.Functions.Aggregate(true, (valid, func) => valid & VisitFunction(func))
                        & VisitStatement(node.Stat);
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a program identifier.");
                return false;
            }
            return valid;
        }

        public override bool VisitVarDecl(VarDeclNode node)
        {
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a variable identifier.");
                return false;
            }
            return true;
        }

        public override bool VisitBlock(BlockNode node) => node.Stats.Aggregate(true, (valid, stat) => valid & VisitStatement(stat));

        public override bool VisitWrite(WriteNode node) => VisitExpression(node.Expression);

        public override bool VisitAssignment(AssignmentNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            var valid = node.VarInfo is not null
                 & VisitExpression(node.Expression);
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
            if (node.Expression is IConstantNode)
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

        public override bool VisitFunction(FunctionNode node)
        {
            var valid = (node.IsFunction && node.ReturnType is not null)
                      | !node.IsFunction;
            var varDecls = node.VarDecls.Concat(node.Params).ToList();
            valid = valid
                & ContainsNoDuplicateVariables(varDecls)
                & ContainsNoDuplicateFunctionSignatures(node.Functions.Concat(node.Procedures))
                & node.Procedures.Aggregate(true, (valid, proc) => valid & VisitFunction(proc))
                & node.Functions.Aggregate(true, (valid, func) => valid & VisitFunction(func))
                & VisitStatement(node.Stat);
            if (BlaiseKeywords.IsKeyword(node.Identifier))
            {
                Errors.Append($"{node.Identifier} is a reserved word and cannot be used as a function identifier.");
                valid = false;
            }
            if (FunctionReturnEvaluator.Visit(node))
            {
                return valid;
            }
            else if (!node.IsFunction) //add explicit return to procedure with implicit terminal return.
            {
                var retNode = Build<ReturnNode>(n => n.Expression = (AbstractTypedAstNode)AbstractAstNode.Empty).WithParent(node);
                switch (node.Stat)
                {
                    case BlockNode block:
                        block.Stats.Add(retNode);
                        return valid;
                    case not (ReturnNode or BlockNode):
                        node.Stat = Build<BlockNode>(n => n.Stats = new List<AbstractAstNode>() { node.Stat, retNode }).WithParent(node);
                        return valid;
                }
            }
            Errors.Append($"{node.Identifier}: Not all code paths return{(node.IsFunction ? " a value" : "")}.");
            return false;
        }

        public override bool VisitIf(IfNode node)
        {
            var valid = VisitExpression(node.Condition)
                    & VisitStatement(node.ThenStat)
                    & VisitStatement(node.ElseStat);
            if (TypeResolver.ResolveType(node.Condition).BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve if condition to a bool.");
                return false;
            }
            return valid;
        }

        public override bool VisitLoop(LoopNode node)
        {
            var valid = VisitExpression(node.Condition)
                      & VisitStatement(node.Body);
            if (node.Condition.GetExprType().BasicType != BOOLEAN)
            {
                Errors.Append("Cannot resolve loop condition to a bool.");
                return false;
            }
            return valid;
        }

        public override bool VisitForLoop(ForLoopNode node)
        {
            var valid = VisitLoop(node)
                      & VisitAssignment(node.Assignment);
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

        public override bool VisitSwitch(SwitchNode node)
        {
            var valid = VisitExpression(node.Input);
            var inType = TypeResolver.ResolveType(node.Input);
            valid = inType.IsValid()
                    & node.Cases.Aggregate(true, (valid, caseNode) => valid & VisitSwitchCase(caseNode));
            if (!node.Default.IsEmpty())
            {
                valid = valid & VisitStatement(node.Default);
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

        public bool VisitSwitchCase(SwitchCaseNode node)
        {
            return VisitExpression(node.Case)
                & VisitStatement(node.Stat);
        }

        public override bool VisitReturn(ReturnNode node)
        {
            var valid = VisitExpression(node.Expression);
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

        public override bool VisitBinaryOperator(BinaryOpNode node)
        {
            var valid = VisitExpression(node.Left) & VisitExpression(node.Right);
            if (!TypeResolver.ResolveType(node).IsValid())
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {node.Left.GetExprType()}, {node.Right.GetExprType()}");
                return false;
            }
            return valid;
        }

        public override bool VisitBooleanOperator(BooleanOpNode node)
        {
            var valid = VisitExpression(node.Left) & VisitExpression(node.Right);
            if (!TypeResolver.ResolveType(node).IsValid())
            {
                Errors.Append($"Cannot apply operator {node.Operator} to types {node.Left.GetExprType()}, {node.Right.GetExprType()}");
                return false;
            }
            return valid;
        }

        public override bool VisitLogicalOperator(LogicalOpNode node)
        {
            var valid = VisitExpression(node.Left) & VisitExpression(node.Right);
            if (!TypeResolver.ResolveType(node).IsValid()
                | node.LeftType.BasicType is not BOOLEAN
                | node.RightType.BasicType is not BOOLEAN)
            {
                Errors.Append($"Could not resolve LogicalOperatorNode operands to booleans. Got {node.LeftType} {node.Operator} {node.RightType}.");
                valid = false;
            }
            return valid;
        }

        public override bool VisitNotOperator(NotOpNode node)
        {
            var valid = VisitExpression(node.Expression);
            if (TypeResolver.ResolveType(node.Expression).BasicType != BOOLEAN)
            {
                Errors.Append($"Could not resolve Not operand to a boolean. Got {node.Expression.GetExprType()}.");
                return false;
            }
            return valid;
        }

        public override bool VisitCall(FunctionCallNode node)
        {
            var valid = node.Arguments.Aggregate(true, (valid, arg) => valid = valid & VisitExpression(arg));
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

        public override bool VisitVarRef(VarRefNode node)
        {
            node.VarInfo = ReferenceResolver.FindVariable(node, node.Identifier);
            if (node.VarInfo is null)
            {
                Errors.Append($"Cannot resolve variable {node.Identifier}");
                return false;
            }
            return true;
        }

        public override bool VisitBoolean(BooleanNode node) => true;

        public override bool VisitChar(CharNode node) => true;

        public override bool VisitInteger(IntegerNode node) => true;

        public override bool VisitReal(RealNode node) => true;

        public override bool VisitString(StringNode node) => true;

        public override bool VisitEmpty(AbstractAstNode node) => true;
    }
}