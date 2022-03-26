using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
                program CharSwitch;
                
                var x : char;

                begin
                    x := 'a';
                    case (x) of
                        'a': write('a');
                        'c': write('c');
                        'D': write('D');
                        'Y': write('Y');
                    end;
                end.");
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

            Console.WriteLine(compiler.Cil);

            compiler.AssembleToObjectCode();

            var result = compiler.ExecuteObjectCode();
            Console.WriteLine($"Output is {result}");
        }
    }
}
