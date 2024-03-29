﻿using System;

namespace Blaise2
{
    public class Program
    {
        static void Main(string[] args)
        {
            Try(@"
                program StringSwitch;
                
                var x : string;

                begin
                    x := 'four';
                    case (x) of
                        'one': write('aa');
                        'two': write('bb');
                        'three': write('cc');
                        'four': write('dd');
                        'five': write('ee');
                        'six': write('ff');
                        'seven': write('gg');
                    end;
                end.");
        }

        static void Try(string input)
        {
            Console.WriteLine(input);

            var compiler = new Compiler();

            // set trace: true to turn on parse tracing
            // set dot: true to turn on dot output for graphviz
            var success = compiler.Compile(input, trace: false, dot: true);

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
