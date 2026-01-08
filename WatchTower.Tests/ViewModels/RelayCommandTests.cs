using Xunit;
using WatchTower.ViewModels;
using System;
using System.Windows.Input;

namespace WatchTower.Tests.ViewModels;

/// <summary>
/// Tests for RelayCommand and RelayCommand{T} implementations.
/// </summary>
public class RelayCommandTests
{
    #region RelayCommand (non-generic) Tests
    
    [Fact]
    public void Constructor_WithNullExecute_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }
    
    [Fact]
    public void Constructor_WithValidExecute_CreatesCommand()
    {
        // Arrange
        Action execute = () => { };
        
        // Act
        var command = new RelayCommand(execute);
        
        // Assert
        Assert.NotNull(command);
        Assert.IsAssignableFrom<ICommand>(command);
    }
    
    [Fact]
    public void Execute_WhenCalled_InvokesAction()
    {
        // Arrange
        var executed = false;
        var command = new RelayCommand(() => executed = true);
        
        // Act
        command.Execute(null);
        
        // Assert
        Assert.True(executed);
    }
    
    [Fact]
    public void CanExecute_WithNoCanExecuteDelegate_ReturnsTrue()
    {
        // Arrange
        var command = new RelayCommand(() => { });
        
        // Act
        var result = command.CanExecute(null);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void CanExecute_WithCanExecuteReturningTrue_ReturnsTrue()
    {
        // Arrange
        var command = new RelayCommand(() => { }, () => true);
        
        // Act
        var result = command.CanExecute(null);
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void CanExecute_WithCanExecuteReturningFalse_ReturnsFalse()
    {
        // Arrange
        var command = new RelayCommand(() => { }, () => false);
        
        // Act
        var result = command.CanExecute(null);
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void RaiseCanExecuteChanged_WhenCalled_RaisesEvent()
    {
        // Arrange
        var command = new RelayCommand(() => { });
        var eventRaised = false;
        
        command.CanExecuteChanged += (sender, args) => eventRaised = true;
        
        // Act
        command.RaiseCanExecuteChanged();
        
        // Assert
        Assert.True(eventRaised);
    }
    
    [Fact]
    public void Execute_MultipleTimes_InvokesActionEachTime()
    {
        // Arrange
        var executeCount = 0;
        var command = new RelayCommand(() => executeCount++);
        
        // Act
        command.Execute(null);
        command.Execute(null);
        command.Execute(null);
        
        // Assert
        Assert.Equal(3, executeCount);
    }
    
    #endregion
    
    #region RelayCommand<T> (generic) Tests
    
    [Fact]
    public void GenericConstructor_WithNullExecute_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RelayCommand<string>(null!));
    }
    
    [Fact]
    public void GenericConstructor_WithValidExecute_CreatesCommand()
    {
        // Arrange
        Action<string?> execute = (param) => { };
        
        // Act
        var command = new RelayCommand<string>(execute);
        
        // Assert
        Assert.NotNull(command);
        Assert.IsAssignableFrom<ICommand>(command);
    }
    
    [Fact]
    public void GenericExecute_WhenCalled_InvokesActionWithParameter()
    {
        // Arrange
        string? capturedParam = null;
        var command = new RelayCommand<string>(param => capturedParam = param);
        
        // Act
        command.Execute("test-value");
        
        // Assert
        Assert.Equal("test-value", capturedParam);
    }
    
    [Fact]
    public void GenericExecute_WithNullParameter_PassesNull()
    {
        // Arrange
        string? capturedParam = "initial";
        var command = new RelayCommand<string>(param => capturedParam = param);
        
        // Act
        command.Execute(null);
        
        // Assert
        Assert.Null(capturedParam);
    }
    
    [Fact]
    public void GenericCanExecute_WithNoCanExecuteDelegate_ReturnsTrue()
    {
        // Arrange
        var command = new RelayCommand<string>(param => { });
        
        // Act
        var result = command.CanExecute("test");
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void GenericCanExecute_WithCanExecuteReturningTrue_ReturnsTrue()
    {
        // Arrange
        var command = new RelayCommand<string>(param => { }, param => true);
        
        // Act
        var result = command.CanExecute("test");
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void GenericCanExecute_WithCanExecuteReturningFalse_ReturnsFalse()
    {
        // Arrange
        var command = new RelayCommand<string>(param => { }, param => false);
        
        // Act
        var result = command.CanExecute("test");
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void GenericCanExecute_WithParameterBasedLogic_EvaluatesCorrectly()
    {
        // Arrange
        var command = new RelayCommand<int>(
            param => { },
            param => param > 0);
        
        // Act & Assert
        Assert.True(command.CanExecute(5));
        Assert.False(command.CanExecute(0));
        Assert.False(command.CanExecute(-1));
    }
    
    [Fact]
    public void GenericRaiseCanExecuteChanged_WhenCalled_RaisesEvent()
    {
        // Arrange
        var command = new RelayCommand<string>(param => { });
        var eventRaised = false;
        
        command.CanExecuteChanged += (sender, args) => eventRaised = true;
        
        // Act
        command.RaiseCanExecuteChanged();
        
        // Assert
        Assert.True(eventRaised);
    }
    
    [Fact]
    public void GenericExecute_WithDifferentParameters_PassesCorrectValues()
    {
        // Arrange
        var capturedParams = new List<int?>();
        var command = new RelayCommand<int>(param => capturedParams.Add(param));
        
        // Act
        command.Execute(1);
        command.Execute(2);
        command.Execute(3);
        
        // Assert
        Assert.Equal(3, capturedParams.Count);
        Assert.Equal(1, capturedParams[0]);
        Assert.Equal(2, capturedParams[1]);
        Assert.Equal(3, capturedParams[2]);
    }
    
    [Fact]
    public void GenericCommand_WithComplexType_WorksCorrectly()
    {
        // Arrange
        var capturedObject = (object?)null;
        var testObject = new { Name = "Test", Value = 42 };
        var command = new RelayCommand<object>(param => capturedObject = param);
        
        // Act
        command.Execute(testObject);
        
        // Assert
        Assert.Same(testObject, capturedObject);
    }
    
    [Fact]
    public void GenericCanExecuteChanged_WithMultipleSubscribers_NotifiesAll()
    {
        // Arrange
        var command = new RelayCommand<string>(param => { });
        var subscriber1Notified = false;
        var subscriber2Notified = false;
        
        command.CanExecuteChanged += (sender, args) => subscriber1Notified = true;
        command.CanExecuteChanged += (sender, args) => subscriber2Notified = true;
        
        // Act
        command.RaiseCanExecuteChanged();
        
        // Assert
        Assert.True(subscriber1Notified);
        Assert.True(subscriber2Notified);
    }
    
    #endregion
}
