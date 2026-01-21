using System.Threading.Tasks;

namespace WatchTower.Services;

/// <summary>
/// Service for secure credential storage using Windows Credential Manager.
/// Credentials are stored in the current user's credential vault and persist across sessions.
/// </summary>
public interface ICredentialStorageService
{
    /// <summary>
    /// Retrieves a stored credential value.
    /// </summary>
    /// <param name="key">Unique identifier (e.g., "github").</param>
    /// <returns>The credential value, or null if not found.</returns>
    Task<string?> GetTokenAsync(string key);

    /// <summary>
    /// Stores a credential securely in Windows Credential Manager.
    /// </summary>
    /// <param name="key">Unique identifier for the credential.</param>
    /// <param name="token">The secret value to store. Never logged.</param>
    Task StoreTokenAsync(string key, string token);

    /// <summary>
    /// Deletes a stored credential.
    /// </summary>
    /// <param name="key">The credential key.</param>
    Task DeleteTokenAsync(string key);

    /// <summary>
    /// Checks if a credential exists without retrieving its value.
    /// </summary>
    /// <param name="key">The credential key.</param>
    Task<bool> HasTokenAsync(string key);
}
