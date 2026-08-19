using System;
using System.Linq;

namespace AuroraRevit.AiProxy;

public static class ProxyValidation
{
    public const int MaxPromptLength = 12000;

    public static bool TrySanitizePrompt(string? prompt, out string sanitized, out string error)
    {
        sanitized = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(prompt))
        {
            error = "The prompt field is required and cannot be empty.";
            return false;
        }

        var normalized = new string(prompt.Where(character => !char.IsControl(character) || character is '\n' or '\r' or '\t').ToArray()).Trim();
        if (normalized.Length == 0)
        {
            error = "The prompt contains no usable text.";
            return false;
        }

        if (normalized.Length > MaxPromptLength)
        {
            error = $"The prompt exceeds the {MaxPromptLength:N0}-character limit.";
            return false;
        }

        sanitized = normalized;
        return true;
    }

    public static bool IsValidApiKey(string? apiKey)
    {
        return !string.IsNullOrWhiteSpace(apiKey)
            && apiKey.Length >= 20
            && apiKey.StartsWith("sk-", StringComparison.OrdinalIgnoreCase)
            && !apiKey.Any(char.IsWhiteSpace);
    }
}
