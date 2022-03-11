using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
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
                ");
            /*Try(@"
                program Logic;

                var x : integer;

                begin
                    write( ! 3 < 2 & 1 <> 2 );
                    write(' ');
                    write( false | 3.14 = 3.141592653589793238462643383279 & true );
                end.
            ");*/
        }

        static void Try(string input)
        {
            Console.WriteLine(input);

            var compiler = new Compiler();

            // set trace: true to turn on parse tracing
            var success = compiler.Compile(input, trace: false);

            if (!success)
            {
                compiler.WriteErrors();
                Environment.Exit(-1);
            }

            // Console.WriteLine($"  parse tree: {compiler.GetStringTree()}");

            Console.WriteLine(compiler.Cil);

            compiler.AssembleToObjectCode();

            var result = compiler.ExecuteObjectCode();
            Console.WriteLine($"Output is {result}");
        }
    }
}
