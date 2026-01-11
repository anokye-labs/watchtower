namespace WatchTower.Models;

/// <summary>
/// Represents the progress of a download operation.
/// </summary>
/// <param name="BytesReceived">Number of bytes received so far.</param>
/// <param name="TotalBytes">Total number of bytes to download (0 if unknown).</param>
/// <param name="PercentComplete">Percentage of download complete (0-100).</param>
/// <param name="BytesPerSecond">Current download speed in bytes per second.</param>
public record DownloadProgress(
    long BytesReceived,
    long TotalBytes,
    double PercentComplete,
    double BytesPerSecond);
