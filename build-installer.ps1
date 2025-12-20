# Build and Package Script for MouseClickerUI Installer
# This script publishes the application and creates an Inno Setup installer

param(
    [string]$Version = "1.0.0",
    [string]$InnoSetupPath = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    [switch]$FrameworkDependent # If set, build a framework-dependent package instead of self-contained
)

$ErrorActionPreference = "Stop"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "MouseClickerUI Installer Build Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if Inno Setup is installed
if (-not (Test-Path $InnoSetupPath)) {
    Write-Host "ERROR: Inno Setup not found at: $InnoSetupPath" -ForegroundColor Red
    Write-Host "Please install Inno Setup from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
    Write-Host "Or update the InnoSetupPath parameter if installed elsewhere." -ForegroundColor Yellow
    exit 1
}

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Green
if (Test-Path "dist") {
    Remove-Item -Recurse -Force "dist" -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path "dist" -Force | Out-Null

# Publish the application
Write-Host ""
if ($FrameworkDependent) {
    Write-Host "Publishing application (framework-dependent)..." -ForegroundColor Green
    dotnet publish "MouseClickerUI.csproj" --configuration Release --runtime win-x64 `
        /p:SelfContained=false `
        /p:PublishSingleFile=false `
        /p:IncludeNativeLibrariesForSelfExtract=false `
        /p:PublishReadyToRun=true
} else {
    Write-Host "Publishing application (self-contained)..." -ForegroundColor Green
    dotnet publish "MouseClickerUI.csproj" --configuration Release --runtime win-x64 `
        /p:SelfContained=true `
        /p:PublishSingleFile=true `
        /p:IncludeNativeLibrariesForSelfExtract=true `
        /p:PublishReadyToRun=true
}

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to publish application" -ForegroundColor Red
    exit 1
}

# Verify publish output exists
$publishPath = "bin\Release\net9.0-windows\win-x64\publish\MouseClickerUI.exe"
if (-not (Test-Path $publishPath)) {
    Write-Host "ERROR: Published executable not found at: $publishPath" -ForegroundColor Red
    exit 1
}

# Update version in setup.iss if provided
if ($Version -ne "1.0.0") {
    Write-Host "Updating version in setup.iss to $Version..." -ForegroundColor Green
    $setupContent = Get-Content "setup.iss" -Raw
    $setupContent = $setupContent -replace 'AppVersion=\d+\.\d+\.\d+', "AppVersion=$Version"
    Set-Content "setup.iss" -Value $setupContent -NoNewline
}

# Build the installer
Write-Host ""
Write-Host "Building installer with Inno Setup..." -ForegroundColor Green
& "$InnoSetupPath" "setup.iss"

if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Failed to build installer" -ForegroundColor Red
    exit 1
}

# Verify installer was created
$installerPath = "dist\MouseClickerUI-Setup.exe"
if (Test-Path $installerPath) {
    $installerSize = (Get-Item $installerPath).Length / 1MB
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Build completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Installer location: $installerPath" -ForegroundColor Cyan
    Write-Host "Installer size: $([math]::Round($installerSize, 2)) MB" -ForegroundColor Cyan
    if ($FrameworkDependent) {
        Write-Host "Note: Framework-dependent build selected. Target machines must have the .NET 9.0 Desktop Runtime (x64) installed." -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "The installer is ready for distribution." -ForegroundColor Green
} else {
    Write-Host "ERROR: Installer was not created" -ForegroundColor Red
    exit 1
}
