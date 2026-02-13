using Avalonia.McpProxy.ViewModels;
using Xunit;

namespace Avalonia.McpProxy.Tests.ViewModels;

/// <summary>
/// Tests for ProxyViewModel and related view model types.
/// Note: ProxyViewModel.Start() requires Avalonia Dispatcher and stdio,
/// so these tests focus on construction, commands, and model behavior.
/// </summary>
public class ProxyViewModelTests
{
    [Fact]
    public void ProxyViewModel_CanBeCreated()
    {
        var vm = new ProxyViewModel();

        Assert.NotNull(vm);
        Assert.Equal("Starting...", vm.StatusText);
        Assert.Empty(vm.ConnectedApps);
        Assert.False(vm.HasConnectedApps);
    }

    [Fact]
    public void ProxyViewModel_LogText_StartsEmpty()
    {
        var vm = new ProxyViewModel();

        Assert.Equal("", vm.LogText);
    }

    [Fact]
    public void ProxyViewModel_ClearLogsCommand_IsNotNull()
    {
        var vm = new ProxyViewModel();

        Assert.NotNull(vm.ClearLogsCommand);
        Assert.True(vm.ClearLogsCommand.CanExecute(null));
    }

    [Fact]
    public void ProxyViewModel_ReconnectCommand_IsNotNull()
    {
        var vm = new ProxyViewModel();

        Assert.NotNull(vm.ReconnectCommand);
        Assert.True(vm.ReconnectCommand.CanExecute(null));
    }

    [Fact]
    public void ProxyViewModel_ClearLogsCommand_ClearsLogText()
    {
        var vm = new ProxyViewModel();

        vm.ClearLogsCommand.Execute(null);

        Assert.Equal("", vm.LogText);
    }
}

/// <summary>
/// Tests for ConnectedAppInfo model.
/// </summary>
public class ConnectedAppInfoTests
{
    [Fact]
    public void ConnectedAppInfo_StoresProperties()
    {
        var info = new ConnectedAppInfo
        {
            Name = "WatchTower",
            Port = 5100,
            ToolCount = 6
        };

        Assert.Equal("WatchTower", info.Name);
        Assert.Equal(5100, info.Port);
        Assert.Equal(6, info.ToolCount);
    }

    [Fact]
    public void ConnectedAppInfo_DefaultValues()
    {
        var info = new ConnectedAppInfo();

        Assert.Equal("", info.Name);
        Assert.Equal(0, info.Port);
        Assert.Equal(0, info.ToolCount);
    }

    [Fact]
    public void ConnectedAppInfo_StatusColor_RaisesPropertyChanged()
    {
        var info = new ConnectedAppInfo();
        var propertyChanged = false;

        info.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ConnectedAppInfo.StatusColor))
                propertyChanged = true;
        };

        info.StatusColor = Avalonia.Media.Brushes.LimeGreen;

        Assert.True(propertyChanged);
    }
}

/// <summary>
/// Tests for RelayCommand.
/// </summary>
public class RelayCommandTests
{
    [Fact]
    public void RelayCommand_ExecutesAction()
    {
        var executed = false;
        var cmd = new RelayCommand(() => executed = true);

        cmd.Execute(null);

        Assert.True(executed);
    }

    [Fact]
    public void RelayCommand_CanExecute_AlwaysTrue()
    {
        var cmd = new RelayCommand(() => { });

        Assert.True(cmd.CanExecute(null));
        Assert.True(cmd.CanExecute("anything"));
    }
}
