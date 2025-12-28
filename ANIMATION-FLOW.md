# Animation Flow Diagram

## Startup Sequence

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. APPLICATION LAUNCH                                           │
│    App.axaml.cs: OnFrameworkInitializationCompleted()          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 2. CREATE SHELL WINDOW (Initial State)                         │
│                                                                 │
│    Screen Size: 1920x1080                                       │
│    Window Size: Frame-based (static components + min content)  │
│    Position: Centered                                           │
│    Content: SplashWindowViewModel                               │
│                                                                 │
│    ┌───────────────────────────────────────┐                   │
│    │ ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │                   │
│    │ ┃  Frame (SVG - Stretches)      ┃  │                   │
│    │ ┃  ┌──────────────────────────┐  ┃  │                   │
│    │ ┃  │  WatchTower Logo         │  ┃  │                   │
│    │ ┃  │  ⚪ Loading Spinner       │  ┃  │                   │
│    │ ┃  │  Status: "Loading..."    │  ┃  │                   │
│    │ ┃  │  00:03                   │  ┃  │                   │
│    │ ┃  └──────────────────────────┘  ┃  │                   │
│    │ ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │                   │
│    └───────────────────────────────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 3. STARTUP ORCHESTRATION                                        │
│    - Initialize services (GameController, AdaptiveCard, etc.)   │
│    - Duration: ~3-5 seconds                                     │
│    - SplashViewModel shows progress                             │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 4. MARK STARTUP COMPLETE                                        │
│    - Show checkmark ✓                                           │
│    - Wait 500ms                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 5. ANIMATE EXPANSION (500ms)                                    │
│                                                                 │
│    Uses Avalonia's Animation system:                            │
│                                                                 │
│    t=0ms:   Splash size (frame-based)                          │
│    t=500ms: Full-screen working area                           │
│                                                                 │
│    Easing: Cubic ease-out                                       │
│    Frame image stretches uniformly                              │
│                                                                 │
│    ┌────────────────────────────────────────────────────┐      │
│    │ ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │      │
│    │ ┃  Frame grows outward                        ┃  │      │
│    │ ┃  ┌────────────────────────────────────────┐ ┃  │      │
│    │ ┃  │  WatchTower Logo                       │ ┃  │      │
│    │ ┃  │  ✓ Startup Complete                    │ ┃  │      │
│    │ ┃  │                                         │ ┃  │      │
│    │ ┃  │                                         │ ┃  │      │
│    │ ┃  └────────────────────────────────────────┘ ┃  │      │
│    │ ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │      │
│    └────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│ 6. TRANSITION TO MAIN CONTENT                                   │
│    - Switch Content to MainWindowViewModel                      │
│    - Content fades in                                           │
│    - Window state: Maximized, Full-screen                       │
│                                                                 │
│    ┌────────────────────────────────────────────────────┐      │
│    │ ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓  │      │
│    │ ┃  Frame (Full-screen)                       ┃  │      │
│    │ ┃  ┌────────────────────────────────────────┐ ┃  │      │
│    │ ┃  │  WatchTower                            │ ┃  │      │
│    │ ┃  │  ┌──────────────────────────────────┐  │ ┃  │      │
│    │ ┃  │  │  Adaptive Card Content           │  │ ┃  │      │
│    │ ┃  │  │  - Main UI                        │  │ ┃  │      │
│    │ ┃  │  │  - Game controller ready          │  │ ┃  │      │
│    │ ┃  │  └──────────────────────────────────┘  │ ┃  │      │
│    │ ┃  └────────────────────────────────────────┘ ┃  │      │
│    │ ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛  │      │
│    └────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────┘
```

## Key Animation Parameters

| Parameter | Value | Notes |
|-----------|-------|-------|
| Initial Size | Frame-based | Calculated from frame static components + min content area |
| Duration | 500ms | Hardcoded in ShellWindow.axaml.cs |
| Animation System | Avalonia Animation | Uses KeyFrame animations with RunAsync |
| Easing | Cubic ease-out | Natural deceleration at end |
| Final State | Full-screen | Fills screen working area |

## Frame Stretching

The frame image uses `Stretch="None"` to scale with the window:

```
Initial (1344x756):              Final (1920x1080):
┌────────────────┐              ┌─────────────────────┐
│ ╔════════════╗ │              │ ╔═══════════════════╗ │
│ ║   CORNER   ║ │              │ ║     CORNER        ║ │
│ ║            ║ │   ========>  │ ║                   ║ │
│ ║   CORNER   ║ │              │ ║     CORNER        ║ │
│ ╚════════════╝ │              │ ╚═══════════════════╝ │
└────────────────┘              └─────────────────────┘
```

The SVG frame scales proportionally, maintaining aspect ratio of decorative elements.

## Code Flow

```
App.axaml.cs
  ├─ OnFrameworkInitializationCompleted()
  │   ├─ Create ShellWindow with SplashViewModel
  │   ├─ Show window (calls ShellWindow.SetSplashSize)
  │   └─ Task.Run(ExecuteStartupAsync)
  │
  └─ ExecuteStartupAsync()
      ├─ StartupOrchestrator.ExecuteStartupAsync()
      ├─ Mark startup complete
      ├─ await shellWindow.AnimateExpansionAsync()  ← Animation here
      └─ shellViewModel.TransitionToMainContent()

ShellWindow.axaml.cs
  ├─ SetSplashSize()
  │   └─ Calculate frame-based size, center position
  │
  └─ AnimateExpansionAsync()
      ├─ Avalonia Animation system
      ├─ CubicEaseOut easing
      ├─ Animate Width, Height, Position (500ms)
      └─ Fill screen working area

ShellWindowViewModel.cs
  └─ TransitionToMainContent(mainViewModel)
      └─ CurrentContent = mainViewModel
```

## Testing Checklist

- [ ] Window opens at frame-based splash size (centered)
- [ ] Frame is visible around splash content
- [ ] Splash shows loading animation and status
- [ ] After ~3-5 seconds, checkmark appears
- [ ] Window smoothly expands to full-screen over 500ms
- [ ] Frame stretches with window (no gaps)
- [ ] Main content appears after animation
- [ ] No second window is created
- [ ] Keyboard shortcuts work in both modes
- [ ] Overlays animate correctly in main mode
