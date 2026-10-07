using System.Net.WebSockets;
using System.Text.Json;

namespace Rules.WebApi;

public static class InteractiveEvaluation
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { MaxDepth = 256 };

    public static async Task HandleAsync(HttpContext http, RuleRuntimeService runtime, string operation)
    {
        if (!http.WebSockets.IsWebSocketRequest || operation is not ("evaluate" or "calculate"))
        {
            http.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        using var socket = await http.WebSockets.AcceptWebSocketAsync();
        // Bound abandoned sessions, including clients that stay connected without answering.
        using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(http.RequestAborted);
        lifetime.CancelAfter(TimeSpan.FromMinutes(15));
        var cancellation = lifetime.Token;

        async Task SendAsync(object message) => await socket.SendAsync(
            JsonSerializer.SerializeToUtf8Bytes(message, JsonOptions).AsMemory(),
            WebSocketMessageType.Text, true, cancellation);

        async Task<JsonElement?> AskAsync(UnknownValueRequest prompt)
        {
            await SendAsync(new { type = "unknownValue", name = prompt.Name, choices = prompt.Choices, why = prompt.Why });
            var answer = await ReceiveAsync(socket, cancellation);
            if (answer.ValueKind != JsonValueKind.Object ||
                !answer.TryGetProperty("type", out var type) || type.ValueKind != JsonValueKind.String ||
                type.GetString() != "answer" || !answer.TryGetProperty("value", out var value))
                throw new JsonException("Expected a fact answer");
            return value;
        }

        try
        {
            var request = await ReceiveAsync(socket, cancellation);
            // The engine's callback is synchronous: keep its stack alive until the browser replies.
            var result = await Task.Run<object>(() => operation == "evaluate"
                ? runtime.Evaluate(request.Deserialize<EvaluateRequest>(JsonOptions)!,
                    name => AskAsync(name).GetAwaiter().GetResult())
                : runtime.Calculate(request.Deserialize<CalculateRequest>(JsonOptions)!,
                    name => AskAsync(name).GetAwaiter().GetResult()), cancellation);
            await SendAsync(new { type = "result", result });
        }
        catch (RuleSyntaxException exception)
        {
            await SendAsync(new { type = "error", error = exception.Message, syntaxErrors = exception.SyntaxErrors });
        }
        catch (RuleRuntimeException exception)
        {
            await SendAsync(new { type = "error", error = exception.Message, missingFacts = exception.MissingFacts, proofs = exception.Proofs });
        }
        catch (JsonException)
        {
            await SendAsync(new { type = "error", error = "Invalid evaluation request or fact answer" });
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested) { }
        catch (WebSocketException) { }
        finally
        {
            if (socket.State == WebSocketState.Open || socket.State == WebSocketState.CloseReceived)
            {
                using var closeTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                try
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Evaluation ended", closeTimeout.Token);
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException) { }
            }
        }
    }

    private static async Task<JsonElement> ReceiveAsync(WebSocket socket, CancellationToken cancellation)
    {
        using var message = new MemoryStream();
        var buffer = new byte[4096];
        ValueWebSocketReceiveResult received;
        do
        {
            received = await socket.ReceiveAsync(buffer.AsMemory(), cancellation);
            if (received.MessageType == WebSocketMessageType.Close)
                throw new WebSocketException("The browser disconnected");
            if (received.MessageType != WebSocketMessageType.Text || message.Length + received.Count > 1024 * 1024)
                throw new JsonException("Expected a JSON text message of at most 1 MiB");
            message.Write(buffer, 0, received.Count);
        } while (!received.EndOfMessage);

        using var document = JsonDocument.Parse(message.ToArray());
        return document.RootElement.Clone();
    }
}
