using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
                program Folding;
                
                begin
                    writeln(1 + 2.0 - 4 * 4 >= 3 / 3 + (4 - 3) ^ 5);
                    writeln('string' + ' ' + 'constants' + ' ' + 'are' + ' ' + 'folded!');
                end.
            ");
            /*Try(@"
                program Logic;

                var x : integer;

                begin
                    write( not 3 < 2 and 1 <> 2 );
                    write(' ');
                    write( false or 3.14 = 3.141592653589793238462643383279 and true );
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
