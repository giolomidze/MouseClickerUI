# Mouse Clicker UI

Mouse Clicker UI is a Windows application that allows users to automate mouse clicks for specific target windows.

## Features

- Select target application from a list of running processes
- Enable/disable listening for key presses
- Start/stop automated mouse clicking
- Adjustable click delay
- Real-time status updates

## How to Use

1. Launch the Mouse Clicker UI application.
2. Select the target application from the dropdown list.
3. Click "Start Listening" to begin monitoring key presses.
4. Use the following key controls in the target application window:
    - Press '1' to enable listening
    - Press '0' to disable listening
    - Press '8' to start mouse clicking
    - Press '9' to stop mouse clicking
5. Adjust the click delay using the slider or by entering a value.
6. The status label will show the current state of the application.

## Key Bindings

- '1': Enable listening
- '0': Disable listening and stop clicking
- '8': Start mouse clicking (when listening is enabled)
- '9': Stop mouse clicking (when clicking is active)

## Notes

- The application will only function when the selected target window is in focus.
- Be cautious when using this tool, as it can perform rapid mouse clicks.
- Always ensure you have a way to quickly stop the clicking (using the '9' key or by switching windows).

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
