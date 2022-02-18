using System;
using Antlr4.Runtime;
using Blaise2.Ast;
using Blaise2.Emitters;
using Blaise2.Errors;

namespace Blaise2
{
    public partial class Compiler
    {
        private readonly ScanErrorListener scanErrorListener = new ScanErrorListener();
        private readonly ParseErrorListener parseErrorListener = new ParseErrorListener();

        private BlaiseParser parser;

        public ParserRuleContext ParseTree { get; private set; }

        public ProgramNode Ast { get; private set; }

        public string Cil { get; private set; }

        public bool Parse(string input, bool trace = false)
        {
            // Set up ANTLR's inputs
            var str = new AntlrInputStream(input);
            var lexer = new BlaiseLexer(str);
            var tokens = new CommonTokenStream(lexer);
            parser = new BlaiseParser(tokens);
            // parser.RemoveErrorListeners();      // remove the Console error listener
            parser.Trace = trace;
            lexer.AddErrorListener(scanErrorListener);
            parser.AddErrorListener(parseErrorListener);

            // Parse the input
            ParseTree = parser.file();
            var parseSuccess = !(scanErrorListener.HadError || parseErrorListener.HadError);
            return parseSuccess;
        }

        public bool Compile(string input, bool trace = false)
        {
            var parseSuccess = Parse(input, trace);

            if (parseSuccess)
            {
                Ast = new AstGenerator().Visit(ParseTree) as ProgramNode;
                if (Ast == null) { return false; }

                Cil = new CilEmitter().Visit(Ast);

                return true;
            }
            else
            {
                return false;
            }
        }

        public string GetStringTree()
        {
            return ParseTree?.ToStringTree(parser);
        }

        public void WriteErrors()
        {
            if (scanErrorListener.HadError)
            {
                Console.Error.WriteLine("Lexer errors:");
                foreach (var err in scanErrorListener.Exceptions)
                {
                    Console.Error.WriteLine($"* {err}");
                }
            }

            if (parseErrorListener.HadError)
            {
                Console.Error.WriteLine("Parser errors:");
                foreach (var err in parseErrorListener.Exceptions)
                {
                    Console.Error.WriteLine($"* {err}");
                }
            }
        }
    }
}