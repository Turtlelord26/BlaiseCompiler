using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    public class SourceCodeSamplesAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            yield return new object[] { "hello, world!", @"
                program HelloWorld;
                writeln('Hello, world!').
            " };

            yield return new object[] { "Degenerate program", @"
                program Hello;
                begin
                end.
            " };

            yield return new object[] { "Degenerate program with 1 variable", @"
                program Hello;
                var i: integer;
                begin
                end.
            " };

            yield return new object[] { "var declarations", @"
                program Vars;
                var
                    a: integer;
                    b: real;
                    c: array[1..4] of integer;
                    d: string;
                    e: char;
                    f: set of real;
                begin end.
            " };

            yield return new object[] { "degenerate procedure", @"
                program Procs;
                procedure Foo(); begin end;
                begin end.
            " };

            yield return new object[] { "degenerate procedure with 1 param", @"
                program Procs;

                procedure Foo(j: integer); 
                begin 
                end;

                begin 
                end.
            " };

            yield return new object[] { "degenerate procedure with 2 params", @"
                program Procs;

                procedure Foo(i: integer; j: integer);
                begin 
                end;

                begin 
                end.
            " };

            yield return new object[] { "degenerate procedure with 2 params and 2 vars", @"
                program Procs;

                procedure Foo(i: integer; j: integer);
                var
                    i: integer;
                    r: real;
                begin 
                end;

                begin 
                end.
            " };

            yield return new object[] { "degenerate function", @"
                program Procs;
                function Foo(): real; begin end;
                begin end.
            " };

            yield return new object[] { "degenerate function with 2 params", @"
                program Procs;

                function Foo(i: integer; j: integer): real;
                begin 
                end;

                begin 
                end.
            " };

            yield return new object[] { "degenerate function with 2 params and 2 vars", @"
                program Procs;

                function Foo(i: integer; j: integer): real;
                var
                    i: integer;
                    r: real;
                begin 
                end;

                begin 
                end.
            " };

            yield return new object[] { "comments", @"
                program Comments;
                begin { main part }
                end (* end of program *).
            " };

            yield return new object[] { "real world example", @"
                program Printing;

                var i : integer;

                procedure PrintAnInteger(j : integer);
                begin
                    writeln(j);
                end;

                function triple(x: integer): integer;
                begin
                    triple := x * 3;
                end;

                begin
                    { ... }
                    PrintAnInteger(i);
                    PrintAnInteger(triple(i));
                end.
            " };
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return data is not null ? $"SourceCodeSamples.{methodInfo.Name} ({data[0]})" : null;
        }
    }
}