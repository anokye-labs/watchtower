using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Windows.Input;
using Avalonia.McpProxy.Services;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.McpProxy.ViewModels;

public class ProxyViewModel : INotifyPropertyChanged
{
    private readonly StringBuilder _logBuilder = new();
    private readonly AppConnectionManager _connectionManager;
    private string _logText = "";
    private string _statusText = "Starting...";

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
    
    public bool HasConnectedApps => ConnectedApps.Count > 0;

    public ICommand ClearLogsCommand { get; }
    public ICommand ReconnectCommand { get; }

    public ProxyViewModel()
    {
        _connectionManager = new AppConnectionManager(Log, OnAppConnected, OnAppDisconnected);
        
        ClearLogsCommand = new RelayCommand(() =>
        {
            _logBuilder.Clear();
            LogText = "";
        });
        
        ReconnectCommand = new RelayCommand(ReconnectAll);
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
        Log("Reconnecting to all known apps...");
        _connectionManager.ReconnectAll();
    }

    private void OnAppConnected(string appName, int port, int toolCount)
    {
        Dispatcher.UIThread.Post(() =>
        {
            // Remove existing entry if any
            for (int i = ConnectedApps.Count - 1; i >= 0; i--)
            {
                if (ConnectedApps[i].Port == port)
                    ConnectedApps.RemoveAt(i);
            }
            
            ConnectedApps.Add(new ConnectedAppInfo
            {
                Name = appName,
                Port = port,
                ToolCount = toolCount,
                StatusColor = Brushes.LimeGreen
            });
            
            OnPropertyChanged(nameof(HasConnectedApps));
        });
    }

    private void OnAppDisconnected(int port)
    {
        Dispatcher.UIThread.Post(() =>
        {
            for (int i = ConnectedApps.Count - 1; i >= 0; i--)
            {
                if (ConnectedApps[i].Port == port)
                {
                    ConnectedApps[i].StatusColor = Brushes.Gray;
                    // Keep it in list but grayed out, or remove:
                    // ConnectedApps.RemoveAt(i);
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
    
    public string Name { get; init; } = "";
    public int Port { get; init; }
    public int ToolCount { get; init; }
    
    public IBrush StatusColor
    {
        get => _statusColor;
        set { _statusColor = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor))); }
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
