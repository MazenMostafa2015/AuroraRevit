using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AuroraRevit.RevitAddin
{
    public sealed class RevitAction
    {
        public string Type { get; set; }
        public string Query { get; set; }
        public string Content { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }

        public bool IsScheduleAction
        {
            get { return string.Equals(Type, "schedule", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsSelectAction
        {
            get { return string.Equals(Type, "select", StringComparison.OrdinalIgnoreCase); }
        }

        public bool IsCodeAction
        {
            get { return string.Equals(Type, "code", StringComparison.OrdinalIgnoreCase); }
        }
    }

    public static class RevitActionParser
    {
        private const string ExecuteMarker = "[EXECUTE_REVIT]";
        public static bool TryParse(RevitQueryResponse response, out RevitAction action)
        {
            action = null;
            if (response == null)
            {
                return false;
            }

            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(response.RawJson))
            {
                candidates.Add(response.RawJson);
            }
            if (!string.IsNullOrWhiteSpace(response.Response))
            {
                candidates.Add(response.Response);
            }

            foreach (var candidate in candidates)
            {
                if (TryDeserializeAction(candidate, out action))
                {
                    return true;
                }

                var markerIndex = candidate.IndexOf(ExecuteMarker, StringComparison.OrdinalIgnoreCase);
                if (markerIndex >= 0)
                {
                    var markedPayload = candidate.Substring(markerIndex + ExecuteMarker.Length).Trim();
                    if (TryDeserializeAction(markedPayload, out action))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool TryDeserializeAction(string candidate, out RevitAction action)
        {
            action = null;
            var json = ExtractJsonObject(candidate);
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using (var document = JsonDocument.Parse(json))
                {
                    var root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object
                        || !root.TryGetProperty("type", out var typeProperty)
                        || typeProperty.ValueKind != JsonValueKind.String)
                    {
                        return false;
                    }

                    action = new RevitAction
                    {
                        Type = typeProperty.GetString(),
                        Query = ReadString(root, "query"),
                        Content = ReadString(root, "content"),
                        Category = ReadString(root, "category"),
                        Name = ReadString(root, "name")
                    };

                    return action.IsSelectAction || action.IsCodeAction || action.IsScheduleAction;
                }
            }
            catch (JsonException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static string ExtractJsonObject(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return null;
            }

            var json = candidate.Trim();
            if (json.StartsWith("```", StringComparison.Ordinal))
            {
                json = Regex.Replace(json, @"^```(?:json)?\s*|\s*```$", string.Empty,
                    RegexOptions.IgnoreCase).Trim();
            }

            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            return json.Substring(start, end - start + 1);
        }

        private static string ReadString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var property)
                && property.ValueKind == JsonValueKind.String
                ? property.GetString()
                : null;
        }
    }
}
