using Blaise2.Ast;

namespace Blaise2.Visualizations
{
    public static class VisualizationExtensions
    {
        public static string ToStringTree(this AbstractAstNode node)
        {
            var visitor = new AstStringTree();
            return visitor.Visit((dynamic)node);
        }
    }
}