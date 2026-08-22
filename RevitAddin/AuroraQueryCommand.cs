using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraRevit.RevitAddin
{
    [Transaction(TransactionMode.Manual)]
    public sealed class AuroraQueryCommand : IExternalCommand
    {
        private readonly AuroraProxyClient _proxyClient = new AuroraProxyClient();

        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            Autodesk.Revit.DB.ElementSet elements)
        {
            try
            {
                var pane = commandData.Application.GetDockablePane(AuroraApplication.PaneId);
                if (!pane.IsShown())
                {
                    pane.Show();
                }
                return Result.Succeeded;
            }
            catch (Exception exception)
            {
                message = "Aurora could not open its dockable command bar. Restart Revit and try the Aurora AI ribbon button again. " + exception.Message;
                return Result.Failed;
            }
        }
    }

    public sealed class AuroraProxyClient
    {
        private static readonly string[] ProxyBaseUrls =
        {
            "http://localhost:5001",
            "http://localhost:5000"
        };
        private const string QueryPath = "/api/revit-query";
        private const string StreamQueryPath = "/api/revit-query/stream";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;
        private string _activeBaseUrl;

        public AuroraProxyClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120)
            };
        }

        public async Task<RevitQueryResponse> SendQueryAsync(string prompt, string model = null)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            var payload = JsonSerializer.Serialize(new RevitQueryRequest { Prompt = prompt, Model = model }, JsonOptions);
            var endpoint = (await ResolveBaseUrlAsync().ConfigureAwait(false)) + QueryPath;
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            using (var response = await _httpClient.PostAsync(endpoint, content).ConfigureAwait(false))
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Proxy returned {(int)response.StatusCode}: {ExtractProxyMessage(json)}");
                }

                return DeserializeResponse(json);
            }
        }

        public async Task<string> GetActiveBaseUrlAsync()
        {
            return await ResolveBaseUrlAsync().ConfigureAwait(false);
        }

        private async Task<string> ResolveBaseUrlAsync()
        {
            if (!string.IsNullOrWhiteSpace(_activeBaseUrl))
            {
                return _activeBaseUrl;
            }

            foreach (var baseUrl in ProxyBaseUrls)
            {
                try
                {
                    using (var response = await _httpClient.GetAsync(baseUrl + "/health").ConfigureAwait(false))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            _activeBaseUrl = baseUrl;
                            return baseUrl;
                        }
                    }
                }
                catch (HttpRequestException)
                {
                    // Try the alternate local proxy port.
                }
                catch (TaskCanceledException)
                {
                    // Try the alternate local proxy port.
                }
            }

            throw new InvalidOperationException(
                "Aurora proxy is not reachable on localhost:5001 or localhost:5000. Start Aurora Revit Proxy and verify its Running endpoint.");
        }

        private static string ExtractProxyMessage(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return "The local proxy returned an empty error response.";
            }

            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    var root = document.RootElement;
                    if (root.TryGetProperty("message", out var message)
                        && message.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(message.GetString()))
                    {
                        return message.GetString();
                    }
                    if (root.TryGetProperty("error", out var error)
                        && error.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(error.GetString()))
                    {
                        return error.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Preserve a short non-JSON proxy response for diagnostics.
            }

            return json.Length > 600 ? json.Substring(0, 600) : json;
        }

        public RevitQueryResponse DeserializeResponse(string json)
        {
            var result = JsonSerializer.Deserialize<RevitQueryResponse>(json, JsonOptions);
            if (result == null)
            {
                throw new InvalidOperationException("The response was empty.");
            }

            result.RawJson = json;
            return result;
        }

        public async Task StreamQueryAsync(
            string prompt,
            Action<AuroraSseEvent> onEvent,
            CancellationToken cancellationToken,
            string model = null)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            var payload = JsonSerializer.Serialize(new RevitQueryRequest { Prompt = prompt, Model = model }, JsonOptions);
            var endpoint = (await ResolveBaseUrlAsync().ConfigureAwait(false)) + StreamQueryPath;
            using (var request = new HttpRequestMessage(HttpMethod.Post, endpoint))
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            {
                request.Content = content;
                using (var response = await _httpClient.SendAsync(
                    request,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken))
                using (var responseStream = await response.Content.ReadAsStreamAsync())
                using (var reader = new System.IO.StreamReader(responseStream))
                {
                    var body = await reader.ReadToEndAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new InvalidOperationException(
                            $"Proxy returned {(int)response.StatusCode}: {body}");
                    }

                    using (var lineReader = new System.IO.StringReader(body))
                    {
                        string line;
                        while ((line = lineReader.ReadLine()) != null)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                            {
                                continue;
                            }

                            var json = line.Substring("data:".Length).Trim();
                            if (string.IsNullOrWhiteSpace(json))
                            {
                                continue;
                            }

                            var sseEvent = JsonSerializer.Deserialize<AuroraSseEvent>(json, JsonOptions);
                            if (sseEvent == null)
                            {
                                continue;
                            }

                            onEvent(sseEvent);
                            if (string.Equals(sseEvent.Type, "done", StringComparison.OrdinalIgnoreCase))
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }
    }

    public sealed class AuroraSseEvent
    {
        public string Type { get; set; }
        public string Text { get; set; }
        public string Message { get; set; }
    }

    public sealed class RevitQueryRequest
    {
        public string Prompt { get; set; }
        public string Model { get; set; }
    }

    public sealed class RevitQueryResponse
    {
        public string Response { get; set; }
        public string Message { get; set; }
        public string ReceivedPrompt { get; set; }
        public DateTimeOffset ServerTimeUtc { get; set; }
        public string RawJson { get; set; }
    }
}
