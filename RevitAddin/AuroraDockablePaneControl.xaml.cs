using System;
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
        private readonly RevitActionHandler _revitActionHandler;
        private CancellationTokenSource _streamCancellation;

        public AuroraDockablePaneControl()
        {
            InitializeComponent();
            _proxyClient = new AuroraProxyClient();
            _revitActionHandler = new RevitActionHandler();
            LoadExampleLibrary();
            AddAssistantMessage("Hello. I’m Aurora, your Revit AI Assistant. Ask me a question to test the local proxy connection.");
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
                ExampleComboBox.ItemsSource = RevitExampleLibrary.LoadAll();
            }
            catch (Exception exception)
            {
                ExampleComboBox.IsEnabled = false;
                ExampleComboBox.ToolTip = "Example Library could not be loaded: " + exception.Message;
            }
        }

        private void ExampleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedExample = ExampleComboBox.SelectedItem as RevitExample;
            if (selectedExample == null)
            {
                return;
            }

            PromptTextBox.Text = selectedExample.Prompt;
            PromptTextBox.Focus();
            PromptTextBox.CaretIndex = PromptTextBox.Text.Length;
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendPromptAsync();
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
            PromptTextBox.Clear();
            SetLoading(true);

            var assistantBubble = AddAssistantMessageBubble();
            var streamedJson = new StringBuilder();
            string streamError = null;
            _streamCancellation = new CancellationTokenSource(TimeSpan.FromMinutes(2));

            try
            {
                await _proxyClient.StreamQueryAsync(
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
                    SetMessageText(assistantBubble, streamError);
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
                SetMessageText(
                    assistantBubble,
                    "I couldn’t reach the streaming Aurora proxy. Start AiProxy on http://localhost:5000 and try again.\n\n" + exception.Message);
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
                if (action.IsSelectAction)
                {
                    var actionResult = await _revitActionHandler.RaiseAsync(action);
                    SetMessageText(assistantBubble, actionResult.Message);
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
                if (action.IsSelectAction)
                {
                    var actionResult = await _revitActionHandler.RaiseAsync(action);
                    AddAssistantMessage(actionResult.Message);
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
                    Clipboard.SetText(code);
                    MessageBox.Show("The generated C# execution was copied to the clipboard for review.", "Aurora AI Assistant", MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void SetLoading(bool isLoading)
        {
            LoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
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
