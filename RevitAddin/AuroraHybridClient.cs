using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AuroraRevit.RevitAddin
{
    public enum AuroraAiProvider
    {
        OpenAI,
        Ollama
    }

    public sealed class AuroraHybridClient
    {
        private readonly AuroraProxyClient _openAiClient;
        private readonly OllamaClient _ollamaClient;
        private AuroraAiProvider _provider;

        public AuroraHybridClient()
        {
            _openAiClient = new AuroraProxyClient();
            _ollamaClient = new OllamaClient();
            _provider = AuroraProviderSettings.LoadProvider();
        }

        public AuroraAiProvider Provider
        {
            get { return _provider; }
            set { _provider = value; }
        }

        public string Model
        {
            get { return AuroraProviderSettings.LoadModel(_provider); }
        }

        public string Endpoint
        {
            get { return _provider == AuroraAiProvider.Ollama ? _ollamaClient.Endpoint : "http://localhost:5001 or http://localhost:5000"; }
        }

        public async Task StreamQueryAsync(
            string prompt,
            Action<AuroraSseEvent> onEvent,
            CancellationToken cancellationToken)
        {
            try
            {
                await StreamWithProviderAsync(_provider, prompt, onEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception primaryError) when (!cancellationToken.IsCancellationRequested)
            {
                var alternate = _provider == AuroraAiProvider.Ollama
                    ? AuroraAiProvider.OpenAI
                    : AuroraAiProvider.Ollama;
                try
                {
                    await StreamWithProviderAsync(alternate, prompt, onEvent, cancellationToken).ConfigureAwait(false);
                    onEvent(new AuroraSseEvent
                    {
                        Type = "fallback",
                        Message = "Primary provider unavailable; Smart Fallback used " + alternate + "."
                    });
                }
                catch (Exception alternateError)
                {
                    throw new InvalidOperationException(
                        "Both Aurora AI providers are unavailable. Primary " + _provider + ": "
                        + primaryError.Message + ". Alternate " + alternate + ": " + alternateError.Message,
                        primaryError);
                }
            }
        }

        private Task StreamWithProviderAsync(
            AuroraAiProvider provider,
            string prompt,
            Action<AuroraSseEvent> onEvent,
            CancellationToken cancellationToken)
        {
            if (provider == AuroraAiProvider.Ollama)
            {
                return _ollamaClient.StreamAsync(prompt, onEvent, cancellationToken);
            }

            return _openAiClient.StreamQueryAsync(
                prompt,
                onEvent,
                cancellationToken,
                AuroraProviderSettings.LoadModel(AuroraAiProvider.OpenAI));
        }

        public async Task<string> GetStatusAsync()
        {
            if (_provider == AuroraAiProvider.Ollama)
            {
                return await _ollamaClient.IsReachableAsync().ConfigureAwait(false)
                    ? "Ollama Local ready"
                    : "Ollama Local unavailable";
            }

            try
            {
                var endpoint = await _openAiClient.GetActiveBaseUrlAsync().ConfigureAwait(false);
                return "OpenAI Cloud via " + endpoint;
            }
            catch
            {
                return "OpenAI Cloud proxy unavailable";
            }
        }
    }

    internal sealed class OllamaClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;

        public OllamaClient()
        {
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
            _endpoint = AuroraProviderSettings.LoadOllamaEndpoint();
        }

        public string Endpoint
        {
            get { return _endpoint; }
        }

        public async Task EnsureRunningAsync()
        {
            if (await IsReachableAsync().ConfigureAwait(false))
            {
                return;
            }

            var executable = FindOllamaExecutable();
            if (!string.IsNullOrWhiteSpace(executable))
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = executable,
                        Arguments = "serve",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    await Task.Delay(800).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException("Ollama is installed but could not be started: " + exception.Message);
                }
            }

            if (!await IsReachableAsync().ConfigureAwait(false))
            {
                throw new InvalidOperationException("Ollama Local is not reachable at " + _endpoint + ". Install Ollama from https://ollama.com/download/windows or start `ollama serve`.");
            }
        }

        public async Task<bool> IsReachableAsync()
        {
            try
            {
                using (var response = await _httpClient.GetAsync(_endpoint + "/api/tags").ConfigureAwait(false))
                {
                    return response.IsSuccessStatusCode;
                }
            }
            catch (HttpRequestException)
            {
                return false;
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

        private static string FindOllamaExecutable()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Ollama", "ollama.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Ollama", "ollama.exe")
            };
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
            return null;
        }

        public async Task StreamAsync(
            string prompt,
            Action<AuroraSseEvent> onEvent,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
            }

            await EnsureRunningAsync().ConfigureAwait(false);

            if (onEvent == null)
            {
                throw new ArgumentNullException(nameof(onEvent));
            }

            var payload = new OllamaChatRequest
            {
                Model = AuroraProviderSettings.LoadModel(AuroraAiProvider.Ollama),
                Stream = true,
                Messages = new List<OllamaMessage>
                {
                    new OllamaMessage { Role = "system", Content = OpenAiChatPrompt() },
                    new OllamaMessage { Role = "user", Content = prompt }
                }
            };
            var json = JsonSerializer.Serialize(payload, JsonOptions);
            using (var request = new HttpRequestMessage(HttpMethod.Post, _endpoint + "/api/chat"))
            using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
            {
                request.Content = content;
                using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false))
                using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var reader = new StreamReader(stream))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        var body = await reader.ReadToEndAsync().ConfigureAwait(false);
                        throw new InvalidOperationException("Ollama returned " + (int)response.StatusCode + ": " + body);
                    }

                    string line;
                    while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            continue;
                        }

                        using (var document = JsonDocument.Parse(line))
                        {
                            var root = document.RootElement;
                            if (root.TryGetProperty("error", out var errorProperty))
                            {
                                throw new InvalidOperationException("Ollama error: " + errorProperty.GetString());
                            }

                            if (root.TryGetProperty("message", out var message)
                                && message.TryGetProperty("content", out var contentProperty))
                            {
                                var text = contentProperty.GetString();
                                if (!string.IsNullOrEmpty(text))
                                {
                                    onEvent(new AuroraSseEvent { Type = "delta", Text = text });
                                }
                            }

                            if (root.TryGetProperty("done", out var doneProperty) && doneProperty.GetBoolean())
                            {
                                onEvent(new AuroraSseEvent { Type = "done" });
                                break;
                            }
                        }
                    }
                }
            }
        }

        private static string OpenAiChatPrompt()
        {
            return OpenAiChatServicePrompt.Value;
        }

        private static readonly Lazy<string> OpenAiChatServicePrompt = new Lazy<string>(() =>
        {
            return "You are an expert Revit API C# assistant. Return exactly one JSON object with type select, schedule, code, or info. For code use {\"type\":\"code\",\"content\":\"...\"}. For information use {\"type\":\"info\",\"message\":\"...\"}. Do not add markdown fences.";
        });

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    internal sealed class OllamaChatRequest
    {
        public string Model { get; set; }
        public bool Stream { get; set; }
        public List<OllamaMessage> Messages { get; set; }
    }

    internal sealed class OllamaMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    internal static class AuroraProviderSettings
    {
        private const string DefaultOpenAiModel = "gpt-4o-mini";
        private const string DefaultOllamaModel = "llama3.2";
        private const string DefaultOllamaEndpoint = "http://localhost:11434";

        public static AuroraAiProvider LoadProvider()
        {
            var environment = Environment.GetEnvironmentVariable("AURORA_AI_PROVIDER");
            if (string.Equals(environment, "ollama", StringComparison.OrdinalIgnoreCase))
            {
                return AuroraAiProvider.Ollama;
            }
            if (string.Equals(environment, "openai", StringComparison.OrdinalIgnoreCase))
            {
                return AuroraAiProvider.OpenAI;
            }

            try
            {
                var settings = ReadSettings();
                if (settings.TryGetProperty("provider", out var provider)
                    && string.Equals(provider.GetString(), "ollama", StringComparison.OrdinalIgnoreCase))
                {
                    return AuroraAiProvider.Ollama;
                }
            }
            catch
            {
                // Fall back to the safe cloud-proxy default.
            }

            return AuroraAiProvider.OpenAI;
        }

        public static string LoadModel(AuroraAiProvider provider)
        {
            var environmentName = provider == AuroraAiProvider.Ollama
                ? "AURORA_OLLAMA_MODEL"
                : "AURORA_OPENAI_MODEL";
            var environment = Environment.GetEnvironmentVariable(environmentName);
            if (!string.IsNullOrWhiteSpace(environment))
            {
                return environment.Trim();
            }

            try
            {
                var settings = ReadSettings();
                var key = provider == AuroraAiProvider.Ollama ? "ollama_model" : "openai_model";
                if (settings.TryGetProperty(key, out var model) && !string.IsNullOrWhiteSpace(model.GetString()))
                {
                    return model.GetString().Trim();
                }
                if (settings.TryGetProperty("model", out var legacy) && !string.IsNullOrWhiteSpace(legacy.GetString()))
                {
                    return legacy.GetString().Trim();
                }
            }
            catch
            {
                // Use defaults when the optional settings file is absent or malformed.
            }

            return provider == AuroraAiProvider.Ollama ? DefaultOllamaModel : DefaultOpenAiModel;
        }

        public static string LoadOllamaEndpoint()
        {
            var environment = Environment.GetEnvironmentVariable("AURORA_OLLAMA_ENDPOINT");
            if (!string.IsNullOrWhiteSpace(environment))
            {
                return NormalizeOllamaEndpoint(environment);
            }

            try
            {
                var settings = ReadSettings();
                if (settings.TryGetProperty("ollama_endpoint", out var endpoint)
                    && !string.IsNullOrWhiteSpace(endpoint.GetString()))
                {
                    return NormalizeOllamaEndpoint(endpoint.GetString());
                }
            }
            catch
            {
                // Use the standard Ollama endpoint.
            }

            return DefaultOllamaEndpoint;
        }

        private static string NormalizeOllamaEndpoint(string value)
        {
            var normalized = value.Trim().TrimEnd('/');
            return normalized.EndsWith("/api", StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(0, normalized.Length - 4).TrimEnd('/')
                : normalized;
        }

        private static JsonElement ReadSettings()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var path = Path.Combine(appData, "AuroraRevit", "command_tools_settings.json");
            return JsonDocument.Parse(File.ReadAllText(path)).RootElement.Clone();
        }
    }
}
