using Rules.Expression;
using Rules.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rules.Model
{
    /// <summary>
    /// Generic parameter to pass on to the rules Evaluate() method, giving the rules a context to work on
    /// </summary>
    /// <remarks>
    /// A <see cref="RuleContainer"/> can supply a <see cref="ValueBag"/> "model" to a rule when there is no model explicitly defined.
    /// Score calculating rules can return their score via the Score property.
    /// Rules can return a stop flag as final conclusion via the Stop property.
    /// The values in the <see cref="ValueBag"/> are objects and should be "immutable" or at least care should be taken not to
    /// accidently change them when evaluating expressions etc.
    /// </remarks>
    public class ValueBag
    {
        private readonly Dictionary<string, TConstantValue> _values = new Dictionary<string, TConstantValue>();

        public IReadOnlyDictionary<string, TConstantValue> Values => _values;

        public void AddOrUpdateValue(string name, TConstantValue value)
        {
            _values[name] = value;
        }

        public TConstantValue GetValue(string name)
        {
            if (_values.TryGetValue(name, out TConstantValue value)) // TODO: figure out if we need to return a Clone or even only the real underlying value?
            {
                return value;
            }
            throw new TExprException($"value with name {name} does not exist");
        }
        public bool TryGetValue(string name, out TConstantValue value)
        {
            return _values.TryGetValue(name, out value);
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();
            foreach (string key in _values.Keys)
            {
                builder.Append($"{key}: ");
                if (_values.TryGetValue(key, out TConstantValue value))
                {
                    builder.Append(value.ToString());
                }
                builder.Append(Environment.NewLine);
            }
            return builder.ToString();
        }

        public void Clear()
        {
            _values.Clear();
        }
    }
}
