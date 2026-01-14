using System;

namespace WatchTower.Models;

/// <summary>
/// PLACEHOLDER model for build information.
/// This is a minimal stub to allow the View to compile.
/// The full implementation is tracked in issue #218.
/// </summary>
public class BuildInfo
{
    /// <summary>
    /// Icon representing the build type (📦 for release, 🔧 for PR build).
    /// </summary>
    public string TypeIcon { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the build (version number or PR title).
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Date when the build was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Status of the build ("Available", "Cached", etc.).
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Color to display the status in (Green for Cached, Gray for Available).
    /// </summary>
    public string StatusColor { get; set; } = "#AAFFFFFF";
}
