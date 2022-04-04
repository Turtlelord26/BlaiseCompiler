using System;
using static Blaise2.Ast.ConstType;

namespace Blaise2.Ast
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
}