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

## Architecture

### Core Components

**MainWindow.xaml.cs** - Single monolithic file containing all application logic:
- Process selection and monitoring
- Win32 API interop (mouse_event, SendInput, GetForegroundWindow, etc.)
- Three simulation modes: mouse clicking, mouse movement, random WASD keypresses
- Polling-based key state detection using GetKeyState
- Timer-based execution model (10ms tick interval)

### Key State Management

The application uses static boolean flags to track active features:
- `_listening` - Whether the app is monitoring the target window
- `_clicking` - Whether automated clicking is active
- `_mouseMoving` - Whether smooth mouse movement is active
- `_randomWasdEnabled` - Whether random WASD keypresses are active

State changes are debounced using `_prev*State` flags to detect key press edges.

### Target Window Detection

Multi-layered approach in `IsTargetWindow()`:
1. Primary: Match foreground window handle against stored `_targetWindowHandle`
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
- `6` - Toggle random WASD keypresses

### Mouse Movement Algorithm

Uses sine/cosine wave patterns for smooth, natural-looking movement:
- Horizontal: sine wave with randomized range (25-35 pixels)
- Vertical: cosine wave (90° phase shift)
- Random offset per step (±3 pixels)
- Direction reverses after each full cycle

### Random WASD Implementation

- Random interval between keypresses: 200-600ms
- Validates target window focus before sending keys via `IsTargetWindow()`
- Uses `SendInput()` with key down + key up events

## Project Configuration

- Target Framework: .NET 9.0 Windows
- UI Framework: WPF
- Platform: Windows x64 only (uses Win32 APIs)
- Publishing: Single-file, self-contained, ReadyToRun enabled
- Nullable reference types enabled
- Implicit usings enabled

## Important Notes

### Security & Safety
This is an input automation tool that can perform rapid mouse clicks and keypresses. The code includes safety mechanisms:
- Only operates when target window has focus
- Validates target window using multiple detection methods
- Monitors target process existence and terminates automation if process closes

### Code Style Conventions
- Win32 API constants use uppercase naming (e.g., `VK_W`, `KEYEVENTF_KEYUP`) - suppress IDE1006 warnings for these
- Static fields use `_lowerCamelCase` with underscore prefix
- Process objects are explicitly disposed after use to prevent resource leaks

### Common Pitfalls

1. **SendInput Failures**: Requires proper INPUT structure marshaling. Use `Marshal.SizeOf(typeof(INPUT))` for size calculation, not manual calculation.

2. **Target Window Detection**: Don't rely solely on process ID or window handle - use the multi-layered approach in `IsTargetWindow()` to handle window recreation scenarios.

3. **Resource Management**: Always dispose Process objects immediately after use (see `LoadProcesses()` pattern).

4. **Timer Intervals**: The main timer runs at 10ms intervals. Click delay is separate and controlled by `_clickDelay` field.
