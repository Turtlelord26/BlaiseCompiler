using Blaise2.Ast;
using Blaise2.Visualizations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [TestClass]
    public class AstStringTreeTests
    {
        [DataTestMethod]
        [SourceCodeSamples]
        public void CanConstructStringTreeSuccessfully(string label, string src)
        {
            // Arrange
            var compiler = new Compiler();
            Assert.IsTrue(compiler.Parse(src));
            var ast = new AstGenerator().Visit(compiler.ParseTree);

            // Act
            Assert.IsNotNull(ast.ToStringTree());
        }
    }
}
