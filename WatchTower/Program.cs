using Avalonia;
using System;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WatchTower;

class Program
{
    // Shared state for MCP integration
    private static NetworkStream? _mcpStream;
    private static readonly object _mcpLock = new();

    // Event for notifying the UI about notifications
    public static event Action<string>? OnNotificationRequested;
    public static event Action<string>? OnSpeakRequested;
    public static event Func<string>? OnGetVoiceStatus;

    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            Console.WriteLine("[WatchTower] Application starting...");
            Console.WriteLine($"[WatchTower] Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

            // Start MCP connection immediately, before Avalonia
            StartMcpConnection();

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
    /// Start MCP connection to proxy immediately (fire and forget).
    /// </summary>
    private static void StartMcpConnection()
    {
        _ = Task.Run(async () =>
        {
            try
            {
                Console.WriteLine("[MCP] Starting connection to proxy...");
                await Task.Delay(200);

                using var client = new TcpClient();
                await client.ConnectAsync("localhost", 5100);
                Console.WriteLine($"[MCP] Connected to proxy at {client.Client.RemoteEndPoint}");

                var stream = client.GetStream();
                _mcpStream = stream;

                // Register WatchTower with its tools
                var registration = JsonSerializer.Serialize(new
                {
                    type = "register",
                    appName = "WatchTower",
                    tools = new object[]
                    {
                        new {
                            name = "show_notification",
                            description = "Show a notification overlay in WatchTower",
                            inputSchema = new {
                                type = "object",
                                properties = new {
                                    message = new { type = "string", description = "The message to display" },
                                    title = new { type = "string", description = "Optional title for the notification" },
                                    duration_seconds = new { type = "integer", description = "How long to show (default 5)" }
                                },
                                required = new[] { "message" }
                            }
                        },
                        new {
                            name = "speak_text",
                            description = "Speak text aloud using text-to-speech",
                            inputSchema = new {
                                type = "object",
                                properties = new {
                                    text = new { type = "string", description = "The text to speak" }
                                },
                                required = new[] { "text" }
                            }
                        },
                        new {
                            name = "get_voice_status",
                            description = "Get the current voice recognition status and settings",
                            inputSchema = new {
                                type = "object",
                                properties = new { }
                            }
                        }
                    }
                });

                await SendMessageAsync(stream, registration);
                Console.WriteLine("[MCP] Registration sent to proxy");

                // Handle incoming messages
                var buffer = new byte[16384];
                var messageBuffer = new StringBuilder();

                while (client.Connected)
                {
                    var read = await stream.ReadAsync(buffer);
                    if (read == 0) break;

                    var data = Encoding.UTF8.GetString(buffer, 0, read);
                    messageBuffer.Append(data);

                    // Process complete messages (newline-delimited)
                    var content = messageBuffer.ToString();
                    var messages = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    if (content.EndsWith('\n'))
                    {
                        foreach (var msg in messages)
                        {
                            await HandleIncomingMessageAsync(stream, msg.Trim());
                        }
                        messageBuffer.Clear();
                    }
                    else if (messages.Length > 1)
                    {
                        // Process all complete messages except the last incomplete one
                        for (int i = 0; i < messages.Length - 1; i++)
                        {
                            await HandleIncomingMessageAsync(stream, messages[i].Trim());
                        }
                        messageBuffer.Clear();
                        messageBuffer.Append(messages[^1]);
                    }
                }

                Console.WriteLine("[MCP] Connection closed");
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"[MCP] Proxy not available: {ex.Message}");
                Console.WriteLine("[MCP] WatchTower will run without MCP integration");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MCP] Connection error: {ex.Message}");
                Console.WriteLine($"[MCP] Stack: {ex.StackTrace}");
            }
            finally
            {
                _mcpStream = null;
            }
        });
    }

    private static async Task HandleIncomingMessageAsync(NetworkStream stream, string message)
    {
        try
        {
            Console.WriteLine($"[MCP] Received: {message.Substring(0, Math.Min(150, message.Length))}...");

            var json = JsonDocument.Parse(message);
            var root = json.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var type = typeElement.GetString();

            if (type == "toolInvocation")
            {
                var correlationId = root.GetProperty("correlationId").GetInt64();
                var toolName = root.GetProperty("tool").GetString()!;
                var parameters = root.TryGetProperty("parameters", out var paramsEl) ? paramsEl : default;

                Console.WriteLine($"[MCP] Tool invocation: {toolName} (correlation: {correlationId})");

                // Handle the tool call
                var result = await HandleToolCallAsync(toolName, parameters);

                // Send response
                var response = JsonSerializer.Serialize(new
                {
                    type = "toolResponse",
                    correlationId = correlationId,
                    result = result
                });

                await SendMessageAsync(stream, response);
                Console.WriteLine($"[MCP] Sent response for {toolName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MCP] Error handling message: {ex.Message}");
        }
    }

    private static async Task<object> HandleToolCallAsync(string toolName, JsonElement parameters)
    {
        try
        {
            return toolName switch
            {
                "show_notification" => await HandleShowNotificationAsync(parameters),
                "speak_text" => await HandleSpeakTextAsync(parameters),
                "get_voice_status" => HandleGetVoiceStatus(),
                _ => CreateErrorResult($"Unknown tool: {toolName}")
            };
        }
        catch (Exception ex)
        {
            return CreateErrorResult($"Tool execution failed: {ex.Message}");
        }
    }

    private static async Task<object> HandleShowNotificationAsync(JsonElement parameters)
    {
        var message = parameters.TryGetProperty("message", out var msgEl) ? msgEl.GetString() : null;
        var title = parameters.TryGetProperty("title", out var titleEl) ? titleEl.GetString() : "WatchTower";
        var duration = parameters.TryGetProperty("duration_seconds", out var durEl) ? durEl.GetInt32() : 5;

        if (string.IsNullOrEmpty(message))
        {
            return CreateErrorResult("Missing required parameter: message");
        }

        Console.WriteLine($"[MCP] Showing notification: {title} - {message}");

        // Trigger the notification in the UI
        OnNotificationRequested?.Invoke($"{title}: {message}");

        // For now, just log it - the UI integration will handle actual display
        await Task.CompletedTask;

        return CreateSuccessResult($"Notification displayed: \"{message}\" (duration: {duration}s)");
    }

    private static async Task<object> HandleSpeakTextAsync(JsonElement parameters)
    {
        var text = parameters.TryGetProperty("text", out var textEl) ? textEl.GetString() : null;

        if (string.IsNullOrEmpty(text))
        {
            return CreateErrorResult("Missing required parameter: text");
        }

        Console.WriteLine($"[MCP] Speaking text: {text}");

        // Trigger TTS in the UI
        OnSpeakRequested?.Invoke(text);

        await Task.CompletedTask;

        return CreateSuccessResult($"Speaking: \"{text}\"");
    }

    private static object HandleGetVoiceStatus()
    {
        Console.WriteLine("[MCP] Getting voice status");

        // Try to get status from the UI
        var status = OnGetVoiceStatus?.Invoke();

        if (status != null)
        {
            return CreateSuccessResult(status);
        }

        // Default status if UI hasn't registered a handler yet
        return CreateSuccessResult(JsonSerializer.Serialize(new
        {
            initialized = false,
            listening = false,
            mode = "unknown",
            message = "Voice service status not yet available - app may still be initializing"
        }));
    }

    private static object CreateSuccessResult(string text)
    {
        return new
        {
            success = true,
            data = text,
            error = (string?)null
        };
    }

    private static object CreateErrorResult(string error)
    {
        return new
        {
            success = false,
            data = (object?)null,
            error = error
        };
    }

    private static async Task SendMessageAsync(NetworkStream stream, string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message + "\n");
        lock (_mcpLock)
        {
            stream.WriteAsync(bytes).AsTask().Wait();
            stream.FlushAsync().Wait();
        }
        await Task.CompletedTask;
    }
}
