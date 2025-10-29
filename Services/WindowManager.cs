using System.Diagnostics;
using MouseClickerUI.Models;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Services;

/// <summary>
/// Service for managing target window detection and validation.
/// </summary>
public class WindowManager
{
    private readonly ApplicationState _state;

    public WindowManager(ApplicationState state)
    {
        _state = state;
    }

    /// <summary>
    /// Sets the target window based on process information.
    /// </summary>
    public void SetTargetWindow(ProcessInfo processInfo)
    {
        _state.TargetProcessId = processInfo.Id;
        _state.TargetWindowHandle = processInfo.MainWindowHandle;
        _state.TargetProcessName = processInfo.ProcessName;
        _state.TargetWindowTitle = processInfo.MainWindowTitle;
    }

    /// <summary>
    /// Checks if the current foreground window is the target window.
    /// Uses multi-layered detection: handle match, process ID match, and fallback re-detection.
    /// </summary>
    /// <returns>True if the target window is in focus, false otherwise</returns>
    public bool IsTargetWindow()
    {
        if (_state.TargetProcessId == 0)
        {
            return false;
        }

        var foregroundWindow = NativeMethods.GetForegroundWindow();

        if (foregroundWindow == IntPtr.Zero)
        {
            return false; // No foreground window
        }

        // Primary check: Verify foreground window handle matches stored handle
        if (_state.TargetWindowHandle != IntPtr.Zero && foregroundWindow == _state.TargetWindowHandle)
        {
            return true;
        }

        // Secondary check: Verify Process ID matches
        if (NativeMethods.GetWindowThreadProcessId(foregroundWindow, out uint processId) != 0 &&
            processId == _state.TargetProcessId)
        {
            // Window handle changed but same process - update stored handle
            _state.TargetWindowHandle = foregroundWindow;
            return true;
        }

        // Fallback: Attempt re-detection by Process Name
        if (!string.IsNullOrEmpty(_state.TargetProcessName))
        {
            try
            {
                var processes = Process.GetProcessesByName(_state.TargetProcessName)
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                    .ToList();

                if (processes.Count > 0)
                {
                    // Find the process with matching window title (if available)
                    var matchingProcess = processes.FirstOrDefault(p =>
                        !string.IsNullOrEmpty(_state.TargetWindowTitle) &&
                        p.MainWindowTitle.Contains(_state.TargetWindowTitle)) ?? processes.First();

                    _state.TargetProcessId = matchingProcess.Id;
                    _state.TargetWindowHandle = matchingProcess.MainWindowHandle;
                    _state.TargetWindowTitle = matchingProcess.MainWindowTitle;

                    // Dispose all Process objects
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }

                    return true;
                }

                // Dispose all Process objects if no match found
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }
            catch
            {
                // Process no longer exists or access denied
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Validates that the target process still exists.
    /// </summary>
    /// <returns>True if the process exists and hasn't exited, false otherwise</returns>
    public bool IsTargetProcessAlive()
    {
        if (_state.TargetProcessId == 0)
        {
            return false;
        }

        try
        {
            using var targetProcess = Process.GetProcessById(_state.TargetProcessId);
            return !targetProcess.HasExited;
        }
        catch (ArgumentException)
        {
            // Process no longer exists
            return false;
        }
    }
}
