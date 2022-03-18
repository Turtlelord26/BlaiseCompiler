using System;
using System.Collections.Generic;
using static Blaise2.ConstType;

namespace Blaise2
{
    public class AstConstant
    {
        public bool BoolValue { get; init; }
        public char CharValue { get; init; }
        public int IntValue { get; init; }
        public double RealValue { get; init; }
        public string StringValue { get; init; }
        public ConstType ConstType { get; init; }

        public AstConstant(bool boolValue)
        {
            BoolValue = boolValue;
            ConstType = BoolConst;
        }

        public AstConstant(char charValue)
        {
            CharValue = charValue;
            ConstType = CharConst;
        }

        public AstConstant(int intValue)
        {
            IntValue = intValue;
            ConstType = IntConst;
        }

        public AstConstant(double realValue)
        {
            RealValue = realValue;
            ConstType = RealConst;
        }

        public AstConstant(string stringValue)
        {
            StringValue = stringValue;
            ConstType = StringConst;
        }

        public bool GetValueAsBool() => ConstType switch
        {
            BoolConst => BoolValue,
            _ => throw new InvalidOperationException($"Cannot convert to bool constant from non-bool.")
        };

        public char GetValueAsChar() => ConstType switch
        {
            CharConst => CharValue,
            IntConst => (char)IntValue,
            RealConst => (char)RealValue,
            StringConst => char.Parse(StringValue),
            BoolConst => throw new InvalidOperationException($"Cannot convert from bool constant to non-bool."),
            _ => throw new InvalidOperationException($"Encountered unrecognized AstConstant ConstType.")
        };

        public int GetValueAsInt() => ConstType switch
        {
            CharConst => (int)CharValue,
            IntConst => (char)IntValue,
            RealConst => (int)RealValue,
            StringConst => int.Parse(StringValue),
            BoolConst => throw new InvalidOperationException($"Cannot convert from bool constant to non-bool."),
            _ => throw new InvalidOperationException($"Encountered unrecognized AstConstant ConstType.")
        };

        public double GetValueAsReal() => ConstType switch
        {
            CharConst => (double)CharValue,
            IntConst => (double)IntValue,
            RealConst => RealValue,
            StringConst => double.Parse(StringValue),
            BoolConst => throw new InvalidOperationException($"Cannot convert from bool constant to non-bool."),
            _ => throw new InvalidOperationException($"Encountered unrecognized AstConstant ConstType.")
        };

        public string GetValueAsString() => ConstType switch
        {
            CharConst => CharValue.ToString(),
            IntConst => IntValue.ToString(),
            RealConst => RealValue.ToString(),
            StringConst => StringValue,
            BoolConst => throw new InvalidOperationException($"Cannot convert from bool constant to non-bool."),
            _ => throw new InvalidOperationException($"Encountered unrecognized AstConstant ConstType.")
        };
    }

    public class AstConstantComparer : IComparer<AstConstant>
    {
        public int Compare(AstConstant a, AstConstant b) => (a.ConstType, b.ConstType) switch
        {
            (CharConst, CharConst) => a.CharValue.CompareTo(b.CharValue),
            (IntConst, CharConst) => a.IntValue.CompareTo(b.CharValue),
            (RealConst, CharConst) => a.RealValue.CompareTo(b.CharValue),
            (StringConst, CharConst) => a.StringValue.CompareTo(b.CharValue.ToString()),
            (CharConst, IntConst) => a.CharValue.CompareTo(b.IntValue),
            (IntConst, IntConst) => a.IntValue.CompareTo(b.IntValue),
            (RealConst, IntConst) => a.RealValue.CompareTo(b.IntValue),
            (StringConst, IntConst) => a.StringValue.CompareTo(b.IntValue.ToString()),
            (CharConst, RealConst) => a.CharValue.CompareTo(b.RealValue),
            (IntConst, RealConst) => a.IntValue.CompareTo(b.RealValue),
            (RealConst, RealConst) => a.RealValue.CompareTo(b.RealValue),
            (StringConst, RealConst) => a.StringValue.CompareTo(b.RealValue.ToString()),
            (CharConst, StringConst) => a.CharValue.ToString().CompareTo(b.StringValue),
            (IntConst, StringConst) => a.IntValue.ToString().CompareTo(b.StringValue),
            (RealConst, StringConst) => a.RealValue.ToString().CompareTo(b.StringValue),
            (StringConst, StringConst) => a.StringValue.CompareTo(b.StringValue),
            (BoolConst, _)
            or (_, BoolConst) => throw new InvalidOperationException($"Cannot compare bools to other constants"),
            _ => throw new InvalidOperationException($"Encountered unrecognized AstConstant contents.")
        };
    }

    public enum ConstType
    {
        BoolConst,
        CharConst,
        IntConst,
        RealConst,
        StringConst
    }
}