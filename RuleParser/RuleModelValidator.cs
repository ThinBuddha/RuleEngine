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
        private readonly HashSet<string> _usedUnits = new(StringComparer.Ordinal);
        public IReadOnlyList<string> Warnings { get; private set; } = Array.Empty<string>();
        private string _location;

        public RuleModelValidator(ExplicitModel model) { _model = model; }

        public IReadOnlyList<string> Validate(RuleContainers containers)
        {
            _errors.Clear();
            _usedUnits.Clear();
            Warnings = Array.Empty<string>();
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
                        else Compatible(target, Expression(conclusion.RightExpr), conclusion.RightExpr.ToString(),
                            conversion => conclusion.RightExpr.ResultConversion = conversion);
                    }
                }
            }
            Warnings = _usedUnits.Select(name => _model.Units[name]).Where(unit => unit.Quantity != null)
                .GroupBy(unit => unit.Quantity).Where(group => group.Count() > 1)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => $"Mixed units for {group.Key}: {string.Join(", ", group.Select(unit => unit.ShortName).OrderBy(symbol => symbol, StringComparer.Ordinal))}. Compatible values are converted automatically.")
                .ToArray();
            containers.ValidationWarnings = Warnings;
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
            Compatible(left, right, comparison.RightExpr.ToString(), conversion => comparison.RightExpr.ResultConversion = conversion);
            if (left.Type == null && right.Type != null) Compatible(right, left, comparison.LeftExpr.ToString());
            if (comparison.Operator is not (TCompareOperator.Equal or TCompareOperator.Unequal) &&
                (left.Kind == "boolean" || right.Kind == "boolean")) Error("boolean values cannot be ordered");
        }

        private Domain Expression(TExpression expression)
        {
            expression.ResultConversion = null;
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
                    next = ConvertDomain(next, result.Unit, conversion => expression.Terms[i].ResultConversion = conversion);
                    if (result.Unit != next.Unit)
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
            term.ResultConversion = null;
            term.UseRealArithmetic = false;
            var factors = term.Factors.Select(Factor).ToArray();
            if (factors.Length == 0) return new Domain(null, null);
            var result = factors[0];
            for (var i = 1; i < factors.Length; i++)
            {
                Numeric(result, factors[i], term.ToString());
                if ((result.Unit != null && factors[i].Unit != null) ||
                    (term.Operators[i - 1] == TMulDivOperator.Divide && factors[i].Unit != null))
                    Error($"Arithmetic combines two unit values in '{term}'");
                result = new Domain("numeric", result.Unit ?? factors[i].Unit,
                    IsInteger: term.Operators[i - 1] == TMulDivOperator.Multiply && result.IsInteger && factors[i].IsInteger);
            }
            term.UseRealArithmetic = result.Unit != null && _model.Units[result.Unit].Kind == "real";
            return result;
        }

        private Domain Factor(TFactor factor) => ResolveUnit(FactorDomain(factor));

        private Domain FactorDomain(TFactor factor) => factor switch
        {
            TId id => CheckId(id),
            TExpression expression => Expression(expression),
            TConstantStringValue text => new Domain("string", null, Text: text.Value),
            TConstantNumValue number when number.Value.ValueType == TConstantNumValue.BoolType => new Domain("boolean", null),
            TConstantNumValue number when number.Value.ValueType == TConstantNumValue.IntType => new Domain("numeric", null, Number: number.Value.IntValue, IsInteger: true, UnitShortName: number.UnitShortName),
            TConstantNumValue number => new Domain("numeric", null, Number: number.Value.DoubleValue, UnitShortName: number.UnitShortName),
            _ => new Domain(null, null)
        };

        private Domain ResolveUnit(Domain domain)
        {
            if (domain.UnitShortName == null) return domain;
            var units = _model.Units.Values.Where(unit => unit.ShortName == domain.UnitShortName).ToArray();
            if (units.Length != 1)
            {
                Error($"Unit short name '{domain.UnitShortName}' must identify exactly one model unit");
                return domain;
            }
            if (units[0].Kind == "integer" && !domain.IsInteger)
                Error($"Value with unit '{domain.UnitShortName}' must be an integer");
            _usedUnits.Add(units[0].Name);
            return domain with { Unit = units[0].Name };
        }

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
            if (unit != null) _usedUnits.Add(unit.Name);
            return new Domain(type.Kind switch { "integer" or "real" => "numeric", null => "numeric", var kind => kind },
                unit?.Name, Type: type, IsInteger: type.Kind == "integer" || unit?.Kind == "integer");
        }

        private void Numeric(Domain left, Domain right, string expression)
        {
            if ((left.Kind != null && left.Kind != "numeric") || (right.Kind != null && right.Kind != "numeric"))
                Error($"Arithmetic requires numbers in '{expression}'");
        }

        private Domain ConvertDomain(Domain value, string targetUnit, Action<ModelUnitConversion> apply = null)
        {
            if (value.Kind != "numeric" || value.Unit == null || targetUnit == null || value.Unit == targetUnit ||
                !_model.Units[value.Unit].TryGetConversionTo(_model.Units[targetUnit], out var conversion)) return value;

            apply?.Invoke(conversion);
            var number = value.Number.HasValue ? conversion.Convert(value.Number.Value) : (double?)null;
            if (number.HasValue && !double.IsFinite(number.Value)) Error("Unit conversion produced a non-finite value");
            return value with
            {
                Unit = targetUnit,
                Number = number,
                Type = null,
                IsInteger = number.HasValue ? number.Value == Math.Truncate(number.Value)
                    : conversion.Target.Kind == "integer" || (value.IsInteger && conversion.PreservesIntegers)
            };
        }

        private void Compatible(Domain target, Domain value, string expression, Action<ModelUnitConversion> apply = null)
        {
            if (target.Kind == null || value.Kind == null) return;
            if (target.Kind != value.Kind) { Error($"'{expression}' is {value.Kind} but expected {target.Kind}"); return; }
            value = ConvertDomain(value, target.Unit, apply);
            if (target.Kind == "numeric" && target.Unit != value.Unit)
            {
                if (target.Unit != null && value.Unit == null)
                    Error($"'{expression}' requires unit short name '{_model.Units[target.Unit].ShortName}'");
                else
                    Error($"'{expression}' uses unit '{value.Unit}' but expected '{target.Unit ?? "no unit"}'");
            }
            if (target.Type != null && target.IsInteger && value.Kind == "numeric" && !value.IsInteger)
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
        private sealed record Domain(string Kind, string Unit, string Text = null, double? Number = null, ModelType Type = null, bool IsInteger = false, string UnitShortName = null);
    }
}
