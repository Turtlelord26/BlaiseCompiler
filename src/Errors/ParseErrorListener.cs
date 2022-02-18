using Antlr4.Runtime;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Blaise2.Errors
{
    public class ParseErrorListener : ConsoleErrorListener<IToken>
    {
        public bool HadError => Exceptions.Any();

        public List<ParseException> Exceptions { get; } = new();

        public override void SyntaxError(TextWriter output, IRecognizer recognizer, IToken offendingSymbol, int line,
            int col, string msg, RecognitionException e)
        {
            Exceptions.Add(new ParseException(msg, offendingSymbol.ToString(), line, col));
        }
    }
}