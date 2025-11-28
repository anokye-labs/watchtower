using Avalonia;
using Avalonia.Win32;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WatchTower;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Log application startup
            Console.WriteLine("[WatchTower] Application starting...");
            Console.WriteLine($"[WatchTower] Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
            
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (ex.Message.Contains("net10.0") || ex.Message.Contains(".NET 10"))
        {
            // Handle missing .NET 10 runtime
            Console.Error.WriteLine("[ERROR] .NET 10 runtime not found.");
            Console.Error.WriteLine("Please install .NET 10 SDK or use a self-contained build.");
            Console.Error.WriteLine($"Details: {ex.Message}");
            Environment.Exit(1);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Application failed to start: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
