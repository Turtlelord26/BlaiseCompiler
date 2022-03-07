using System.Collections.Generic;
using System.Linq;

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