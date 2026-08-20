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
        private const string Endpoint = "http://localhost:5000/api/revit-query";
        private const string StreamEndpoint = "http://localhost:5000/api/revit-query/stream";
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        private readonly HttpClient _httpClient;

        public AuroraProxyClient()
        {
            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(120)
            };
        }

        public async Task<RevitQueryResponse> SendQueryAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            var payload = JsonSerializer.Serialize(new RevitQueryRequest { Prompt = prompt }, JsonOptions);
            using (var content = new StringContent(payload, Encoding.UTF8, "application/json"))
            using (var response = await _httpClient.PostAsync(Endpoint, content).ConfigureAwait(false))
            {
                var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"Proxy returned {(int)response.StatusCode}: {json}");
                }

                return DeserializeResponse(json);
            }
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
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            var payload = JsonSerializer.Serialize(new RevitQueryRequest { Prompt = prompt }, JsonOptions);
            using (var request = new HttpRequestMessage(HttpMethod.Post, StreamEndpoint))
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
