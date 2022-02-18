using System.Linq;

namespace Blaise2.Ast
{
    // TODO: you're going to need GetVarByName() methods for
    // ProcedureNode and FunctionNode, too.
    public partial class ProgramNode : AbstractAstNode, IVarOwner
    {
        public SymbolInfo GetVarByName(string name)
        {
            var results = VarDecls.Where(v => v.Identifier == name);
            return results.Select(v => new SymbolInfo { VarType = VarType.Global, VarDecl = v }).FirstOrDefault();
        }
    }
}