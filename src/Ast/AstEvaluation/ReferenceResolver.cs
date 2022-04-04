using System.Linq;

namespace Blaise2.Ast
{
    public class ReferenceResolver
    {
        public static SymbolInfo FindVariable(AbstractAstNode climber, string variableName) => climber switch
        {
            IVarOwner vo => vo.GetVarByName(variableName) switch
            {
                SymbolInfo info => info,
                _ => FindVariable(climber.Parent, variableName)
            },
            null => null,
            _ => FindVariable(climber.Parent, variableName)
        };

        public static FunctionNode FindFunction(FunctionCallNode caller, string funcName, bool isFunction) =>
            FindFunction(caller, caller.Parent, funcName, isFunction);

        private static FunctionNode FindFunction(FunctionCallNode caller, AbstractAstNode climber, string funcName, bool isFunction) =>
            climber switch
            {
                ProgramNode prog => FindFunctionByName(caller, prog, funcName, isFunction) switch
                {
                    FunctionNode func => func,
                    _ => FindFunction(caller, climber.Parent, funcName, isFunction)
                },
                null => null,
                _ => FindFunction(caller, climber.Parent, funcName, isFunction)
            };

        private static FunctionNode FindFunctionByName(FunctionCallNode caller, ProgramNode prog, string funcName, bool isFunction)
        {
            var routines = isFunction ? prog.Functions : prog.Procedures;
            return routines.Where(r => r.Identifier.Equals(funcName) && SignaturesMatch(caller, r))
                           .OfType<FunctionNode>()
                           .FirstOrDefault();
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