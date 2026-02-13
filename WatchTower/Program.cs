using Avalonia;
using Avalonia.Mcp.Core.Diagnostics;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace WatchTower;

class Program
{
    // Diagnostic listener for proxy connections
    private static DiagnosticListener? _diagnosticListener;

    // Event for notifying the UI about notifications
    public static event Action<string>? OnNotificationRequested;
    public static event Action<string>? OnSpeakRequested;
    public static event Func<string>? OnGetVoiceStatus;
    public static event Func<Task<string?>>? OnCaptureScreenshot;
    
    /// <summary>
    /// The port WatchTower is listening on for diagnostic connections.
    /// </summary>
    public static int DiagnosticPort => _diagnosticListener?.Port ?? 0;

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("[WatchTower] Application starting...");
            Console.WriteLine($"[WatchTower] Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            // Start diagnostic listener for proxy connections
            StartDiagnosticListener();

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

    /// <summary>
    /// Start diagnostic listener for proxy connections.
    /// The app listens; proxy connects. Proxy can reconnect after restarts.
    /// </summary>
    private static void StartDiagnosticListener()
    {
        var tools = new[]
        {
            new DiagnosticTool
            {
                Name = "show_notification",
                Description = "Show a notification overlay in WatchTower",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        message = new { type = "string", description = "The message to display" },
                        title = new { type = "string", description = "Optional title for the notification" },
                        duration_seconds = new { type = "integer", description = "How long to show (default 5)" }
                    },
                    required = new[] { "message" }
                }
            },
            new DiagnosticTool
            {
                Name = "speak_text",
                Description = "Speak text aloud using text-to-speech",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        text = new { type = "string", description = "The text to speak" }
                    },
                    required = new[] { "text" }
                }
            },
            new DiagnosticTool
            {
                Name = "get_voice_status",
                Description = "Get the current voice recognition status and settings",
                InputSchema = new
                {
                    type = "object",
                    properties = new { }
                }
            },
            new DiagnosticTool
            {
                Name = "capture_screenshot",
                Description = "Capture a screenshot of the WatchTower application window",
                InputSchema = new
                {
                    type = "object",
                    properties = new
                    {
                        format = new { type = "string", description = "Image format (png)", @default = "png" }
                    }
                }
            }
        };

        _diagnosticListener = new DiagnosticListener("WatchTower", tools);
        _diagnosticListener.OnToolInvoked += HandleToolCallAsync;
        
        // Start listening - port is written to stdout for proxy to read
        var port = _diagnosticListener.Start();
        
        // Output port in a format the proxy can parse
        Console.WriteLine($"DIAGNOSTIC_PORT:{port}");
    }

    private static async Task<DiagnosticResult> HandleToolCallAsync(string toolName, JsonElement parameters)
    {
        try
        {
            return toolName switch
            {
                "show_notification" => await HandleShowNotificationAsync(parameters),
                "speak_text" => await HandleSpeakTextAsync(parameters),
                "get_voice_status" => HandleGetVoiceStatus(),
                "capture_screenshot" => await HandleCaptureScreenshotAsync(parameters),
                _ => DiagnosticResult.Fail($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            return DiagnosticResult.Fail($"Tool execution failed: {ex.Message}");
        }
    }

    private static Task<DiagnosticResult> HandleShowNotificationAsync(JsonElement parameters)
    {
        var message = parameters.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
        var title = parameters.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : "WatchTower";
        var duration = parameters.TryGetProperty("duration_seconds", out var durEl) ? durEl.GetInt32() : 5;

        if (string.IsNullOrEmpty(message))
        {
            return Task.FromResult(DiagnosticResult.Fail("Missing required parameter: message"));
        }

        Console.WriteLine($"[Diagnostics] Showing notification: {title} - {message}");

        // Trigger the notification in the UI
        OnNotificationRequested?.Invoke($"{title}: {message}");

        return Task.FromResult(DiagnosticResult.Ok($"Notification displayed: \"{message}\" (duration: {duration}s)"));
    }

    private static Task<DiagnosticResult> HandleSpeakTextAsync(JsonElement parameters)
    {
        var text = parameters.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;

        if (string.IsNullOrEmpty(text))
        {
            return Task.FromResult(DiagnosticResult.Fail("Missing required parameter: text"));
        }

        Console.WriteLine($"[Diagnostics] Speaking text: {text}");

        // Trigger TTS in the UI
        OnSpeakRequested?.Invoke(text);

        return Task.FromResult(DiagnosticResult.Ok($"Speaking: \"{text}\""));
    }

    private static DiagnosticResult HandleGetVoiceStatus()
    {
        Console.WriteLine("[Diagnostics] Getting voice status");

        // Try to get status from the UI
        var status = OnGetVoiceStatus?.Invoke();

        if (status != null)
        {
            return DiagnosticResult.Ok(status);
        }

        // Default status if UI hasn't registered a handler yet
        return DiagnosticResult.Ok(JsonSerializer.Serialize(new
        {
            initialized = false,
            listening = false,
            mode = "unknown",
            message = "Voice service status not yet available - app may still be initializing"
        }));
    }

    private static async Task<DiagnosticResult> HandleCaptureScreenshotAsync(JsonElement parameters)
    {
        Console.WriteLine("[Diagnostics] Capturing screenshot");

        var handler = OnCaptureScreenshot;
        if (handler == null)
        {
            return DiagnosticResult.Fail("Screenshot handler not registered - app may still be initializing");
        }

        try
        {
            var base64Data = await handler();

            if (base64Data == null)
            {
                return DiagnosticResult.Fail("No window available for screenshot capture");
            }

            var result = JsonSerializer.Serialize(new
            {
                success = true,
                format = "png",
                base64Data
            });

            return DiagnosticResult.Ok(result);
        }
        catch (Exception ex)
        {
            return DiagnosticResult.Fail($"Screenshot capture failed: {ex.Message}");
        }
    }
}
