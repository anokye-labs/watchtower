# Issue Updates: Remove Cross-Platform Requirements

This document tracks the bulk update of open issues to remove cross-platform requirements as part of issue #172.

## Overview

WatchTower has shifted to a Windows-only target. This document provides detailed update instructions for ~35+ open issues that currently contain cross-platform references.

## Update Pattern

### Remove These Phrases
- ❌ "Works correctly on Windows, macOS, and Linux"
- ❌ "Works on all platforms (Windows, macOS, Linux)"
- ❌ "Cross-platform testing complete (Windows, macOS, Linux)"
- ❌ "Test on all three platforms"
- ❌ "Works correctly on all supported platforms"
- ❌ "Cross-platform matrix tests on Windows, macOS, Ubuntu"
- ❌ "test on all three platforms (win-x64, osx-x64, linux-x64)"
- ❌ "macos-latest", "ubuntu-latest" in CI configurations
- ❌ References to "osx-x64", "linux-x64"
- ❌ macOS-specific references (traffic lights, NSWindow, etc.)
- ❌ Linux-specific references (X11, Wayland, window manager, etc.)

### Replace With
- ✅ "Works correctly on Windows 10/11"
- ✅ "Tested on Windows with different DPI settings"
- ✅ "Windows-only testing complete"
- ✅ "Windows-native implementation"
- ✅ "Windows 10/11 compatibility"

### Standard Comment to Add
Add this comment to each updated issue:
```
Updated to reflect Windows-only target per #172. Removed cross-platform references and updated acceptance criteria to Windows 10/11 target.
```

---

## High Priority Issues (CI/CD & Testing)

### Issue #162 - PR Build and Test Validation Workflows

**Current Cross-Platform References:**
- "Build the solution on all target platforms (Windows, macOS, Linux)"
- "Multi-platform build verification (win-x64, osx-x64, linux-x64)"
- "PR validation workflow builds solution on 3 platforms"
- "Cross-platform: Build matrix validates all three target platforms"
- "NAudio limitation (Windows-only) won't block builds but may cause test warnings on Linux/macOS"

**Required Changes:**
1. **Objective section** - Change "all target platforms (Windows, macOS, Linux)" to "Windows 10/11"
2. **Context section** - Change "Multi-platform build verification (win-x64, osx-x64, linux-x64)" to "Windows build verification (win-x64)"
3. **Definition of Done** - Change "PR validation workflow builds solution on 3 platforms" to "PR validation workflow builds solution on Windows"
4. **Technical Considerations** - Remove entire "Cross-platform" bullet point about build matrix and NAudio warnings
5. Add: "- **Windows-native**: Build validates on Windows 10/11 only"

---

### Issue #135 - CI/CD Test Automation

**Current Cross-Platform References:**
- "test across all three target platforms (Windows, macOS, Linux)"
- Entire "Cross-Platform Testing" section with 3-platform matrix
- "Cross-platform matrix tests on Windows, macOS, Ubuntu"
- "Test on windows-latest runner", "Test on macos-latest runner", "Test on ubuntu-latest runner"
- Branch protection requiring all three platform checks

**Required Changes:**
1. **Objective** - Change "test across all three target platforms (Windows, macOS, Linux)" to "test on Windows 10/11"
2. **Remove entire section**: "3. Cross-Platform Testing" (4 bullet points)
3. **Implementation Notes** - Update test workflow YAML:
   ```yaml
   strategy:
     matrix:
       os: [windows-latest]
   ```
4. **Branch Protection Configuration** - Change to:
   ```
   - Require "Tests / test (windows-latest)" check
   ```
   Remove the macos and ubuntu checks
5. **Key Considerations** - Remove "Platform Failures: Don't fail entire build if single platform fails"
6. Add to Acceptance Criteria: "- [ ] Tests run on Windows 10/11"
7. Update Acceptance Criteria - Change "Cross-platform matrix tests on Windows, macOS, Ubuntu" to "Tests run on windows-latest runner"

---

### Issue #128 - Robust Test Infrastructure (Epic)

**Current Cross-Platform References:**
- "Validates cross-platform behavior (win-x64, osx-x64, linux-x64)"
- "Tests pass on all three platforms (win-x64, osx-x64, linux-x64)"

**Required Changes:**
1. **Goal section** - Remove bullet point "Validates cross-platform behavior (win-x64, osx-x64, linux-x64)"
2. Add to Goal: "Validates Windows 10/11 behavior"
3. **Definition of Done** - Change "Tests pass on all three platforms (win-x64, osx-x64, linux-x64)" to "Tests pass on Windows 10/11"

---

### Issue #131 - Core Service Test Suite

**Current Cross-Platform References:**
- "Cross-platform testing considerations"
- "All tests pass on Windows (win-x64)" - implies other platforms exist
- "Platform-specific services (NAudio) have appropriate test guards"

**Required Changes:**
1. **Complexity section** - Remove bullet "Cross-platform testing considerations"
2. **Acceptance Criteria** - Change "All tests pass on Windows (win-x64)" to "All tests pass on Windows 10/11"
3. **Acceptance Criteria** - Remove "Platform-specific services (NAudio) have appropriate test guards"
4. **Special Considerations** - Remove "Audio Services: NAudio is Windows-only, add platform guards"
5. Add: "**Audio Services**: NAudio is the accepted Windows-native audio library"

---

## Window/UI Issues

### Issue #45 - Shell Window alpha-based click-through

**Current Cross-Platform References:**
- "Works correctly on Windows, macOS, and Linux" in Acceptance Criteria
- "Cross-Platform Considerations" section with Windows/macOS/Linux testing
- "Test resize/drag behavior on all platforms"

**Required Changes:**
1. **Acceptance Criteria** - Change "Works correctly on all supported platforms (Windows, macOS, Linux)" to "Works correctly on Windows 10/11"
2. **Remove entire section**: "### Cross-Platform Considerations" (3 bullet points about Windows/macOS/Linux)
3. **Technical Notes** - Remove "Test resize behavior with minimum constraints on all platforms"
4. Add to Technical Notes: "Test resize behavior with different DPI settings on Windows 10/11"

---

### Issue #119 - Windowed mode with size constraints

**Current Cross-Platform References:**
- "Works correctly on Windows, macOS, and Linux" in Acceptance Criteria
- "### Cross-Platform Considerations" section
- References to macOS "traffic lights"
- References to "X11 and Wayland" for Linux

**Required Changes:**
1. **Acceptance Criteria** - Change "Works correctly on Windows, macOS, and Linux" to "Works correctly on Windows 10/11 with different DPI settings"
2. **Remove entire section**: "### Cross-Platform Considerations" (3 bullet points)
3. **Technical Notes** - Remove "Test resize behavior with minimum constraints on all platforms"
4. **Technical Notes** - Change "Test with multiple monitors and different DPI settings" to "Test with multiple monitors and different DPI settings on Windows 10/11"

---

### Issue #120 - Cursor management and visual feedback

**Current Cross-Platform References:**
- "Cursor behavior is consistent across Windows, macOS, and Linux" in Acceptance Criteria

**Required Changes:**
1. **Acceptance Criteria** - Change "Cursor behavior is consistent across Windows, macOS, and Linux" to "Cursor behavior works correctly on Windows 10/11"

---

### Issue #122 - Pointer event handlers in ShellWindow

**Status**: Need to fetch and review
**Expected References**: "Window interactions work on Windows, macOS, and Linux"

---

### Issue #136 - Polish, Test, and Document Enhanced Welcome Screen

**Status**: Need to fetch and review
**Expected References**: "Cross-platform testing complete (Windows, macOS, Linux)"

---

### Issue #139 - Enhance Splash Screen UI with Branding

**Status**: Need to fetch and review
**Expected References**: "Works on all platforms (Windows, macOS, Linux) with correct DPI scaling"

---

### Issue #141 - Create Welcome Content Experience

**Status**: Need to fetch and review
**Expected References**: "Works on all platforms (Windows, macOS, Linux)"

---

### Issue #54 - Enhanced Welcome Screen (Epic)

**Status**: Need to fetch and review
**Expected References**: "Cross-platform testing complete (Windows, macOS, Linux)"

---

### Issue #44 - Remember last display position

**Status**: Need to fetch and review
**Expected References**: "Works correctly on all supported platforms (Windows, macOS, Linux)"

---

### Issue #55 - Panel frame extension

**Status**: Need to fetch and review
**Expected References**: "Works correctly on all platforms (Windows, macOS, Linux)"

---

## Documentation Issues

### Issue #130 - Testing Documentation & Patterns

**Status**: Need to fetch and review
**Expected Changes**: Remove cross-platform testing patterns section, update to Windows-only guidance

---

### Issue #146 - Add Confidence Score Documentation

**Status**: Need to fetch and review
**Expected References**: "cross-platform compatible" references

---

## Voice Issues

### Issue #38 - Use actual confidence scores (Epic)

**Status**: Need to fetch and review
**Expected References**: "Cross-platform: Both Vosk and Azure SDK remain compatible"

---

### Issue #46 - Voice Feature Epic

**Status**: Need to fetch and review
**Expected Changes**: 
- Update to reflect NAudio is now accepted (Windows-only)
- Remove blocker status from #35
- Explicitly note NAudio (Windows-native audio library) is the accepted solution

---

### Issue #47 - Auto-download voice models

**Status**: Need to fetch and review
**Expected References**: "cross-platform functionality" references

---

### Issue #48 - Wire up Voice Input Panel

**Status**: Need to fetch and review
**Expected References**: Cross-platform requirements

---

### Issue #49 - Add echo cancellation

**Status**: Need to fetch and review
**Expected References**: Cross-platform compatibility considerations

---

## MCP/Architecture Issues

### Issue #50 - MCP tool routing

**Status**: Need to fetch and review
**Expected References**: "Cross-platform: TCP networking works identically on win-x64, osx-x64, linux-x64"

---

### Issue #113 - Window System & Frame Enhancement Suite (Epic)

**Status**: Need to fetch and review
**Expected References**: "All features work across Windows, macOS, and Linux"

---

## Progress Tracking

### Completed
- [ ] #162 - PR Build and Test Validation Workflows
- [ ] #135 - CI/CD Test Automation
- [ ] #128 - Robust Test Infrastructure (Epic)
- [ ] #131 - Core Service Test Suite
- [ ] #45 - Shell Window alpha-based click-through
- [ ] #119 - Windowed mode with size constraints
- [ ] #120 - Cursor management and visual feedback
- [ ] #122 - Pointer event handlers in ShellWindow
- [ ] #136 - Polish, Test, and Document Enhanced Welcome Screen
- [ ] #139 - Enhance Splash Screen UI with Branding
- [ ] #141 - Create Welcome Content Experience
- [ ] #54 - Enhanced Welcome Screen (Epic)
- [ ] #44 - Remember last display position
- [ ] #55 - Panel frame extension
- [ ] #130 - Testing Documentation & Patterns
- [ ] #146 - Add Confidence Score Documentation
- [ ] #38 - Use actual confidence scores (Epic)
- [ ] #46 - Voice Feature Epic
- [ ] #47 - Auto-download voice models
- [ ] #48 - Wire up Voice Input Panel
- [ ] #49 - Add echo cancellation
- [ ] #50 - MCP tool routing
- [ ] #113 - Window System & Frame Enhancement Suite (Epic)

### Statistics
- Total Issues: 23
- Updated: 0
- Remaining: 23

---

## Automation Script (Optional)

If you have GitHub CLI (`gh`) authenticated, you can use the following script template to automate updates:

```bash
#!/bin/bash
# update-issues.sh

# Issue #162
gh issue edit 162 --repo anokye-labs/watchtower --body "$(cat updated-162.md)"
gh issue comment 162 --repo anokye-labs/watchtower --body "Updated to reflect Windows-only target per #172. Removed cross-platform references and updated acceptance criteria to Windows 10/11 target."

# Issue #135
gh issue edit 135 --repo anokye-labs/watchtower --body "$(cat updated-135.md)"
gh issue comment 135 --repo anokye-labs/watchtower --body "Updated to reflect Windows-only target per #172. Removed cross-platform references and updated acceptance criteria to Windows 10/11 target."

# ... continue for all issues
```

**Note**: You would need to prepare the updated markdown files for each issue first.

---

## Verification Checklist

After all updates are complete, verify:
- [ ] No open issues contain "cross-platform" in acceptance criteria
- [ ] No open issues require "macOS" or "Linux" testing
- [ ] No open issues mention "osx-x64" or "linux-x64"
- [ ] All issues updated to "Windows 10/11" target
- [ ] CI/CD issues (#162, #135) updated to Windows-only builds
- [ ] Testing epic (#128) updated to single-platform testing
- [ ] Voice epic (#46) updated to reflect NAudio acceptance
- [ ] Each updated issue has comment referencing #172
