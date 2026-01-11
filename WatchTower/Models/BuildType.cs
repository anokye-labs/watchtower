namespace WatchTower.Models;

/// <summary>
/// Represents the type of build cached locally.
/// </summary>
public enum BuildType
{
    /// <summary>
    /// Official release build.
    /// </summary>
    Release,

    /// <summary>
    /// Pull request preview build.
    /// </summary>
    PullRequest
}
