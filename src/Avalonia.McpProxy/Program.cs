using Avalonia;
using System;

namespace Avalonia.McpProxy;

class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.Error.WriteLine("[Proxy] MCP Proxy starting...");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[ERROR] Proxy failed to start: {ex.Message}");
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
