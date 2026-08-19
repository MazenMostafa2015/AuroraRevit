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
    public const string RevitSystemPrompt = "You are an expert Revit API C# assistant. Use the namespace Autodesk.Revit.DB. If the user asks for a selection, return a JSON object { \"type\": \"select\", \"query\": \"some filter\" }. If they ask for C# code, return { \"type\": \"code\", \"content\": \"the code here\" }. If the user asks for information, return { \"type\": \"info\", \"message\": \"the answer\" }. Do not add markdown formatting to the JSON.";

    private readonly ChatClient? _chatClient;
    private readonly bool _isConfigured;
    private readonly string _configurationMessage;

    public OpenAiChatService(IOptions<OpenAiOptions> options)
    {
        var resolvedOptions = options.Value;
        _isConfigured = ProxyValidation.IsValidApiKey(resolvedOptions.ApiKey);
        _configurationMessage = _isConfigured
            ? "OpenAI is configured."
            : "OpenAI API key is missing or has an invalid format. Set OpenAI:ApiKey through user-secrets or OpenAI__ApiKey.";

        if (_isConfigured)
        {
            _chatClient = new ChatClient(
                string.IsNullOrWhiteSpace(resolvedOptions.Model) ? "gpt-4o-mini" : resolvedOptions.Model.Trim(),
                resolvedOptions.ApiKey.Trim());
        }
    }

    public bool IsConfigured => _isConfigured;

    public string ConfigurationMessage => _configurationMessage;

    public async IAsyncEnumerable<string> StreamAsync(
        string prompt,
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

        await foreach (var update in _chatClient!.CompleteChatStreamingAsync(messages).WithCancellation(cancellationToken))
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

    public async Task<string> CompleteAsync(string prompt)
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

        var completion = await _chatClient!.CompleteChatAsync(messages);
        if (completion.Content.Count == 0 || string.IsNullOrWhiteSpace(completion.Content[0].Text))
        {
            throw new InvalidOperationException("OpenAI returned an empty response.");
        }

        return completion.Content[0].Text.Trim();
    }
}
