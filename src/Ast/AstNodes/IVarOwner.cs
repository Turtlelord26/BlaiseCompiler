namespace Blaise2.Ast
{
    public interface IVarOwner
    {
        public SymbolInfo GetVarByName(string name);
    }
}