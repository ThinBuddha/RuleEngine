using System.Text.Json;
using Rules.WebApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<RuleRuntimeService>();
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
        return Results.Ok(runtime.Parse(request.Source));
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
        return Results.BadRequest(new { error = exception.Message, missingFacts = exception.MissingFacts });
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
        return Results.BadRequest(new { error = exception.Message, missingFacts = exception.MissingFacts });
    }
});

app.Run();

public sealed record ParseRequest(string Source);

public sealed record EvaluateRequest(
    string Source,
    string Mode,
    string? Query,
    Dictionary<string, JsonElement>? Context);

public sealed record CalculateRequest(
    string Expression,
    Dictionary<string, JsonElement>? Context,
    string? Source = null);
