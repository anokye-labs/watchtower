using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Avalonia.McpProxy.Services;

/// <summary>
/// Persistent registry of apps. Stores to %LOCALAPPDATA%/AvaloniaProxy/registry.json.
/// Registration is separate from launching - an app can be registered once and started many times.
/// </summary>
public class AppRegistry
{
    private readonly string _registryPath;
    private readonly Action<string> _log;
    private List<RegisteredApp> _apps = new();

    public AppRegistry(Action<string> log)
    {
        _log = log;
        _registryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AvaloniaProxy", "registry.json");
        Load();
    }

    public IReadOnlyList<RegisteredApp> GetApps() => _apps.AsReadOnly();

    public RegisteredApp? GetApp(string name) =>
        _apps.FirstOrDefault(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    public bool Register(string name, string path, string? args, string? workingDirectory)
    {
        // Remove existing with same name
        _apps.RemoveAll(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        _apps.Add(new RegisteredApp
        {
            Name = name,
            Path = path,
            Args = args ?? "",
            WorkingDirectory = workingDirectory ?? "",
            RegisteredAt = DateTime.UtcNow
        });
        Save();
        _log($"Registered app: {name}");
        return true;
    }

    public bool Unregister(string name)
    {
        var removed = _apps.RemoveAll(a => a.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (removed > 0)
        {
            Save();
            _log($"Unregistered app: {name}");
            return true;
        }
        return false;
    }

    private void Load()
    {
        try
        {
            if (File.Exists(_registryPath))
            {
                var json = File.ReadAllText(_registryPath);
                _apps = JsonSerializer.Deserialize<List<RegisteredApp>>(json) ?? new();
                _log($"Loaded {_apps.Count} registered apps");
            }
        }
        catch (Exception ex) { _log($"Error loading registry: {ex.Message}"); }
    }

    private void Save()
    {
        try
        {
            var dir = Path.GetDirectoryName(_registryPath)!;
            Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(_apps, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_registryPath, json);
        }
        catch (Exception ex) { _log($"Error saving registry: {ex.Message}"); }
    }
}

public class RegisteredApp
{
    public string Name { get; set; } = "";
    public string Path { get; set; } = "";
    public string Args { get; set; } = "";
    public string WorkingDirectory { get; set; } = "";
    public DateTime RegisteredAt { get; set; }
}
