using System.Text.Json;
using Rules.WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<RuleRuntimeService>();
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.MaxDepth = 256);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
app.UseWebSockets();

app.Map("/api/rules/interactive/{operation}", InteractiveEvaluation.HandleAsync);

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", service = "Rules.WebApi" }));

app.MapPost("/api/rules/parse", (ParseRequest request, RuleRuntimeService runtime) =>
{
    try
    {
        return Results.Ok(runtime.Parse(request.Source, request.ModelSource, request.ModelFileName));
    }
    catch (RuleSyntaxException exception)
    {
        return Results.BadRequest(new { error = exception.Message, syntaxErrors = exception.SyntaxErrors });
    }
});

app.MapPost("/api/rules/evaluate", (EvaluateRequest request, RuleRuntimeService runtime) =>
{
    try
    {
        return Results.Ok(runtime.Evaluate(request));
    }
    catch (RuleSyntaxException exception)
    {
        return Results.BadRequest(new { error = exception.Message, syntaxErrors = exception.SyntaxErrors });
    }
    catch (RuleRuntimeException exception)
    {
        return Results.BadRequest(new { error = exception.Message, missingFacts = exception.MissingFacts, proofs = exception.Proofs });
    }
});

app.MapPost("/api/rules/calculate", (CalculateRequest request, RuleRuntimeService runtime) =>
{
    try
    {
        return Results.Ok(runtime.Calculate(request));
    }
    catch (RuleSyntaxException exception)
    {
        return Results.BadRequest(new { error = exception.Message, syntaxErrors = exception.SyntaxErrors });
    }
    catch (RuleRuntimeException exception)
    {
        return Results.BadRequest(new { error = exception.Message, missingFacts = exception.MissingFacts, proofs = exception.Proofs });
    }
});

app.Run();

public sealed record ParseRequest(string Source, string? ModelSource = null, string? ModelFileName = null);

public sealed record EvaluateRequest(
    string Source,
    string Mode,
    string? Query,
    Dictionary<string, JsonElement>? Context,
    string? ModelSource = null,
    string? ModelFileName = null);

public sealed record CalculateRequest(
    string Expression,
    Dictionary<string, JsonElement>? Context,
    string? Source = null,
    string? ModelSource = null,
    string? ModelFileName = null);
