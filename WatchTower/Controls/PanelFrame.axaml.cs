using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Configuration;
using WatchTower.Models;
using WatchTower.Services;
using WatchTower.ViewModels;

namespace WatchTower.Controls;

/// <summary>
/// Panel frame control that renders a decorative 5x5 grid frame around content.
/// Supports different slide directions to hide/show appropriate edges.
/// </summary>
public partial class PanelFrame : UserControl
{
    private PanelFrameViewModel? _viewModel;
    private IConfiguration? _configuration;
    
    /// <summary>
    /// Defines the Content property for the panel's inner content.
    /// </summary>
    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<PanelFrame, object?>(nameof(Content));
    
    /// <summary>
    /// Defines the SlideDirection property.
    /// </summary>
    public static readonly StyledProperty<PanelSlideDirection> SlideDirectionProperty =
        AvaloniaProperty.Register<PanelFrame, PanelSlideDirection>(
            nameof(SlideDirection), 
            PanelSlideDirection.Left);
    
    /// <summary>
    /// Gets or sets the content to display inside the panel frame.
    /// </summary>
    public object? Content
    {
        get => GetValue(ContentProperty);
        set => SetValue(ContentProperty, value);
    }
    
    /// <summary>
    /// Gets or sets the slide direction which determines frame edge visibility.
    /// </summary>
    public PanelSlideDirection SlideDirection
    {
        get => GetValue(SlideDirectionProperty);
        set => SetValue(SlideDirectionProperty, value);
    }
    
    public PanelFrame()
    {
        InitializeComponent();
        
        // Create ViewModel
        _viewModel = new PanelFrameViewModel();
        DataContext = _viewModel;
        
        // Subscribe to property changes
        this.GetObservable(SlideDirectionProperty)
            .Subscribe(direction =>
            {
                if (_viewModel != null)
                {
                    _viewModel.SlideDirection = direction;
                }
            });
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    /// <summary>
    /// Sets the configuration for frame settings.
    /// Should be called before the control is displayed.
    /// </summary>
    public void SetConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
        LoadFrameImage();
    }
    
    /// <summary>
    /// Loads the frame image from configuration.
    /// </summary>
    private void LoadFrameImage()
    {
        if (_viewModel == null || _configuration == null)
        {
            return;
        }
        
        // Read panel frame configuration
        var frameSourceUri = _configuration.GetValue<string?>("PanelFrame:SourceUri") 
            ?? _configuration.GetValue<string?>("Frame:SourceUri")
            ?? "avares://WatchTower/Assets/main-frame.png";
        
        // X coordinates - use nullable int to allow fallback
        var sliceLeft = _configuration.GetValue<int?>("PanelFrame:Slice:Left") 
            ?? _configuration.GetValue<int?>("Frame:Slice:Left") 
            ?? 1330;
        var sliceLeftInner = _configuration.GetValue<int?>("PanelFrame:Slice:LeftInner") 
            ?? _configuration.GetValue<int?>("Frame:Slice:LeftInner") 
            ?? 2600;
        var sliceRightInner = _configuration.GetValue<int?>("PanelFrame:Slice:RightInner") 
            ?? _configuration.GetValue<int?>("Frame:Slice:RightInner") 
            ?? 4280;
        var sliceRight = _configuration.GetValue<int?>("PanelFrame:Slice:Right") 
            ?? _configuration.GetValue<int?>("Frame:Slice:Right") 
            ?? 5560;
        
        // Y coordinates - use nullable int to allow fallback
        var sliceTop = _configuration.GetValue<int?>("PanelFrame:Slice:Top") 
            ?? _configuration.GetValue<int?>("Frame:Slice:Top") 
            ?? 955;
        var sliceTopInner = _configuration.GetValue<int?>("PanelFrame:Slice:TopInner") 
            ?? _configuration.GetValue<int?>("Frame:Slice:TopInner") 
            ?? 1400;
        var sliceBottomInner = _configuration.GetValue<int?>("PanelFrame:Slice:BottomInner") 
            ?? _configuration.GetValue<int?>("Frame:Slice:BottomInner") 
            ?? 2415;
        var sliceBottom = _configuration.GetValue<int?>("PanelFrame:Slice:Bottom") 
            ?? _configuration.GetValue<int?>("Frame:Slice:Bottom") 
            ?? 2860;
        
        var frameScale = _configuration.GetValue<double?>("PanelFrame:Scale") 
            ?? _configuration.GetValue<double?>("Frame:Scale") 
            ?? 0.15;
        
        var sliceDefinition = new FrameSliceDefinition
        {
            Left = sliceLeft,
            LeftInner = sliceLeftInner,
            RightInner = sliceRightInner,
            Right = sliceRight,
            Top = sliceTop,
            TopInner = sliceTopInner,
            BottomInner = sliceBottomInner,
            Bottom = sliceBottom
        };
        
        if (_viewModel.LoadFrameImage(frameSourceUri, sliceDefinition))
        {
            _viewModel.FrameDisplayScale = frameScale;
            _viewModel.RenderScale = 1.0; // Default, will be updated by parent
        }
    }
    
    /// <summary>
    /// Updates the render scale based on DPI changes.
    /// </summary>
    public void UpdateRenderScale(double renderScale)
    {
        if (_viewModel != null)
        {
            _viewModel.RenderScale = renderScale;
        }
    }
}
