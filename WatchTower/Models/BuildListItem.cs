using System;
using System.ComponentModel;

namespace WatchTower.Models;

/// <summary>
/// Display model for build list items in the Developer Menu.
/// </summary>
public class BuildListItem : INotifyPropertyChanged
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required BuildType Type { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required string Author { get; init; }
    public required string DownloadUrl { get; init; }
    
    private bool _isCached;
    public bool IsCached
    {
        get => _isCached;
        set
        {
            if (_isCached == value) return;
            _isCached = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsCached)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Status)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StatusColor)));
        }
    }
    
    public string Status => IsCached ? "Cached" : "Available";
    public string TypeIcon => Type == BuildType.Release ? "📦" : "🔧";
    public string StatusColor => IsCached ? "#4AFF4A" : "#AAFFFFFF";
    
    public event PropertyChangedEventHandler? PropertyChanged;
}
