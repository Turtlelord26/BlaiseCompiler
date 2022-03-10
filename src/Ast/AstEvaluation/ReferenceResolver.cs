using System;
using System.Collections.Generic;
using System.Linq;

namespace Blaise2.Ast
{
    public class ReferenceResolver
    {
        public static SymbolInfo FindVariable(AbstractAstNode climber, string variableName) => climber switch
        {
            IVarOwner vo when vo.GetVarByName(variableName, out var symbol) is not null => symbol,
            null => null,
            _ => FindVariable(climber.Parent, variableName)
        };

        public static FunctionNode FindFunction(FunctionCallNode caller, string funcName, bool isFunction) =>
            FindFunction(caller, caller.Parent, funcName, isFunction);

        private static FunctionNode FindFunction(FunctionCallNode caller, AbstractAstNode climber, string funcName, bool isFunction) =>
            climber switch
            {
                ProgramNode prog when FindFunctionByName(caller, prog, funcName, isFunction, out var func) is not null => func,
                null => null,
                _ => FindFunction(caller, climber.Parent, funcName, isFunction)
            };

        private static FunctionNode FindFunctionByName(FunctionCallNode caller, ProgramNode prog, string funcName, bool isFunction, out FunctionNode func)
        {
            var routines = isFunction ? prog.Functions : prog.Procedures;
            func = routines.Where(r => r.Identifier.Equals(funcName) && SignaturesMatch(caller, r))
                           .OfType<FunctionNode>()
                           .FirstOrDefault();
            return func;
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