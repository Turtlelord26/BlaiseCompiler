using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
​
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
​
            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();
​
            // Assert
            Assert.AreEqual(expected, result);
        }
​
        [DataTestMethod]
        [CompilerErrorSamples]
        public void ThrowsCompileErrorsCorrectly(string label, string src, Type expected)
        {
            // Arrange
            var compiler = new Compiler();
​
            // Act
            try
            {
                Assert.IsTrue(compiler.Compile(src));
                compiler.AssembleToObjectCode();
                var result = compiler.ExecuteObjectCode();
            }
            catch (Exception e)
            {
                Assert.AreEqual(expected, e.GetType());
                return;
            }
​
            Assert.Fail($"ThrowsCompileErrorsCorrectly {label} failed to throw {expected}");
        }
​
        [TestMethod]
        public void CanDoWhileLoops()
        {
            // Arrange
            const string src = @"
                program Printing;
​
                var x : integer;
​
                begin
                    x := 0;
                    while x < 5 do begin
                        x := x + 1;
                    end;
                    write( x );
                end.";
            var compiler = new Compiler();
​
            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();
​
            // Assert
            Assert.AreEqual("5", result);
        }
​
        [TestMethod]
        public void CanDoRepeatUntilLoops()
        {
            // Arrange
            const string src = @"
                program Printing;
​
                var x : integer;
​
                begin
                    x := 0;
                    repeat
                        x := x + 1;
                    until x > 5;
                    write( x );
                end.";
            var compiler = new Compiler();
​
            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();
​
            // Assert
            Assert.AreEqual("6", result);
        }
​
        [TestMethod]
        public void CanDoForLoops()
        {
            // Arrange
            const string src = @"
                program Printing;
​
                var x : integer;
​
                begin
                    for x := 1 to 5 do
                    begin end;
                    write( x );
                end.";
            var compiler = new Compiler();
​
            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();
​
            // Assert
            Assert.AreEqual("5", result);
        }
​
        [TestMethod]
        public void CanDoForDowntoLoops()
        {
            // Arrange
            const string src = @"
                program Printing;
​
                var x : integer;
​
                begin
                    for x := 5 downto 1 do
                    begin end;
                    write( x );
                end.";
            var compiler = new Compiler();
​
            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();
​
            // Assert
            Assert.AreEqual("1", result);
        }
    }
}
