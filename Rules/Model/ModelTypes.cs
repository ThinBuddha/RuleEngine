using System;
using System.Collections.Generic;
using System.Linq;
using Rules.Expression;
using Rules.Rules;

namespace Rules.Model
{
    /// <summary>
    /// Represents inferred types and literal string choices for identifiers and expressions discovered from rule
    /// containers.
    /// </summary>
    /// <remarks>
    /// Maintains a mapping from identifier names to domains (Kind and known string Values). Build a
    /// model with Derive(RuleContainers). StringValues exposes identifiers whose Kind is "string"; 
    /// GetStringValues returns known literal strings for an identifier. 
    /// Merge combines domains, propagates literal values and aliases and throws ArgumentException on conflicting Kind values.
    /// </remarks>
    public class ModelTypes
    {
        private readonly Dictionary<string, Domain> _ids = new(StringComparer.Ordinal);

        public IReadOnlyDictionary<string, IReadOnlyList<string>> StringValues => _ids
            .Where(item => item.Value.Kind == "string")
            .ToDictionary(item => item.Key, item => (IReadOnlyList<string>)item.Value.Values.ToArray(), StringComparer.Ordinal);

        public IReadOnlyList<string> GetStringValues(string id) =>
            _ids.TryGetValue(id, out var domain) ? domain.Values.ToArray() : Array.Empty<string>();

        public static ModelTypes Derive(RuleContainers containers)
        {
            var model = new ModelTypes();
            foreach (var container in containers.Containers)
            {
                foreach (var rule in container.Rules)
                {
                    foreach (var condition in rule.Conditions) model.Compare(condition);
                    foreach (var conclusion in rule.Conclusions)
                        model.Merge(model.Id(conclusion.LeftExpr), conclusion.RightExpr == null
                            ? new Domain { Kind = "Boolean" } : model.Expression(conclusion.RightExpr));
                }
                if (container.StopConditionExpr != null) model.Compare(container.StopConditionExpr);
            }
            return model;
        }

        private Domain Id(string name)
        {
            if (!_ids.TryGetValue(name, out var domain)) _ids.Add(name, domain = new Domain());
            return domain;
        }

        private void Compare(TCompareExpression comparison)
        {
            Merge(Expression(comparison.LeftExpr), comparison.RightExpr == null
                ? new Domain { Kind = "Boolean" } : Expression(comparison.RightExpr));
        }

        private Domain Expression(TExpression expression)
        {
            // Unwrap parentheses, but do not present fragments of computed strings as choices.
            if (expression.Terms.Count == 1 && expression.Terms[0].Factors.Count == 1)
                return Factor(expression.Terms[0].Factors[0]);

            var operands = expression.Terms.SelectMany(term => term.Factors).Select(Factor).ToList();
            // Arithmetic dispatches on its left value. In particular, string addition
            // can append numbers; operands need not share either a type or a domain.
            return new Domain { Kind = operands.FirstOrDefault()?.Kind };
        }

        private Domain Factor(TFactor factor)
        {
            switch (factor)
            {
                case TId id:
                    var domain = Id(id.Name);
                    if (id.ShouldBeNegated) Merge(domain, new Domain { Kind = "Boolean" });
                    if (id.ShouldBeMinused) Merge(domain, new Domain { Kind = "numeric" });
                    return domain;

                case TExpression nested: 
                    return Expression(nested);

                case TConstantStringValue text:
                    var strings = new Domain { Kind = "string" };
                    if (!string.IsNullOrWhiteSpace(text.Value)) strings.Values.Add(text.Value);
                    return strings;

                case TConstantNumValue number:
                    return new Domain { Kind = number.Value.ValueType == TConstantNumValue.BoolType ? "Boolean" : "numeric" };

                default: 
                    return new Domain();
            }
        }

        private void Merge(Domain left, Domain right)
        {
            if (ReferenceEquals(left, right)) return;
            if (left.Kind != null && right.Kind != null && left.Kind != right.Kind)
            {
                var id = _ids.FirstOrDefault(item => ReferenceEquals(item.Value, left) || ReferenceEquals(item.Value, right)).Key;
                throw new ArgumentException($"Conflicting types for ID '{id ?? "expression"}': {left.Kind} and {right.Kind}");
            }
            left.Kind ??= right.Kind;
            foreach (var value in right.Values)
                if (!left.Values.Contains(value, StringComparer.Ordinal)) left.Values.Add(value);
            // Aliases share a domain, so later literals and type constraints reach both IDs.
            foreach (var id in _ids.Where(item => ReferenceEquals(item.Value, right)).Select(item => item.Key).ToList())
                _ids[id] = left;
        }

        private sealed class Domain
        {
            public string Kind;
            public List<string> Values { get; } = [];
        }

    }
}
