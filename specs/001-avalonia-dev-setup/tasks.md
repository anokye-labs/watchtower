---
description: "Task list for Avalonia Development Environment Setup"
---

# Tasks: Avalonia Development Environment Setup

**Input**: Design documents from `/specs/001-avalonia-dev-setup/`
**Prerequisites**: plan.md (complete), spec.md (complete)

**Tests**: Not requested for this infrastructure feature - focus is on scaffolding and configuration

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

This is a desktop application with the following structure:
- **Application**: `WatchTower/` (main project directory)
- **VS Code config**: `.vscode/` at repository root
- **Configuration**: `.config/` at repository root

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

- [X] T001 Create WatchTower project directory with standard Avalonia structure (WatchTower/, Views/, ViewModels/, Models/, Services/, Assets/)
- [X] T002 Create WatchTower.csproj targeting net10.0 with Avalonia packages (Avalonia 11.x, Avalonia.Desktop, Avalonia.Diagnostics)
- [X] T003 [P] Create .vscode directory with placeholder files for launch.json, tasks.json, settings.json, extensions.json
- [X] T004 [P] Create .config directory for configuration files
- [X] T005 Create .gitignore file for .NET/Avalonia projects (bin/, obj/, .vs/, *.user files)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 Implement Program.cs entry point with Avalonia AppBuilder configuration and .NET 10 compatibility
- [X] T007 Create App.axaml with basic application-level styles and resources
- [X] T008 Implement App.axaml.cs with application initialization and lifetime configuration
- [X] T009 [P] Create Services/LoggingService.cs with configurable logging levels (minimal/normal/verbose)
- [X] T010 [P] Create appsettings.json in WatchTower/ with default logging configuration (normal level)
- [X] T011 Configure self-contained publish in WatchTower.csproj (PublishSingleFile=true, SelfContained=true, RuntimeIdentifiers: win-x64, osx-x64, linux-x64)

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel

---

## Phase 3: User Story 1 - Initial Application Launch (Priority: P1) 🎯 MVP

**Goal**: Launch a basic Avalonia application window with "Hello World" message to verify development environment is working

**Independent Test**: Run `dotnet run` from WatchTower/ directory and verify window appears with title and "Hello World" message within 5 seconds

### Implementation for User Story 1

- [X] T012 [P] [US1] Create Views/MainWindow.axaml with window definition (Title="WatchTower", default size 800x600)
- [X] T013 [P] [US1] Add "Hello World" TextBlock to MainWindow.axaml centered in content area
- [X] T014 [US1] Implement Views/MainWindow.axaml.cs code-behind with window initialization
- [X] T015 [US1] Wire up MainWindow as startup window in App.axaml.cs OnFrameworkInitializationCompleted method
- [X] T016 [US1] Add startup logging in Program.cs (log application start, initialization milestones)
- [X] T017 [US1] Add error handling in Program.cs for missing .NET 10 runtime with diagnostic message (FR-026, FR-008b)
- [X] T018 [US1] Test application launch timing - verify < 5 seconds startup (SC-001, SC-006)

**Checkpoint**: At this point, application should launch with window displaying "Hello World" message

---

## Phase 4: User Story 2 - Debug from VS Code (Priority: P1)

**Goal**: Enable full debugging support from VS Code with breakpoints, variable inspection, and step-through debugging

**Independent Test**: Open workspace in VS Code, press F5, set breakpoint in MainWindow.axaml.cs constructor, verify debugger pauses at breakpoint

### Implementation for User Story 2

- [X] T019 [P] [US2] Create .vscode/extensions.json with recommended extensions (avaloniateam.vscode-avalonia, ms-dotnettools.csharp, nromanov.dotnet-meteor, rogalmic.vscode-xml-complete)
- [X] T020 [P] [US2] Create .vscode/launch.json with "Debug" configuration (coreclr debugger, preLaunchTask: build)
- [X] T021 [P] [US2] Add "Run (without debugging)" configuration to .vscode/launch.json
- [X] T022 [P] [US2] Create .vscode/tasks.json with build task (dotnet build WatchTower/WatchTower.csproj)
- [X] T023 [P] [US2] Add clean task to .vscode/tasks.json (dotnet clean)
- [X] T024 [P] [US2] Add run task to .vscode/tasks.json (dotnet run --project WatchTower/WatchTower.csproj)
- [X] T025 [US2] Create .vscode/settings.json with Avalonia-specific settings and problem matcher configuration
- [ ] T026 [US2] Test F5 debug launch - verify application starts with debugger attached (SC-002)
- [ ] T027 [US2] Test breakpoint functionality - set breakpoint in code, verify execution pauses (SC-002)
- [ ] T028 [US2] Test variable inspection and step-through debugging (SC-002)
- [ ] T029 [US2] Verify build errors appear in VS Code Problems panel with file locations (FR-013)

**Checkpoint**: At this point, full VS Code debugging should work with breakpoints and variable inspection

---

## Phase 5: User Story 3 - Hot Reload UI Changes (Priority: P2)

**Goal**: Enable XAML hot reload and simple C# hot reload to rapidly iterate on UI design without restarting

**Independent Test**: Launch app with debugger, modify MainWindow.axaml text content, save file, observe change reflected in running app within 2 seconds without restart

### Implementation for User Story 3

- [X] T030 [P] [US3] Verify Avalonia.Diagnostics package is included in WatchTower.csproj with proper configuration
- [X] T031 [P] [US3] Create Properties/launchSettings.json with hotReloadEnabled=true for .NET Hot Reload
- [X] T032 [P] [US3] Configure Avalonia hot reload in App.axaml.cs (enable DevTools in debug mode)
- [X] T033 [US3] Add hot reload detection and logging in App.axaml.cs to track reload events
- [X] T034 [US3] Implement hot reload error handler in App.axaml.cs to display syntax errors without crashing (FR-019, FR-027, FR-029)
- [X] T035 [US3] Add notification mechanism for when restart is required (complex logic changes per FR-020b, FR-020c)
- [X] T036 [US3] Update logging to capture hot reload events and timing
- [ ] T037 [US3] Test XAML hot reload - modify MainWindow.axaml content, verify < 2 second update (SC-003, SC-004, FR-017)
- [ ] T038 [US3] Test simple C# hot reload - modify method body in MainWindow.axaml.cs, verify hot reload works (FR-020a)
- [ ] T039 [US3] Test complex C# change - add new method, verify restart notification appears (FR-020b, FR-020c)
- [ ] T040 [US3] Test XAML syntax error handling - introduce invalid XAML, verify error message without crash (SC-009, FR-019)
- [ ] T041 [US3] Verify application state preserved during hot reload (window position, etc.) (FR-018)

**Checkpoint**: All hot reload features should work for both XAML and simple C# changes with proper error handling

---

## Phase 6: User Story 4 - Build and Run from Command Line (Priority: P3)

**Goal**: Enable command-line build and run for CI/CD integration and alternative workflows

**Independent Test**: Open terminal in repo root, run `dotnet build WatchTower/WatchTower.csproj && dotnet run --project WatchTower/WatchTower.csproj`, verify app launches

### Implementation for User Story 4

- [X] T042 [P] [US4] Document command-line build command in specs/001-avalonia-dev-setup/quickstart.md
- [X] T043 [P] [US4] Document command-line run command in quickstart.md
- [X] T044 [P] [US4] Document self-contained publish commands for each platform (win-x64, osx-x64, linux-x64) in quickstart.md
- [X] T045 [US4] Test dotnet build from command line - verify successful compilation (SC-008, SC-006)
- [ ] T046 [US4] Test dotnet run from command line - verify application launches (SC-008)
- [ ] T047 [US4] Test self-contained publish for Windows - verify single executable created (FR-008, FR-008a, FR-008c)
- [ ] T048 [US4] Test build error reporting - introduce syntax error, verify clear error messages with file locations (FR-024)
- [ ] T049 [US4] Verify published executable runs without .NET runtime installed (test on clean VM if possible) (SC-011)

**Checkpoint**: All command-line operations should work for build, run, and publish

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories and final documentation

- [X] T050 [P] Create comprehensive quickstart.md with prerequisites, setup steps, development workflow, troubleshooting (covers all user stories)
- [X] T051 [P] Create README.md in repository root with project overview and getting started link
- [X] T052 [P] Update .github/copilot-instructions.md with Avalonia 11.x, .NET 10, MVVM, hot reload info
- [X] T053 [P] Add inline code comments to Program.cs explaining initialization flow
- [X] T054 [P] Add inline code comments to App.axaml.cs explaining hot reload configuration
- [X] T055 [P] Add inline code comments to Services/LoggingService.cs explaining configurable logging
- [X] T056 [P] Document logging configuration options in appsettings.json (add comments)
- [ ] T057 Verify all success criteria are met - run through SC-001 to SC-012 validation checklist
- [ ] T058 Test cross-platform - verify application builds and runs on Windows (if not primary development OS)
- [ ] T059 Test cross-platform - verify application builds and runs on macOS (if available)
- [ ] T060 Test cross-platform - verify application builds and runs on Linux (if available)
- [ ] T061 Performance validation - measure and document actual launch time, hot reload time, build time
- [ ] T062 Fresh developer onboarding test - follow quickstart.md on clean machine, verify < 10 minute setup (SC-010)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3-6)**: All depend on Foundational phase completion
  - User Story 1 (P1) and User Story 2 (P1): Should be done first as they are highest priority
  - User Story 2 depends on User Story 1 being complete (need working app to debug)
  - User Story 3 (P2) depends on User Story 2 being complete (hot reload requires debug mode)
  - User Story 4 (P3) can be done in parallel with User Stories 1-3 but lowest priority
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P1)**: Depends on User Story 1 completion - requires working application to debug
- **User Story 3 (P2)**: Depends on User Story 2 completion - hot reload works best with debug mode
- **User Story 4 (P3)**: Can start after Foundational (Phase 2) - Independent of other stories

### Within Each User Story

**User Story 1**:
- T012, T013 can run in parallel (different aspects of XAML)
- T014 depends on T012 completion (code-behind for window)
- T015 depends on T014 completion (wire up window)
- T016-T018 are sequential validation steps

**User Story 2**:
- T019-T025 can run in parallel (all separate .vscode config files)
- T026-T029 are sequential testing/validation steps

**User Story 3**:
- T030-T032 can run in parallel (separate config aspects)
- T033-T036 are sequential (build on each other)
- T037-T041 are sequential testing/validation steps

**User Story 4**:
- T042-T044 can run in parallel (documentation tasks)
- T045-T049 are sequential testing/validation steps

**Polish**:
- T050-T056 can run in parallel (all separate documentation files)
- T057-T062 are sequential validation steps

### Parallel Opportunities

**Within Setup (Phase 1)**:
- T003, T004 can run in parallel (different directories)

**Within Foundational (Phase 2)**:
- T009, T010 can run in parallel (separate files for logging)

**Within User Story 1**:
```bash
# Launch XAML tasks in parallel:
Task T012: Create Views/MainWindow.axaml
Task T013: Add "Hello World" TextBlock
```

**Within User Story 2**:
```bash
# Launch all VS Code config files in parallel:
Task T019: Create .vscode/extensions.json
Task T020: Create .vscode/launch.json (Debug config)
Task T021: Add Run config to launch.json
Task T022: Create .vscode/tasks.json (build task)
Task T023: Add clean task
Task T024: Add run task
Task T025: Create .vscode/settings.json
```

**Within User Story 3**:
```bash
# Launch hot reload configuration in parallel:
Task T030: Verify Avalonia.Diagnostics package
Task T031: Create Properties/launchSettings.json
Task T032: Configure hot reload in App.axaml.cs
```

**Within Polish (Phase 7)**:
```bash
# Launch all documentation tasks in parallel:
Task T050: Create quickstart.md
Task T051: Create README.md
Task T052: Update copilot-instructions.md
Task T053: Comment Program.cs
Task T054: Comment App.axaml.cs
Task T055: Comment LoggingService.cs
Task T056: Document appsettings.json
```

---

## Implementation Strategy

### MVP First (User Stories 1 & 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL - blocks all stories)
3. Complete Phase 3: User Story 1 - Basic app launches
4. Complete Phase 4: User Story 2 - VS Code debugging works
5. **STOP and VALIDATE**: Test that you can launch, debug, and develop
6. This gives you a working development environment (MVP)

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Working application! (Basic MVP)
3. Add User Story 2 → Test independently → Can debug! (Development MVP)
4. Add User Story 3 → Test independently → Hot reload! (Productivity MVP)
5. Add User Story 4 → Test independently → CI/CD ready! (Complete)
6. Polish → Documentation complete

### Recommended Execution Order

1. **Day 1**: Phase 1 (Setup) + Phase 2 (Foundational) + Phase 3 (User Story 1)
   - Goal: Working Avalonia app that launches
2. **Day 2**: Phase 4 (User Story 2)
   - Goal: Full VS Code debugging support
3. **Day 3**: Phase 5 (User Story 3)
   - Goal: Hot reload for rapid iteration
4. **Day 4**: Phase 6 (User Story 4) + Phase 7 (Polish)
   - Goal: Command-line support and complete documentation

---

## Notes

- [P] tasks = different files, no dependencies, can run in parallel
- [Story] label (US1, US2, US3, US4) maps task to specific user story
- Each user story builds on previous ones (1→2→3→4 dependency chain)
- User Story 1 & 2 together form the minimal viable development environment
- Tests not included as this is infrastructure/scaffolding work
- Commit after each task or logical group of parallel tasks
- Stop at any checkpoint to validate story independently
- If .NET 10 SDK is not available, use .NET 8 as fallback (see Risk Assessment in plan.md)
- Focus on getting to User Stories 1 & 2 quickly - those are the critical path

---

**Total Tasks**: 62
**Tasks per User Story**: US1: 7, US2: 11, US3: 12, US4: 8
**Parallel Tasks**: ~20 can run in parallel within their phases
**Estimated Timeline**: 3-4 days for complete implementation
**MVP Timeline**: 1-2 days (through User Story 2)
