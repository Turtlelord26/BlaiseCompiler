using System;
using System.Collections.Generic;
using System.Linq;

namespace Blaise2.Ast
{
    public class ReferenceResolver
    {
        // This function searches up the AST, looking for a node that is
        // an IVarOwner.  It then asks the IVarOwner if it knows about the
        // variable in question.  If not, then we continue our search up
        // the tree.
        public static SymbolInfo FindVariable(AbstractAstNode climber, string variableName)
        {
            while (climber is not null)
            {
                if (climber is IVarOwner vo)
                {
                    var symbolInfo = vo.GetVarByName(variableName);
                    if (symbolInfo is not null)
                    {
                        return symbolInfo;
                    }
                }
                climber = climber.Parent;
            }
            return null;
        }

        public static FunctionNode FindFunction(FunctionCallNode caller, string funcName, bool isFunction)
        {
            var climber = caller.Parent;
            while (climber is not null)
            {
                if (climber is ProgramNode)
                {
                    var prog = climber as ProgramNode;
                    var routines = isFunction ? prog.Functions : prog.Procedures;
                    var match = routines.Where(r => r.Identifier.Equals(funcName)
                                                    && SignaturesMatch(caller, r))
                                        .OfType<FunctionNode>()
                                        .FirstOrDefault();
                    if (match is not null)
                    {
                        return match;
                    }
                }
                else if (climber is FunctionNode)
                {
                    var func = climber as FunctionNode;
                    if (func.Identifier.Equals(funcName) && SignaturesMatch(caller, func))
                    {
                        return func;
                    }
                }
                climber = climber.Parent;
            }
            throw new InvalidOperationException($"Target not found for function call to {funcName}");
        }

        public static bool SignaturesMatch(FunctionCallNode caller, FunctionNode func)
        {
            var callArgTypes = caller.Arguments.Select(a => a.GetExprType()).ToList();
            var funcParamTypes = func.Params.Select(p => p.BlaiseType).ToList();
            if (callArgTypes.Count != funcParamTypes.Count)
            {
                return false;
            }
            for (var i = 0; i < callArgTypes.Count; i++)
            {
                if (!callArgTypes[i].Equals(funcParamTypes[i]))
                {
                    return false;
                }
            }
            return true;
        }
    }
}