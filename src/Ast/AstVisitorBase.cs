namespace Blaise2.Ast
{
    public class AstVisitorBase<R>
    {
        public virtual R Visit(ProgramNode node)
        {
            return default;
        }

        public virtual R Visit(VarDeclNode node)
        {
            return default;
        }

        public virtual R Visit(ProcedureNode node)
        {
            return default;
        }

        public virtual R Visit(FunctionNode node)
        {
            return default;
        }

        public virtual R Visit(BlockNode node)
        {
            return default;
        }

        public virtual R Visit(WriteNode node)
        {
            return default;
        }

        public virtual R Visit(AssignmentNode node)
        {
            return default;
        }

        public virtual R Visit(FunctionCallNode node)
        {
            return default;
        }

        public virtual R Visit(IntegerNode node)
        {
            return default;
        }

        public virtual R Visit(RealNode node)
        {
            return default;
        }

        public virtual R Visit(StringNode node)
        {
            return default;
        }

        public virtual R Visit(VarRefNode node)
        {
            return default;
        }

        public virtual R Visit(BinaryOpNode node)
        {
            return default;
        }

        public virtual R Visit(BooleanOpNode node)
        {
            return default;
        }
    }
}