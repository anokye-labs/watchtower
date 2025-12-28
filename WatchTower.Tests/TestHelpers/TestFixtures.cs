namespace WatchTower.Tests.TestHelpers;

/// <summary>
/// Provides test fixtures and common test setup utilities.
/// </summary>
public static class TestFixtures
{
    /// <summary>
    /// Creates a test configuration with default values.
    /// </summary>
    public static Dictionary<string, string?> CreateTestConfiguration()
    {
        return new Dictionary<string, string?>
        {
            ["Logging:LogLevel"] = "normal"
        };
    }
    
    // Add more test fixture helpers as needed
}
