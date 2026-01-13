using System;

namespace WatchTower.Models;

/// <summary>
/// Represents a pull request build artifact with its metadata and download information.
/// </summary>
public record PullRequestBuildInfo(
    int PullRequestNumber,
    string Title,
    string BranchName,
    string Author,
    DateTimeOffset CreatedAt,
    long ArtifactId,
    string ArtifactDownloadUrl,
    long ArtifactSize);
