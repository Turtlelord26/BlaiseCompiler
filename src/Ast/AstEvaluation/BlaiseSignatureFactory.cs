using System.Linq;

namespace Blaise2.Ast
{
    public class BlaiseSignatureFactory
    {
        public static BlaiseSignature MakeFunctionSignature(FunctionNode function) => new BlaiseSignature()
        {
            Identifier = function.Identifier,
            ReturnType = function.ReturnType,
            Parameters = function.Params.Select(p => (p as VarDeclNode).BlaiseType).ToList()
        };
    }
}