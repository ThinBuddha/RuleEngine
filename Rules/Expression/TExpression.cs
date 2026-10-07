using Rules.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;

/// <summary>
/// To prevent name clashes with Linq expression classes, all classes in this unit are prefixed with 'T'
/// </summary>
namespace Rules.Expression
{
    public enum TCompareOperator { Equal, Unequal, Bigger, Smaller, BiggerOrEqual, SmallerOrEqual };
    public enum TPlusMinOperator { Plus, Minus };
    public enum TMulDivOperator { Multiply, Divide };

    public class TCompareExpression
    {
        public TExpression LeftExpr { get; private set; }
        public TExpression RightExpr { get; private set; }
        public TCompareOperator Operator { get; set; } = TCompareOperator.Equal;
        public void AddExpr(TExpression expr)
        {
            if (LeftExpr == null)
            {
                LeftExpr = expr;
            }
            else
            {
                RightExpr = expr;
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(LeftExpr.ToString());

            if (RightExpr != null)
            {
                result.Append(" ");
                result.Append(OperatorToString(Operator));
                result.Append(" ");
                result.Append(RightExpr.ToString());
            }

            return result.ToString();
        }

        private string OperatorToString(TCompareOperator op)
        {
            return op switch
            {
                TCompareOperator.Equal => "=",
                TCompareOperator.Unequal => "<>",
                TCompareOperator.Bigger => ">",
                TCompareOperator.Smaller => "<",
                TCompareOperator.BiggerOrEqual => ">=",
                TCompareOperator.SmallerOrEqual => "<=",
                _ => throw new NotImplementedException(),
            };
        }

        public bool Evaluate(ValueBag context)
        {
            TConstantValue result = LeftExpr.Evaluate(context);
            if (RightExpr != null)
            {
                return ApplyOperator(result, context);
            }
            else
            {
                // Id as singular boolean expression
                if (!(result is TConstantNumValue)) throw new TExprException("result is not of class TConstantNumValue");
                return (result as TConstantNumValue).Value.BoolValue;
            }
        }

        public void AddOperator(string op)
        {
            switch (op)
            {
                case "=": Operator = TCompareOperator.Equal; break;
                case "<>": Operator = TCompareOperator.Unequal; break;
                case ">": Operator = TCompareOperator.Bigger; break;
                case "<": Operator = TCompareOperator.Smaller; break;
                case ">=": Operator = TCompareOperator.BiggerOrEqual; break;
                case "<=": Operator = TCompareOperator.SmallerOrEqual; break;
                default: throw new NotImplementedException();
            }
        }

        public override bool Equals(object expression)
        {
            // TODO: override GetHashCode() instead
            return this == expression || ToString() == (expression as TCompareExpression).ToString(); 
        }

        private bool ApplyOperator(TConstantValue leftResult, ValueBag context)
        {
            TConstantValue rightResult = RightExpr.Evaluate(context);
            return Operator switch
            {
                TCompareOperator.Equal => leftResult.IsEqual(rightResult),
                TCompareOperator.Unequal => leftResult.IsUnequal(rightResult),
                TCompareOperator.Bigger => leftResult.IsBigger(rightResult),
                TCompareOperator.Smaller => leftResult.IsSmaller(rightResult),
                TCompareOperator.BiggerOrEqual => leftResult.IsBiggerOrEqual(rightResult),
                TCompareOperator.SmallerOrEqual => leftResult.IsSmallerOrEqual(rightResult),
                _ => throw new NotImplementedException(),
            };
        }
    }

    public class TAssignExpression
    {
        public string LeftExpr { get; private set; }
        public TExpression RightExpr { get; private set; }

        public void SetLeftExpr(string id)
        {
            LeftExpr = id;
        }
        public void SetRightExpr(TExpression expr)
        {
            RightExpr = expr;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            result.Append(LeftExpr.ToString());

            if (RightExpr != null)
            {
                result.Append(" := ");
                result.Append(RightExpr.ToString());
            }
            return result.ToString();
        }

        public void Evaluate(ValueBag context)
        {
            if (RightExpr == null)
            {
                context.AddOrUpdateValue(LeftExpr, new TConstantNumValue(true));
            }
            else
            {
                TConstantValue result = RightExpr.Evaluate(context);
                context.AddOrUpdateValue(LeftExpr, result);
            }
        }
    }

    public class TExpression : TFactor
    {
        public List<TTerm> Terms { get; } = new List<TTerm>();
        public List<TPlusMinOperator> Operators { get; } = new List<TPlusMinOperator>();

        public void AddTerm(TTerm term)
        {
            Terms.Add(term);
        }

        public void AddOperator(string value)
        {
            switch (value)
            {
                case "+": Operators.Add(TPlusMinOperator.Plus); break;
                case "-": Operators.Add(TPlusMinOperator.Minus); break;
                default:  throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            if (Operators.Count > 0) result.Append("(");
            for (int i = 0; i < Terms.Count; i++)
            {
                TTerm term = Terms[i];
                result.Append(term.ToString());
                if (i < Operators.Count)
                {
                    result.Append(" ");
                    result.Append(OperatorToString(Operators[i]));
                    result.Append(" ");
                }
            }
            if (Operators.Count > 0) result.Append(")");
            return result.ToString();
        }

        private string OperatorToString(TPlusMinOperator op)
        {
            return op switch
            {
                TPlusMinOperator.Plus => "+",
                TPlusMinOperator.Minus => "-",
                _ => throw new NotImplementedException(),
            };
        }

        public override TConstantValue Evaluate(ValueBag context)
        {
            Debug.Assert(Terms.Count > Operators.Count);
            int i = 0;
            int j = 0;
            TTerm term = Terms[i++];
            TConstantValue result = term.Evaluate(context);
            while (i < Terms.Count)
            {
                if (j < Operators.Count)
                {
                    result = ApplyOperator(result, Terms[i++], Operators[j], context);
                    j++;
                }
            }
            return result;
        }

        private TConstantValue ApplyOperator(TConstantValue leftResult, TTerm term, TPlusMinOperator op, ValueBag context)
        {
            switch (op)
            {
                case TPlusMinOperator.Plus: return leftResult.Add(term.Evaluate(context));
                case TPlusMinOperator.Minus: return leftResult.Subtract(term.Evaluate(context));
                default: throw new NotImplementedException();
            }
        }
    }

    public class TTerm
    {
        public List<TFactor> Factors { get; } = new List<TFactor>();
        public List<TMulDivOperator> Operators { get; } = new List<TMulDivOperator>();

        public void AddFactor(TFactor factor)
        {
            Factors.Add(factor);
        }

        public void SetOperator(string value)
        {
            switch (value)
            {
                case "*": Operators.Add(TMulDivOperator.Multiply); break;
                case "/": Operators.Add(TMulDivOperator.Divide); break;
                default:  throw new NotImplementedException();
            }
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();

            if (Operators.Count > 0) result.Append("(");
            for (int i = 0; i < Factors.Count; i++)
            {
                TFactor factor = Factors[i];
                result.Append(factor.ToString());
                if (i < Operators.Count)
                {
                    result.Append(" ");
                    result.Append(OperatorToString(Operators[i]));
                    result.Append(" ");
                }
            }
            if (Operators.Count > 0) result.Append(")");
            return result.ToString();
        }

        public TConstantValue Evaluate(ValueBag context)
        {
            Debug.Assert(Factors.Count > Operators.Count);
            int i = 0;
            int j = 0;
            TFactor factor = Factors[i++];
            TConstantValue result = factor.Evaluate(context);
            while (i < Factors.Count)
            {
                if (j < Operators.Count)
                {
                    result = ApplyOperator(result, Factors[i++], Operators[j], context);
                    j++;
                }
            }
            return result;
        }

        private TConstantValue ApplyOperator(TConstantValue leftResult, TFactor factor, TMulDivOperator op, ValueBag context)
        {
            switch (op)
            {
                case TMulDivOperator.Multiply: return leftResult.Multiply(factor.Evaluate(context) as TConstantNumValue);
                case TMulDivOperator.Divide: return leftResult.Divide(factor.Evaluate(context) as TConstantNumValue);
                default: throw new NotImplementedException();
            }
        }

        private string OperatorToString(TMulDivOperator op)
        {
            switch (op)
            {
                case TMulDivOperator.Multiply: return "*";
                case TMulDivOperator.Divide: return "/";
                default: throw new NotImplementedException();
            }
        }
    }

    public class TFactor
    {
        public virtual TConstantValue Evaluate(ValueBag context)
        {
            return null;
        }

        public virtual void ApplyMinusSign()
        {
            throw new TExprException($"Cannot apply minus sign to a class of type {GetType()}");
        }
    }

    public class TId : TFactor
    {
        public string Name { get; set; }
        public bool ShouldBeNegated { get; set; }
        public bool ShouldBeMinused { get; internal set; }

        public override bool Equals(object id)
        {
            // TODO: override GetHashCode() instead
            return this == id || ToString() == (id as TId).ToString();
        }

        public override TConstantValue Evaluate(ValueBag context)
        {
            TConstantValue result;
            if (!context.TryGetValue(Name, out TConstantValue currentValue))
            {
                // There's no value known yet, we must derive it
                // It's outside our scope, so we delegate it and wait for an answer
                currentValue = context.MessageSink.NotifyUnknownValue?.Invoke(this, context);
                if (currentValue == null) throw new TExprException($"Cannot continue, getting value by notification failed for Id {Name}", this);

                // Update the context with the now known value
                context.AddOrUpdateValue(Name, currentValue);
            };

            // Make sure the returning value is cloned, since of course we cannot continue evaluating with the value in the context
            result = currentValue.Clone();

            // Apply "not" or "-" operator if applicable
            if (result is TConstantNumValue castedValue)
            {
                if (ShouldBeNegated && castedValue.Value.ValueType == TConstantNumValue.BoolType)
                {
                    castedValue.Value.BoolValue = !castedValue.Value.BoolValue;
                }
                else if (ShouldBeMinused && castedValue.Value.ValueType != TConstantNumValue.BoolType)
                {
                    castedValue.Value.IntValue = -castedValue.Value.IntValue;
                }
            }
            return result;
        }

        public override void ApplyMinusSign()
        {
            ShouldBeMinused = true;
        }

        public override string ToString()
        {
            StringBuilder result = new StringBuilder();
            if (ShouldBeNegated) result.Append("Not( ");
            result.Append(Name);
            if (ShouldBeNegated) result.Append(" )");
            return result.ToString();
        }
    }

    
    [Serializable]
    public class TExprException : Exception
    {
        public TId Id { get; set; }

        public TExprException()
        {
        }

        public TExprException(string message) : base(message)
        {
        }

        public TExprException(string message, TId id) : base(message)
        {
            Id = id;
        }

        public TExprException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected TExprException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }

        public bool CouldNotDetermineIdValue()
        {
            return Id != null;
        }

        public bool CouldNotDetermineRootIdValue(TCompareExpression query)
        {
            return CouldNotDetermineIdValue() && Id.Equals(query.LeftExpr.Terms[0].Factors[0] as TId);
        }
    }
}
