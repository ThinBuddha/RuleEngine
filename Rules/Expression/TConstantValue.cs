using Rules.Model;
using System.Runtime.InteropServices;

namespace Rules.Expression
{
    public abstract class TConstantValue : TFactor
    {
        public abstract bool IsEqual(TConstantValue value);
        public abstract bool IsBigger(TConstantValue value);
        public abstract bool IsSmaller(TConstantValue value);
        public abstract bool IsBiggerOrEqual(TConstantValue value);
        public abstract bool IsSmallerOrEqual(TConstantValue value);
        public abstract bool IsUnequal(TConstantValue value);
        public abstract TConstantValue Add(TConstantValue constantValue);
        public abstract TConstantValue Subtract(TConstantValue constantValue);
        public abstract TConstantValue Multiply(TConstantValue value);
        public abstract TConstantValue Divide(TConstantValue value);
        public override string ToString()
        {
            return base.ToString();
        }

        public virtual TConstantValue Clone()
        {
            return null;
        }
    }

    public class TConstantNumValue : TConstantValue
    {
        public const byte IntType = 0;
        public const byte DoubleType = 1;
        public const byte BoolType = 2;

        public TNumValueStruct Value;

        // Source annotation used by explicit-model validation; execution remains numeric.
        public string UnitShortName { get; set; }


        public TConstantNumValue(int value)
        {
            Value.ValueType = IntType;
            Value.IntValue = value;
        }
        public TConstantNumValue(double value)
        {
            Value.ValueType = DoubleType;
            Value.DoubleValue = value;
        }
        public TConstantNumValue(bool value)
        {
            Value.ValueType = BoolType;
            Value.BoolValue = value;
        }
        public override TConstantValue Clone()
        {
            switch (Value.ValueType)
            {
                case IntType: return new TConstantNumValue(Value.IntValue);
                case DoubleType: return new TConstantNumValue(Value.DoubleValue);
                case BoolType: return new TConstantNumValue(Value.BoolValue);
                default: throw new TExprException("Unknown value type");
            }
        }

        public override string ToString()
        {
            var text = Value.ValueType switch
            {
                IntType => Value.IntValue.ToString(),
                DoubleType => Value.DoubleValue.ToString(),
                BoolType => Value.BoolValue.ToString(),
                _ => throw new TExprException("Unknown value type")
            };
            return UnitShortName == null ? text : $"{text} {UnitShortName}";
        }

        public override void ApplyMinusSign()
        {
            switch (Value.ValueType)
            {
                case IntType: Value.IntValue *= -1; break;
                case DoubleType: Value.DoubleValue *= -1; break;
                default: throw new TExprException("Unknown or not applicable value type");
            }
        }

        public override TConstantValue Evaluate(ValueBag context)
        {
            return Clone();
        }

        public override bool IsEqual(TConstantValue rightResult)
        {
            ValidateNumTypeConsistency(rightResult);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, (TConstantNumValue)rightResult);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return safeThisValue.Value.IntValue == safeThatValue.Value.IntValue;
                case DoubleType: return safeThisValue.Value.DoubleValue == safeThatValue.Value.DoubleValue;
                case BoolType: throw new TExprException("Cannot compare boolean values like that");
                default: throw new TExprException("Unknown value type");
            }
        }

        public override bool IsBigger(TConstantValue rightResult)
        {
            ValidateNumTypeConsistency(rightResult);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, (TConstantNumValue)rightResult);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return safeThisValue.Value.IntValue > safeThatValue.Value.IntValue;
                case DoubleType: return safeThisValue.Value.DoubleValue > safeThatValue.Value.DoubleValue;
                case BoolType: throw new TExprException("Cannot compare boolean values like that");
                default: throw new TExprException("Unknown value type");
            }
        }

        public override bool IsSmaller(TConstantValue rightResult)
        {
            ValidateNumTypeConsistency(rightResult);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, (TConstantNumValue)rightResult);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return safeThisValue.Value.IntValue < safeThatValue.Value.IntValue;
                case DoubleType: return safeThisValue.Value.DoubleValue < safeThatValue.Value.DoubleValue;
                case BoolType: throw new TExprException("Cannot compare boolean values like that");
                default: throw new TExprException("Unknown value type");
            }
        }

        public override bool IsBiggerOrEqual(TConstantValue rightResult)
        {
            return IsBigger(rightResult) || IsEqual(rightResult);
        }

        public override bool IsSmallerOrEqual(TConstantValue rightResult)
        {
            return IsSmaller(rightResult) || IsEqual(rightResult);
        }

        public override bool IsUnequal(TConstantValue rightResult)
        {
            return !IsEqual(rightResult);
        }

        public override TConstantValue Add(TConstantValue value)
        {
            ValidateNumTypeConsistency(value);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, value as TConstantNumValue);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return new TConstantNumValue(safeThisValue.Value.IntValue + safeThatValue.Value.IntValue);
                case DoubleType: return new TConstantNumValue(safeThisValue.Value.DoubleValue + safeThatValue.Value.DoubleValue);
                case BoolType: throw new TExprException("Cannot add boolean values");
                default: throw new TExprException("Unknown value type");
            }
        }

        public override TConstantValue Subtract(TConstantValue value)
        {
            ValidateNumTypeConsistency(value);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, value as TConstantNumValue);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return new TConstantNumValue(safeThisValue.Value.IntValue - safeThatValue.Value.IntValue);
                case DoubleType: return new TConstantNumValue(safeThisValue.Value.DoubleValue - safeThatValue.Value.DoubleValue);
                case BoolType: throw new TExprException("Cannot subtract boolean values");
                default: throw new TExprException("Unknown value type");
            }
        }

        public override TConstantValue Multiply(TConstantValue value)
        {
            ValidateNumTypeConsistency(value);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, value as TConstantNumValue);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return new TConstantNumValue(safeThisValue.Value.IntValue * safeThatValue.Value.IntValue);
                case DoubleType: return new TConstantNumValue(safeThisValue.Value.DoubleValue * safeThatValue.Value.DoubleValue);
                case BoolType: throw new TExprException("Cannot multiply boolean values");
                default: throw new TExprException("Unknown value type");
            }
        }

        public override TConstantValue Divide(TConstantValue value)
        {
            ValidateNumTypeConsistency(value);

            (TConstantNumValue safeThisValue, TConstantNumValue safeThatValue) = GetSafeTypeValues(this, value as TConstantNumValue);
            switch (safeThisValue.Value.ValueType)
            {
                case IntType: return new TConstantNumValue(safeThisValue.Value.IntValue / safeThatValue.Value.IntValue);
                case DoubleType: return new TConstantNumValue(safeThisValue.Value.DoubleValue / safeThatValue.Value.DoubleValue);
                case BoolType: throw new TExprException("Cannot devide boolean values");
                default: throw new TExprException("Unknown value type");
            }
        }

        private void ValidateNumTypeConsistency(TConstantValue rightResult)
        {
            if (!(rightResult is TConstantNumValue))
            {
                throw new TExprException($"Cannot compare values {this} and {rightResult}, they are of different types");
            }
        }

        /// <summary>
        /// Convert both values to the dominant type
        /// </summary>
        /// <remarks>
        /// Without safe type conversons, behaviour would be unexpected:
        /// E.g. 2 * 0.5 = 0, 0.5 * 2 = 5E-324, while only 2.0 * 0.5 = 1.0
        /// With converted values we get 2 * 0.5 = 1.0, 0.5 * 2 = 1.0 and 2.0 * 0.5 = 1.0
        /// We can also be rest asured that 1 / 2 = 0, 3 / 2 = 1 while 3 / 2.0 = 1.5 and 3.0 / 2.0 = 1.5, etc.
        /// </remarks>
        /// <param name="thisValue">one of two <see cref="TConstantNumValue"/></param>
        /// <param name="thatValue">the oher of two <see cref="TConstantNumValue"/></param>
        /// <returns></returns>
        private (TConstantNumValue, TConstantNumValue) GetSafeTypeValues(TConstantNumValue thisValue, TConstantNumValue thatValue)
        {
            switch (thisValue.Value.ValueType)
            {
                case IntType:
                    {
                        switch (thatValue.Value.ValueType)
                        {
                            case IntType: return (thisValue, thatValue);
                            case DoubleType: return (new TConstantNumValue((double)thisValue.Value.IntValue), thatValue);
                            case BoolType: throw new TExprException("Cannot compare numerical type with boolean type");
                            default: throw new TExprException("Unknown value type");
                        }
                    }
                case DoubleType:
                    {
                        switch (thatValue.Value.ValueType)
                        {
                            case IntType: return (thisValue, new TConstantNumValue((double)thatValue.Value.IntValue));
                            case DoubleType: return (thisValue, thatValue);
                            case BoolType: throw new TExprException("Cannot compare numerical type with boolean type");
                            default: throw new TExprException("Unknown value type");
                        }
                    }
                case BoolType:
                    switch (thatValue.Value.ValueType)
                    {
                        case IntType: throw new TExprException("Cannot compare numerical type with boolean type");
                        case DoubleType: throw new TExprException("Cannot compare numerical type with boolean type");
                        case BoolType: return (thisValue, thatValue);
                        default: throw new TExprException("Unknown value type");
                    }
                default:
                    {
                        throw new TExprException("Unknown value type");
                    }
            }
        }
    }

    public class TConstantStringValue : TConstantValue
    {
        public TConstantStringValue(string value)
        {
            Value = value;
        }

        public string Value { get; set; }
        public override string ToString()
        {
            return $"\"{Value}\"";
        }

        public override TConstantValue Evaluate(ValueBag context)
        {
            return new TConstantStringValue(Value);
        }

        public override bool IsEqual(TConstantValue value)
        {
            TConstantStringValue strValue = value as TConstantStringValue;
            return Value == strValue.Value;
        }
        public override bool IsBigger(TConstantValue value)
        {
            TConstantStringValue strValue = value as TConstantStringValue;
            return Size(Value) > Size(strValue.Value);
        }

        public override bool IsSmaller(TConstantValue value)
        {
            TConstantStringValue strValue = value as TConstantStringValue;
            return Size(Value) < Size(strValue.Value);
        }
        public override bool IsBiggerOrEqual(TConstantValue value)
        {
            TConstantStringValue strValue = value as TConstantStringValue;
            return Size(Value) >= Size(strValue.Value);
        }
        public override bool IsSmallerOrEqual(TConstantValue value)
        {
            TConstantStringValue strValue = value as TConstantStringValue;
            return Size(Value) <= Size(strValue.Value);
        }
        public override bool IsUnequal(TConstantValue value)
        {
            TConstantStringValue strValue = value as TConstantStringValue;
            return Value != strValue.Value;
        }
        public override TConstantValue Add(TConstantValue constantValue)
        {
            // "noten" + "boom" = "notenboom"
            return new TConstantStringValue(
                Value + (constantValue as TConstantStringValue).Value
            );
        }
        public override TConstantValue Subtract(TConstantValue constantValue)
        {
            // "notenboom" - "boom" = "noten"
            string v = (constantValue as TConstantStringValue).Value;
            int i = Value.IndexOf(v);
            return new TConstantStringValue(i > -1 ? Value.Substring(0, i) : Value);
        }
        public override TConstantValue Multiply(TConstantValue value)
        {
            // TODO: "abcdbcdf" * "b" = abbbcbdbbbcbdbfb"
            throw new TExprException("Cannot multiply strings");
        }
        public override TConstantValue Divide(TConstantValue value)
        {
            // TODO: "abcdbcdf" / "b" = { "a", "cd", "cdf" }  
            throw new TExprException("Cannot devide strings");
        }

        public override TConstantValue Clone()
        {
            return new TConstantStringValue(Value);
        }

        private int Size(string value)
        {
            int result = 0;
            foreach (char c in value)
            {
                result += c;
            }
            return result;
        }
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct TNumValueStruct
    {
        [FieldOffset(0)] public byte ValueType;
        [FieldOffset(1)] public int IntValue;
        [FieldOffset(1)] public double DoubleValue;
        [FieldOffset(1)] public bool BoolValue;
    }

}
