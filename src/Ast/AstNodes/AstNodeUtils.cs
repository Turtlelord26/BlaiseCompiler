using System.Linq;

namespace Blaise2.Ast
{
    public partial class ProgramNode : AbstractAstNode, IVarOwner
    {
        public virtual SymbolInfo GetVarByName(string name) => VarDecls.Where(v => v.Identifier == name)
                                                                       .Select(v => new SymbolInfo { VarType = VarType.Global, VarDecl = v })
                                                                       .FirstOrDefault();
    }

    public partial class FunctionNode : ProgramNode
    {
        public override SymbolInfo GetVarByName(string name) => VarDecls.Where(v => v.Identifier == name)
                                                                        .Select(v => new SymbolInfo { VarType = VarType.Local, VarDecl = v })
                                                                        .FirstOrDefault()
                                                                ?? Params.Where(v => v.Identifier == name)
                                                                         .Select(v => new SymbolInfo { VarType = VarType.Argument, VarDecl = v })
                                                                         .FirstOrDefault();
    }
}