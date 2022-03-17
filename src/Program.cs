using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
            program DegenerateIf;

            begin
                if (true) 
                then 
                    write(3); 
                else 
                    write(4);
                if (false)
                then
                    write(7);
                else
                    write(0);
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
