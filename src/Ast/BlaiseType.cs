namespace Blaise2.Ast
{
    public class BlaiseType
    {
        public BlaiseTypeEnum BasicType { get; set; }
        public BlaiseTypeEnum ExtendedType { get; set; }

        public int StartIndex { get; set; }
        public int EndIndex { get; set; }

        public bool IsExtended => BasicType is BlaiseTypeEnum.ARRAY or BlaiseTypeEnum.SET;

        public override string ToString()
        {
            return BasicType switch
            {
                BlaiseTypeEnum.ARRAY => $"array[{StartIndex}..{EndIndex}] of {ExtendedType.ToString().ToLowerInvariant()}",
                BlaiseTypeEnum.SET => $"set of {ExtendedType.ToString().ToLowerInvariant()}",
                _ => BasicType.ToString().ToLowerInvariant(),
            };
        }
    }
}
