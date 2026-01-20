using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;
using WatchTower.ViewModels;

namespace WatchTower.Views;

/// <summary>
/// Modal dialog window for the Developer Build Menu.
/// Displays available builds, handles authentication, and manages download/launch operations.
/// This is the first modal dialog in the WatchTower codebase.
/// </summary>
public partial class DevBuildMenuWindow : Window
{
    private DevBuildMenuViewModel? _viewModel;
    private TextBox? _tokenInputBox;
    private ListBox? _buildsListBox;

    public DevBuildMenuWindow()
    {
        InitializeComponent();

        // Subscribe to events
        DataContextChanged += OnDataContextChanged;
        KeyDown += OnKeyDown;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Get references to controls for focus management
        _tokenInputBox = this.FindControl<TextBox>("TokenInputBox");
        _buildsListBox = this.FindControl<ListBox>("BuildsListBox");
    }

    private void OnUnloaded(object? sender, RoutedEventArgs e)
    {
        // Cleanup subscriptions to avoid memory leaks
        DataContextChanged -= OnDataContextChanged;
        KeyDown -= OnKeyDown;
        Loaded -= OnLoaded;
        Unloaded -= OnUnloaded;

        // Detach from ViewModel
        if (_viewModel != null)
        {
            _viewModel.RequestClose -= OnRequestClose;
            _viewModel.RequestTokenInput -= OnRequestTokenInput;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel = null;
        }
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous ViewModel
        if (_viewModel != null)
        {
            _viewModel.RequestClose -= OnRequestClose;
            _viewModel.RequestTokenInput -= OnRequestTokenInput;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // Subscribe to new ViewModel
        if (DataContext is DevBuildMenuViewModel viewModel)
        {
            _viewModel = viewModel;
            _viewModel.RequestClose += OnRequestClose;
            _viewModel.RequestTokenInput += OnRequestTokenInput;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
        else
        {
            // Clear stale reference when DataContext is not a DevBuildMenuViewModel
            _viewModel = null;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Auto-focus token input box when it becomes visible
        if (e.PropertyName == nameof(DevBuildMenuViewModel.ShowTokenInput))
        {
            if (_viewModel?.ShowTokenInput == true && _tokenInputBox != null)
            {
                // Capture the control reference before posting to prevent NullReferenceException
                // if the window is closed/unloaded before the deferred action executes
                var tokenBox = _tokenInputBox;
                
                // Small delay to ensure UI has updated before focusing
                Dispatcher.UIThread.Post(() =>
                {
                    tokenBox?.Focus();
                    tokenBox?.SelectAll();
                }, DispatcherPriority.Background);
            }
        }
    }

    private void OnRequestClose()
    {
        Close();
    }

    private Task<string?> OnRequestTokenInput(string prompt)
    {
        // This method is a placeholder for future extensibility.
        // In this implementation, we use an inline TextBox (ShowTokenInput property)
        // that appears directly in the window UI, eliminating the need for a separate dialog.
        // The ViewModel manages token input through the TokenInput property and commands.
        // 
        // This event handler exists to support alternative input methods in the future,
        // such as a separate modal dialog or integration with a credential manager.
        // For now, it returns null as the inline TextBox handles all token input.
        return Task.FromResult<string?>(null);
    }

    private void OnBuildDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Code-behind is used here for simplicity to handle the DoubleTapped event.
        // When a build is double-clicked, trigger the launch command if available.
        if (_viewModel?.LaunchBuildCommand?.CanExecute(null) == true)
        {
            _viewModel.LaunchBuildCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        // Escape key closes the dialog
        if (e.Key == Key.Escape)
        {
            // If token input is showing, cancel it first
            if (_viewModel.ShowTokenInput)
            {
                _viewModel.CancelTokenInputCommand?.Execute(null);
            }
            else
            {
                _viewModel.CancelCommand?.Execute(null);
            }
            e.Handled = true;
            return;
        }

        // Enter key launches selected build only when build list has focus
        // This prevents Enter from triggering launch when other controls (buttons) are focused
        if (e.Key == Key.Enter && !_viewModel.ShowTokenInput && IsBuildListFocused())
        {
            if (_viewModel.LaunchBuildCommand?.CanExecute(null) == true)
            {
                _viewModel.LaunchBuildCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }

        // Enter key in token input submits the token
        if (e.Key == Key.Enter && _viewModel.ShowTokenInput && e.Source == _tokenInputBox)
        {
            if (_viewModel.SubmitTokenCommand?.CanExecute(null) == true)
            {
                _viewModel.SubmitTokenCommand.Execute(null);
                e.Handled = true;
            }
            return;
        }
    }

    /// <summary>
    /// Checks if the build list or one of its items currently has keyboard focus.
    /// </summary>
    private bool IsBuildListFocused()
    {
        if (_buildsListBox == null)
            return false;

        // In Avalonia, check if the ListBox or any of its children is focused
        // by checking if the focused element is within the ListBox visual tree
        var topLevel = TopLevel.GetTopLevel(this);
        var focusManager = topLevel?.FocusManager;
        var focusedElement = focusManager?.GetFocusedElement();
        
        if (focusedElement == null)
            return false;

        // Check if focused element is the ListBox or a descendant of it
        if (ReferenceEquals(focusedElement, _buildsListBox))
            return true;

        // Check if focused element is within the ListBox visual tree
        if (focusedElement is Visual visual)
        {
            var parent = visual.Parent;
            while (parent != null)
            {
                if (ReferenceEquals(parent, _buildsListBox))
                    return true;
                parent = (parent as Visual)?.Parent;
            }
        }

        return false;
    }
}
