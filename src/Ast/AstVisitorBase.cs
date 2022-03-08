using System;

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

        public virtual R Visit(ReturnNode node)
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

        public virtual R Visit(BooleanNode node)
        {
            return default;
        }

        public virtual R Visit(CharNode node)
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

        public virtual R Visit(LogicalOpNode node)
        {
            return default;
        }

        public virtual R Visit(NotOpNode node)
        {
            return default;
        }

        public virtual R Visit(AbstractAstNode node)
        {
            if (node.IsEmpty())
            {
                return default;
            }
            throw new InvalidOperationException($"Unrecognized AstNode of type {node.Type}");
        }
    }
}