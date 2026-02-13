using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Avalonia.Mcp.Core.Extensions;
using Avalonia.Mcp.Core.Handlers;

namespace McpTestApp;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var services = new ServiceCollection();
            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));
            
            var proxyEndpoint = Environment.GetEnvironmentVariable("MCP_PROXY_ENDPOINT") ?? "tcp://localhost:5100";
            
            services.AddMcpHandler(config =>
            {
                config.ApplicationName = "McpTestApp";
                config.ProxyEndpoint = proxyEndpoint;
                config.AutoConnect = true;
                config.HeadlessMode = false;
            }, registerStandardTools: true);

            _serviceProvider = services.BuildServiceProvider();
            
            // Resolve handler to trigger auto-connect
            var handler = _serviceProvider.GetService<IMcpHandler>();

            desktop.MainWindow = new MainWindow();
            
            desktop.ShutdownRequested += (_, _) =>
            {
                _serviceProvider?.Dispose();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
