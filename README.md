# Mouse Clicker UI

Mouse Clicker UI is a Windows application that allows users to automate mouse clicks for specific target windows.

## Features

- Select target application from a list of running processes
- Enable/disable listening for key presses
- Start/stop automated mouse clicking with adjustable delay
- Smooth mouse movement with sine/cosine wave patterns
- Random WASD keypresses with 50% probability mouse clicks
- Real-time status updates
- Only operates when target window is in focus

## How to Use

1. Launch the Mouse Clicker UI application.
2. Select the target application from the dropdown list.
3. Set click delay (1-1000ms) using the slider or by entering a value.
4. Choose your hotkey keyboard section:
    - `NumPad` (default, right-side keypad)
    - `Number Row` (top row above QWERTY)
5. Click "Start Listening" or press '1' (in your selected hotkey section) to begin monitoring.
6. Focus on the target application window and use the following key controls:
    - Press '1' to enable listening
    - Press '0' to disable listening and stop all features
    - Press '8' to start mouse clicking
    - Press '9' to stop mouse clicking
    - Press '7' to toggle smooth mouse movement (left-right and up-down)
    - Press '6' to toggle random WASD keypresses + mouse clicks (50% chance)
7. The status label will show the current state of the application.

## Key Bindings

All key bindings work only when the target application window is in focus.
You can choose the hotkey keyboard section from the UI (`NumPad` default or top `Number Row`).

- **'1'**: Enable listening
- **'0'**: Disable listening and stop all features
- **'8'**: Start automated mouse clicking
- **'9'**: Stop automated mouse clicking
- **'7'**: Toggle smooth mouse movement (sine/cosine wave pattern)
- **'6'**: Toggle random WASD keypresses with 50% probability mouse clicks

## Notes

- The application will only function when the selected target window is in focus.
- Be cautious when using this tool, as it can perform rapid mouse clicks and keypresses.
- Always ensure you have a way to quickly stop automation (press hotkey '0' in your selected section or switch windows).
- The random WASD feature performs keypresses every 200-600ms, with each keypress having a 50% chance of also triggering a mouse click.
- The smooth mouse movement uses natural sine/cosine wave patterns for realistic motion.

## Requirements

- Windows operating system
- .NET 9.0 SDK or later

## Building and Publishing

### Prerequisites

1. Install .NET 9.0 SDK from [Microsoft's official download page](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Ensure you have Visual Studio 2022 or VS Code with C# extension (optional but recommended)

### Building the Application

#### Debug Build
```bash
dotnet build
```

#### Release Build
```bash
dotnet build --configuration Release
```

### Running Tests

The project includes a comprehensive test suite using xUnit:

```bash
# Run all tests
dotnet test

# Run tests with detailed output
dotnet test --verbosity normal

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

The test project is located at `MouseClickerUI.Tests/` and includes:
- Model layer tests (ApplicationState)
- Feature layer tests (RandomWasdFeature)
- Service layer tests (ProcessManager, MouseMovementFeature)

### Publishing the Application

The project is configured for single-file publishing with the following settings:
- **PublishSingleFile**: true
- **SelfContained**: true  
- **RuntimeIdentifier**: win-x64
- **PublishReadyToRun**: true

#### Publish for Windows x64 (Self-Contained)
```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true
```

This will create a single executable file in:
```
bin/Release/net9.0-windows/win-x64/publish/MouseClickerUI.exe
```

#### Publish for Framework-Dependent Deployment
```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained false
```

When distributing the framework-dependent build, users must have the matching .NET runtime installed on their machine. Point them to the official download page for the .NET 9.0 Desktop Runtime (x64): https://dotnet.microsoft.com/en-us/download/dotnet/9.0/runtime. Once installed, they can run `MouseClickerUI.exe` from the publish directory.

#### Build the Inno Setup Installer
```powershell
.\build-installer.ps1
```

Pass `-FrameworkDependent` to generate a smaller installer that depends on the target machine having the .NET Desktop Runtime:
```powershell
.\build-installer.ps1 -FrameworkDependent
```

Inno Setup 6 is required (the script defaults to `C:\Program Files (x86)\Inno Setup 6\ISCC.exe`). For framework-dependent builds, the installer now includes all published DLLs/config files (no single-file packing); the target machine still needs the [.NET 9.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/en-us/download/dotnet/9.0/runtime) before running `MouseClickerUI.exe`.

### Build Output

After publishing, you'll find:
- **MouseClickerUI.exe** - The main executable
- **app.ico** - Application icon (embedded in the executable)
- All dependencies bundled in the single file (when using self-contained)

### Distribution

The published executable is a standalone application that can be distributed without requiring .NET runtime installation on target machines (when using self-contained publishing).

### Troubleshooting

- If you encounter build errors, ensure you have the correct .NET SDK version
- For publishing issues, check that the target runtime identifier matches your deployment target
- The application requires Windows API access, so it will only run on Windows systems

## Disclaimer

This tool is for educational and productivity purposes only. Use responsibly and at your own risk. The developers are
not responsible for any unintended consequences of using this application.
