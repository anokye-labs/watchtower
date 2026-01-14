using System.Threading.Tasks;

namespace WatchTower.Services;

/// <summary>
/// Service for securely storing and retrieving credentials.
/// </summary>
public interface ICredentialStorageService
{
    /// <summary>
    /// Stores a token securely for the specified service.
    /// </summary>
    Task StoreTokenAsync(string serviceName, string token);
    
    /// <summary>
    /// Retrieves a stored token for the specified service.
    /// </summary>
    /// <returns>The token if found, or null if not found.</returns>
    Task<string?> GetTokenAsync(string serviceName);
    
    /// <summary>
    /// Deletes a stored token for the specified service.
    /// </summary>
    Task DeleteTokenAsync(string serviceName);
}
