using static Blaise2.Ast.BlaiseTypeEnum;

namespace Blaise2.Ast
{
    public class BlaiseType
    {
        public static readonly BlaiseType ErrorType = new() { };

        public BlaiseTypeEnum BasicType { get; set; }
        public BlaiseTypeEnum ExtendedType { get; set; }

        public int StartIndex { get; set; }
        public int EndIndex { get; set; }

        public bool IsExtended => BasicType is BlaiseTypeEnum.ARRAY or BlaiseTypeEnum.SET;

        public bool IsValid() => this != ErrorType;

        public BlaiseType DeepCopy() => new()
        {
            BasicType = this.BasicType,
            ExtendedType = this.ExtendedType,
            StartIndex = this.StartIndex,
            EndIndex = this.EndIndex
        };

        public override string ToString()
        {
            return BasicType switch
            {
                BlaiseTypeEnum.ARRAY => $"array[{StartIndex}..{EndIndex}] of {ExtendedType.ToString().ToLowerInvariant()}",
                BlaiseTypeEnum.SET => $"set of {ExtendedType.ToString().ToLowerInvariant()}",
                _ => BasicType.ToString().ToLowerInvariant(),
            };
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null || !(obj is BlaiseType))
            {
                return false;
            }
            var bt = (BlaiseType)obj;
            if (BasicType == ARRAY)
            {
                return bt.BasicType == ARRAY
                    & ExtendedType == bt.ExtendedType
                    & StartIndex == bt.StartIndex
                    & EndIndex == bt.EndIndex;
            }
            else if (BasicType == SET)
            {
                return bt.BasicType == ARRAY
                    & ExtendedType == bt.ExtendedType;
            }
            else
            {
                return BasicType == bt.BasicType;
            }
        }

        public override int GetHashCode()
        {
            int p = 31;
            int hash = BasicType.GetHashCode();
            if (BasicType == SET)
            {
                hash += p * ExtendedType.GetHashCode();
            }
            else if (BasicType == ARRAY)
            {
                hash += p * ExtendedType.GetHashCode()
                        + p * p * StartIndex
                        + p * p * p * EndIndex;
            }
            return hash;

        }
    }
}
