using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [TestClass]
    public class CompilerSWitchTests
    {
        [TestMethod]
        public void CanUseCilSwitch()
        {
            // Arrange
            const string src = @"
                program IntegralSwitch;
                
                var x : integer;

                begin
                    x := 4;
                    case (x) of
                        1: write(1);
                        2: write(2);
                        4: write(4);
                        11: write(11);
                    end;
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.IsTrue(compiler.Cil.Contains("switch"));
            Assert.AreEqual("4", result);
        }

        [TestMethod]
        public void DoesNotUseCilSwitchWithInsufficientDensity()
        {
            // Arrange
            const string src = @"
                program IntegralSwitch;

                var x : integer;

                begin
                    x := 6;
                    case (x) of
                        1: write(1);
                        2: write(2);
                        6: write(6);
                        11: write(11);
                    end;
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.IsFalse(compiler.Cil.Contains("switch"));
            Assert.AreEqual("6", result);
        }

        [TestMethod]
        public void CanBinarySearchIntegralSwitchCases()
        {
            // Arrange
            const string src = @"
                program BigIntegralSwitch;

                function TwentyFive() : integer;
                    return 25;

                begin
                    case (TwentyFive()) of
                        1: write(1);
                        2: write(2);
                        4: write(4);
                        11: write(11);
                        14: write(14);
                        23: write(23);
                        25: write(25);
                        29: write(29);
                        36: write(36);
                    end;
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.IsTrue(compiler.Cil.Contains("bgt"));
            Assert.AreEqual("25", result);
        }

        [TestMethod]
        public void DoesNotUseBinarySearchWithInsufficientCases()
        {
            // Arrange
            const string src = @"
                program IntegralSwitch;

                var x : integer;

                begin
                    x := 3;
                    case (x) of
                        1: write(1);
                        2: write(2);
                        3: write(3);
                        11: write(11);
                    end;
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.IsFalse(compiler.Cil.Contains("bgt"));
            Assert.AreEqual("3", result);
        }

        [TestMethod]
        public void CanSwitchOnReals()
        {
            // Arrange
            const string src = @"
                program RealSwitch;
                
                var x : real;

                begin
                    x := 4.5;
                    case (x) of
                        1.0: write(1.0);
                        2.5: write(2.5);
                        4.5: write(4.5);
                        11.25: write(11.25);
                    end;
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("4.5", result);
        }

        [TestMethod]
        public void CanSwitchOnChars()
        {
            // Arrange
            const string src = @"
                program CharSwitch;
                
                var x : char;

                begin
                    x := 'a';
                    case (x) of
                        'a': write('a');
                        'c': write('c');
                        'D': write('D');
                        'Y': write('Y');
                    end;
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("a", result);
        }
    }
}