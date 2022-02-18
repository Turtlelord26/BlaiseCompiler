using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [TestClass]
    public class ParserTests
    {
        [DataTestMethod]
        [SourceCodeSamples]
        public void CanParseSuccessfully(string label, string src)
        {
            // Arrange
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Parse(src));
        }
    }
}
