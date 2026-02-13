using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Windows.Input;
using Avalonia.McpProxy.Services;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;

namespace Avalonia.McpProxy.ViewModels;

public class ProxyViewModel : INotifyPropertyChanged
{
    private readonly StringBuilder _logBuilder = new();
    private readonly AppConnectionManager _connectionManager;
    private string _logText = "";
    private string _statusText = "Starting...";
    private string _appStatusText = "Idle";
    private ConnectedAppInfo? _selectedApp;
    private string? _screenshotBase64;
    private Bitmap? _screenshotImage;
    private string _launchAppPath = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ConnectedAppInfo> ConnectedApps { get; } = new();
    
    public string LogText
    {
        get => _logText;
        private set { _logText = value; OnPropertyChanged(); }
    }
    
    public string StatusText
    {
        get => _statusText;
        private set { _statusText = value; OnPropertyChanged(); }
    }

    public string AppStatusText
    {
        get => _appStatusText;
        private set { _appStatusText = value; OnPropertyChanged(); }
    }
    
    public bool HasConnectedApps => ConnectedApps.Count > 0;

    public ConnectedAppInfo? SelectedApp
    {
        get => _selectedApp;
        set { _selectedApp = value; OnPropertyChanged(); }
    }

    public string? ScreenshotBase64
    {
        get => _screenshotBase64;
        private set { _screenshotBase64 = value; OnPropertyChanged(); }
    }

    public Bitmap? ScreenshotImage
    {
        get => _screenshotImage;
        private set { _screenshotImage = value; OnPropertyChanged(); }
    }

    public string LaunchAppPath
    {
        get => _launchAppPath;
        set { _launchAppPath = value; OnPropertyChanged(); }
    }

    public ICommand ClearLogsCommand { get; }
    public ICommand ReconnectCommand { get; }
    public ICommand LaunchAppCommand { get; }
    public ICommand StopAppCommand { get; }
    public ICommand TakeScreenshotCommand { get; }

    public ProxyViewModel()
    {
        _connectionManager = new AppConnectionManager(Log, OnAppConnected, OnAppDisconnected);
        
        ClearLogsCommand = new RelayCommand(() =>
        {
            _logBuilder.Clear();
            LogText = "";
        });
        
        ReconnectCommand = new RelayCommand(ReconnectAll);
        LaunchAppCommand = new RelayCommand(LaunchApp);
        StopAppCommand = new AsyncRelayCommand(StopSelectedAppAsync);
        TakeScreenshotCommand = new AsyncRelayCommand(TakeScreenshotAsync);
    }

    public void Start()
    {
        Log("MCP Proxy starting...");
        _connectionManager.Start();
        StatusText = "Running";
        Log("Proxy ready. Waiting for apps to register...");
    }

    public void Stop()
    {
        Log("Shutting down...");
        _connectionManager.Stop();
        StatusText = "Stopped";
    }

    public void ReconnectAll()
    {
        Log("Apps reconnect automatically - no manual reconnect needed");
    }

    private void LaunchApp()
    {
        var path = LaunchAppPath?.Trim();
        if (string.IsNullOrEmpty(path))
        {
            Log("No app path specified");
            return;
        }

        if (!File.Exists(path))
        {
            Log($"File not found: {path}");
            return;
        }

        try
        {
            Log($"Launching app: {path}");
            AppStatusText = "Launching...";
            var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });

            if (process != null)
            {
                Log($"Launched PID {process.Id}, waiting for app to connect...");
                AppStatusText = "Launched, waiting for connection...";
            }
        }
        catch (Exception ex)
        {
            Log($"Failed to launch app: {ex.Message}");
            AppStatusText = "Launch failed";
        }
    }

    private async Task StopSelectedAppAsync()
    {
        var app = SelectedApp;
        if (app == null)
        {
            Log("No app selected");
            return;
        }

        try
        {
            Log($"Sending shutdown to {app.Name}...");
            AppStatusText = $"Stopping {app.Name}...";
            var (success, data, error) = await _connectionManager.InvokeToolAsync(
                app.Name, "__shutdown__", default, CancellationToken.None);

            if (success)
            {
                Log($"Shutdown sent to {app.Name}");
                AppStatusText = $"{app.Name} stopped";
            }
            else
            {
                Log($"Shutdown failed for {app.Name}: {error}");
                AppStatusText = $"Stop failed: {error}";
            }
        }
        catch (Exception ex)
        {
            Log($"Error stopping {app.Name}: {ex.Message}");
            AppStatusText = "Stop failed";
        }
    }

    private async Task TakeScreenshotAsync()
    {
        var app = SelectedApp;
        if (app == null)
        {
            Log("No app selected for screenshot");
            return;
        }

        try
        {
            Log($"Capturing screenshot from {app.Name}...");
            AppStatusText = "Capturing screenshot...";

            var (success, data, error) = await _connectionManager.InvokeToolAsync(
                app.Name, "CaptureScreenshot", default, CancellationToken.None);

            if (!success || data == null)
            {
                Log($"Screenshot failed: {error ?? "no data returned"}");
                AppStatusText = "Screenshot failed";
                return;
            }

            // Parse the JSON response to extract base64 data
            var json = JsonDocument.Parse(data);
            var base64Data = json.RootElement.TryGetProperty("base64Data", out var b64El)
                ? b64El.GetString()
                : null;

            if (string.IsNullOrEmpty(base64Data))
            {
                Log("Screenshot returned no image data");
                AppStatusText = "Screenshot: no data";
                return;
            }

            ScreenshotBase64 = base64Data;

            // Decode base64 to Bitmap for display
            var bytes = Convert.FromBase64String(base64Data);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ScreenshotImage = bitmap;
            });

            Log($"Screenshot captured from {app.Name} ({bytes.Length:N0} bytes)");
            AppStatusText = "Screenshot captured";
        }
        catch (Exception ex)
        {
            Log($"Screenshot error: {ex.Message}");
            AppStatusText = "Screenshot error";
        }
    }

    /// <summary>
    /// Take a screenshot of a specific app by port (called from UI per-app buttons).
    /// </summary>
    public async Task TakeScreenshotOfAppAsync(ConnectedAppInfo app)
    {
        SelectedApp = app;
        await TakeScreenshotAsync();
    }

    /// <summary>
    /// Stop a specific app by port (called from UI per-app buttons).
    /// </summary>
    public async Task StopAppAsync(ConnectedAppInfo app)
    {
        SelectedApp = app;
        await StopSelectedAppAsync();
    }

    private void OnAppConnected(string appName, int toolCount)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Remove existing entry if any
            for (int i = ConnectedApps.Count - 1; i >= 0; i--)
            {
                if (string.Equals(ConnectedApps[i].Name, appName, StringComparison.OrdinalIgnoreCase))
                    ConnectedApps.RemoveAt(i);
            }
            
            ConnectedApps.Add(new ConnectedAppInfo
            {
                Name = appName,
                ToolCount = toolCount,
                StatusColor = Brushes.LimeGreen
            });
            
            OnPropertyChanged(nameof(HasConnectedApps));
        });
    }

    private void OnAppDisconnected(string appName)
    {
        Dispatcher.UIThread.Post(() =>
        {
            for (int i = ConnectedApps.Count - 1; i >= 0; i--)
            {
                if (string.Equals(ConnectedApps[i].Name, appName, StringComparison.OrdinalIgnoreCase))
                {
                    ConnectedApps[i].StatusColor = Brushes.Gray;
                }
            }
            OnPropertyChanged(nameof(HasConnectedApps));
        });
    }

    private readonly object _logLock = new();

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        var line = $"[{timestamp}] {message}\n";

        string snapshot;
        lock (_logLock)
        {
            _logBuilder.Append(line);

            // Keep log size reasonable
            if (_logBuilder.Length > 50000)
            {
                _logBuilder.Remove(0, 10000);
            }

            snapshot = _logBuilder.ToString();
        }
        
        Dispatcher.UIThread.Post(() =>
        {
            LogText = snapshot;
        });
        
        Console.Error.WriteLine(message);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

public class ConnectedAppInfo : INotifyPropertyChanged
{
    private IBrush _statusColor = Brushes.Gray;
    private bool _isSelected;
    
    public string Name { get; init; } = "";
    public int ToolCount { get; init; }
    
    public IBrush StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor))); }
    }

    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class RelayCommand : ICommand
{
    private readonly Action _execute;
    
    public RelayCommand(Action execute) => _execute = execute;
    
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _execute();
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<Task> _execute;
    private bool _isExecuting;

    public AsyncRelayCommand(Func<Task> execute) => _execute = execute;

    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => !_isExecuting;

    public async void Execute(object? parameter)
    {
        if (_isExecuting) return;
        
        _isExecuting = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        
        try
        {
            await _execute();
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
