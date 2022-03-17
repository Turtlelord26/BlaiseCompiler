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

            yield return new object[] { "Constant Fibonacci", @"
                program ConstantFib;

                var x : integer;
                    term : integer;
                    sqrt5 : real;
                    a : real;
                    b : real;

                begin
                    sqrt5 := 5 ^ 0.5;
                    a := (1 + sqrt5) / 2.0;
                    b := (1 - sqrt5) / 2.0;
                    writeln('Constant Fibonacci');
                    for x := 1 to 20 do
                    begin
                        term := (a ^ x - b ^ x) / sqrt5;
                        write('Term ');
                        write(x);
                        write(': ');
                        writeln(term);
                    end;
                end.
            ",
            @"Constant Fibonacci
Term 1: 1
Term 2: 1
Term 3: 2
Term 4: 3
Term 5: 5
Term 6: 8
Term 7: 13
Term 8: 21
Term 9: 34
Term 10: 55
Term 11: 89
Term 12: 144
Term 13: 233
Term 14: 377
Term 15: 610
Term 16: 987
Term 17: 1597
Term 18: 2584
Term 19: 4181" };

            yield return new object[] { "Can do basic logical ops", @"
                program Logic;
                begin
                    write( not 3 < 2 and 1 <> 2 );
                    write(' ');
                    write( false or 3.14 = 3.141592653589793238462643383279 and true );
                end.
            ",
            "True False" };

            yield return new object[] { "Procedure with uneven return flow", @"
                program UnevenReturnProcedure;
                
                var x : real;

                procedure DoTheThing();
                    begin
                        x := 1.5;
                        if (x > 1) then
                            write(1 + ' ');
                        else
                            return;
                        write('Got Here');
                        return;
                    end;
                
                DoTheThing().
                ",
                "1 Got Here" };

            yield return new object[] { "Procedure that does not end in a return", @"
                program NonreturningCodePath;
                
                var x : real;

                procedure DoTheThing();
                    begin
                        x := 1.5;
                        if (x > 1) then
                            write(1 + ' ');
                        else
                            return;
                        write('Got Here');
                    end;
                
                DoTheThing().
                ",
                "1 Got Here"};

            yield return new object[] { "Case Statement", @"
                program Cases;
                
                var x : integer;
                    s : string;
                
                function AString() : string;
                    return 'strawberry';
                
                function AnotherString() : string;
                    return 'apple';
                
                procedure Run(x: integer; s: string);
                begin
                    case (x) of
                        1: writeln(1);
                        2: writeln(143);
                        3: writeln(88);
                    else
                        writeln('None of the ints');
                    end;
                    case (s) of 
                        'banana': 
                            begin
                                writeln('yellow');
                                return;
                            end;
                        'mango': 
                            begin
                                writeln('orange');
                                return;
                            end;
                        'strawberry':
                            begin
                                writeln('red');
                                return;
                            end;
                    end;
                    writeln('Fell through fruit cases');
                end;

                begin
                    Run(1, AString());
                    Run(3, AnotherString());
                    Run(5, 'man' + 'go');
                    Run(0, 'banananana');
                end.
                ",
                @"1
red
88
Fell through fruit cases
None of the ints
orange
None of the ints
Fell through fruit cases"};
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            return data is not null ? $"CompilerCodeSamplesAttribute.{methodInfo.Name} ({data[0]})" : null;
        }
    }
}
