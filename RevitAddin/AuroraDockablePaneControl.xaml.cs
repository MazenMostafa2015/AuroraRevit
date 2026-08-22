using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AuroraRevit.RevitAddin
{
    public partial class AuroraDockablePaneControl : UserControl
    {
        private readonly AuroraProxyClient _proxyClient;
        private readonly AuroraHybridClient _hybridClient;
        private readonly RevitActionHandler _revitActionHandler;
        private CancellationTokenSource _streamCancellation;
        private readonly StringBuilder _actionLog = new StringBuilder();
        private IReadOnlyList<RevitExample> _exampleLibrary = new List<RevitExample>();
        private bool _isLightTheme;

        public AuroraDockablePaneControl()
        {
            InitializeComponent();
            _proxyClient = new AuroraProxyClient();
            _hybridClient = new AuroraHybridClient();
            _revitActionHandler = new RevitActionHandler();
            ProviderComboBox.SelectedIndex = _hybridClient.Provider == AuroraAiProvider.Ollama ? 1 : 0;
            LoadExampleLibrary();
            AddAssistantMessage("Hello. I’m Aurora, your Revit AI Assistant. Choose OpenAI Cloud or Ollama Local, then ask me a question.");
            _ = RefreshProviderStatusAsync();
        }

        private async void CompactSendButton_Click(object sender, RoutedEventArgs e)
        {
            PromptTextBox.Text = CompactPromptTextBox.Text;
            CompactPromptTextBox.Clear();
            await SendPromptAsync();
        }

        private async void CompactPromptTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                PromptTextBox.Text = CompactPromptTextBox.Text;
                CompactPromptTextBox.Clear();
                await SendPromptAsync();
            }
        }

        private void ExpandChatButton_Click(object sender, RoutedEventArgs e)
        {
            CompactBar.Visibility = Visibility.Collapsed;
            ExpandedChatView.Visibility = Visibility.Visible;
            MinHeight = 420;
            CompactPromptTextBox.Clear();
            PromptTextBox.Focus();
        }

        private void CollapseChatButton_Click(object sender, RoutedEventArgs e)
        {
            ExpandedChatView.Visibility = Visibility.Collapsed;
            CompactBar.Visibility = Visibility.Visible;
            MinHeight = 50;
            CompactPromptTextBox.Focus();
        }

        private void LoadExampleLibrary()
        {
            try
            {
                _exampleLibrary = RevitExampleLibrary.LoadAll();
                ArchitectureExamplesList.ItemsSource = _exampleLibrary.Where(x => x.Discipline == "Architecture").ToList();
                StructureExamplesList.ItemsSource = _exampleLibrary.Where(x => x.Discipline == "Structure").ToList();
                MepExamplesList.ItemsSource = _exampleLibrary.Where(x => x.Discipline == "MEP").ToList();
                GeneralExamplesList.ItemsSource = _exampleLibrary.Where(x => x.Discipline == "General").ToList();
            }
            catch (Exception exception)
            {
                ProxyStatusText.Text = "  Example gallery unavailable";
                ProxyStatusText.ToolTip = exception.Message;
            }
        }

        private string BuildExampleCode(RevitExample selectedExample)
        {
            if (selectedExample == null)
            {
                return string.Empty;
            }

            if (selectedExample.HasCodeTemplate)
            {
                return selectedExample.CodeTemplate;
            }

            var prompt = (selectedExample.Prompt ?? string.Empty)
                .Replace("\r", " ")
                .Replace("\n", " ");
            return "// AuroraRevit safe preview scaffold\r\n"
                + "// This example has a prompt but no embedded executable template.\r\n"
                + "// Prompt: " + prompt + "\r\n\r\n"
                + "using Autodesk.Revit.DB;\r\n\r\n"
                + "public static class AuroraExamplePreview\r\n"
                + "{\r\n"
                + "    public static void Run(Document doc)\r\n"
                + "    {\r\n"
                + "        // Ask Aurora to generate reviewed Revit API code for this prompt.\r\n"
                + "        // No model-changing code is executed from this preview.\r\n"
                + "    }\r\n"
                + "}";
        }

        private async void ExampleList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var list = sender as ListBox;
            var selectedExample = list == null ? null : list.SelectedItem as RevitExample;
            if (selectedExample == null)
            {
                return;
            }

            PromptTextBox.Text = selectedExample.Prompt;
            ExampleCodeTextBox.Text = BuildExampleCode(selectedExample);
            ExampleCodePanel.Visibility = Visibility.Visible;
            PromptTextBox.Focus();
            PromptTextBox.CaretIndex = PromptTextBox.Text.Length;
            await SendPromptAsync();
            list.SelectedItem = null;
        }

        private void CopyExampleCodeButton_Click(object sender, RoutedEventArgs e)
        {
            CopyTextToClipboard(ExampleCodeTextBox.Text, "The example code template was copied to the clipboard.");
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPromptAsync();
        }

        private async void ProviderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_hybridClient == null || ProviderComboBox.SelectedIndex < 0)
            {
                return;
            }

            _hybridClient.Provider = ProviderComboBox.SelectedIndex == 1
                ? AuroraAiProvider.Ollama
                : AuroraAiProvider.OpenAI;
            AppendActionLog("PROVIDER", _hybridClient.Provider == AuroraAiProvider.Ollama ? "Ollama Local selected." : "OpenAI Cloud selected.");
            await RefreshProviderStatusAsync();
        }

        private async System.Threading.Tasks.Task RefreshProviderStatusAsync()
        {
            if (_hybridClient == null)
            {
                return;
            }

            try
            {
                var status = await _hybridClient.GetStatusAsync();
                ProxyStatusText.Text = "  " + status;
            }
            catch (Exception exception)
            {
                ProxyStatusText.Text = "  Provider status unavailable";
                ProxyStatusText.ToolTip = exception.Message;
            }
        }

        private async void PromptTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers != ModifierKeys.Shift)
            {
                e.Handled = true;
                await SendPromptAsync();
            }
        }

        private async System.Threading.Tasks.Task SendPromptAsync()
        {
            var prompt = PromptTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt) || !SendButton.IsEnabled || !CompactSendButton.IsEnabled)
            {
                return;
            }

            AddUserMessage(prompt);
            AppendActionLog("PROMPT", prompt);
            PromptTextBox.Clear();
            SetLoading(true);

            var assistantBubble = AddAssistantMessageBubble();
            var streamedJson = new StringBuilder();
            string streamError = null;
            _streamCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            try
            {
                await _hybridClient.StreamQueryAsync(
                    prompt,
                    streamEvent =>
                    {
                        if (string.Equals(streamEvent.Type, "delta", StringComparison.OrdinalIgnoreCase))
                        {
                            var text = streamEvent.Text ?? string.Empty;
                            streamedJson.Append(text);
                            AppendMessageText(assistantBubble, text);
                        }
                        else if (string.Equals(streamEvent.Type, "error", StringComparison.OrdinalIgnoreCase))
                        {
                            streamError = streamEvent.Message ?? "The AI stream failed.";
                        }
                    },
                    _streamCancellation.Token);

                if (!string.IsNullOrWhiteSpace(streamError))
                {
                    SetMessageText(assistantBubble, FriendlyError(streamError));
                    AppendActionLog("ERROR", streamError);
                }
                else
                {
                    await HandleStreamedResponseAsync(streamedJson.ToString(), assistantBubble);
                }

                ShowToast(assistantBubble.Text);
            }
            catch (OperationCanceledException)
            {
                SetMessageText(assistantBubble, "The AI response was cancelled or timed out.");
            }
            catch (Exception exception)
            {
                var friendly = FriendlyError(exception.Message, _hybridClient.Provider);
                SetMessageText(assistantBubble, friendly);
                AppendActionLog("ERROR", exception.Message);
                ShowToast(assistantBubble.Text);
            }
            finally
            {
                if (_streamCancellation != null)
                {
                    _streamCancellation.Dispose();
                    _streamCancellation = null;
                }

                SetLoading(false);
                PromptTextBox.Focus();
            }
        }

        private async System.Threading.Tasks.Task HandleStreamedResponseAsync(
            string rawJson,
            TextBlock assistantBubble)
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                SetMessageText(assistantBubble, "The proxy returned an empty streamed response.");
                return;
            }

            RevitQueryResponse result;
            try
            {
                result = _proxyClient.DeserializeResponse(rawJson);
            }
            catch
            {
                SetMessageText(assistantBubble, rawJson);
                return;
            }

            RevitAction action;
            if (RevitActionParser.TryParse(result, out action))
            {
                if (action.IsScheduleAction || action.IsSelectAction)
                {
                    var actionResult = await _revitActionHandler.RaiseAsync(action);
                    SetMessageText(assistantBubble, actionResult.Message);
                    AppendActionLog(action.Type.ToUpperInvariant(), actionResult.Message);
                    return;
                }

                if (action.IsCodeAction)
                {
                    if (string.IsNullOrWhiteSpace(action.Content))
                    {
                        SetMessageText(assistantBubble, "The proxy returned a code action without any code content.");
                        return;
                    }

                    var codeWindow = new CodeViewerWindow(action.Content)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    codeWindow.Show();
                    SetMessageText(assistantBubble, "I opened the generated C# code in a review window. You can also copy the execution directly from this chat message.");
                    AddCopyExecutionButton(assistantBubble, action.Content);
                    return;
                }
            }

            var displayText = !string.IsNullOrWhiteSpace(result.Response)
                ? result.Response
                : !string.IsNullOrWhiteSpace(result.Message)
                    ? result.Message
                    : rawJson;
            SetMessageText(assistantBubble, displayText);
        }

        private async System.Threading.Tasks.Task HandleProxyResponseAsync(RevitQueryResponse result)
        {
            RevitAction action;
            if (RevitActionParser.TryParse(result, out action))
            {
                if (action.IsScheduleAction || action.IsSelectAction)
                {
                    var actionResult = await _revitActionHandler.RaiseAsync(action);
                    AddAssistantMessage(actionResult.Message);
                    AppendActionLog(action.Type.ToUpperInvariant(), actionResult.Message);
                    return;
                }

                if (action.IsCodeAction)
                {
                    if (string.IsNullOrWhiteSpace(action.Content))
                    {
                        AddAssistantMessage("The proxy returned a code action without any code content.");
                        return;
                    }

                    var codeWindow = new CodeViewerWindow(action.Content)
                    {
                        Owner = Window.GetWindow(this)
                    };
                    codeWindow.Show();
                    var codeMessage = AddAssistantMessage("I opened the generated C# code in a review window. You can also copy the execution directly from this chat message.");
                    AddCopyExecutionButton(codeMessage, action.Content);
                    return;
                }
            }

            var displayText = result == null
                ? "The proxy returned an empty response."
                : !string.IsNullOrWhiteSpace(result.Response)
                    ? result.Response
                    : !string.IsNullOrWhiteSpace(result.Message)
                        ? result.Message
                        : result.RawJson;

            AddAssistantMessage(string.IsNullOrWhiteSpace(displayText)
                ? "The proxy returned an empty response."
                : displayText);
        }

        private void AddCopyExecutionButton(TextBlock messageText, string code)
        {
            if (messageText.Parent is not Border bubble)
            {
                return;
            }

            var panel = new StackPanel();
            bubble.Child = null;
            panel.Children.Add(messageText);

            var copyButton = new Button
            {
                Content = "Copy this Execution",
                Foreground = Brushes.White,
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#29356D")),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(0, 9, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = Cursors.Hand
            };
            copyButton.Click += (_, _) =>
            {
                try
                {
                    CopyTextToClipboard(code, "The generated C# execution was copied to the clipboard for review.");
                }
                catch (Exception exception)
                {
                    MessageBox.Show("Unable to copy the execution: " + exception.Message, "Aurora AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            panel.Children.Add(copyButton);
            bubble.Child = panel;
            ChatScrollViewer.ScrollToEnd();
        }

        private void ThemeToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _isLightTheme = !_isLightTheme;
            var panel = (SolidColorBrush)FindResource("PanelBackground");
            var surface = (SolidColorBrush)FindResource("SurfaceBackground");
            var primary = (SolidColorBrush)FindResource("PrimaryText");
            var secondary = (SolidColorBrush)FindResource("SecondaryText");
            panel.Color = (Color)ColorConverter.ConvertFromString(_isLightTheme ? "#F4F7FB" : "#10141C");
            surface.Color = (Color)ColorConverter.ConvertFromString(_isLightTheme ? "#FFFFFF" : "#171D28");
            primary.Color = (Color)ColorConverter.ConvertFromString(_isLightTheme ? "#172033" : "#F4F7FB");
            secondary.Color = (Color)ColorConverter.ConvertFromString(_isLightTheme ? "#526078" : "#97A3B6");
            ThemeToggleButton.Content = _isLightTheme ? "Dark Theme" : "Light Theme";
            AppendActionLog("THEME", _isLightTheme ? "Light theme enabled." : "Dark theme enabled.");
        }

        private void FeedbackButton_Click(object sender, RoutedEventArgs e)
        {
            var text = _actionLog.Length == 0 ? "Aurora action log is empty." : _actionLog.ToString();
            CopyTextToClipboard(text, "The Aurora action log was copied for debugging feedback.");
        }

        private void CopyTextToClipboard(string text, string confirmation)
        {
            try
            {
                Clipboard.SetText(text ?? string.Empty);
                MessageBox.Show(confirmation, "Aurora AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception exception)
            {
                MessageBox.Show("Unable to copy the text. " + exception.Message, "Aurora AI Assistant", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AppendActionLog(string type, string message)
        {
            _actionLog.Append('[').Append(DateTime.Now.ToString("HH:mm:ss")).Append("] ")
                .Append(type ?? "INFO").Append(" | ").AppendLine(message ?? string.Empty);
        }

        private static string FriendlyError(string message, AuroraAiProvider provider)
        {
            var value = message ?? string.Empty;
            if (provider == AuroraAiProvider.Ollama)
            {
                if (value.IndexOf("ollama", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("11434", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0)
                    return "Aurora cannot reach Ollama Local. Start Ollama and verify its endpoint at http://localhost:11434.";
            }
            if (value.IndexOf("401", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("api key", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Aurora could not authenticate with OpenAI Cloud. Check the OpenAI API key in the local proxy settings, then try again.";
            if (value.IndexOf("localhost", StringComparison.OrdinalIgnoreCase) >= 0 || value.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Aurora cannot reach the selected local provider. Check the provider status indicator and start the required local service.";
            return "Aurora could not complete that request. Review the prompt and active Revit document, then try again.\n\nDetails: " + value;
        }

        private void SetLoading(bool isLoading)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            ThinkingProgressBar.IsIndeterminate = isLoading;
            SendButton.IsEnabled = !isLoading;
            PromptTextBox.IsEnabled = !isLoading;
            CompactSendButton.IsEnabled = !isLoading;
            CompactPromptTextBox.IsEnabled = !isLoading;
            CompactStatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(isLoading ? "#F0B95A" : "#54D69A"));
            CompactStatusText.Text = isLoading ? "Working..." : "Ready";
        }

        private void ShowToast(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            ToastText.Text = text.Trim();
            ToastBorder.Visibility = Visibility.Visible;
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(8)
            };
            timer.Tick += (sender, args) =>
            {
                ToastBorder.Visibility = Visibility.Collapsed;
                timer.Stop();
            };
            timer.Start();
        }

        private void AddUserMessage(string text)
        {
            AddMessageBubble(text, true);
        }

        private TextBlock AddAssistantMessage(string text)
        {
            return AddMessageBubble(text, false);
        }

        private TextBlock AddAssistantMessageBubble()
        {
            return AddMessageBubble(string.Empty, false);
        }

        private TextBlock AddMessageBubble(string text, bool isUser)
        {
            var bubble = new Border
            {
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
                    isUser ? "#303A66" : "#1B2330")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(12, 9, 12, 9),
                Margin = new Thickness(isUser ? 34 : 0, 0, isUser ? 0 : 34, 10),
                HorizontalAlignment = isUser ? HorizontalAlignment.Right : HorizontalAlignment.Left,
                MaxWidth = 440
            };

            var messageText = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F4F7FB")),
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap
            };

            bubble.Child = messageText;
            ChatHistoryPanel.Children.Add(bubble);
            ChatScrollViewer.ScrollToEnd();
            return messageText;
        }

        private void AppendMessageText(TextBlock messageText, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Action append = () =>
            {
                messageText.Text += text;
                ChatScrollViewer.ScrollToEnd();
            };

            if (Dispatcher.CheckAccess())
            {
                append();
            }
            else
            {
                Dispatcher.Invoke(append);
            }
        }

        private void SetMessageText(TextBlock messageText, string text)
        {
            Action set = () =>
            {
                messageText.Text = text ?? string.Empty;
                ChatScrollViewer.ScrollToEnd();
            };

            if (Dispatcher.CheckAccess())
            {
                set();
            }
            else
            {
                Dispatcher.Invoke(set);
            }
        }
    }
}
