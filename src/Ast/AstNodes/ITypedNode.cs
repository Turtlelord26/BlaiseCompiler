namespace Blaise2.Ast
{
    public interface ITypedNode
    {
        public abstract BlaiseType GetExprType();
    }
}