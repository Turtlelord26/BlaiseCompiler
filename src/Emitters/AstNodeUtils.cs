using System.Linq;

namespace Blaise2.Ast
{
    public partial class ProgramNode : AbstractAstNode, IVarOwner
    {
        public SymbolInfo GetVarByName(string name)
        {
            var results = VarDecls.Where(v => v.Identifier == name);
            return results.Select(v => new SymbolInfo { VarType = VarType.Global, VarDecl = v }).FirstOrDefault();
        }
    }

    public partial class FunctionNode : AbstractAstNode, IVarOwner
    {
        public SymbolInfo GetVarByName(string name)
        {
            var decls = VarDecls.Where(v => v.Identifier == name);
            if (decls.Count() != 0)
            {
                return decls.Select(v => new SymbolInfo { VarType = VarType.Local, VarDecl = v }).FirstOrDefault();
            }
            var parameters = Args.Where(v => v.Identifier == name);
            return parameters.Select(v => new SymbolInfo { VarType = VarType.Argument, VarDecl = v }).FirstOrDefault();
        }
    }

    public partial class ProcedureNode : AbstractAstNode, IVarOwner
    {
        public SymbolInfo GetVarByName(string name)
        {
            var decls = VarDecls.Where(v => v.Identifier == name);
            if (decls.Count() != 0)
            {
                return decls.Select(v => new SymbolInfo { VarType = VarType.Local, VarDecl = v }).FirstOrDefault();
            }
            var parameters = Args.Where(v => v.Identifier == name);
            return parameters.Select(v => new SymbolInfo { VarType = VarType.Argument, VarDecl = v }).FirstOrDefault();
        }
    }
}