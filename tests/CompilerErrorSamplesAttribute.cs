using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CompilerErrorSamplesAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            yield return new object[] { "Basic math and writeln", @"
                program Printing;

                var a : integer;

                begin
                    { x does not exist }
                    x := 12;
                    writeln( 4 * x + 3 );
                end.
            ",
            typeof(InvalidOperationException) };

            yield return new object[] { "If condition expression typecheck", @"
                program IfCondition;
                
                var x : real;
                
                if (x + 1) then
                    x := x + 1.
                ",
                typeof(InvalidOperationException) };

            yield return new object[] { "Loop condition expression typecheck", @"
                program UntilCondition;
                
                var x : real;
                
                begin
                    x := 1.5;
                    repeat
                        x := x + 1;
                        write(x);
                    until (x + 1.5);
                end.
                ",
                typeof(InvalidOperationException) };
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return data is not null ? $"CompilerCodeSamplesAttribute.{methodInfo.Name} ({data[0]})" : null;
        }
    }
}