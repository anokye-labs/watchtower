using Avalonia.Controls;
using Avalonia.Interactivity;

namespace McpTestApp;

public partial class MainWindow : Window
{
    private int _clickCount;

    public MainWindow()
    {
        InitializeComponent();
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        _clickCount++;
        var inputBox = this.FindControl<TextBox>("InputBox");
        var clickLabel = this.FindControl<TextBlock>("ClickCountLabel");
        var inputLabel = this.FindControl<TextBlock>("LastInputLabel");
        
        if (clickLabel != null)
            clickLabel.Text = $"Clicks: {_clickCount}";
        
        if (inputLabel != null && inputBox != null)
            inputLabel.Text = $"Last input: {inputBox.Text ?? "(empty)"}";
    }
}
