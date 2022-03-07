using System;
using System.Collections.Generic;
using static Blaise2.Ast.BlaiseOperator;
using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public partial class AstGenerator : BlaiseBaseVisitor<AbstractAstNode>
    {
        public static Dictionary<string, BlaiseOperator> OpMap = new()
        {
            { "^", Pow },
            { "*", Mul },
            { "/", Div },
            { "+", Add },
            { "-", Sub },
            { "<", Lt },
            { "<=", Lte },
            { ">", Gt },
            { ">=", Gte },
            { "=", Eq },
            { "<>", Ne },
            { "&", And },
            { "|", Or },
            { "!", Not }
        };

        private static BlaiseTypeEnum GetBasicBlaiseType(string t)
        {
            return t switch
            {
                "string" => STRING,
                "integer" => INTEGER,
                "real" => REAL,
                "boolean" => BOOLEAN,
                "char" => CHAR,
                "array" => ARRAY,
                "set" => SET,
                _ => throw new InvalidOperationException($"Unknown Blaise type {t}"),
            };
        }

        private static BlaiseType BuildBlaiseType(BlaiseParser.TypeExprContext context)
        {
            var btype = new BlaiseType();

            if (context.simpleTypeExpr() != null)
            {
                btype.BasicType = GetBasicBlaiseType(context.simpleTypeExpr().GetText());
            }
            else if (context.arrayTypeExpr() != null)
            {
                var ate = context.arrayTypeExpr();
                btype.BasicType = ARRAY;
                btype.StartIndex = int.Parse(ate.startIndex.Text);
                btype.EndIndex = int.Parse(ate.endIndex.Text);
                btype.ExtendedType = GetBasicBlaiseType(context.arrayTypeExpr().simpleTypeExpr().GetText());
            }
            else if (context.setTypeExpr() != null)
            {
                btype.BasicType = SET;
                btype.ExtendedType = GetBasicBlaiseType(context.setTypeExpr().simpleTypeExpr().GetText());
            }

            return btype;
        }
    }
}