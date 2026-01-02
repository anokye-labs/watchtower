# Complete Issue Update Instructions - Windows-Only Migration

This document contains the complete, detailed update instructions for ALL 23+ identified issues that need cross-platform references removed.

## Standard Update Comment
Add to EVERY updated issue:
```
Updated to reflect Windows-only target per #172. Removed cross-platform references and updated acceptance criteria to Windows 10/11 target.
```

---

## DETAILED ISSUE-BY-ISSUE INSTRUCTIONS

### Issue #44 - Remember last display position
**STATUS**: CLOSED - Skip this issue

---

### Issue #45 - Shell Window alpha-based click-through

**Current Cross-Platform Text:**
- Acceptance Criteria: "Works correctly on all supported platforms (Windows, macOS, Linux)"
- Technical Context section: "Cross-Platform Considerations" with Windows/macOS/Linux specifics
- Technical Notes: "Test resize behavior with minimum constraints on all platforms"

**UPDATE INSTRUCTIONS:**

1. In **Acceptance Criteria** section, find and replace:
```
- [ ] Works correctly on all supported platforms (Windows, macOS, Linux)
```
With:
```
- [ ] Works correctly on Windows 10/11 with different DPI settings
```

2. **DELETE entire section** "Cross-Platform Considerations" (3 bullet points):
```
### Cross-Platform Considerations
- **Windows**: `WindowState.Normal` with `CanResize=true`
- **macOS**: Ensure native window controls (traffic lights) work correctly
- **Linux**: Test on X11 and Wayland; ensure window manager compatibility
```

3. In **Technical Notes** section, find and replace:
```
- Test resize behavior with minimum constraints on all platforms
```
With:
```
- Test resize behavior with different DPI settings on Windows 10/11
```

---

### Issue #50 - MCP tool routing
**STATUS**: Need to fetch - Expected to contain "Cross-platform: TCP networking works identically on win-x64, osx-x64, linux-x64"

---

### Issue #54 - Enhanced Welcome Screen (Epic)

**Current Cross-Platform Text:**
- Technical Considerations: "Cross-platform: Asset paths use forward slashes..."
- Definition of Done: "Cross-platform testing complete (Windows, macOS, Linux)"

**UPDATE INSTRUCTIONS:**

1. In **Technical Considerations** section, find this bullet:
```
- **Cross-platform**: Asset paths use forward slashes; avoid platform-specific image formats; test DPI scaling
```
Replace with:
```
- **Windows Compatibility**: Asset paths use forward slashes; ensure proper DPI scaling on Windows 10/11
```

2. In **Definition of Done** section, find:
```
- [ ] Cross-platform testing complete (Windows, macOS, Linux)
```
Replace with:
```
- [ ] Windows testing complete (Windows 10/11 with DPI testing)
```

---

### Issue #55 - Panel frame extension

**Current Cross-Platform Text:**
- Acceptance Criteria: "Works correctly on all platforms (Windows, macOS, Linux)"

**UPDATE INSTRUCTIONS:**

1. In **Acceptance Criteria** section (last item), find:
```
- [ ] Works correctly on all platforms (Windows, macOS, Linux)
```
Replace with:
```
- [ ] Works correctly on Windows 10/11 with different DPI settings
```

---

### Issue #113 - Window System & Frame Enhancement Suite (Epic)
**STATUS**: Need to fetch - Expected to contain "All features work across Windows, macOS, and Linux"

---

### Issue #119 - Windowed mode with size constraints

**Current Cross-Platform Text:**
- Technical Details section: "Cross-Platform Considerations" with Windows/macOS/Linux specifics
- Acceptance Criteria: "Works correctly on Windows, macOS, and Linux"
- Technical Notes: "Test with multiple monitors and different DPI settings"

**UPDATE INSTRUCTIONS:**

1. **DELETE entire section** "Cross-Platform Considerations":
```
### Cross-Platform Considerations
- **Windows**: `WindowState.Normal` with `CanResize=true`
- **macOS**: Ensure native window controls (traffic lights) work correctly
- **Linux**: Test on X11 and Wayland; ensure window manager compatibility
```

2. In **Acceptance Criteria** section, find:
```
- [ ] Works correctly on Windows, macOS, and Linux
```
Replace with:
```
- [ ] Works correctly on Windows 10/11 with different DPI settings
```

3. In **Technical Notes** section, find:
```
- Test with multiple monitors and different DPI settings
```
Replace with:
```
- Test with multiple monitors and different DPI settings on Windows 10/11
```

---

### Issue #120 - Cursor management and visual feedback

**Current Cross-Platform Text:**
- Acceptance Criteria: "Cursor behavior is consistent across Windows, macOS, and Linux"

**UPDATE INSTRUCTIONS:**

1. In **Acceptance Criteria** section, find:
```
- [ ] Cursor behavior is consistent across Windows, macOS, and Linux
```
Replace with:
```
- [ ] Cursor behavior works correctly on Windows 10/11
```

---

### Issue #122 - Pointer event handlers in ShellWindow

**Current Cross-Platform Text:**
- Acceptance Criteria: "Window interactions work on Windows, macOS, and Linux"
- Technical Notes: "Test thoroughly on all three platforms (Windows, macOS, Linux)"
- Technical Notes: "`BeginMoveDrag()` and `BeginResizeDrag()` are cross-platform but may have subtle platform-specific behaviors"

**UPDATE INSTRUCTIONS:**

1. In **Acceptance Criteria** section, find:
```
- [ ] Window interactions work on Windows, macOS, and Linux
```
Replace with:
```
- [ ] Window interactions work on Windows 10/11
```

2. In **Technical Notes** section, find:
```
- `BeginMoveDrag()` and `BeginResizeDrag()` are cross-platform but may have subtle platform-specific behaviors
- Test thoroughly on all three platforms (Windows, macOS, Linux)
```
Replace with:
```
- Test thoroughly on Windows 10/11 with different DPI settings
```

---

### Issue #128 - Robust Test Infrastructure (Epic)

**Current Cross-Platform Text:**
- Goal: "Validates cross-platform behavior (win-x64, osx-x64, linux-x64)"
- Definition of Done: "Tests pass on all three platforms (win-x64, osx-x64, linux-x64)"

**UPDATE INSTRUCTIONS:**

1. In **Goal** section, find this bullet:
```
- Validates cross-platform behavior (win-x64, osx-x64, linux-x64)
```
**DELETE this entire bullet** (remove it completely)

2. Add new bullet to **Goal** section:
```
- Validates Windows 10/11 behavior
```

3. In **Definition of Done** section, find:
```
- [ ] Tests pass on all three platforms (win-x64, osx-x64, linux-x64)
```
Replace with:
```
- [ ] Tests pass on Windows 10/11
```

---

### Issue #130 - Testing Documentation & Patterns

**Current Cross-Platform Text:**
- Documentation Deliverables: "Cross-platform testing considerations"
- Acceptance Criteria: "Cross-platform testing considerations documented"
- Implementation Notes - testing-guide.md Structure: "## Cross-Platform Testing" section

**UPDATE INSTRUCTIONS:**

1. In **Documentation Deliverables** section, find:
```
- Cross-platform testing considerations
```
Replace with:
```
- Windows-specific testing considerations
```

2. In **Acceptance Criteria** section, find:
```
- [ ] Cross-platform testing considerations documented
```
Replace with:
```
- [ ] Windows-specific testing considerations documented
```

3. In **Implementation Notes** section, find the entire "## Cross-Platform Testing" section:
```
## Cross-Platform Testing
### Platform-Specific Guards
[Example with RuntimeInformation]

### Mocking Platform Dependencies
[Example for NAudio/SDL2]
```
Replace with:
```
## Windows-Specific Testing
### Windows Native Dependencies
[Example for NAudio]

### DPI Testing Considerations
[Testing at different Windows DPI scales]
```

---

### Issue #131 - Core Service Test Suite

**Current Cross-Platform Text:**
- Complexity: "Cross-platform testing considerations"
- Acceptance Criteria: "All tests pass on Windows (win-x64)"
- Acceptance Criteria: "Platform-specific services (NAudio) have appropriate test guards"
- Special Considerations: "Audio Services: NAudio is Windows-only, add platform guards"

**UPDATE INSTRUCTIONS:**

1. In **Complexity** section, find:
```
- Cross-platform testing considerations
```
**DELETE this bullet** (remove it completely)

2. In **Acceptance Criteria** section, find:
```
- [ ] All tests pass on Windows (win-x64)
```
Replace with:
```
- [ ] All tests pass on Windows 10/11
```

3. In **Acceptance Criteria** section, find:
```
- [ ] Platform-specific services (NAudio) have appropriate test guards
```
**DELETE this bullet** (remove it completely)

4. In **Special Considerations** section, find:
```
- **Audio Services**: NAudio is Windows-only, add platform guards
```
Replace with:
```
- **Audio Services**: NAudio is the accepted Windows-native audio library
```

---

### Issue #135 - CI/CD Test Automation

**Current Cross-Platform Text:**
- Objective: "test across all three target platforms (Windows, macOS, Linux)"
- CI/CD Capabilities section: Entire "3. Cross-Platform Testing" subsection
- Acceptance Criteria: "Cross-platform matrix tests on Windows, macOS, Ubuntu"
- Implementation Notes: Test workflow matrix with three OS
- Implementation Notes: Branch protection requiring three platform checks
- Key Considerations: "Platform Failures: Don't fail entire build if single platform fails"

**UPDATE INSTRUCTIONS:**

1. In **Objective** section, find:
```
test across all three target platforms (Windows, macOS, Linux)
```
Replace with:
```
test on Windows 10/11
```

2. **DELETE entire subsection** "3. Cross-Platform Testing":
```
3. **Cross-Platform Testing**
   - Test on windows-latest runner
   - Test on macos-latest runner  
   - Test on ubuntu-latest runner
   - Report platform-specific failures
```

3. In **Acceptance Criteria** section, find:
```
- [ ] Cross-platform matrix tests on Windows, macOS, Ubuntu
```
Replace with:
```
- [ ] Tests run on windows-latest runner
```

4. In **Implementation Notes** - Test Workflow section, find:
```yaml
strategy:
  matrix:
    os: [windows-latest, macos-latest, ubuntu-latest]
runs-on: ${{ matrix.os }}
```
Replace with:
```yaml
runs-on: windows-latest
```

5. In **Implementation Notes** - Branch Protection Configuration section, find:
```
- Require "Tests / test (windows-latest)" check
- Require "Tests / test (macos-latest)" check
- Require "Tests / test (ubuntu-latest)" check
```
Replace with:
```
- Require "Tests / test (windows-latest)" check
```

6. In **Key Considerations** section, **DELETE this bullet**:
```
- **Platform Failures**: Don't fail entire build if single platform fails
```

---

### Issue #136 - Polish, Test, and Document Enhanced Welcome Screen

**Current Cross-Platform Text:**
- Testing Requirements - Integration Tests: "Cross-Platform" subsection
- Acceptance Criteria: "Cross-platform testing completed on Windows, macOS, Linux"

**UPDATE INSTRUCTIONS:**

1. In **Testing Requirements - Integration Tests** section, find:
```
**Cross-Platform:**
- Windows 10/11 (win-x64)
- macOS 12+ (osx-x64)
- Linux Ubuntu 22.04+ (linux-x64)
```
Replace with:
```
**Windows Testing:**
- Windows 10/11
```

2. In **Acceptance Criteria** section, find:
```
- [ ] Cross-platform testing completed on Windows, macOS, Linux
```
Replace with:
```
- [ ] Windows testing completed (Windows 10/11 with DPI testing)
```

---

### Issue #139 - Enhance Splash Screen UI with Branding

**Current Cross-Platform Text:**
- Acceptance Criteria: "Works on all platforms (Windows, macOS, Linux) with correct DPI scaling"

**UPDATE INSTRUCTIONS:**

1. In **Acceptance Criteria** section, find:
```
- [ ] Works on all platforms (Windows, macOS, Linux) with correct DPI scaling
```
Replace with:
```
- [ ] Works on Windows 10/11 with correct DPI scaling at 100%, 125%, 150%, 200%
```

---

### Issue #141 - Create Welcome Content Experience

**Current Cross-Platform Text:**
- Acceptance Criteria: "Works on all platforms (Windows, macOS, Linux)"

**UPDATE INSTRUCTIONS:**

1. In **Acceptance Criteria** section, find:
```
- [ ] Works on all platforms (Windows, macOS, Linux)
```
Replace with:
```
- [ ] Works on Windows 10/11 with different DPI settings
```

---

### Issue #146 - Add Confidence Score Documentation
**STATUS**: Need to fetch - Expected to contain "cross-platform compatible" references

---

### Issue #162 - PR Build and Test Validation Workflows

**Current Cross-Platform Text:**
- Objective: "Build the solution on all target platforms (Windows, macOS, Linux)"
- Goal: "Multi-platform build verification (win-x64, osx-x64, linux-x64)"
- Context: "Multi-platform build verification (win-x64, osx-x64, linux-x64)"
- Definition of Done: "PR validation workflow builds solution on 3 platforms"
- Technical Considerations: Entire "Cross-platform" bullet with build matrix info and NAudio warnings

**UPDATE INSTRUCTIONS:**

1. In **Objective** section, find:
```
- Build the solution on all target platforms (Windows, macOS, Linux)
```
Replace with:
```
- Build the solution on Windows 10/11
```

2. In **Goal** section, find:
```
- Multi-platform build verification (win-x64, osx-x64, linux-x64)
```
Replace with:
```
- Windows build verification (win-x64)
```

3. In **Context** section, find:
```
- Multi-platform build verification (win-x64, osx-x64, linux-x64)
```
Replace with:
```
- Windows build verification (win-x64)
```

4. In **Definition of Done** section, find:
```
- [ ] PR validation workflow builds solution on 3 platforms
```
Replace with:
```
- [ ] PR validation workflow builds solution on Windows
```

5. In **Technical Considerations** section, **DELETE entire "Cross-platform" bullet**:
```
- **Cross-platform**: 
  - Build matrix validates all three target platforms
  - NAudio limitation (Windows-only) won't block builds but may cause test warnings on Linux/macOS
  - Format check runs on Linux only for speed (formatting is platform-agnostic)
```

6. Add new bullet to **Technical Considerations** section:
```
- **Windows-native**: 
  - Build validates on Windows 10/11 only
  - NAudio is the accepted Windows-native audio library
```

---

## Voice Issues (38, 46, 47, 48, 49) - Need to fetch these
## MCP/Architecture Issues (50, 113) - Need to fetch these

---

## Verification Script

After updates, run these GitHub searches to verify:

```bash
# Should return 0 results after updates
gh search issues --repo anokye-labs/watchtower "is:open cross-platform"
gh search issues --repo anokye-labs/watchtower "is:open macOS"
gh search issues --repo anokye-labs/watchtower "is:open osx-x64"
gh search issues --repo anokye-labs/watchtower "is:open linux-x64"
gh search issues --repo anokye-labs/watchtower "is:open \"all platforms\""
gh search issues --repo anokye-labs/watchtower "is:open \"three platforms\""
```

---

## Completion Checklist

- [ ] #45 - Shell Window alpha-based click-through
- [ ] #50 - MCP tool routing
- [ ] #54 - Enhanced Welcome Screen (Epic)
- [ ] #55 - Panel frame extension
- [ ] #113 - Window System & Frame Enhancement Suite (Epic)
- [ ] #119 - Windowed mode with size constraints
- [ ] #120 - Cursor management and visual feedback
- [ ] #122 - Pointer event handlers in ShellWindow
- [ ] #128 - Robust Test Infrastructure (Epic)
- [ ] #130 - Testing Documentation & Patterns
- [ ] #131 - Core Service Test Suite
- [ ] #135 - CI/CD Test Automation
- [ ] #136 - Polish, Test, and Document Enhanced Welcome Screen
- [ ] #139 - Enhance Splash Screen UI with Branding
- [ ] #141 - Create Welcome Content Experience
- [ ] #146 - Add Confidence Score Documentation
- [ ] #162 - PR Build and Test Validation Workflows
- [ ] #38 - Use actual confidence scores (Epic)
- [ ] #46 - Voice Feature Epic
- [ ] #47 - Auto-download voice models
- [ ] #48 - Wire up Voice Input Panel
- [ ] #49 - Add echo cancellation

Total: 22 issues (excluding closed #44)
