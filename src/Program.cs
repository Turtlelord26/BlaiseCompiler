using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
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
                    end;;
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
