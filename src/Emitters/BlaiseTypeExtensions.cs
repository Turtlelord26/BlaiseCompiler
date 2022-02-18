using System;
using Blaise2.Ast;

namespace Blaise2.Emitters
{
    public static class BlaiseTypeExtensions
    {
        // Convert a BlaiseType to its equivalent CIL type
        public static string ToCilType(this BlaiseType bt)
        {
            return bt.BasicType switch
            {
                BlaiseTypeEnum.CHAR => "char",
                BlaiseTypeEnum.BOOLEAN => "int32",
                BlaiseTypeEnum.INTEGER => "int32",
                BlaiseTypeEnum.REAL => "double",
                BlaiseTypeEnum.STRING => "string",
                BlaiseTypeEnum.ARRAY => throw new NotImplementedException(),
                BlaiseTypeEnum.SET => throw new NotImplementedException(),
                _ => throw new InvalidOperationException($"unknown BlaiseType {bt.BasicType}"),
            };
        }
    }
}