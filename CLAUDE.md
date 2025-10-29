# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MouseClickerUI is a Windows WPF application that automates mouse clicks and keyboard inputs for a selected target application window. It uses Win32 API interop for input simulation and window management.

## Build & Run Commands

### Standard Build
```bash
dotnet build
```

### Release Build
```bash
dotnet build --configuration Release
```

### Publish (Self-Contained Single File)
```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true
```
Output: `bin/Release/net9.0-windows/win-x64/publish/MouseClickerUI.exe`

### Build Installer
```powershell
.\build-installer.ps1
```
Creates installer at `dist/MouseClickerUI-Setup.exe`

### Run Tests
```bash
dotnet test MouseClickerUI.Tests/MouseClickerUI.Tests.csproj
```
Runs all unit tests in the test project

## Architecture

The application follows a clean, layered architecture with separated concerns:

### Directory Structure

```
MouseClickerUI/
├── Win32/                    # Win32 API P/Invoke layer
│   ├── NativeMethods.cs     # P/Invoke declarations
│   ├── InputStructures.cs   # Win32 input structures
│   └── Constants.cs         # Win32 constants
├── Services/                 # Business logic services
│   ├── InputSimulator.cs    # Mouse/keyboard simulation
│   ├── WindowManager.cs     # Window detection/tracking
│   └── ProcessManager.cs    # Process enumeration
├── Features/                 # Individual feature implementations
│   ├── IFeature.cs          # Feature interface
│   ├── MouseClickerFeature.cs
│   ├── MouseMovementFeature.cs
│   └── RandomWasdFeature.cs
├── Models/                   # Data models
│   ├── ProcessInfo.cs       # Process information
│   └── ApplicationState.cs  # Centralized state management
├── MouseClickerUI.Tests/     # Unit tests (xUnit)
│   └── ApplicationStateTests.cs  # Tests for ApplicationState
└── MainWindow.xaml.cs       # UI orchestration (325 lines)
```

### Core Components

**Win32 Layer** (`Win32/`):
- `NativeMethods.cs` - All P/Invoke declarations (mouse_event, SendInput, GetForegroundWindow, etc.)
- `InputStructures.cs` - Win32 structures (INPUT, KEYBDINPUT, etc.) with proper marshaling
- `Constants.cs` - Win32 constants (VK codes, event flags)

**Services Layer** (`Services/`):
- `InputSimulator.cs` - Encapsulates all mouse and keyboard simulation logic
- `WindowManager.cs` - Handles target window detection and validation
- `ProcessManager.cs` - Manages process enumeration and caching

**Features Layer** (`Features/`):
- Each feature implements `IFeature` interface with `Execute()` and `Reset()` methods
- `MouseClickerFeature` - Automated clicking
- `MouseMovementFeature` - Smooth sine/cosine wave movement
- `RandomWasdFeature` - Random WASD keypresses with 50% probability mouse clicks

**Models Layer** (`Models/`):
- `ApplicationState` - Centralized state management for features and configuration
- `ProcessInfo` - Process metadata (name, window title, handle, ID)

**UI Layer**:
- `MainWindow.xaml.cs` - Lightweight orchestration, event handling, and UI updates

### Key State Management

The `ApplicationState` class centralizes all state:
- Feature flags: `IsListening`, `IsClicking`, `IsMouseMoving`, `IsRandomWasdEnabled`
- Target info: `TargetProcessId`, `TargetWindowHandle`, `TargetProcessName`, `TargetWindowTitle`
- Configuration: `ClickDelay`
- Previous key states for edge detection

State changes are debounced using `Prev*State` flags to detect key press edges.

### Target Window Detection

`WindowManager.IsTargetWindow()` uses a multi-layered approach:
1. Primary: Match foreground window handle against stored `TargetWindowHandle`
2. Secondary: Verify Process ID if handle changed
3. Fallback: Re-detect by process name and window title

This handles scenarios where target applications recreate windows or lose focus.

### Win32 API Interop

**Input Simulation:**
- Mouse clicks use `mouse_event()` API
- Keyboard inputs use `SendInput()` with proper INPUT/KEYBDINPUT structures
- Structures use explicit/sequential layout for correct 64-bit marshaling

**Critical Implementation Detail:**
The `INPUT` structure uses a union pattern via `InputUnion` with `LayoutKind.Explicit` and `FieldOffset(0)` to match the Windows API union exactly. All union members (MOUSEINPUT, KEYBDINPUT, HARDWAREINPUT) must be defined even if only keyboard input is used.

### Key Bindings (In Target Window)

- `1` - Enable listening
- `0` - Disable listening and stop all features
- `8` - Start mouse clicking
- `9` - Stop mouse clicking
- `7` - Toggle smooth mouse movement (sine/cosine wave pattern)
- `6` - Toggle random WASD keypresses with 50% probability mouse clicks

### Mouse Movement Algorithm

Uses sine/cosine wave patterns for smooth, natural-looking movement:
- Horizontal: sine wave with randomized range (25-35 pixels)
- Vertical: cosine wave (90° phase shift)
- Random offset per step (±3 pixels)
- Direction reverses after each full cycle

### Random WASD Implementation

- Random interval between keypresses: 200-600ms
- Each WASD keypress has a 50% probability of also triggering a mouse click
- Validates target window focus before sending keys via `IsTargetWindow()`
- Uses `SendInput()` with key down + key up events for keyboard input
- Uses `mouse_event()` API for mouse clicks

## Project Configuration

- Target Framework: .NET 9.0 Windows
- UI Framework: WPF
- Platform: Windows x64 only (uses Win32 APIs)
- Publishing: Single-file, self-contained, ReadyToRun enabled
- Nullable reference types enabled
- Implicit usings enabled

## Unit Testing

The project uses xUnit as the testing framework with the following setup:

### Test Project Structure

- **Project**: `MouseClickerUI.Tests`
- **Framework**: xUnit with .NET 9.0 Windows
- **Test Coverage**: Currently tests the Models layer (`ApplicationState`)
- **Location**: `MouseClickerUI.Tests/` directory

### Running Tests

```bash
# Run all tests
dotnet test MouseClickerUI.Tests/MouseClickerUI.Tests.csproj

# Run tests with detailed output
dotnet test MouseClickerUI.Tests/MouseClickerUI.Tests.csproj --verbosity normal

# Run tests from solution root
dotnet test
```

### Testing Conventions

1. **File Naming**: Test files follow the pattern `{ClassName}Tests.cs`
2. **Test Naming**: Test methods use descriptive names: `MethodName_Scenario_ExpectedResult`
3. **Arrange-Act-Assert**: Tests follow the AAA pattern with clear comments
4. **Test Organization**: Tests are organized by the layer they test (Models, Services, Features)

### Current Test Coverage

**Models Layer:**
- `ApplicationStateTests.cs` - Tests for state management
  - `ResetFeatures_ResetsAllFeatureFlags` - Verifies feature reset behavior
  - `StopAll_ResetsListeningAndAllFeatures` - Verifies complete shutdown
  - `Constructor_SetsDefaultValues` - Verifies initialization

**Features Layer:**
- `RandomWasdFeatureTests.cs` - Tests for random WASD with mouse clicking
  - `Reset_SetsAllFieldsToInitialState` - Verifies reset behavior
  - `Execute_FirstCall_PressesKeyImmediately` - Verifies immediate first keypress
  - `Execute_PressesWasdKeysOnly` - Verifies only WASD keys are pressed
  - `Execute_ClicksWithApproximately50PercentProbability` - Verifies click probability
  - `Execute_DoesNotPressKeyWhenTargetWindowNotActive` - Verifies window validation
  - `Execute_RespectsRandomInterval` - Verifies timing behavior
  - `Execute_KeyPressAlwaysAccompaniedByOptionalClick` - Verifies click/keypress relationship
  - `Execute_AllWasdKeysGetSelected` - Verifies random key selection

### Adding New Tests

To add tests for new components:

1. Create a new test file in `MouseClickerUI.Tests/` following the naming convention
2. Reference the appropriate namespace: `using MouseClickerUI.{Layer};`
3. Follow the existing test patterns in `ApplicationStateTests.cs`
4. For Services/Features layers, focus on testing business logic without UI dependencies

### Test Project Configuration

The test project is configured to:
- Match the main project's target framework (net9.0-windows)
- Disable assembly info generation to avoid conflicts with the main project
- Exclude the test folder from the main project compilation
- Use xUnit with Visual Studio test runner and code coverage support

### Recommended Testing Strategy

**High Priority for Testing:**
- Models layer (state management, data validation)
- Services layer (business logic without Win32 dependencies)
- Features layer (feature behavior and lifecycle)

**Low Priority for Testing:**
- Win32 interop layer (difficult to mock, platform-specific)
- UI layer (requires WPF testing infrastructure)

## Important Notes

### Security & Safety
This is an input automation tool that can perform rapid mouse clicks and keypresses. The code includes safety mechanisms:
- Only operates when target window has focus
- Validates target window using multiple detection methods
- Monitors target process existence and terminates automation if process closes

### Code Style Conventions
- Win32 API constants use uppercase naming (e.g., `VK_W`, `KEYEVENTF_KEYUP`) - suppress IDE1006 warnings for these
- Instance fields use `_lowerCamelCase` with underscore prefix
- Process objects are explicitly disposed after use to prevent resource leaks
- Each layer has a clear responsibility and doesn't cross boundaries

### Architecture Benefits

**Separation of Concerns:**
- Win32 interop isolated in dedicated namespace
- Business logic separated from UI code
- Features are self-contained and independently testable

**Maintainability:**
- MainWindow.xaml.cs reduced from 675 to 325 lines
- Adding new features requires only implementing `IFeature` interface
- Win32 API changes isolated to Win32 namespace

**Testability:**
- Services can be unit tested without UI
- Features can be tested in isolation
- Dependency injection ready (all services accept dependencies)
- xUnit test project established with example tests for Models layer

**Extensibility:**
- New features easily added by implementing `IFeature`
- New input methods can extend `InputSimulator`
- Window detection logic centralized in `WindowManager`

### Common Pitfalls

1. **SendInput Failures**: `InputSimulator` uses proper INPUT structure marshaling with `Marshal.SizeOf(typeof(INPUT))`. Don't manually calculate structure sizes.

2. **Target Window Detection**: Always use `WindowManager.IsTargetWindow()` - it handles window recreation, PID changes, and fallback detection automatically.

3. **Resource Management**: `ProcessManager` automatically disposes Process objects. Follow this pattern when working with unmanaged resources.

4. **Timer Intervals**: The main timer runs at 10ms intervals. Click delay is controlled by `ApplicationState.ClickDelay`.

5. **State Management**: Always modify state through `ApplicationState` properties. Use `ResetFeatures()` or `StopAll()` methods instead of manually toggling flags.

6. **Feature Lifecycle**: Call `Reset()` on features when toggling them on to ensure clean state.
