using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace AuroraRevit.AiProxy;

public sealed class OpenAiOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
}

public sealed class OpenAiChatService
{
    public const string ActionCatalog = "Allowed Revit categories: walls, doors, windows, floors, roofs, ceilings, columns, beams, rooms, mechanical_equipment, ducts, pipes, cable_trays, air_terminals, electrical_equipment, lighting_fixtures, plumbing_fixtures, structural_framing, structural_columns, structural_foundations, rebar, sheets, views, areas, revisions, and generic_model. Ducts, pipes, and cable_trays are explicitly supported categories and must never be rejected as unsupported.";

    public const string RevitSystemPrompt = "You are an expert Revit API C# assistant. Use the namespace Autodesk.Revit.DB. " + ActionCatalog + " " +
        "For selection requests, return exactly { \"type\": \"select\", \"query\": \"a supported category query\" }. " +
        "For schedule requests, return exactly { \"type\": \"schedule\", \"category\": \"ducts|pipes|cable_trays|...\", \"name\": \"schedule name\" }. " +
        "For C# code requests, return { \"type\": \"code\", \"content\": \"the code here\" }. " +
        "For information requests, return { \"type\": \"info\", \"message\": \"the answer\" }. " +
        "Never reject ducts, pipes, or cable_trays. Do not add markdown formatting to the JSON.";

    private readonly ChatClient? _chatClient;
    private readonly string _apiKey;
    private readonly string _defaultModel;
    private readonly bool _isConfigured;
    private readonly string _configurationMessage;

    public OpenAiChatService(IOptions<OpenAiOptions> options)
    {
        var resolvedOptions = options.Value;
        _apiKey = resolvedOptions.ApiKey?.Trim() ?? string.Empty;
        _defaultModel = string.IsNullOrWhiteSpace(resolvedOptions.Model) ? "gpt-4o-mini" : resolvedOptions.Model.Trim();
        _isConfigured = ProxyValidation.IsValidApiKey(_apiKey);
        _configurationMessage = _isConfigured
            ? "OpenAI is configured."
            : "OpenAI API key is missing or has an invalid format. Set OpenAI:ApiKey through user-secrets or OpenAI__ApiKey.";

        if (_isConfigured)
        {
            _chatClient = new ChatClient(_defaultModel, _apiKey);
        }
    }

    public bool IsConfigured => _isConfigured;

    public string ConfigurationMessage => _configurationMessage;

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
        string modelOverride = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(ConfigurationMessage);
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(RevitSystemPrompt),
            new UserChatMessage(prompt)
        };

        var client = ResolveClient(modelOverride);
        await foreach (var update in client.CompleteChatStreamingAsync(messages).WithCancellation(cancellationToken))
        {
            foreach (var contentUpdate in update.ContentUpdate)
            {
                if (!string.IsNullOrEmpty(contentUpdate.Text))
                {
                    yield return contentUpdate.Text;
                }
            }
        }
    }

    public async Task<string> CompleteAsync(string prompt, string modelOverride = null)
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(ConfigurationMessage);
        }

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(RevitSystemPrompt),
            new UserChatMessage(prompt)
        };

        var completionResult = await ResolveClient(modelOverride).CompleteChatAsync(messages);
        var completion = completionResult.Value;
        if (completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text))
        {
            throw new InvalidOperationException("OpenAI returned an empty response.");
        }

        return completion.Content[0].Text.Trim();
    }

    private ChatClient ResolveClient(string modelOverride)
    {
        if (!string.IsNullOrWhiteSpace(modelOverride) && !string.Equals(modelOverride.Trim(), _defaultModel, StringComparison.OrdinalIgnoreCase))
        {
            return new ChatClient(modelOverride.Trim(), _apiKey);
        }

        return _chatClient!;
    }
}
