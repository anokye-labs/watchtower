using System;
using System.Text.Json.Serialization;

namespace WatchTower.Models;

/// <summary>
/// Represents metadata for a cached build.
/// </summary>
/// <param name="BuildId">Unique identifier for the build (e.g., "release-v1.0.0" or "pr-123").</param>
/// <param name="DisplayName">Human-readable name for the build (e.g., "v1.0.0" or "PR #123").</param>
/// <param name="LocalPath">Relative path to the build folder within the cache directory.</param>
/// <param name="DownloadedAt">Timestamp when the build was downloaded.</param>
/// <param name="SizeBytes">Size of the build in bytes.</param>
/// <param name="Type">Type of build (Release or PullRequest).</param>
public record BuildInfo(
    [property: JsonPropertyName("buildId")] string BuildId,
    [property: JsonPropertyName("displayName")] string DisplayName,
    [property: JsonPropertyName("localPath")] string LocalPath,
    [property: JsonPropertyName("downloadedAt")] DateTimeOffset DownloadedAt,
    [property: JsonPropertyName("sizeBytes")] long SizeBytes,
    [property: JsonPropertyName("type")] BuildType Type);
