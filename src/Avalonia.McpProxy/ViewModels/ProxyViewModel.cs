using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    private readonly AppRegistry _registry;
    private string _logText = "";
    private string _statusText = "Starting...";
    private string _appStatusText = "Idle";
    private string? _screenshotBase64;
    private Bitmap? _screenshotImage;
    private string _registerAppName = "";
    private string _registerAppPath = "";
    private string _registerAppArgs = "";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<AppGroupViewModel> RegisteredApps { get; } = new();

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

    public bool HasRegisteredApps => RegisteredApps.Count > 0;

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

    public string RegisterAppName
    {
        get => _registerAppName;
        set { _registerAppName = value; OnPropertyChanged(); }
    }

    public string RegisterAppPath
    {
        get => _registerAppPath;
        set { _registerAppPath = value; OnPropertyChanged(); }
    }

    public string RegisterAppArgs
    {
        get => _registerAppArgs;
        set { _registerAppArgs = value; OnPropertyChanged(); }
    }

    public ICommand ClearLogsCommand { get; }
    public ICommand RegisterAppCommand { get; }

    public ProxyViewModel(AppRegistry registry)
    {
        _registry = registry;
        _connectionManager = new AppConnectionManager(Log, OnAppConnected, OnAppDisconnected, registry);

        ClearLogsCommand = new RelayCommand(() =>
        {
            _logBuilder.Clear();
            LogText = "";
        });

        RegisterAppCommand = new RelayCommand(RegisterApp);

        // Load registered apps into UI on startup
        LoadRegisteredApps();
    }

    /// <summary>
    /// Access the ProcessManager through the MCP bridge for process queries.
    /// </summary>
    internal ProcessManager? ProcessManager => _connectionManager.McpBridge?.ProcessManager;

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

    private void RegisterApp()
    {
        var name = RegisterAppName?.Trim();
        var path = RegisterAppPath?.Trim();

        if (string.IsNullOrEmpty(name))
        {
            Log("App name is required");
            return;
        }

        if (string.IsNullOrEmpty(path))
        {
            Log("App path is required");
            return;
        }

        var args = RegisterAppArgs?.Trim();
        _registry.Register(name, path, string.IsNullOrEmpty(args) ? null : args, null);

        // Refresh UI
        LoadRegisteredApps();

        // Clear inputs
        RegisterAppName = "";
        RegisterAppPath = "";
        RegisterAppArgs = "";

        AppStatusText = $"Registered: {name}";
    }

    public async Task StartAppAsync(string appName)
    {
        var app = _registry.GetApp(appName);
        if (app == null)
        {
            Log($"App '{appName}' not found in registry");
            return;
        }

        var pm = ProcessManager;
        if (pm == null)
        {
            Log("ProcessManager not available");
            return;
        }

        Log($"Starting {appName}...");
        AppStatusText = $"Starting {appName}...";

        try
        {
            var result = await pm.LaunchRegisteredAppAsync(app, CancellationToken.None);
            Log($"Start result: {result}");
            AppStatusText = $"Started: {appName}";

            // Refresh process list in UI
            RefreshAppInstances(appName);
        }
        catch (Exception ex)
        {
            Log($"Failed to start {appName}: {ex.Message}");
            AppStatusText = $"Start failed: {appName}";
        }
    }

    public void UnregisterApp(string appName)
    {
        _registry.Unregister(appName);
        LoadRegisteredApps();
        AppStatusText = $"Unregistered: {appName}";
    }

    public void StopProcessInstance(int pid, string appName)
    {
        var pm = ProcessManager;
        if (pm == null) return;

        pm.StopProcess(pid);
        _connectionManager.DisconnectApp(appName);
        Log($"Stopped PID {pid} ({appName})");

        RefreshAppInstances(appName);
    }

    public void StopAllForApp(string appName)
    {
        var pm = ProcessManager;
        if (pm == null) return;

        var killed = pm.StopAllForApp(appName);
        _connectionManager.DisconnectApp(appName);
        Log($"Stopped {killed} process(es) for {appName}");

        RefreshAppInstances(appName);
    }

    public async Task TakeScreenshotOfInstanceAsync(string appName)
    {
        try
        {
            Log($"Capturing screenshot from {appName}...");
            AppStatusText = "Capturing screenshot...";

            var (success, data, error) = await _connectionManager.InvokeToolAsync(
                appName, $"{appName}:CaptureScreenshot", default, CancellationToken.None);

            if (!success || data == null)
            {
                Log($"Screenshot failed: {error ?? "no data returned"}");
                AppStatusText = "Screenshot failed";
                return;
            }

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

            var bytes = Convert.FromBase64String(base64Data);
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ScreenshotImage = bitmap;
            });

            Log($"Screenshot captured from {appName} ({bytes.Length:N0} bytes)");
            AppStatusText = "Screenshot captured";
        }
        catch (Exception ex)
        {
            Log($"Screenshot error: {ex.Message}");
            AppStatusText = "Screenshot error";
        }
    }

    private void LoadRegisteredApps()
    {
        Dispatcher.UIThread.Post(() =>
        {
            RegisteredApps.Clear();

            foreach (var app in _registry.GetApps())
            {
                var group = CreateAppGroup(app);
                RegisteredApps.Add(group);
            }

            OnPropertyChanged(nameof(HasRegisteredApps));
        });
    }

    private AppGroupViewModel CreateAppGroup(RegisteredApp app)
    {
        var group = new AppGroupViewModel
        {
            Name = app.Name,
            Path = $"{app.Path} {app.Args}".Trim(),
            IsRegistered = true,
            StartCommand = new AsyncRelayCommand(async () => await StartAppAsync(app.Name)),
            UnregisterCommand = new RelayCommand(() => UnregisterApp(app.Name))
        };

        // Populate instances from process manager
        var pm = ProcessManager;
        if (pm != null)
        {
            var connectedApps = _connectionManager.GetConnectedApps().ToList();
            var connected = connectedApps.FirstOrDefault(c =>
                c.Name.Equals(app.Name, StringComparison.OrdinalIgnoreCase));
            var toolCount = connected.Tools?.Count ?? 0;
            var isConnected = connected.Name != null;

            foreach (var proc in pm.GetProcessesForApp(app.Name))
            {
                group.Instances.Add(new ProcessInstanceViewModel
                {
                    Pid = proc.Pid,
                    AppName = app.Name,
                    IsRunning = proc.IsRunning,
                    IsConnected = isConnected,
                    ToolCount = toolCount,
                    StatusColor = isConnected ? Brushes.LimeGreen : (proc.IsRunning ? Brushes.Orange : Brushes.Gray),
                    StopCommand = new RelayCommand(() => StopProcessInstance(proc.Pid, app.Name)),
                    ScreenshotCommand = new AsyncRelayCommand(async () => await TakeScreenshotOfInstanceAsync(app.Name))
                });
            }
        }

        return group;
    }

    private void RefreshAppInstances(string appName)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var existing = RegisteredApps.FirstOrDefault(a =>
                a.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var idx = RegisteredApps.IndexOf(existing);
                var app = _registry.GetApp(appName);
                if (app != null)
                {
                    RegisteredApps[idx] = CreateAppGroup(app);
                }
            }
            else
            {
                // App might have been added by the MCP tool - reload all
                LoadRegisteredApps();
            }
        });
    }

    private void OnAppConnected(string appName, int toolCount)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Log($"App connected: {appName} ({toolCount} tools)");

            var existing = RegisteredApps.FirstOrDefault(a =>
                a.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                // Refresh the group to pick up connection state
                var idx = RegisteredApps.IndexOf(existing);
                var app = _registry.GetApp(appName);
                if (app != null)
                {
                    RegisteredApps[idx] = CreateAppGroup(app);
                }
                else
                {
                    // App connected but not in registry (ad-hoc) - add a transient group
                    UpdateOrAddTransientGroup(appName, toolCount, isConnected: true);
                }
            }
            else
            {
                // App connected but not in registry - add transient entry
                UpdateOrAddTransientGroup(appName, toolCount, isConnected: true);
            }

            OnPropertyChanged(nameof(HasRegisteredApps));
        });
    }

    private void OnAppDisconnected(string appName)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Log($"App disconnected: {appName}");

            var existing = RegisteredApps.FirstOrDefault(a =>
                a.Name.Equals(appName, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                var app = _registry.GetApp(appName);
                if (app != null)
                {
                    var idx = RegisteredApps.IndexOf(existing);
                    RegisteredApps[idx] = CreateAppGroup(app);
                }
                else
                {
                    // Transient group - update instances to disconnected
                    foreach (var inst in existing.Instances)
                    {
                        inst.IsConnected = false;
                        inst.StatusColor = inst.IsRunning ? Brushes.Orange : Brushes.Gray;
                        inst.ToolCount = 0;
                    }
                }
            }

            OnPropertyChanged(nameof(HasRegisteredApps));
        });
    }

    private void UpdateOrAddTransientGroup(string appName, int toolCount, bool isConnected)
    {
        var group = new AppGroupViewModel
        {
            Name = appName,
            Path = "(not registered - connected ad-hoc)",
            IsRegistered = false,
            StartCommand = new RelayCommand(() => { }),
            UnregisterCommand = new RelayCommand(() => { })
        };

        // Add a virtual instance for the connection
        group.Instances.Add(new ProcessInstanceViewModel
        {
            Pid = 0,
            AppName = appName,
            IsRunning = true,
            IsConnected = isConnected,
            ToolCount = toolCount,
            StatusColor = isConnected ? Brushes.LimeGreen : Brushes.Gray,
            StopCommand = new RelayCommand(() => StopAllForApp(appName)),
            ScreenshotCommand = new AsyncRelayCommand(async () => await TakeScreenshotOfInstanceAsync(appName))
        });

        RegisteredApps.Add(group);
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

public class AppGroupViewModel : INotifyPropertyChanged
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public bool IsRegistered { get; init; }
    public ObservableCollection<ProcessInstanceViewModel> Instances { get; } = new();
    public bool HasRunningInstances => Instances.Any(i => i.IsRunning);

    public ICommand StartCommand { get; init; } = null!;
    public ICommand UnregisterCommand { get; init; } = null!;

    public event PropertyChangedEventHandler? PropertyChanged;
}

public class ProcessInstanceViewModel : INotifyPropertyChanged
{
    private bool _isRunning;
    private bool _isConnected;
    private int _toolCount;
    private IBrush _statusColor = Brushes.Gray;

    public int Pid { get; init; }
    public string AppName { get; init; } = "";

    public bool IsRunning
    {
        get => _isRunning;
        set { _isRunning = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning))); }
    }

    public bool IsConnected
    {
        get => _isConnected;
        set { _isConnected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected))); }
    }

    public int ToolCount
    {
        get => _toolCount;
        set { _toolCount = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ToolCount))); }
    }

    public IBrush StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor))); }
    }

    public ICommand StopCommand { get; init; } = null!;
    public ICommand ScreenshotCommand { get; init; } = null!;

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
