namespace WatchTower.Models;

/// <summary>
/// Represents the type of build artifact.
/// </summary>
public enum BuildType
{
    /// <summary>A published release build.</summary>
    Release,
    
    /// <summary>A build from a pull request workflow.</summary>
    PullRequest
}
