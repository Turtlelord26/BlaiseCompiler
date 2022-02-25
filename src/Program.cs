using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
                program Printing;

                var x : integer;
                    sqrt5 : real;
                    a : real;
                    b : real;

                begin
                    sqrt5 := 5 ^ 0.5;
                    a := (1 + sqrt5) / 2.0;
                    b := (1 - sqrt5) / 2.0;
                    x := 1;
                    while x < 10 do
                    begin
                        writeln((a ^ x - b ^ x) / sqrt5);
                        x := x + 1;
                    end;
                end.
            ");
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
