using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace AuroraRevit.RevitAddin
{
    public partial class CodeViewerWindow : Window
    {
        private readonly string _code;

        public CodeViewerWindow(string code)
        {
            InitializeComponent();
            _code = code ?? string.Empty;
            RenderSyntaxHighlightedCode(_code);
        }

        private void CopyCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CopyCodeToClipboard("Code copied to the clipboard.");
        }

        private void ExecutePythonShellButton_Click(object sender, RoutedEventArgs e)
        {
            // Do not execute arbitrary AI-generated code automatically. Copying the
            // reviewed code gives the user an explicit approval boundary in RevitPythonShell.
            CopyCodeToClipboard("Code copied. Review it, then paste it into RevitPythonShell to execute.");
        }

        private void CopyCodeToClipboard(string message)
        {
            try
            {
                Clipboard.SetText(_code);
                MessageBox.Show(message, "Aurora Code Action", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Unable to copy the code to the clipboard: " + exception.Message,
                    "Aurora Code Action",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void RenderSyntaxHighlightedCode(string code)
        {
            var document = new FlowDocument
            {
                PagePadding = new Thickness(0),
                Background = Brushes.Transparent,
                FontFamily = new FontFamily("Cascadia Mono, Consolas"),
                FontSize = 13
            };
            var paragraph = new Paragraph { Margin = new Thickness(0) };

            var tokenPattern = "//.*?$|/\\*[\\s\\S]*?\\*/|\\\"(?:\\\\\\.|[^\\\"\\\\])*\\\"|'(?:\\\\\\.|[^'\\\\])*'|\\b(?:public|private|protected|internal|static|class|interface|void|return|new|using|namespace|if|else|foreach|for|while|var|string|int|bool|true|false|null|async|await|Task|Transaction)\\b|\\b\\d+(?:\\.\\d+)?\\b";
            var tokenMatches = Regex.Matches(code ?? string.Empty, tokenPattern, RegexOptions.Multiline);
            var currentIndex = 0;

            foreach (Match tokenMatch in tokenMatches)
            {
                if (tokenMatch.Index > currentIndex)
                {
                    paragraph.Inlines.Add(new Run(code.Substring(currentIndex, tokenMatch.Index - currentIndex))
                    {
                        Foreground = new SolidColorBrush(Color.FromRgb(217, 226, 242))
                    });
                }

                var token = tokenMatch.Value;
                paragraph.Inlines.Add(new Run(token)
                {
                    Foreground = GetTokenBrush(token)
                });
                currentIndex = tokenMatch.Index + tokenMatch.Length;
            }

            if (currentIndex < (code ?? string.Empty).Length)
            {
                paragraph.Inlines.Add(new Run(code.Substring(currentIndex))
                {
                    Foreground = new SolidColorBrush(Color.FromRgb(217, 226, 242))
                });
            }

            document.Blocks.Add(paragraph);
            CodeViewer.Document = document;
        }

        private static Brush GetTokenBrush(string token)
        {
            if (token.StartsWith("//", StringComparison.Ordinal) || token.StartsWith("/*", StringComparison.Ordinal))
            {
                return new SolidColorBrush(Color.FromRgb(106, 171, 115));
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) || token.StartsWith("'", StringComparison.Ordinal))
            {
                return new SolidColorBrush(Color.FromRgb(224, 176, 104));
            }

            if (Regex.IsMatch(token, @"^\d"))
            {
                return new SolidColorBrush(Color.FromRgb(181, 156, 220));
            }

            if (token == "true" || token == "false" || token == "null")
            {
                return new SolidColorBrush(Color.FromRgb(224, 128, 170));
            }

            return new SolidColorBrush(Color.FromRgb(126, 176, 255));
        }
    }
}
