using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace AuroraRevit.RevitAddin
{
    public sealed class RevitExample
    {
        public string Discipline { get; set; }
        public string Title { get; set; }
        public string Prompt { get; set; }

        public string DisplayName
        {
            get { return $"[{Discipline}] - {Title}"; }
        }
    }

    public static class RevitExampleLibrary
    {
        private static readonly string[] Disciplines = { "Architecture", "Structure", "MEP", "General" };
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public static IReadOnlyList<RevitExample> LoadAll()
        {
            var assembly = typeof(RevitExampleLibrary).Assembly;
            var resources = assembly.GetManifestResourceNames();
            var examples = new List<RevitExample>();

            foreach (var discipline in Disciplines)
            {
                var suffix = $".Examples.{discipline}.examples.json";
                var resourceName = resources.FirstOrDefault(name =>
                    name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrWhiteSpace(resourceName))
                {
                    throw new InvalidOperationException(
                        $"The embedded example library for {discipline} was not found.");
                }

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    var disciplineExamples = JsonSerializer.Deserialize<List<RevitExample>>(
                        reader.ReadToEnd(), JsonOptions);
                    if (disciplineExamples == null || disciplineExamples.Count != 10)
                    {
                        throw new InvalidOperationException(
                            $"The {discipline} example library must contain exactly 10 prompts.");
                    }

                    foreach (var example in disciplineExamples)
                    {
                        example.Discipline = discipline;
                    }

                    examples.AddRange(disciplineExamples);
                }
            }

            return examples;
        }
    }
}
