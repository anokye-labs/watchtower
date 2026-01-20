using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Meziantou.Framework.Win32;
using Microsoft.Extensions.Logging;

namespace WatchTower.Services;

/// <summary>
/// Windows Credential Manager implementation for secure token storage.
/// </summary>
[SupportedOSPlatform("windows5.1.2600")]
public class CredentialStorageService : ICredentialStorageService
{
    private readonly ILogger<CredentialStorageService> _logger;
    private const string CredentialPrefix = "WatchTower";
    private const string DefaultUserName = "token";

    public CredentialStorageService(ILogger<CredentialStorageService> logger)
    {
        _logger = logger;
    }

    public Task<string?> GetTokenAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        try
        {
            var targetName = FormatTargetName(key);
            var credential = CredentialManager.ReadCredential(targetName);
            
            if (credential == null)
            {
                _logger.LogDebug("Credential not found for key {Key}", key);
                return Task.FromResult<string?>(null);
            }

            _logger.LogDebug("Retrieved credential for key {Key}", key);
            return Task.FromResult<string?>(credential.Password);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read credential for key {Key}", key);
            return Task.FromResult<string?>(null);
        }
    }

    public Task StoreTokenAsync(string key, string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        
        try
        {
            var targetName = FormatTargetName(key);
            
            // Note: LocalMachine persistence is used per specification to ensure credentials
            // survive system reboots. While "LocalMachine" may sound global, Windows Credential
            // Manager still scopes these credentials to the current user account. The name refers
            // to persistence behavior (survives logoff/reboot) not access scope.
            CredentialManager.WriteCredential(
                applicationName: targetName,
                userName: DefaultUserName,
                secret: token,
                persistence: CredentialPersistence.LocalMachine);

            _logger.LogInformation("Stored credential for key {Key}", key);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store credential for key {Key}", key);
            throw new InvalidOperationException($"Failed to store credential: {ex.Message}", ex);
        }
    }

    public Task DeleteTokenAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        try
        {
            var targetName = FormatTargetName(key);
            CredentialManager.DeleteCredential(targetName);
            _logger.LogInformation("Deleted credential for key {Key}", key);
        }
        catch (Exception ex) when (IsCredentialNotFoundError(ex))
        {
            // Treat "not found" as success - credential is already gone
            _logger.LogDebug(ex, "Credential not found or already deleted for key {Key}", key);
        }
        catch (Exception ex)
        {
            // Rethrow other errors (access denied, corrupted store, OS errors)
            // so callers know the credential may still exist
            _logger.LogError(ex, "Failed to delete credential for key {Key}", key);
            throw new InvalidOperationException($"Failed to delete credential: {ex.Message}", ex);
        }
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// Determines if the exception indicates the credential was not found.
    /// </summary>
    private static bool IsCredentialNotFoundError(Exception ex)
    {
        // Windows Credential Manager throws exceptions with specific messages/HResults
        // when a credential is not found. Check for common patterns.
        var message = ex.Message?.ToLowerInvariant() ?? string.Empty;
        return message.Contains("not found") || 
               message.Contains("element not found") ||
               message.Contains("does not exist") ||
               (ex is System.ComponentModel.Win32Exception win32Ex && win32Ex.NativeErrorCode == 1168); // ERROR_NOT_FOUND
    }

    public Task<bool> HasTokenAsync(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        
        try
        {
            var targetName = FormatTargetName(key);
            var credential = CredentialManager.ReadCredential(targetName);
            return Task.FromResult(credential != null);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string FormatTargetName(string key) => $"{CredentialPrefix}:{key}";
}
