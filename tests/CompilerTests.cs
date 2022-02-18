using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [TestClass]
    public class CompilerTests
    {
        [DataTestMethod]
        [CompilerCodeSamples]
        public void CanRunSuccessfully(string label, string src, string expected)
        {
            // Arrange
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual(expected, result);
        }

        [DataTestMethod]
        [CompilerErrorSamples]
        public void ThrowsCompileErrorsCorrectly(string label, string src, Type expected)
        {
            // Arrange
            var compiler = new Compiler();

            // Act
            try
            {
                Assert.IsTrue(compiler.Compile(src));
                compiler.AssembleToObjectCode();
                var result = compiler.ExecuteObjectCode();
            }
            catch (Exception e)
            {
                Assert.AreEqual(e.GetType(), expected);
                return;
            }

            Assert.Fail($"ThrowsCompileErrorsCorrectly {label} failed to throw {expected}");
        }
    }
}