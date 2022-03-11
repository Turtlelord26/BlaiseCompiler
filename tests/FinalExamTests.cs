//I have altered CanDoSimpleIfElseStat to expect "True" instead of "1", because I like supporting the Boolean CIL type.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [TestClass]
    public class FinalExamTests
    {
        [TestMethod]
        public void CanDoSimpleIfElseStat()
        {
            // Arrange
            const string src = @"
                program Test;

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
            Assert.AreEqual("True", result);//Assert.AreEqual("1", result);
        }

        [TestMethod]
        public void CanDoNestedIfElseStat()
        {
            // Arrange
            const string src = @"
                program Test;

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
        public void CanDoSimpleProcedure()
        {
            // Arrange
            const string src = @"
                program Test;

                procedure Ten();
                begin
                    write(10);
                end;

                begin
                    Ten();
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
        public void ProceduresCanReferenceGlobals()
        {
            // Arrange
            const string src = @"
                program Test;

                var x: integer;

                procedure Ten();
                begin
                    write(x);
                end;

                begin
                    x := 10;
                    Ten();
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
                program Test;

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
                program Test;

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
                program Test;

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
        public void CanFlagFunctionsWithNoReturnInConditionals()
        {
            // Arrange
            const string src = @"
                program Test;

                function Oops(n: integer): integer;
                begin
                    if n > 5 then
                        n := n * 2;
                    else
                        return n;
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
        public void CanDoConstantFolding()
        {
            // Arrange
            const string src = @"
                program Test;
                var
                    x: integer;
                begin
                    x := 2 * 3;
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));

            // Assert
            Assert.IsFalse(compiler.Cil.Contains("mul"));
        }

        [TestMethod]
        public void CanCoerceIntToString()
        {
            // Arrange
            const string src = @"
                program Test;
                var
                    x: string;
                begin
                    x := 12;
                    write(x);
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("12", result);
        }

        [TestMethod]
        public void CanPreventCoercionFromStringToInt()
        {
            // Arrange
            const string src = @"
                program Test;
                var
                    x: integer;
                begin
                    x := 'hello';
                    write(x);
                end.
            ";
            var compiler = new Compiler();

            // Act and Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                compiler.Compile(src);
            });
        }

        [TestMethod]
        public void CanCoerceDoubleToString()
        {
            // Arrange
            const string src = @"
                program Test;
                var
                    x: string;
                begin
                    x := 3.14;
                    write(x);
                end.
            ";
            var compiler = new Compiler();

            // Act
            Assert.IsTrue(compiler.Compile(src));
            compiler.AssembleToObjectCode();
            var result = compiler.ExecuteObjectCode();

            // Assert
            Assert.AreEqual("3.14", result);
        }

        [TestMethod]
        public void CanPreventCoercionFromStringToDouble()
        {
            // Arrange
            const string src = @"
                program Test;
                var
                    x: real;
                begin
                    x := 'hello';
                    write(x);
                end.
            ";
            var compiler = new Compiler();

            // Act and Assert
            Assert.ThrowsException<InvalidOperationException>(() =>
            {
                compiler.Compile(src);
            });
        }

        [TestMethod]
        public void CanDoFibonacciThreeWays()
        {
            // Arrange
            const string src = @"
                program Test;

                procedure FibProc(n: integer);
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
                        
                        write(c);
                    end;
                
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
                    FibProc(10);
                    write(' ');
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
            Assert.AreEqual("55 55 55", result);
        }
    }
}