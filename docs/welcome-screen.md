# Welcome Screen

## Overview

The WatchTower welcome screen provides an informative, branded first-run experience that guides new users through the application's key features and controls. The welcome screen is displayed automatically after successful startup for first-time users or when the user hasn't seen it before.

## Features

### First-Run Detection

The welcome screen uses the `IUserPreferencesService` to track whether the user has seen the welcome content:

- **IsFirstRun**: Tracks if this is the first time the application has been launched
- **HasSeenWelcomeScreen**: Tracks if the user has dismissed the welcome screen
- **WelcomeScreenDismissedDate**: Records when the user dismissed the welcome screen

The preferences are persisted to `%AppData%/WatchTower/user-preferences.json` on Windows, `~/.config/WatchTower/user-preferences.json` on Linux, and `~/Library/Application Support/WatchTower/user-preferences.json` on macOS.

### Content Sections

The welcome screen displays the following information:

#### 1. Header
- Application name: "Welcome to WatchTower"
- Tagline: "Ancestral Futurism AI Framework"
- Styled with Ashanti Gold (#FFD700) and Holographic Cyan (#00F0FF)

#### 2. Introduction
- Brief description of WatchTower's purpose and design language
- Explains the fusion of modern AI orchestration with West African cultural elements

#### 3. Keyboard Shortcuts
A reference guide for keyboard controls:
- **Ctrl + R**: Open rich text input overlay
- **Ctrl + M**: Open voice input overlay
- **Ctrl + L**: Toggle event log
- **Escape**: Close overlays

#### 4. Gamepad Controls
A guide for gamepad/controller navigation:
- **D-Pad / Left Stick**: Navigate UI elements
- **A Button**: Select / Confirm
- **B Button**: Back / Cancel
- Note about automatic controller detection

#### 5. Adaptive Cards
Information about WatchTower's use of Microsoft Adaptive Cards with the custom "Ancestral Futurism" theme and gamepad-first navigation.

### User Actions

#### Dismiss Button
- Primary action button: "Get Started"
- Styled with Holographic Cyan background (#00F0FF)
- Hover effect changes to Ashanti Gold (#FFD700)
- Closes the welcome screen

#### "Don't Show Again" Checkbox
- Allows users to opt out of seeing the welcome screen on subsequent launches
- When checked, calls `IUserPreferencesService.MarkWelcomeScreenSeen()` to persist the preference
- User can still access the welcome content later through a help menu (future implementation)

## Architecture

### Components

1. **WelcomeContent.axaml** (`Views/WelcomeContent.axaml`)
   - User control displaying welcome content
   - Styled with Ancestral Futurism design language
   - Responsive layout with scrollable content area
   - Maximum dimensions: 700x550px

2. **WelcomeContentViewModel** (`ViewModels/WelcomeContentViewModel.cs`)
   - ViewModel for welcome screen
   - Manages "Don't show again" state
   - Handles dismiss command
   - Raises `WelcomeDismissed` event

3. **Integration in App.axaml.cs**
   - Checks first-run status after main content loads
   - Creates modal window to host welcome content
   - Transparent window with no decorations for clean presentation

## Design Language

The welcome screen follows the "Ancestral Futurism" design system:

### Colors
- **Ashanti Gold (#FFD700)**: Headers and important text
- **Holographic Cyan (#00F0FF)**: Interactive elements, shortcuts, and accents
- **Deep Mahogany (#4A1812)**: Background containers (with transparency)
- **Void Black (#050508)**: Main background (with high transparency)

### Typography
- Headers: Bold, larger font sizes (32px, 18px)
- Body text: 13-14px, white with appropriate opacity
- Code/Shortcuts: Monospace font (Consolas, Courier New)

### Layout
- Centered modal window
- Gold border with drop shadow
- Rounded corners (16px border radius)
- Generous padding (40px)
- Scrollable content area for long content

### Animations
- Future: Entrance animation with cubic ease-out (500ms) to match shell expansion
- Button hover effects (color transition)

## Usage

### Programmatic Control

To manually show the welcome screen:

```csharp
var userPreferencesService = serviceProvider.GetRequiredService<IUserPreferencesService>();
var welcomeViewModel = new WelcomeContentViewModel(userPreferencesService);
var welcomeContent = new Views.WelcomeContent
{
    DataContext = welcomeViewModel
};

// Create and show window
var welcomeWindow = new Window
{
    Content = welcomeContent,
    Width = 800,
    Height = 600,
    CanResize = false,
    WindowStartupLocation = WindowStartupLocation.CenterOwner,
    Background = Brushes.Transparent,
    TransparencyLevelHint = new[] { WindowTransparencyLevel.Transparent },
    SystemDecorations = SystemDecorations.None,
    ShowInTaskbar = false
};

welcomeViewModel.WelcomeDismissed += (s, e) => welcomeWindow.Close();
welcomeWindow.ShowDialog(parentWindow);
```

### Resetting First-Run Status

To reset the first-run status for testing:

1. Delete the preferences file:
   - Windows: `%AppData%/WatchTower/user-preferences.json`
   - Linux: `~/.config/WatchTower/user-preferences.json`
   - macOS: `~/Library/Application Support/WatchTower/user-preferences.json`

2. Or programmatically:
```csharp
var preferences = userPreferencesService.GetPreferences();
preferences.IsFirstRun = true;
preferences.HasSeenWelcomeScreen = false;
preferences.WelcomeScreenDismissedDate = null;
userPreferencesService.SavePreferences(preferences);
```

## Testing

### Manual Testing

1. Build and run the application
2. Verify welcome screen appears after splash screen
3. Test "Don't show again" checkbox
4. Restart application and verify welcome screen doesn't show if checkbox was checked
5. Delete preferences file and verify welcome screen shows again

### Automated Testing

Test the `WelcomeContentViewModel`:

```csharp
[Fact]
public void DismissWithDontShowAgain_MarksWelcomeScreenSeen()
{
    // Arrange
    var mockService = new Mock<IUserPreferencesService>();
    var viewModel = new WelcomeContentViewModel(mockService.Object);
    viewModel.DontShowAgain = true;
    
    // Act
    viewModel.DismissCommand.Execute(null);
    
    // Assert
    mockService.Verify(s => s.MarkWelcomeScreenSeen(), Times.Once);
}
```

## Future Enhancements

Planned improvements for the welcome screen:

1. **Help Menu Integration**: Add a "Show Welcome Screen" option in the application menu
2. **Entrance Animation**: Add smooth entrance animation matching shell window expansion (500ms cubic ease-out)
3. **Interactive Tutorial**: Add optional interactive walkthrough of key features
4. **Video/GIF Demos**: Include animated demonstrations of features
5. **Localization**: Support for multiple languages
6. **Customizable Content**: Allow content to be loaded from external JSON file for easy updates
7. **Analytics**: Track which sections users spend time reading (privacy-respecting, local only)
8. **Sankofa Symbol**: Integrate Sankofa symbol to represent learning and looking back at the guide

## Related Documentation

- [Splash Screen Startup](splash-screen-startup.md) - Startup flow and splash screen details
- [Architecture](ARCHITECTURE.md) - Overall application architecture
- [Assets](ASSETS.md) - Visual assets and branding guidelines

---

Last Updated: 2026-01-01
