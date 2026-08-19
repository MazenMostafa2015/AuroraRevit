using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
var listenUrl = ProxyPortResolver.ResolveUrl(args);
builder.WebHost.UseUrls(listenUrl);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
});

builder.Services.Configure<OpenAiOptions>(builder.Configuration.GetSection("OpenAI"));
builder.Services.AddSingleton<OpenAiChatService>();
builder.Services.AddCors(options =>
{
    options.AddPolicy("RevitLocal", policy =>
    {
        policy.SetIsOriginAllowed(IsLocalOrigin)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();
app.UseCors("RevitLocal");
app.Logger.LogInformation("Aurora AI Proxy listening at {ListenUrl}", listenUrl);

app.MapGet("/", (OpenAiChatService chatService) => Results.Ok(new
{
    service = "AuroraRevit.AiProxy",
    status = "running",
    mode = "openai",
    configured = chatService.IsConfigured,
    endpoint = listenUrl
}));

app.MapGet("/health", (OpenAiChatService chatService) => Results.Ok(new
{
    status = "healthy",
    mode = "openai",
    configured = chatService.IsConfigured,
    endpoint = listenUrl
}));

app.MapPost("/api/revit-query", async (RevitQueryRequest request, OpenAiChatService chatService) =>
{
    if (!ProxyValidation.TrySanitizePrompt(request?.Prompt, out var prompt, out var validationError))
    {
        return Results.BadRequest(new { error = validationError });
    }

    try
    {
        var modelResponse = await chatService.CompleteAsync(prompt);
        var normalizedJson = OpenAiResponseNormalizer.Normalize(modelResponse);
        return Results.Content(normalizedJson, "application/json", Encoding.UTF8);
    }
    catch (InvalidOperationException exception) when (!chatService.IsConfigured)
    {
        return Results.Json(new
        {
            type = "info",
            message = exception.Message
        }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
    catch (Exception exception)
    {
        return Results.Json(new
        {
            type = "info",
            message = SafeProviderError(exception)
        }, statusCode: StatusCodes.Status502BadGateway);
    }
});

app.MapPost("/api/revit-query/stream", async (HttpContext context, RevitQueryRequest request, OpenAiChatService chatService) =>
{
    if (!ProxyValidation.TrySanitizePrompt(request?.Prompt, out var prompt, out var validationError))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = validationError });
        return;
    }

    if (!chatService.IsConfigured)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new
        {
            type = "info",
            message = chatService.ConfigurationMessage
        });
        return;
    }

    context.Response.StatusCode = StatusCodes.Status200OK;
    context.Response.ContentType = "text/event-stream; charset=utf-8";
    context.Response.Headers.CacheControl = "no-cache";
    context.Response.Headers["X-Accel-Buffering"] = "no";

    try
    {
        await foreach (var delta in chatService.StreamAsync(prompt, context.RequestAborted))
        {
            await WriteSseEventAsync(context.Response, new { type = "delta", text = delta }, context.RequestAborted);
        }

        await WriteSseEventAsync(context.Response, new { type = "done" }, context.RequestAborted);
    }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
    {
        // The Revit client disconnected or cancelled the request.
    }
    catch (Exception exception)
    {
        if (!context.RequestAborted.IsCancellationRequested)
        {
            await WriteSseEventAsync(
                context.Response,
                new { type = "error", message = SafeProviderError(exception) },
                context.RequestAborted);
        }
    }
});

app.Run();

static string SafeProviderError(Exception exception)
{
    var message = exception.Message ?? string.Empty;
    if (message.Contains("401", StringComparison.OrdinalIgnoreCase)
        || message.Contains("unauthorized", StringComparison.OrdinalIgnoreCase)
        || message.Contains("invalid api key", StringComparison.OrdinalIgnoreCase))
    {
        return "OpenAI rejected the configured API key. Check OpenAI:ApiKey or OpenAI__ApiKey.";
    }

    return "The OpenAI request failed. Check the proxy logs for details.";
}

static async Task WriteSseEventAsync(HttpResponse response, object payload, CancellationToken cancellationToken)
{
    var json = JsonSerializer.Serialize(payload);
    await response.WriteAsync("data: " + json + "\n\n", cancellationToken);
    await response.Body.FlushAsync(cancellationToken);
}

static bool IsLocalOrigin(string origin)
{
    return Uri.TryCreate(origin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && (string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase));
}

public sealed record RevitQueryRequest(
    [property: JsonPropertyName("prompt")] string Prompt);

public static class OpenAiResponseNormalizer
{
    public static string Normalize(string modelResponse)
    {
        var candidate = ExtractJsonObject(modelResponse);
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return JsonSerializer.Serialize(new
            {
                type = "info",
                message = modelResponse?.Trim() ?? "The model returned an empty response."
            });
        }

        try
        {
            using var document = JsonDocument.Parse(candidate);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("type", out var typeProperty)
                || typeProperty.ValueKind != JsonValueKind.String)
            {
                return InvalidAction(candidate);
            }

            var type = typeProperty.GetString();
            if (string.Equals(type, "select", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("query", out var query)
                && query.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(query.GetString()))
            {
                return root.GetRawText();
            }

            if (string.Equals(type, "code", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(content.GetString()))
            {
                return root.GetRawText();
            }

            if (string.Equals(type, "info", StringComparison.OrdinalIgnoreCase)
                && root.TryGetProperty("message", out var message)
                && message.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(message.GetString()))
            {
                return root.GetRawText();
            }

            return InvalidAction(candidate);
        }
        catch (JsonException)
        {
            return InvalidAction(candidate);
        }
    }

    private static string ExtractJsonObject(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var candidate = value.Trim();
        if (candidate.StartsWith("```", StringComparison.Ordinal))
        {
            candidate = candidate
                .Replace("```json", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("```", string.Empty, StringComparison.Ordinal)
                .Trim();
        }

        var start = candidate.IndexOf('{');
        var end = candidate.LastIndexOf('}');
        return start >= 0 && end > start
            ? candidate.Substring(start, end - start + 1)
            : string.Empty;
    }

    private static string InvalidAction(string rawJson)
    {
        return JsonSerializer.Serialize(new
        {
            type = "info",
            message = "The model returned an invalid Revit action JSON: " + rawJson
        });
    }
}

public partial class Program { }
