using Avalonia;
using System;

namespace WatchTower;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("[WatchTower] Application starting...");
            Console.WriteLine($"[WatchTower] Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex) when (ex.Message.Contains("net10.0") || ex.Message.Contains(".NET 10"))
        {
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

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
