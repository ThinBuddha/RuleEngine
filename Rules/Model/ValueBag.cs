using Rules.Expression;
using Rules.Rules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rules.Model
{
    /// <summary>
    /// Generic parameter to pass on to the rules Evaluate() method, giving the rules a context to work on.
    /// 0The context is model-unaware, it's just a "bag of values". 
    /// </summary>
    /// <remarks>
    /// A <see cref="RuleContainer"/> can supply a <see cref="ValueBag"/> "model" to a rule (there are no model instances to work on).
    /// Models are used to verify rule syntax and supply choice values for a GUI, but they cannot explictly be instantiated to store values.
    /// Score calculating rules can return their score via the Score property, not store them here
    /// Rules can return a stop flag as final conclusion via the Stop property, not store them here.
    /// The values in the <see cref="ValueBag"/> are objects and should be "immutable" or at least care should be taken not to
    /// accidently change them when evaluating expressions etc.
    /// </remarks>
    public class ValueBag
    {
        public MessageSink MessageSink { get; } = new MessageSink();

        internal ProofRecorder ProofRecorder { get; set; }
        /// <summary>The proof so far, including the active reason for an input request.</summary>
        public DeductionProof GetDeductionProof() => ProofRecorder?.Snapshot();

        private readonly Dictionary<string, TConstantValue> _values = [];

        public IReadOnlyDictionary<string, TConstantValue> Values => _values;

        public void AddOrUpdateValue(string name, TConstantValue value)
        {
            _values[name] = value;
        }

        public TConstantValue GetValue(string name)
        {
            if (_values.TryGetValue(name, out TConstantValue value))
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
            StringBuilder builder = new();
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
