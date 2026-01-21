# Developer Build Menu Window - Implementation Notes

## Files Created

This directory contains the implementation for the Developer Build Menu Window (Issue #219).

### View Files
- **DevBuildMenuWindow.axaml** - Modal dialog XAML with complete UI layout
- **DevBuildMenuWindow.axaml.cs** - Code-behind with event handling and keyboard shortcuts

### Placeholder Files (Temporary)
The following files are minimal placeholders to allow the View to compile. They will be replaced by the full implementations from issue #218:

- **WatchTower/ViewModels/DevBuildMenuViewModel.cs** - Placeholder ViewModel stub
- **WatchTower/Models/BuildInfo.cs** - Placeholder build information model

## Features Implemented

### XAML Structure ✅
- Modal window (600x450, non-resizable, centered on owner)
- Dark theme styling matching application (background: #EE1A1A1A)
- Build list with 4-column layout (Type Icon, Display Name, Created Date, Status)
- Collapsible authentication section with inline token input
- Progress bar section with download speed display
- Status message area
- Bottom action buttons (Clear Cache, Refresh, Cancel, Launch)

### Code-Behind ✅
- Window lifecycle management (Loaded, Unloaded)
- DataContext change handling for ViewModel subscription
- Auto-focus token input box when it appears
- Double-click on build to launch
- Keyboard shortcuts:
  - **Escape**: Cancel token input (if showing) or close dialog
  - **Enter**: Launch selected build (when not in token input)
  - **Enter** (in token input): Submit token

### Design Details
- **Inline Token Input**: TextBox with password masking (●) appears when "Authenticate" is clicked
- **Status Colors**: Green (#4AFF4A) for authenticated/cached, Gray (#AAFFFFFF) for available
- **Progress Indicator**: Holographic cyan (#00F0FF) progress bar with percentage and speed
- **Button Styling**: Accent color for primary "Launch" action, consistent with app theme

## Dependencies

**Blocked by**: Issue #218 - DevBuildMenuViewModel implementation

Once #218 is complete, replace the placeholder files:
1. Delete `WatchTower/ViewModels/DevBuildMenuViewModel.cs` (placeholder)
2. Delete `WatchTower/Models/BuildInfo.cs` (placeholder)
3. The View files are ready to use with the real ViewModel

## Testing Status

- ✅ XAML syntax validated (compiles successfully)
- ✅ Code-behind compiles successfully
- ⏳ Runtime testing blocked by #218 (ViewModel dependency)
- ⏳ Keyboard navigation testing pending ViewModel
- ⏳ Accessibility testing pending ViewModel

## Usage Example (After #218 Complete)

```csharp
// Show the Developer Build Menu from any window
var viewModel = serviceProvider.GetRequiredService<DevBuildMenuViewModel>();
var dialog = new DevBuildMenuWindow
{
    DataContext = viewModel
};

await dialog.ShowDialog(parentWindow);
```

## Technical Notes

1. **First Modal Dialog**: This establishes the pattern for modal dialogs in WatchTower
2. **WindowStartupLocation**: Set to `CenterOwner` for proper modal positioning
3. **ShowInTaskbar**: Set to `False` to keep taskbar clean for modal dialogs
4. **CanResize**: Set to `False` for consistent dialog experience
5. **Token Input**: Uses `PasswordChar="●"` for secure token entry
6. **Event Handling**: Follows MainWindow.axaml.cs patterns for consistency

## Acceptance Criteria Status

All View-level acceptance criteria are met:
- ✅ Window configuration (600x450, modal, centered, non-resizable)
- ✅ Build list layout (4 columns with proper bindings)
- ✅ Double-click handler for build launch
- ✅ Authentication UI (button and status display)
- ✅ Inline token input (collapsible TextBox)
- ✅ Progress bar with speed display
- ✅ Status message area
- ✅ All buttons with proper command bindings
- ✅ Keyboard shortcuts (Escape, Enter)
- ✅ Dark theme styling consistent with app

Runtime functionality depends on ViewModel implementation (issue #218).
