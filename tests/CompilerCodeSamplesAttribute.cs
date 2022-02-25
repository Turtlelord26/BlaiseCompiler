using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Blaise2.Tests
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CompilerCodeSamplesAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            yield return new object[] { "Basic math and writeln", @"
                program Printing;

                var x : integer;

                begin
                    x := 12;
                    write( 4 * x + 3 );
                end.
            ",
            "51" };

            yield return new object[] { "Write strings", @"
                program Printing;
                begin
                    write( 'hello world' );
                end.
            ",
            "hello world" };

            yield return new object[] { "Write reals", @"
                program Printing;
                begin
                    write( 3.14159 );
                end.
            ",
            "3.14159" };
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return data is not null ? $"CompilerCodeSamplesAttribute.{methodInfo.Name} ({data[0]})" : null;
        }
    }
}
