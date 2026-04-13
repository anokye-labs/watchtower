using Avalonia.McpProxy.Services;
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
    private static AppRegistry CreateTestRegistry() =>
        new AppRegistry(_ => { });

    [Fact]
    public void ProxyViewModel_CanBeCreated()
    {
        var vm = new ProxyViewModel(CreateTestRegistry());

        Assert.NotNull(vm);
        Assert.Equal("Starting...", vm.StatusText);
        Assert.Empty(vm.RegisteredApps);
        Assert.False(vm.HasRegisteredApps);
    }

    [Fact]
    public void ProxyViewModel_LogText_StartsEmpty()
    {
        var vm = new ProxyViewModel(CreateTestRegistry());

        Assert.Equal("", vm.LogText);
    }

    [Fact]
    public void ProxyViewModel_ClearLogsCommand_IsNotNull()
    {
        var vm = new ProxyViewModel(CreateTestRegistry());

        Assert.NotNull(vm.ClearLogsCommand);
        Assert.True(vm.ClearLogsCommand.CanExecute(null));
    }

    [Fact]
    public void ProxyViewModel_RegisterAppCommand_IsNotNull()
    {
        var vm = new ProxyViewModel(CreateTestRegistry());

        Assert.NotNull(vm.RegisterAppCommand);
        Assert.True(vm.RegisterAppCommand.CanExecute(null));
    }

    [Fact]
    public void ProxyViewModel_ClearLogsCommand_ClearsLogText()
    {
        var vm = new ProxyViewModel(CreateTestRegistry());

        vm.ClearLogsCommand.Execute(null);

        Assert.Equal("", vm.LogText);
    }
}

/// <summary>
/// Tests for AppGroupViewModel model.
/// </summary>
public class AppGroupViewModelTests
{
    [Fact]
    public void AppGroupViewModel_StoresProperties()
    {
        var group = new AppGroupViewModel
        {
            Name = "WatchTower",
            Path = "/path/to/app",
            IsRegistered = true,
            StartCommand = new RelayCommand(() => { }),
            UnregisterCommand = new RelayCommand(() => { })
        };

        Assert.Equal("WatchTower", group.Name);
        Assert.Equal("/path/to/app", group.Path);
        Assert.True(group.IsRegistered);
        Assert.Empty(group.Instances);
        Assert.False(group.HasRunningInstances);
    }
}

/// <summary>
/// Tests for ProcessInstanceViewModel model.
/// </summary>
public class ProcessInstanceViewModelTests
{
    [Fact]
    public void ProcessInstanceViewModel_DefaultValues()
    {
        var instance = new ProcessInstanceViewModel
        {
            Pid = 0,
            AppName = "",
            StopCommand = new RelayCommand(() => { }),
            ScreenshotCommand = new RelayCommand(() => { })
        };

        Assert.Equal(0, instance.Pid);
        Assert.Equal("", instance.AppName);
        Assert.False(instance.IsRunning);
        Assert.False(instance.IsConnected);
        Assert.Equal(0, instance.ToolCount);
    }

    [Fact]
    public void ProcessInstanceViewModel_StatusColor_RaisesPropertyChanged()
    {
        var instance = new ProcessInstanceViewModel
        {
            Pid = 0,
            AppName = "",
            StopCommand = new RelayCommand(() => { }),
            ScreenshotCommand = new RelayCommand(() => { })
        };
        var propertyChanged = false;

        instance.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ProcessInstanceViewModel.StatusColor))
                propertyChanged = true;
        };

        instance.StatusColor = Avalonia.Media.Brushes.LimeGreen;

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
