using System;

namespace Blaise2.Ast
{
    public static class AstNodeExtensions
    {
        public static T WithParent<T>(this T node, AbstractAstNode parent) where T : AbstractAstNode
        {
            node.Parent = parent;
            return node;
        }

        public static T Build<T>(this T node, Action<T> action) where T : AbstractAstNode
        {
            action(node);
            return node;
        }

        public static T Build<T>(Action<T> action) where T : AbstractAstNode, new()
        {
            var node = new T();
            return node.Build(action);
        }
    }
}