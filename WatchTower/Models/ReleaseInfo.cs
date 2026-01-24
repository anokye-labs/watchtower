using System;

namespace WatchTower.Models;

/// <summary>
/// Represents a GitHub release with its metadata and download information.
/// </summary>
public record ReleaseInfo(
    string TagName,
    string Name,
    DateTimeOffset CreatedAt,
    string AssetDownloadUrl,
    long AssetSize,
    bool IsPrerelease);
