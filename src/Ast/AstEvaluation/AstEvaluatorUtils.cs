using System.Collections.Generic;
using System.Linq;
using static Blaise2.Ast.AstNodeExtensions;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public partial class AstEvaluator
    {
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

        private static bool ContainsNoDuplicateVariables(List<VarDeclNode> varDecls)
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

        private static AbstractAstNode GetContainingFunction(AbstractAstNode climber) => climber switch
        {
            FunctionNode => climber,
            null => AbstractAstNode.Empty,
            _ => GetContainingFunction(climber.Parent)
        };

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

        private static bool IsConstantNode(AbstractAstNode node) => node is BooleanNode
                                                                         or CharNode
                                                                         or IntegerNode
                                                                         or RealNode
                                                                         or StringNode;

        private static bool IsAllowedPromotion(BlaiseType exprType, BlaiseType varType)
        {
            return (exprType.BasicType, varType.BasicType) is (CHAR, INTEGER)
                                                           or (CHAR, REAL)
                                                           or (CHAR, STRING)
                                                           or (INTEGER, REAL)
                                                           or (INTEGER, STRING)
                                                           or (REAL, STRING)
                                                           //Implicit downcasts below
                                                           or (INTEGER, CHAR)
                                                           or (REAL, CHAR)
                                                           or (REAL, INTEGER);
        }

        private static AbstractTypedAstNode PromoteAssignmentExpressionOrEmpty(AbstractTypedAstNode expression, BlaiseTypeEnum varType)
        {
            return (expression, varType) switch
            {
                (CharNode node, INTEGER) => Build<IntegerNode>(n => n.IntValue = node.CharValue),
                (CharNode node, REAL) => Build<RealNode>(n => n.RealValue = node.CharValue),
                (IntegerNode node, REAL) => Build<RealNode>(n => n.RealValue = node.IntValue),
                (CharNode node, STRING) => Build<StringNode>(n => n.StringValue = node.CharValue.ToString()),
                (IntegerNode node, STRING) => Build<StringNode>(n => n.StringValue = node.IntValue.ToString()),
                (RealNode node, STRING) => Build<StringNode>(n => n.StringValue = node.RealValue.ToString()),
                //Implicit downcasts below
                (IntegerNode node, CHAR) => Build<CharNode>(n => n.CharValue = (char)node.IntValue),
                (RealNode node, CHAR) => Build<CharNode>(n => n.CharValue = (char)node.RealValue),
                (RealNode node, INTEGER) => Build<IntegerNode>(n => n.IntValue = (int)node.RealValue),
                _ => (AbstractTypedAstNode)AbstractAstNode.Empty
            };
        }

        private static bool IsStatButNotBlockOrReturn(AbstractAstNode node) => node is AssignmentNode
                                                                                    or WriteNode
                                                                                    or FunctionCallNode
                                                                                    or IfNode
                                                                                    or LoopNode
                                                                                    or SwitchNode;
    }
}