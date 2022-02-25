using System;

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

        public static FunctionNode FindFunction(AbstractAstNode climber, string funcName)
        {
            while (climber is not null)
            {
                if (climber is FunctionNode)
                {
                    var func = climber as FunctionNode;
                    if (func.Identifier.Equals(funcName))
                    {
                        return func;
                    }
                }
                climber = climber.Parent;
            }
            throw new InvalidOperationException($"Target not found for function call to {funcName}");
        }
    }
}