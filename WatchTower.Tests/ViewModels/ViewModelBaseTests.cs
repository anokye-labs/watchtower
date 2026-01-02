using Xunit;
using WatchTower.ViewModels;
using WatchTower.Tests.TestHelpers;
using System.ComponentModel;

namespace WatchTower.Tests.ViewModels;

/// <summary>
/// Tests for ViewModelBase to ensure proper INotifyPropertyChanged implementation.
/// </summary>
public class ViewModelBaseTests
{
    private class TestViewModel : ViewModelBase
    {
        private string _testProperty = string.Empty;
        private int _testNumber;
        
        public string TestProperty
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value);
        }
        
        public int TestNumber
        {
            get => _testNumber;
            set => SetProperty(ref _testNumber, value);
        }
        
        public void CallOnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName);
        }
    }
    
    [Fact]
    public void PropertyChanged_WhenPropertySet_IsRaised()
    {
        // Arrange
        var vm = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;
        
        vm.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = args.PropertyName;
        };
        
        // Act
        vm.TestProperty = "NewValue";
        
        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal(nameof(TestViewModel.TestProperty), changedPropertyName);
        Assert.Equal("NewValue", vm.TestProperty);
    }
    
    [Fact]
    public void PropertyChanged_WhenSameValueSet_IsNotRaised()
    {
        // Arrange
        var vm = new TestViewModel();
        vm.TestProperty = "InitialValue";
        
        var changeCount = 0;
        vm.PropertyChanged += (sender, args) => changeCount++;
        
        // Act
        vm.TestProperty = "InitialValue";
        
        // Assert
        Assert.Equal(0, changeCount);
    }
    
    [Fact]
    public void PropertyChanged_WhenDifferentValueSet_IsRaised()
    {
        // Arrange
        var vm = new TestViewModel();
        vm.TestProperty = "InitialValue";
        
        var changeCount = 0;
        vm.PropertyChanged += (sender, args) => changeCount++;
        
        // Act
        vm.TestProperty = "NewValue";
        
        // Assert
        Assert.Equal(1, changeCount);
    }
    
    [Fact]
    public void SetProperty_WhenValueChanges_ReturnsTrue()
    {
        // Arrange
        var vm = new TestViewModel();
        vm.TestProperty = "InitialValue";
        
        // Act
        var result = TestUtilities.WasPropertyChangedRaised(vm, nameof(TestViewModel.TestProperty), () =>
        {
            vm.TestProperty = "NewValue";
        });
        
        // Assert
        Assert.True(result);
    }
    
    [Fact]
    public void SetProperty_WhenValueSame_ReturnsFalse()
    {
        // Arrange
        var vm = new TestViewModel();
        vm.TestProperty = "SameValue";
        
        // Act
        var result = TestUtilities.WasPropertyChangedRaised(vm, nameof(TestViewModel.TestProperty), () =>
        {
            vm.TestProperty = "SameValue";
        });
        
        // Assert
        Assert.False(result);
    }
    
    [Fact]
    public void OnPropertyChanged_WhenCalledManually_RaisesEvent()
    {
        // Arrange
        var vm = new TestViewModel();
        var propertyChangedRaised = false;
        string? changedPropertyName = null;
        
        vm.PropertyChanged += (sender, args) =>
        {
            propertyChangedRaised = true;
            changedPropertyName = args.PropertyName;
        };
        
        // Act
        vm.CallOnPropertyChanged("ManualProperty");
        
        // Assert
        Assert.True(propertyChangedRaised);
        Assert.Equal("ManualProperty", changedPropertyName);
    }
    
    [Fact]
    public void PropertyChanged_WithMultipleProperties_RaisesCorrectEvents()
    {
        // Arrange
        var vm = new TestViewModel();
        var changes = new List<string?>();
        
        vm.PropertyChanged += (sender, args) => changes.Add(args.PropertyName);
        
        // Act
        vm.TestProperty = "Value1";
        vm.TestNumber = 42;
        vm.TestProperty = "Value2";
        
        // Assert
        Assert.Equal(3, changes.Count);
        Assert.Equal(nameof(TestViewModel.TestProperty), changes[0]);
        Assert.Equal(nameof(TestViewModel.TestNumber), changes[1]);
        Assert.Equal(nameof(TestViewModel.TestProperty), changes[2]);
    }
    
    [Fact]
    public void SetProperty_WithValueTypes_WorksCorrectly()
    {
        // Arrange
        var vm = new TestViewModel();
        var changeCount = 0;
        
        vm.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(TestViewModel.TestNumber))
                changeCount++;
        };
        
        // Act
        vm.TestNumber = 10;
        vm.TestNumber = 10; // Same value
        vm.TestNumber = 20; // Different value
        
        // Assert
        Assert.Equal(2, changeCount); // Should raise only for 10 and 20, not for duplicate
        Assert.Equal(20, vm.TestNumber);
    }
    
    [Fact]
    public void ViewModelBase_ImplementsINotifyPropertyChanged()
    {
        // Arrange & Act
        var vm = new TestViewModel();
        
        // Assert
        Assert.IsAssignableFrom<INotifyPropertyChanged>(vm);
    }
}
