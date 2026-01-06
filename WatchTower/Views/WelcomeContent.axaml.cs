using Avalonia.Controls;
using Avalonia.Input;

namespace WatchTower.Views;

public partial class WelcomeContent : UserControl
{
    public WelcomeContent()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        
        // Allow Escape key to dismiss the welcome screen
        if (e.Key == Key.Escape && DataContext is ViewModels.WelcomeContentViewModel viewModel)
        {
            viewModel.DismissCommand.Execute(null);
            e.Handled = true;
        }
    }
}
