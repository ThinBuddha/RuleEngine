using System.Text.Json;
using Rules.Expression;
using Rules.Model;
using Rules.Parser;
using Rules.QueryInterpreter;
using Rules.Rules;
using Rules.Tasks;

namespace Rules.WebApi;

public sealed class RuleRuntimeService
{
    public ParseResponse Parse(string source)
    {
        var containers = ParseContainers(source, out var parser, out var modelTypes);
        return new ParseResponse(
            containers.Containers.Select(container => new ContainerSummary(
                container.Name,
                container.Rules.Select(rule => new RuleSummary(rule.Name, rule.Priority)).ToList())).ToList(),
            parser.ParseTree, modelTypes.StringValues)
        {
            Model = ModelDeriver.Derive(modelTypes)
        };
    }

    public EvaluateResponse Evaluate(EvaluateRequest request, Func<UnknownValueRequest, JsonElement?>? getUserInput = null)
    {
        var containers = ParseContainers(request.Source, out _, out var modelTypes);
        var context = CreateContext(request.Context);
        var missingFacts = new HashSet<string>(StringComparer.Ordinal);
        var evaluationType = ParseEvaluationType(request.Mode);
        TCompareExpression? query = null;

        ConfigureUnknownValueResolution(context, missingFacts, getUserInput, modelTypes);
        try
        {
            if (evaluationType == EvaluationType.Deduction)
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    throw new RuleRuntimeException("A query is required for deduction mode", missingFacts);
                }

                var interpreter = new Interpreter();
                var queryResult = interpreter.QueryToExpression(request.Query);
                if (queryResult.Item2 is { Count: > 0 })
                {
                    throw new RuleSyntaxException(queryResult.Item2);
                }

                query = queryResult.Item1;
            }

            var task = new TTask { RuleContainers = containers };
            var result = task.Evaluate(context, evaluationType, query);

            if (missingFacts.Count > 0)
            {
                throw new RuleRuntimeException("Evaluation needs additional fact values", missingFacts)
                { Proofs = ToProofs(result) };
            }

            return ToEvaluateResponse(result, context);
        }
        catch (TExprException exception)
        {
            if (exception.Id is not null)
            {
                missingFacts.Add(exception.Id.Name);
            }

            throw new RuleRuntimeException(exception.Message, missingFacts, exception);
        }
        finally
        {
            context.MessageSink.NotifyUnknownValue = null;
            context.MessageSink.GetUserInput = null;
        }
    }

    public CalculateResponse Calculate(CalculateRequest request, Func<UnknownValueRequest, JsonElement?>? getUserInput = null)
    {
        var context = CreateContext(request.Context);
        var missingFacts = new HashSet<string>(StringComparer.Ordinal);

        var modelTypes = new ModelTypes();
        if (!string.IsNullOrWhiteSpace(request.Source)) ParseContainers(request.Source, out _, out modelTypes);
        ConfigureUnknownValueResolution(context, missingFacts, getUserInput, modelTypes);
        try
        {
            var parser = new RuleParser(RulePart.Expression);
            parser.Parse(request.Expression);
            if (parser.SyntaxErrors.Count > 0 || parser.Builder.Result is not TExpression expression)
            {
                throw new RuleSyntaxException(parser.SyntaxErrors);
            }

            return new CalculateResponse(ToJsonValue(expression.Evaluate(context)), ToContext(context));
        }
        catch (TExprException exception)
        {
            if (exception.Id is not null)
            {
                missingFacts.Add(exception.Id.Name);
            }

            throw new RuleRuntimeException(exception.Message, missingFacts, exception);
        }
        finally
        {
            context.MessageSink.NotifyUnknownValue = null;
            context.MessageSink.GetUserInput = null;
        }
    }

    private static RuleContainers ParseContainers(string source, out RuleParser parser, out ModelTypes modelTypes)
    {
        parser = new RuleParser(RulePart.RuleContainer);
        parser.Parse(source);
        if (parser.SyntaxErrors.Count > 0 || parser.Builder is not RulesBuilder builder || builder.RuleContainers is null)
        {
            throw new RuleSyntaxException(parser.SyntaxErrors);
        }

        try
        {
            modelTypes = ModelTypes.Derive(builder.RuleContainers);
        }
        catch (ArgumentException exception)
        {
            throw new RuleSyntaxException(new[] { Tuple.Create(exception.Message, string.Empty) });
        }

        return builder.RuleContainers;
    }

    private static ValueBag CreateContext(Dictionary<string, JsonElement>? input)
    {
        var context = new ValueBag();
        if (input is null)
        {
            return context;
        }

        foreach (var item in input)
        {
            context.AddOrUpdateValue(item.Key, FromJsonValue(item.Value));
        }

        return context;
    }

    private static void ConfigureUnknownValueResolution(ValueBag context, ISet<string> missingFacts,
        Func<UnknownValueRequest, JsonElement?>? getUserInput, ModelTypes modelTypes)
    {
        TConstantValue? Resolve(TId subject, ValueBag _) 
        {
            if (context.TryGetValue(subject.Name, out var value))
            {
                return value;
            }

            var answer = getUserInput?.Invoke(new UnknownValueRequest(subject.Name, modelTypes.GetStringValues(subject.Name),
                ProofSnapshot.From(context.GetDeductionProof())));
            if (answer is JsonElement input && input.ValueKind != JsonValueKind.Null &&
                !(input.ValueKind == JsonValueKind.String && string.IsNullOrWhiteSpace(input.GetString())))
            {
                return FromJsonValue(input);
            }

            missingFacts.Add(subject.Name);
            return null;
        }

        context.MessageSink.NotifyUnknownValue = Resolve;
        context.MessageSink.GetUserInput = Resolve;
    }

    private static EvaluationType ParseEvaluationType(string mode) => mode.Trim().ToLowerInvariant() switch
    {
        "once" or "fireonce" or "fire-once" => EvaluationType.FireOnce,
        "iteration" => EvaluationType.Iteration,
        "deduction" => EvaluationType.Deduction,
        _ => throw new RuleRuntimeException($"Unknown evaluation mode '{mode}'", new HashSet<string>()),
    };

    private static EvaluateResponse ToEvaluateResponse(TTask.TaskEvaluateResults result, ValueBag context) =>
        new(
            result.QueryResult,
            result.TaskTrace.SelectMany(task => task.RuleResult.RuleTrace.Select(trace => new TraceEntry(
                trace.Rule.Name,
                trace.ContainerName ?? task.ContainerName,
                trace.Rule.Conditions.Select(condition => condition.ToString()).ToList()))).ToList(),
            result.TaskTrace.Count,
            ToContext(context), ToProofs(result));

    private static IReadOnlyList<ProofSnapshot> ToProofs(TTask.TaskEvaluateResults result) =>
        result.Evaluations.Where(item => item.RuleResult.Proof is not null)
            .Select(item => ProofSnapshot.From(item.RuleResult.Proof)!).ToArray();

    private static Dictionary<string, object?> ToContext(ValueBag context)
    {
        return context.Values.ToDictionary(item => item.Key, item => (object?)ToJsonValue(item.Value), StringComparer.Ordinal);
    }

    private static TConstantValue FromJsonValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => new TConstantNumValue(true),
        JsonValueKind.False => new TConstantNumValue(false),
        JsonValueKind.Number when value.TryGetInt32(out var integer) => new TConstantNumValue(integer),
        JsonValueKind.Number => new TConstantNumValue(value.GetDouble()),
        JsonValueKind.String => new TConstantStringValue(value.GetString() ?? string.Empty),
        _ => throw new RuleRuntimeException("Context values must be Boolean, numeric, or string values", new HashSet<string>()),
    };

    private static object ToJsonValue(TConstantValue value) => value switch
    {
        TConstantStringValue text => text.Value,
        TConstantNumValue number when number.Value.ValueType == TConstantNumValue.BoolType => number.Value.BoolValue,
        TConstantNumValue number when number.Value.ValueType == TConstantNumValue.DoubleType => number.Value.DoubleValue,
        TConstantNumValue number => number.Value.IntValue,
        _ => value.ToString() ?? string.Empty,
    };
}

public sealed record UnknownValueRequest(string Name, IReadOnlyList<string> Choices, ProofSnapshot? Why = null);
public sealed record ParseResponse(List<ContainerSummary> Containers, string ParseTree,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ModelTypes)
{
    public required ModelClass Model { get; init; }
}
public sealed record ContainerSummary(string Name, List<RuleSummary> Rules);
public sealed record RuleSummary(string Name, int Priority);
public sealed record EvaluateResponse(bool? QueryResult, List<TraceEntry> Trace, int Rounds,
    Dictionary<string, object?> Context, IReadOnlyList<ProofSnapshot> Proofs);
public sealed record TraceEntry(string Rule, string Container, List<string> Conditions);
public sealed record CalculateResponse(object Value, Dictionary<string, object?> Context);

public sealed class RuleSyntaxException : Exception
{
    public RuleSyntaxException(IReadOnlyList<Tuple<string, string>> syntaxErrors)
        : base(syntaxErrors.Count == 0 ? "The rule source is invalid" : syntaxErrors[0].Item1)
    {
        SyntaxErrors = syntaxErrors.Select(error => error.Item1).ToList();
    }

    public List<string> SyntaxErrors { get; }
}

public sealed class RuleRuntimeException : Exception
{
    public RuleRuntimeException(string message, IEnumerable<string> missingFacts, Exception? inner = null)
        : base(message, inner)
    {
        MissingFacts = missingFacts.Distinct(StringComparer.Ordinal).ToList();
    }

    public List<string> MissingFacts { get; }
    public IReadOnlyList<ProofSnapshot> Proofs { get; init; } = Array.Empty<ProofSnapshot>();
}
