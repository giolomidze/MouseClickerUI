# MouseClickerUI Installer Build Guide

This guide explains how to build a professional Windows installer for MouseClickerUI using Inno Setup.

## Prerequisites

1. **Inno Setup** - Download and install from [https://jrsoftware.org/isdl.php](https://jrsoftware.org/isdl.php)
   - The free version is sufficient
   - Default installation path: `C:\Program Files (x86)\Inno Setup 6\`

2. **.NET 9.0 SDK** - Required for publishing the application
   - Download from [https://dotnet.microsoft.com/download/dotnet/9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

## Quick Start

### Automatic Build (Recommended)

Run the PowerShell build script:

```powershell
.\build-installer.ps1
```

This will:
1. Publish the application
2. Build the installer using Inno Setup
3. Output the installer to `dist\MouseClickerUI-Setup.exe`

### Custom Version

To build with a specific version number:

```powershell
.\build-installer.ps1 -Version "1.2.3"
```

### Custom Inno Setup Path

If Inno Setup is installed in a different location:

```powershell
.\build-installer.ps1 -InnoSetupPath "C:\Custom\Path\ISCC.exe"
```

## Manual Build

### Step 1: Publish the Application

```bash
dotnet publish --configuration Release --runtime win-x64 --self-contained true
```

This creates the executable at:
```
bin\Release\net9.0-windows\win-x64\publish\MouseClickerUI.exe
```

### Step 2: Build the Installer

1. Open **Inno Setup Compiler**
2. Load `setup.iss`
3. Click **Build** → **Compile** (or press F9)
4. The installer will be created in `dist\MouseClickerUI-Setup.exe`

## Configuration

### Customizing the Installer

Edit `setup.iss` to customize:

- **AppVersion**: Version number displayed in installer
- **AppPublisher**: Your company/developer name
- **AppPublisherURL**: Your website URL
- **AppSupportURL**: Support page URL
- **AppUpdatesURL**: Updates page URL

### Adding License File

If you have a license file, add it to `setup.iss`:

```ini
LicenseFile=LICENSE.txt
```

Then add a `[Languages]` section entry if needed.

### Adding Readme File

To show a readme before installation:

```ini
InfoBeforeFile=README.txt
```

## Distribution

The generated installer (`dist\MouseClickerUI-Setup.exe`) is a single executable file that:
- Can be distributed via email, website, or USB
- Includes all dependencies (self-contained)
- Installs to `Program Files\MouseClickerUI`
- Creates Start Menu shortcuts
- Includes an uninstaller
- Can be run silently with `/SILENT` flag

### Silent Installation

For automated deployments:

```bash
MouseClickerUI-Setup.exe /SILENT
```

## Troubleshooting

### Inno Setup Not Found

If the build script can't find Inno Setup:
1. Verify it's installed at `C:\Program Files (x86)\Inno Setup 6\`
2. Or use the `-InnoSetupPath` parameter to specify the correct path

### Build Fails

- Ensure the application publishes successfully first
- Check that `bin\Release\net9.0-windows\win-x64\publish\MouseClickerUI.exe` exists
- Verify Inno Setup is installed correctly

### Installer Too Large

The installer includes the entire .NET runtime (self-contained). To reduce size:
- Use framework-dependent deployment (requires .NET 9.0 on target machines)
- Modify `MouseClickerUI.csproj` to set `SelfContained=false`

## File Structure

```
MouseClickerUI/
├── setup.iss                 # Inno Setup script
├── build-installer.ps1       # Automated build script
├── INSTALLER.md              # This file
├── app.ico                   # Installer icon
└── dist/                     # Output directory (created during build)
    └── MouseClickerUI-Setup.exe
```

## Next Steps

Consider:
- Code signing the installer (reduces Windows security warnings)
- Adding version information to the executable
- Setting up automated builds in CI/CD
- Creating a download page for distribution

