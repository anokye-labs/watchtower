# Developer Build Menu Window - Visual Design Documentation

## Window Overview

**Dimensions**: 600x450 pixels (non-resizable)  
**Background**: Dark theme (#EE0A0A0A outer, #EE1A1A1A inner)  
**Border**: Subtle white border (#4AFFFFFF) with 8px corner radius  
**Modal**: Yes - centers on parent window, blocks interaction with parent

## Layout Breakdown (Top to Bottom)

### 1. Header Section (Top)
```
┌────────────────────────────────────────────────────────┐
│  Developer Build Menu                            [✕]   │
│  ^-- 20px Bold, White                   ^-- Close btn  │
└────────────────────────────────────────────────────────┘
```
- **Title**: "Developer Build Menu" - FontSize 20, Bold, White
- **Close Button**: ✕ symbol, transparent background, top-right aligned
- **Height**: ~36px + 16px margin

### 2. Build List Section
```
┌────────────────────────────────────────────────────────┐
│ ┌────────────────────────────────────────────────────┐ │
│ │ 📦  v1.2.0         Jan 9, 2026      Cached         │ │ ← Green (#4AFF4A)
│ │ 📦  v1.1.0         Jan 5, 2026      Available      │ │ ← Gray (#AAFFFFFF)
│ │ 🔧  PR #123        Jan 10, 2026     Available      │ │ ← Needs auth
│ │ 🔧  PR #119        Jan 8, 2026      Available      │ │
│ │                                                    │ │
│ └────────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
```
- **Container**: Light background (#1AFFFFFF), 6px radius
- **Height**: 180px fixed, scrollable if needed
- **Padding**: 8px
- **Columns**:
  - Icon (Auto) - 📦 release / 🔧 PR
  - Name (*) - Display name, ellipsis if too long
  - Date (Auto) - "MMM d, yyyy" format
  - Status (Auto) - Colored by state
- **Interaction**: Single-click to select, double-click to launch
- **Selection**: Highlighted row

### 3. Authentication Section
```
When NOT authenticated:
┌────────────────────────────────────────────────────────┐
│  [ Authenticate ]                                      │
│   ^-- Button (120px min width)                         │
└────────────────────────────────────────────────────────┘

When authenticated:
┌────────────────────────────────────────────────────────┐
│  ✓ Authenticated as @hoopsomuah                        │
│  ^-- Green checkmark and text (#4AFF4A)                │
└────────────────────────────────────────────────────────┘

When token input showing:
┌────────────────────────────────────────────────────────┐
│  Enter GitHub Personal Access Token:                   │
│  [●●●●●●●●●●●●●●●●●●●●]  [ Cancel ]  [ Submit ]       │
│   ^-- Password masked          ^-- Buttons              │
└────────────────────────────────────────────────────────┘
```
- **States**: Button → Token Input → Authenticated
- **Token Input**: Background #1AFFFFFF, PasswordChar "●", placeholder "ghp_..."
- **Buttons**: Cancel (gray), Submit (accent color)

### 4. Progress Section (Conditional)
```
When downloading:
┌────────────────────────────────────────────────────────┐
│  ████████████░░░░░░░░░░░░░░░░░░░░░░░░░░  45%  2.3 MB/s│
│   ^-- Cyan progress bar (#00F0FF)      ^-- Stats       │
└────────────────────────────────────────────────────────┘
```
- **Visibility**: Only when `IsDownloading == true`
- **Progress Bar**: Height 8px, cyan foreground (#00F0FF)
- **Stats**: Percentage (F0 format) and download speed
- **Colors**: Light gray text (#AAFFFFFF)

### 5. Status Message Section
```
┌────────────────────────────────────────────────────────┐
│  Downloading v1.2.0...                                 │
│  ^-- Status message (wraps if needed)                  │
└────────────────────────────────────────────────────────┘
```
- **Font**: 14px, White
- **Height**: Min 20px (to prevent layout shift)
- **Wrapping**: Enabled for long messages

### 6. Action Buttons (Bottom)
```
┌────────────────────────────────────────────────────────┐
│                    [Clear Cache (234 MB)] [Refresh]    │
│                    [Cancel]  [Launch]                   │
│                     ^-- Gray  ^-- Accent (cyan/blue)   │
└────────────────────────────────────────────────────────┘
```
- **Alignment**: Right-aligned
- **Spacing**: 8px between buttons
- **Buttons**:
  - **Clear Cache**: Shows size, disabled if no cache
  - **Refresh**: Reloads build list
  - **Cancel**: Closes dialog (Esc)
  - **Launch**: Accent colored, disabled if no selection (Enter)
- **Sizes**: Min 80px width (except Clear Cache: 120px)
- **Padding**: 12px horizontal, 6px vertical
- **Radius**: 4px rounded corners

## Color Palette

| Element | Color | Usage |
|---------|-------|-------|
| Background (outer) | `#EE0A0A0A` | Window background |
| Background (inner) | `#EE1A1A1A` | Main panel |
| Border | `#4AFFFFFF` | Panel borders (semi-transparent white) |
| Text (primary) | `White` / `#FFFFFF` | Headers, main text |
| Text (secondary) | `#AAFFFFFF` | Dates, stats (semi-transparent) |
| Text (muted) | `#CCFFFFFF` | Close button |
| Success/Green | `#4AFF4A` | Cached status, authenticated |
| Accent/Cyan | `#00F0FF` | Progress bar, Launch button |
| Element background | `#1AFFFFFF` | List box, token input (10% white) |
| Input background | `#2AFFFFFF` | TextBox (20% white) |
| Gray text | `#AAFFFFFF` | Available status |

## Typography

| Element | Size | Weight | Color |
|---------|------|--------|-------|
| Window title | 20px | Bold | White |
| Close button | 20px | Normal | #CCFFFFFF |
| Build icon | 16px | Normal | White |
| Build name | 14px | Normal | White |
| Build date | 12px | Normal | #AAFFFFFF |
| Build status | 12px | Normal | Variable |
| Auth text | 14px | Normal | #4AFF4A |
| Token label | 12px | Normal | #CCFFFFFF |
| Progress stats | 12px | Normal | #AAFFFFFF |
| Status message | 14px | Normal | White |
| Button text | 14px | Normal | White |

## Spacing & Dimensions

- **Window**: 600 × 450px
- **Outer margin**: 8px
- **Inner padding**: 16px
- **Section spacing**: 12px vertical
- **Button spacing**: 8px horizontal
- **Corner radius**: 
  - Window border: 8px
  - Buttons: 4px
  - Input boxes: 4px
  - List container: 6px
- **Build list height**: 180px (fixed)
- **Progress bar height**: 8px
- **Button height**: ~30px (from padding 12×6)
- **Min button width**: 80-120px

## Interaction States

### Buttons
- **Normal**: Background per style (gray/accent)
- **Hover**: Slightly lighter (handled by Avalonia)
- **Pressed**: Slightly darker (handled by Avalonia)
- **Disabled**: Reduced opacity (handled by Avalonia)

### List Items
- **Normal**: Transparent background
- **Hover**: Subtle highlight (Avalonia default)
- **Selected**: Highlighted background (Avalonia default)
- **Double-click**: Triggers launch

### Token Input
- **Normal**: #2AFFFFFF background, #4AFFFFFF border
- **Focused**: Border color intensifies (Avalonia default)
- **Password**: Characters masked with ●

## Keyboard Navigation

| Key | Action | Context |
|-----|--------|---------|
| **Esc** | Cancel token input OR Close dialog | Any |
| **Enter** | Submit token OR Launch build | Token input / Build selected |
| **Tab** | Navigate between controls | Any |
| **Arrow Keys** | Navigate build list | List focused |
| **Space** | Select current item | List focused |

## Accessibility

- **Tab Order**: Logical top-to-bottom, left-to-right
- **AutomationProperties.Name**: Set on:
  - Build list: "Available builds list"
  - Token input: "GitHub token input"
  - Buttons: Labels match content
- **Focus Indicators**: Visible (Avalonia default)
- **Screen Readers**: All interactive elements labeled

## Visual Consistency

This design matches the existing WatchTower patterns:
- Same dark background as MainWindow overlays (#EE1A1A1A)
- Same border style (#4AFFFFFF, 8px radius)
- Same accent colors (cyan #00F0FF, green #4AFF4A)
- Same button styling (accent for primary action)
- Same font sizes and weights
- Consistent spacing with existing overlays

## Design Language: Ancestral Futurism

- **Dark Theme**: Void black backgrounds (#050508, #0A0A0A)
- **Holographic Cyan**: Interactive elements (#00F0FF)
- **Ashanti Gold**: Not used in this dialog (reserved for special elements)
- **Mahogany**: Border tones (#4A1812 → #4AFFFFFF semi-transparent white)
- **Typography**: Clean, modern sans-serif (default Avalonia font)
- **Spacing**: Generous padding for readability
- **Borders**: Subtle, semi-transparent for depth

## Example States

### State 1: Initial Load (Not Authenticated)
- Build list shows only releases (📦)
- "Authenticate" button visible
- No progress bar
- "Launch" button enabled if build selected

### State 2: Token Input
- "Authenticate" button hidden
- Token input box visible with Cancel/Submit
- Password-masked input field
- Auto-focused on appearance

### State 3: Authenticated
- Build list shows releases + PR builds (📦 + 🔧)
- Green checkmark + username visible
- Token input hidden
- Full functionality available

### State 4: Downloading
- Progress bar visible with percentage
- Download speed shown (e.g., "2.3 MB/s")
- Status message: "Downloading v1.2.0..."
- Launch button disabled during download

### State 5: Cached Build Selected
- Status shows "Cached" in green
- Launch instant (no download needed)
- Clear Cache button shows size
