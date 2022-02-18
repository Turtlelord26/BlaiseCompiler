using System;

namespace Blaise2.Errors
{
    public class ParseException : Exception
    {
        public ParseException(string msg, string offendingSymbol, int line, int col) : base($"{msg} - offending symbol \"{offendingSymbol}\" ({line}:{col})") { }
    }
}