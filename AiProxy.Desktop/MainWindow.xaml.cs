using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace AuroraRevit.AiProxy.Desktop;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _healthTimer;
    private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(2) };
    private Process? _proxyProcess;
    private string _proxyUrl = "http://localhost:5000";
    private bool _isClosing;

    public MainWindow()
    {
        InitializeComponent();
        _healthTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _healthTimer.Tick += async (_, _) => await RefreshHealthAsync();
        Loaded += async (_, _) =>
        {
            StartProxy();
            await RefreshHealthAsync();
        };
        Closing += (_, _) =>
        {
            _isClosing = true;
            StopProxy();
        };
    }

    public void StartProxy()
    {
        if (_proxyProcess is { HasExited: false })
        {
            AppendLog("Proxy process is already running.");
            return;
        }

        var proxyPath = ResolveProxyPath();
        if (proxyPath is null)
        {
            SetStatus("Unavailable", false);
            AppendLog("AiProxy.exe was not found. Build or publish AiProxy before starting the GUI.");
            return;
        }

        _proxyUrl = ResolveStartUrl();
        EndpointText.Text = _proxyUrl;

        var startInfo = new ProcessStartInfo
        {
            FileName = proxyPath,
            WorkingDirectory = Path.GetDirectoryName(proxyPath)!,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = $"--urls {_proxyUrl}"
        };

        try
        {
            _proxyProcess = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            _proxyProcess.OutputDataReceived += (_, eventArgs) => AppendLog(eventArgs.Data);
            _proxyProcess.ErrorDataReceived += (_, eventArgs) => AppendLog(eventArgs.Data, true);
            _proxyProcess.Exited += (_, _) => Dispatcher.Invoke(() =>
            {
                if (!_isClosing)
                {
                    SetStatus("Stopped", false);
                    AppendLog("AiProxy exited.");
                    _healthTimer.Stop();
                }
            });
            _proxyProcess.Start();
            _proxyProcess.BeginOutputReadLine();
            _proxyProcess.BeginErrorReadLine();
            _healthTimer.Start();
            AppendLog($"Started AiProxy at {_proxyUrl}.");
        }
        catch (Exception exception)
        {
            SetStatus("Start failed", false);
            AppendLog("Could not start AiProxy: " + exception.Message, true);
        }
    }

    public void StopProxy()
    {
        _healthTimer?.Stop();
        if (_proxyProcess is null)
        {
            return;
        }

        try
        {
            if (!_proxyProcess.HasExited)
            {
                _proxyProcess.Kill(entireProcessTree: true);
                _proxyProcess.WaitForExit(2000);
            }

            AppendLog("AiProxy stopped.");
        }
        catch (Exception exception)
        {
            AppendLog("Could not stop AiProxy: " + exception.Message, true);
        }
        finally
        {
            _proxyProcess.Dispose();
            _proxyProcess = null;
            SetStatus("Stopped", false);
        }
    }

    private async Task RefreshHealthAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync(_proxyUrl + "/health");
            if (response.IsSuccessStatusCode)
            {
                SetStatus("Running", true);
            }
            else
            {
                SetStatus("Unhealthy", false);
            }
        }
        catch
        {
            if (_proxyProcess is { HasExited: false })
            {
                SetStatus("Starting", false);
            }
            else
            {
                SetStatus("Stopped", false);
            }
        }
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        StartProxy();
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        StopProxy();
    }

    private void SetStatus(string status, bool running)
    {
        StatusText.Text = status;
        StatusDot.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(
            running ? "#54D69A" : status == "Starting" ? "#E0B068" : "#E35D6A"));
        StartButton.IsEnabled = !running;
        StopButton.IsEnabled = running || _proxyProcess is { HasExited: false };
        EndpointText.Text = _proxyUrl;
    }

    private void AppendLog(string? message, bool isError = false)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        void Append()
        {
            LogTextBlock.Text += $"[{DateTime.Now:HH:mm:ss}] {(isError ? "ERROR " : string.Empty)}{message}{Environment.NewLine}";
            LogScrollViewer.ScrollToEnd();
        }

        if (Dispatcher.CheckAccess())
        {
            Append();
        }
        else
        {
            Dispatcher.BeginInvoke(DispatcherPriority.Background, (Action)Append);
        }
    }

    private static string ResolveStartUrl()
    {
        if (IsPortAvailable(5000))
        {
            return "http://localhost:5000";
        }

        if (IsPortAvailable(5001))
        {
            return "http://localhost:5001";
        }

        throw new InvalidOperationException("Ports 5000 and 5001 are both occupied.");
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static string? ResolveProxyPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "AiProxy.exe"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "AiProxy", "AiProxy.exe"))
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
}
