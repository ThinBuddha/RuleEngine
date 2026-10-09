using System;
using System.Collections.Generic;
using System.Linq;
using Rules.Expression;
using Rules.Model;
using Rules.Rules;

namespace Rules.Parser
{
    /// <summary>Checks rule identifiers and expression operands against an explicit model.</summary>
    public sealed class RuleModelValidator
    {
        private readonly ExplicitModel _model;
        private readonly List<string> _errors = [];
        private string _location;

        public RuleModelValidator(ExplicitModel model) { _model = model; }

        public IReadOnlyList<string> Validate(RuleContainers containers)
        {
            _errors.Clear();
            foreach (var container in containers.Containers)
            {
                _location = $"Container '{container.Name}' stop condition";
                if (container.StopConditionExpr != null) Compare(container.StopConditionExpr);
                foreach (var rule in container.Rules)
                {
                    _location = $"Rule '{rule.Name}'";
                    foreach (var condition in rule.Conditions) Compare(condition);
                    foreach (var conclusion in rule.Conclusions)
                    {
                        var target = Id(conclusion.LeftExpr);
                        if (conclusion.RightExpr == null)
                        {
                            if (target.Kind != null && target.Kind != "boolean") Error($"'{conclusion.LeftExpr}' must be boolean for a bare conclusion");
                        }
                        else Compatible(target, Expression(conclusion.RightExpr), conclusion.RightExpr.ToString());
                    }
                }
            }
            return _errors.Distinct(StringComparer.Ordinal).ToArray();
        }

        private void Compare(TCompareExpression comparison)
        {
            var left = Expression(comparison.LeftExpr);
            if (comparison.RightExpr == null)
            {
                if (left.Kind != null && left.Kind != "boolean") Error($"'{comparison.LeftExpr}' must be boolean in a bare condition");
                return;
            }
            var right = Expression(comparison.RightExpr);
            Compatible(left, right, comparison.RightExpr.ToString());
            if (left.Type == null && right.Type != null) Compatible(right, left, comparison.LeftExpr.ToString());
            if (comparison.Operator is not (TCompareOperator.Equal or TCompareOperator.Unequal) &&
                (left.Kind == "boolean" || right.Kind == "boolean")) Error("boolean values cannot be ordered");
        }

        private Domain Expression(TExpression expression)
        {
            var terms = expression.Terms.Select(Term).ToArray();
            if (terms.Length == 0) return new Domain(null, null);
            var result = terms[0];
            for (var i = 1; i < terms.Length; i++)
            {
                var next = terms[i];
                if (result.Kind == "string" && next.Kind is "string" or "numeric" && expression.Operators[i - 1] == TPlusMinOperator.Plus)
                    result = new Domain("string", null);
                else
                {
                    Numeric(result, next, expression.ToString());
                    if (result.Unit != null && next.Unit != null && result.Unit != next.Unit)
                        Error($"Arithmetic mixes units '{result.Unit}' and '{next.Unit}' in '{expression}'");
                    result = new Domain("numeric", result.Unit ?? next.Unit, IsInteger: result.IsInteger && next.IsInteger);
                }
            }
            if (expression.ToString().StartsWith("-(", StringComparison.Ordinal))
            {
                if (result.Kind != null && result.Kind != "numeric") Error($"Unary minus requires a number in '{expression}'");
                return new Domain("numeric", result.Unit, IsInteger: result.IsInteger);
            }
            return result;
        }

        private Domain Term(TTerm term)
        {
            var factors = term.Factors.Select(Factor).ToArray();
            if (factors.Length == 0) return new Domain(null, null);
            var result = factors[0];
            for (var i = 1; i < factors.Length; i++)
            {
                Numeric(result, factors[i], term.ToString());
                if (result.Unit != null && factors[i].Unit != null)
                    Error($"Arithmetic combines two unit values in '{term}'");
                result = new Domain("numeric", result.Unit ?? factors[i].Unit,
                    IsInteger: term.Operators[i - 1] == TMulDivOperator.Multiply && result.IsInteger && factors[i].IsInteger);
            }
            return result;
        }

        private Domain Factor(TFactor factor) => factor switch
        {
            TId id => CheckId(id),
            TExpression expression => Expression(expression),
            TConstantStringValue text => new Domain("string", null, Text: text.Value),
            TConstantNumValue number when number.Value.ValueType == TConstantNumValue.BoolType => new Domain("boolean", null),
            TConstantNumValue number when number.Value.ValueType == TConstantNumValue.IntType => new Domain("numeric", null, Number: number.Value.IntValue, IsInteger: true),
            TConstantNumValue number => new Domain("numeric", null, Number: number.Value.DoubleValue),
            _ => new Domain(null, null)
        };

        private Domain CheckId(TId id)
        {
            var domain = Id(id.Name);
            if (id.ShouldBeNegated && domain.Kind != null && domain.Kind != "boolean") Error($"'not {id.Name}' requires a boolean");
            if (id.ShouldBeMinused && domain.Kind != null && domain.Kind != "numeric") Error($"'-{id.Name}' requires a number");
            return domain;
        }

        private Domain Id(string name)
        {
            var type = _model.Resolve(name);
            if (type == null)
            {
                Error($"ID '{name}' does not map to a model property");
                return new Domain(null, null);
            }
            var unit = type.Kind == null ? _model.Units.GetValueOrDefault(type.BaseName) : null;
            return new Domain(type.Kind switch { "integer" or "real" => "numeric", null => "numeric", var kind => kind },
                unit?.Name, Type: type, IsInteger: type.Kind == "integer" || unit?.Kind == "integer");
        }

        private void Numeric(Domain left, Domain right, string expression)
        {
            if ((left.Kind != null && left.Kind != "numeric") || (right.Kind != null && right.Kind != "numeric"))
                Error($"Arithmetic requires numbers in '{expression}'");
        }

        private void Compatible(Domain target, Domain value, string expression)
        {
            if (target.Kind == null || value.Kind == null) return;
            if (target.Kind != value.Kind) { Error($"'{expression}' is {value.Kind} but expected {target.Kind}"); return; }
            if (target.Kind == "numeric" && target.Unit != null && value.Unit != null && target.Unit != value.Unit)
                Error($"'{expression}' uses unit '{value.Unit}' but expected '{target.Unit}'");
            if (target.IsInteger && value.Kind == "numeric" && !value.IsInteger)
                Error($"'{expression}' must be an integer");
            if (target.Type == null) return;
            if (value.Text != null && target.Type.Values.Count > 0 && !target.Type.Values.Contains(value.Text, StringComparer.Ordinal))
                Error($"Value '{value.Text}' is outside type '{target.Type.Name}'");
            if (value.Number is double number &&
                ((target.Type.Minimum is { } min && (number < min.Value || (number == min.Value && !min.Inclusive))) ||
                 (target.Type.Maximum is { } max && (number > max.Value || (number == max.Value && !max.Inclusive)))))
                Error($"Value {number} is outside type '{target.Type.Name}'");
        }

        private void Error(string message) => _errors.Add($"{_location}: {message}");
        private sealed record Domain(string Kind, string Unit, string Text = null, double? Number = null, ModelType Type = null, bool IsInteger = false);
    }
}
