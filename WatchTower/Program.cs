using Avalonia;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;

namespace WatchTower;

/// <summary>
/// Application entry point with comprehensive error handling and startup logging.
/// This class initializes the Avalonia UI framework and manages the application lifetime.
/// </summary>
class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        var startupTimer = Stopwatch.StartNew();
        
        try
        {
            // Log application startup milestone
            Console.WriteLine("[WatchTower] Application starting...");
            Console.WriteLine($"[WatchTower] Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            Console.WriteLine($"[WatchTower] Platform: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
            Console.WriteLine($"[WatchTower] Working Directory: {Directory.GetCurrentDirectory()}");
            
            // Initialize and start Avalonia application
            // This will:
            // 1. Configure the Avalonia framework (BuildAvaloniaApp)
            // 2. Detect the current platform (Windows/macOS/Linux)
            // 3. Initialize the application class (App.Initialize)
            // 4. Create and show the main window (App.OnFrameworkInitializationCompleted)
            // 5. Start the UI event loop (StartWithClassicDesktopLifetime)
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            
            startupTimer.Stop();
            Console.WriteLine($"[WatchTower] Application started in {startupTimer.ElapsedMilliseconds}ms");
        }
        catch (Exception ex) when (ex.Message.Contains("net10.0") || ex.Message.Contains(".NET 10"))
        {
            // Handle missing .NET 10 runtime with diagnostic information
            // This provides clear guidance for the 0.01% failure case (FR-008b)
            Console.Error.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.Error.WriteLine("[FATAL ERROR] .NET 10 Runtime Not Found");
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════");
            Console.Error.WriteLine("\nThe application requires .NET 10 to run.");
            Console.Error.WriteLine("\nResolution Options:");
            Console.Error.WriteLine("  1. Install .NET 10 SDK from: https://dotnet.microsoft.com/download");
            Console.Error.WriteLine("  2. Use a self-contained build that includes the runtime");
            Console.Error.WriteLine($"\nTechnical Details: {ex.Message}");
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════\n");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            // Handle any other startup failures with detailed diagnostics (FR-026, FR-008b)
            Console.Error.WriteLine("\n═══════════════════════════════════════════════════════════");
            Console.Error.WriteLine($"[FATAL ERROR] Application Failed to Start");
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════");
            Console.Error.WriteLine($"\nError: {ex.Message}");
            Console.Error.WriteLine($"\nLocation: {ex.Source}");
            Console.Error.WriteLine("\nStack Trace:");
            Console.Error.WriteLine(ex.StackTrace);
            
            // Check for common issues and provide guidance
            if (ex.Message.Contains("FileNotFoundException") || ex.Message.Contains("appsettings.json"))
            {
                Console.Error.WriteLine("\n⚠️  Possible Cause: Missing configuration file (appsettings.json)");
                Console.Error.WriteLine("   Ensure appsettings.json is in the application directory.");
            }
            else if (ex.Message.Contains("DllNotFoundException"))
            {
                Console.Error.WriteLine("\n⚠️  Possible Cause: Missing system dependencies");
                Console.Error.WriteLine("   Install required runtime libraries for your platform.");
            }
            
            Console.Error.WriteLine("═══════════════════════════════════════════════════════════\n");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Configures the Avalonia application builder with platform detection and fonts.
    /// This method is also used by the Avalonia visual designer in VS Code.
    /// </summary>
    /// <returns>Configured AppBuilder ready to start the application</returns>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()        // Automatically detect Windows/macOS/Linux
            .WithInterFont()            // Use Inter font family for modern UI
            .LogToTrace();              // Enable diagnostic logging to Debug output
}
