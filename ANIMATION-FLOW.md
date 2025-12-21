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
│    Window Size: 1344x756 (70% of screen)                       │
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
│ 5. ANIMATE EXPANSION (800ms)                                    │
│                                                                 │
│    Frame-by-frame (60 FPS):                                     │
│                                                                 │
│    t=0ms:   Width=1344, Height=756, Pos=(288,162)             │
│    t=200ms: Width=1488, Height=864, Pos=(216,108)             │
│    t=400ms: Width=1632, Height=972, Pos=(144,54)              │
│    t=600ms: Width=1776, Height=1024, Pos=(72,28)              │
│    t=800ms: Width=1920, Height=1080, Pos=(0,0)                │
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
| Initial Size | 70% of screen | Configurable via `SplashSizeRatio` |
| Duration | 800ms | Defined in `AnimationDurationMs` |
| Frame Rate | ~60 FPS | 16ms per frame via DispatcherTimer |
| Easing | Cubic ease-out | Natural deceleration at end |
| Final State | Maximized | Full-screen, borderless |

## Frame Stretching

The frame image uses `Stretch="Fill"` to scale with the window:

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
  │   └─ Calculate 70% size, center position
  │
  └─ AnimateExpansionAsync()
      ├─ DispatcherTimer (16ms interval)
      ├─ CubicEaseOut(progress)
      ├─ Interpolate Width, Height, Position
      └─ WindowState.Maximized

ShellWindowViewModel.cs
  └─ TransitionToMainContent(mainViewModel)
      └─ CurrentContent = mainViewModel
```

## Testing Checklist

- [ ] Window opens at splash size (centered, 70% of screen)
- [ ] Frame is visible around splash content
- [ ] Splash shows loading animation and status
- [ ] After ~3-5 seconds, checkmark appears
- [ ] Window smoothly expands to full-screen over ~1 second
- [ ] Frame stretches with window (no gaps)
- [ ] Main content appears after animation
- [ ] No second window is created
- [ ] Keyboard shortcuts work in both modes
- [ ] Overlays animate correctly in main mode
