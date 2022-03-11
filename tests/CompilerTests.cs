using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Blaise2.Visualizations.VisualizationExtensions;

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
                Assert.AreEqual(expected, e.GetType());
                return;
            }

            Assert.Fail($"ThrowsCompileErrorsCorrectly {label} failed to throw {expected}");
        }

        [TestMethod]
        public void CanDoWhileLoops()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    x := 0;
                    while x < 5 do begin
                        x := x + 1;
                    end;
                    write( x );
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("5", result);
        }

        [TestMethod]
        public void CanDoRepeatUntilLoops()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    x := 0;
                    repeat
                        x := x + 1;
                    until x > 5;
                    write( x );
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("6", result);
        }

        [TestMethod]
        public void CanDoForLoops()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    for x := 1 to 5 do
                    begin end;
                    write( x );
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("5", result);
        }

        [TestMethod]
        public void CanDoForDowntoLoops()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    for x := 5 downto 1 do
                    begin end;
                    write( x );
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void CanDoIf()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    x := 5;
                    if (x > 5) then
                        x := x - 1;
                    else if (x <= 5) then
                        x := x + 1;
                    write( x );
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("6", result);
        }

        [TestMethod]
        public void CanDoSimpleIfStat()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    x := 0;
                    if x = 0 then write(true);
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("True", result);
        }

        [TestMethod]
        public void CanDoSimpleIfElseStat()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    x := 0;
                    if x = 1 then write(false);
                    else write(true);
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("True", result);
        }

        [TestMethod]
        public void CanDoNestedIfElseStat()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x : integer;

                begin
                    x := 3;
                    if x = 1 then write(1);
                    else if x = 2 then write(2);
                    else write(3);
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("3", result);
        }

        [TestMethod]
        public void CanDoSimpleFunction()
        {
            // Arrange
            const string src = @"
                program Printing;

                function Ten(): integer;
                begin
                    return 10;
                end;

                begin
                    write(Ten());
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("10", result);
        }

        [TestMethod]
        public void FunctionsCanReferenceGlobals()
        {
            // Arrange
            const string src = @"
                program Printing;

                var x: integer;

                function X(): integer;
                begin
                    return x;
                end;

                begin
                    x := 10;
                    write(X());
                end.";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("10", result);
        }


        [TestMethod]
        public void CanFlagIllegalReturns()
        {
            // Arrange
            const string src = @"
                program Printing;

                begin
                    return 10;
                end.";
            var compiler = new Compiler();

            // Act and Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                compiler.Compile(src);
            });
        }

        [TestMethod]
        public void CanFlagFunctionsWithNoReturn()
        {
            // Arrange
            const string src = @"
                program Printing;

                function Oops(n: integer): integer;
                begin
                    n := n * 2;
                end;

                begin
                    write(Oops(10));
                end.";
            var compiler = new Compiler();

            // Act and Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                compiler.Compile(src);
            });
        }

        [TestMethod]
        public void CanFlagFunctionsWithNonreturningPaths()
        {
            // Arrange
            const string src = @"
                program Printing;

                var b : boolean;

                procedure Oops(n: integer);
                begin
                    if b then
                        write(n);
                    else
                        return;
                end;

                begin
                    b := false;
                    write(Oops(10));
                end.";
            var compiler = new Compiler();

            // Act and Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                compiler.Compile(src);
            });
        }

        [TestMethod]
        public void CanDoFibonacciTwoWays()
        {
            // Arrange
            const string src = @"
                program Printing;
                
                function FibFunc(n: integer): integer;
                    var a : integer;
                        b : integer;
                        c : integer;
                    begin
                        a := 0;
                        b := 1;
                        while n > 1 do begin
                            c := a + b;
                            a := b;
                            b := c;
                            n := n - 1;
                        end;

                        return c;
                    end;

                function FibRecurse(n: integer): integer;
                    begin
                        if n = 0 then 
                            return 0;
                        else if n = 1 then 
                            return 1;
                        else 
                            return FibRecurse(n-1) + FibRecurse(n-2); 
                    end;

                begin
                    write(FibFunc(10));
                    write(' ');
                    write(FibRecurse(10));
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("55 55", result);
        }


        [TestMethod]
        public void CanDoFibonacciThreeWays()
        {
            // Arrange
            const string src = @"
                program Printing;
                
                function FibFunc(n: integer): integer;
                    var a : integer;
                        b : integer;
                        c : integer;
                    begin
                        a := 0;
                        b := 1;
                        while n > 1 do begin
                            c := a + b;
                            a := b;
                            b := c;
                            n := n - 1;
                        end;

                        return c;
                    end;

                function FibRecurse(n: integer): integer;
                    begin
                        if n = 0 then 
                            return 0;
                        else if n = 1 then 
                            return 1;
                        else 
                            return FibRecurse(n-1) + FibRecurse(n-2); 
                    end;
                
                function FibAccumulate(n: integer; a: integer; b: integer): integer;
                    begin
                        if n = 1 then 
                            return b;
                        else 
                            return FibAccumulate(n - 1, b, a + b);
                    end;
                
                function FibAccumulate(n: integer): integer;
                    return FibAccumulate(n, 0, 1);

                begin
                    write(FibFunc(10));
                    write(' ');
                    write(FibRecurse(10));
                    write(' ');
                    write(FibAccumulate(10));
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("55 55 55", result);
        }

        [TestMethod]
        public void CanFoldConstants_StringTreeComparison()
        {
            // Arrange
            const string src = @"
                program Folding;
                
                begin
                    writeln(1 + 2.0 - 4 * 4 >= 3 / 3 + (4 - 3) ^ 5);
                    writeln('string' + ' ' + 'constants' + ' ' + 'are' + ' ' + 'folded!');
                end.
            ";
            const string control = @"
                program Folding;
                
                begin
                    writeln(false);
                    writeln('string constants are folded!');
                end.
            ";
            var compiler = new Compiler();
            var controlCompiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            Assert.IsTrue(controlCompiler.Compile(control));
            var stringTree = compiler.Ast.ToStringTree();
            var controlTree = controlCompiler.Ast.ToStringTree();
            // Assert
            Assert.AreEqual(controlTree, stringTree);
        }

        [TestMethod]
        public void CanFoldConstants_CilInspection()
        {
            // Arrange
            const string src = @"
                program Test;
                var
                    x: integer;
                begin
                    x := 2 * 3;
                    write(x);
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.IsFalse(compiler.Cil.Contains("mul"));
            Assert.AreEqual("6", result);
        }

        [TestMethod]
        public void CanPromoteRealFunctionReturnToString()
        {
            // Arrange
            const string src = @"
                program PromoteNonconstant;
                
                var s : string;

                function StringyThingy() : real;
                    return 1.1;

                begin
                    s := StringyThingy();
                    write(s);
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("1.1", result);
        }
    }
}
