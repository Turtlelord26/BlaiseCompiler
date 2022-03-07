using System.Collections.Generic;

namespace Blaise2.Ast
{
    public class BlaiseKeywords
    {
        public static bool IsKeyword(string word) => keywords.Contains(word);

        private static HashSet<string> keywords = new()
        {
            "program",
            "var",
            "boolean",
            "char",
            "integer",
            "real",
            "string",
            "set",
            "array",
            "of",
            "begin",
            "end",
            "write",
            "writeln",
            "procedure",
            "function",
            "if",
            "then",
            "else",
            "while",
            "do",
            "for",
            "to",
            "downto",
            "repeat",
            "until",
            "case",
            "return",
            "true",
            "false",
            "and",
            "or",
            "not",
            "E",
            "e"
        };
    }
}