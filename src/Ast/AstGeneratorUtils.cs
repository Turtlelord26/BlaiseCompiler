using System;

namespace Blaise2.Ast
{
    public partial class AstGenerator : BlaiseBaseVisitor<AbstractAstNode>
    {
        private static BlaiseTypeEnum GetBasicBlaiseType(string t)
        {
            return t switch
            {
                "string" => BlaiseTypeEnum.STRING,
                "integer" => BlaiseTypeEnum.INTEGER,
                "real" => BlaiseTypeEnum.REAL,
                "boolean" => BlaiseTypeEnum.BOOLEAN,
                "char" => BlaiseTypeEnum.CHAR,
                "array" => BlaiseTypeEnum.ARRAY,
                "set" => BlaiseTypeEnum.SET,
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
                btype.BasicType = BlaiseTypeEnum.ARRAY;
                btype.StartIndex = int.Parse(ate.startIndex.Text);
                btype.EndIndex = int.Parse(ate.endIndex.Text);
                btype.ExtendedType = GetBasicBlaiseType(context.arrayTypeExpr().simpleTypeExpr().GetText());
            }
            else if (context.setTypeExpr() != null)
            {
                btype.BasicType = BlaiseTypeEnum.SET;
                btype.ExtendedType = GetBasicBlaiseType(context.setTypeExpr().simpleTypeExpr().GetText());
            }

            return btype;
        }
    }
}