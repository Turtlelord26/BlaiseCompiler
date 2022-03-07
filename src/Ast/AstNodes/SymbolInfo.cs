namespace Blaise2.Ast
{
    // This class encapsulates a VarDeclNode with information
    // about its scope--is it a local variable, a global,
    // or the argument to a routine?
    public class SymbolInfo
    {
        public VarType VarType { get; set; }
        public VarDeclNode VarDecl { get; set; }
    }
}