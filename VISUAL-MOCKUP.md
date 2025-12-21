# Visual Mockup of Animation Sequence

## Stage 1: Application Launch (t=0s)
Window appears centered at 70% of screen size with decorative frame.

```
                    Screen (1920x1080)
┌────────────────────────────────────────────────────────┐
│                                                        │
│                                                        │
│          ┏━━━━━━━━━━━━━━━━━━━━━━━━━━┓                │
│          ┃ ╔════════════════════╗   ┃                │
│          ┃ ║   CORNER ORNATE   ║   ┃  (1344x756)    │
│          ┃ ║                    ║   ┃   Frame SVG    │
│          ┃ ║   ╭─────────────╮  ║   ┃                │
│          ┃ ║   │ WatchTower  │  ║   ┃                │
│          ┃ ║   │      ⚪      │  ║   ┃                │
│          ┃ ║   │  Loading... │  ║   ┃                │
│          ┃ ║   │    00:00    │  ║   ┃                │
│          ┃ ║   ╰─────────────╯  ║   ┃                │
│          ┃ ║                    ║   ┃                │
│          ┃ ║   CORNER ORNATE   ║   ┃                │
│          ┃ ╚════════════════════╝   ┃                │
│          ┗━━━━━━━━━━━━━━━━━━━━━━━━━━┛                │
│                                                        │
│                                                        │
└────────────────────────────────────────────────────────┘
```

## Stage 2: During Initialization (t=3s)
Services loading, status updates shown in splash.

```
                    Screen (1920x1080)
┌────────────────────────────────────────────────────────┐
│                                                        │
│                                                        │
│          ┏━━━━━━━━━━━━━━━━━━━━━━━━━━┓                │
│          ┃ ╔════════════════════╗   ┃                │
│          ┃ ║   CORNER ORNATE   ║   ┃                │
│          ┃ ║                    ║   ┃                │
│          ┃ ║   ╭─────────────╮  ║   ┃                │
│          ┃ ║   │ WatchTower  │  ║   ┃                │
│          ┃ ║   │      ⚪      │  ║   ┃  Pulsing       │
│          ┃ ║   │ Initializing│  ║   ┃  Spinner       │
│          ┃ ║   │ services... │  ║   ┃                │
│          ┃ ║   │    00:03    │  ║   ┃                │
│          ┃ ║   ╰─────────────╯  ║   ┃                │
│          ┃ ║                    ║   ┃                │
│          ┃ ║   CORNER ORNATE   ║   ┃                │
│          ┃ ╚════════════════════╝   ┃                │
│          ┗━━━━━━━━━━━━━━━━━━━━━━━━━━┛                │
│                                                        │
│                                                        │
└────────────────────────────────────────────────────────┘
```

## Stage 3: Startup Complete (t=5s)
Checkmark appears, ready to animate.

```
                    Screen (1920x1080)
┌────────────────────────────────────────────────────────┐
│                                                        │
│                                                        │
│          ┏━━━━━━━━━━━━━━━━━━━━━━━━━━┓                │
│          ┃ ╔════════════════════╗   ┃                │
│          ┃ ║   CORNER ORNATE   ║   ┃                │
│          ┃ ║                    ║   ┃                │
│          ┃ ║   ╭─────────────╮  ║   ┃                │
│          ┃ ║   │ WatchTower  │  ║   ┃                │
│          ┃ ║   │      ✓      │  ║   ┃  Success       │
│          ┃ ║   │   Startup   │  ║   ┃  Checkmark     │
│          ┃ ║   │  complete   │  ║   ┃                │
│          ┃ ║   │    00:05    │  ║   ┃                │
│          ┃ ║   ╰─────────────╯  ║   ┃                │
│          ┃ ║                    ║   ┃                │
│          ┃ ║   CORNER ORNATE   ║   ┃                │
│          ┃ ╚════════════════════╝   ┃                │
│          ┗━━━━━━━━━━━━━━━━━━━━━━━━━━┛                │
│                                                        │
│                                                        │
└────────────────────────────────────────────────────────┘
```

## Stage 4: Animation In Progress (t=5.4s, 50% complete)
Window expanding, frame growing outward.

```
                    Screen (1920x1080)
┌────────────────────────────────────────────────────────┐
│                                                        │
│      ┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓          │
│      ┃ ╔══════════════════════════════╗   ┃          │
│      ┃ ║   CORNER ORNATE              ║   ┃          │
│      ┃ ║                               ║   ┃ Growing │
│      ┃ ║    ╭──────────────────────╮   ║   ┃ Smoothly│
│      ┃ ║    │   WatchTower         │   ║   ┃         │
│      ┃ ║    │         ✓            │   ║   ┃ (1632x  │
│      ┃ ║    │    Startup complete  │   ║   ┃  972px) │
│      ┃ ║    │                      │   ║   ┃         │
│      ┃ ║    ╰──────────────────────╯   ║   ┃         │
│      ┃ ║                               ║   ┃         │
│      ┃ ║              CORNER ORNATE    ║   ┃         │
│      ┃ ╚══════════════════════════════╝   ┃         │
│      ┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛         │
│                                                        │
└────────────────────────────────────────────────────────┘
```

## Stage 5: Animation Complete, Maximizing (t=5.8s)
Window reaches full screen, maximizes.

```
                    Screen (1920x1080)
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ ╔════════════════════════════════════════════════╗   ┃
┃ ║   CORNER ORNATE                                ║   ┃
┃ ║                                                 ║   ┃
┃ ║    ╭─────────────────────────────────────────╮  ║   ┃
┃ ║    │      WatchTower                         │  ║   ┃
┃ ║    │            ✓                            │  ║   ┃
┃ ║    │       Startup complete                  │  ║   ┃  Full
┃ ║    │                                         │  ║   ┃  Screen
┃ ║    │                                         │  ║   ┃
┃ ║    │                                         │  ║   ┃ (1920x
┃ ║    ╰─────────────────────────────────────────╯  ║   ┃  1080)
┃ ║                                                 ║   ┃
┃ ║                                                 ║   ┃
┃ ║                         CORNER ORNATE           ║   ┃
┃ ╚════════════════════════════════════════════════╝   ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

## Stage 6: Main Content Displayed (t=6s)
Content transitions to main application view.

```
                    Screen (1920x1080)
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ ╔════════════════════════════════════════════════╗   ┃
┃ ║   CORNER ORNATE                                ║   ┃
┃ ║                                                 ║   ┃
┃ ║    ╭─────────────────────────────────────────╮  ║   ┃
┃ ║    │         WatchTower                      │  ║   ┃
┃ ║    │  ┌───────────────────────────────────┐  │  ║   ┃
┃ ║    │  │                                   │  │  ║   ┃
┃ ║    │  │  Adaptive Card Content            │  │  ║   ┃  Main
┃ ║    │  │  - Status: Ready                  │  │  ║   ┃  App
┃ ║    │  │  - Controller: Connected          │  │  ║   ┃  UI
┃ ║    │  │  - [Interactive Elements]         │  │  ║   ┃
┃ ║    │  │                                   │  │  ║   ┃
┃ ║    │  └───────────────────────────────────┘  │  ║   ┃
┃ ║    ╰─────────────────────────────────────────╯  ║   ┃
┃ ║                                                 ║   ┃
┃ ║                         CORNER ORNATE           ║   ┃
┃ ╚════════════════════════════════════════════════╝   ┃
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

## Legend

- `┏━━━┓` / `┗━━━┛` : Window border (transparent, no chrome)
- `╔═══╗` / `╚═══╝` : Decorative frame image (main-frame-complete-2.svg)
- `╭───╮` / `╰───╯` : Content area (splash or main)
- `⚪` : Animated spinner (pulsing)
- `✓` : Success checkmark
- CORNER ORNATE : Decorative golden/bronze corner elements

## Animation Characteristics

**Duration:** 800ms (configurable)
**Easing:** Cubic ease-out (fast start, slow end)
**Frame Rate:** ~60 FPS (16ms per frame)

**Size Progression:**
- t=0ms:   1344×756 px (70% of screen)
- t=200ms: 1488×864 px
- t=400ms: 1632×972 px
- t=600ms: 1776×1024 px
- t=800ms: 1920×1080 px (full screen)

**Position:** Centers during resize, then snaps to (0,0) at maximization

**Visual Effect:**
- Frame appears to "grow" outward from center
- Corners remain proportional (SVG scaling)
- Content area expands inside frame
- Splash content remains visible during animation
- Main content fades in after animation completes
