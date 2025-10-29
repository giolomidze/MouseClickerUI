using System.Diagnostics;
using MouseClickerUI.Models;

namespace MouseClickerUI.Services;

/// <summary>
/// Service for managing process enumeration and caching.
/// </summary>
public class ProcessManager
{
    private List<string> _cachedProcessNames = [];

    /// <summary>
    /// Loads all processes with windows and returns them as ProcessInfo objects.
    /// </summary>
    /// <returns>List of ProcessInfo objects</returns>
    public List<ProcessInfo> LoadProcesses()
    {
        var processes = Process.GetProcesses()
            .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
            .OrderBy(p => p.ProcessName)
            .ToList();

        var processNames = processes.Select(p => p.ProcessName).ToList();

        // Check if the process list has changed
        if (_cachedProcessNames.SequenceEqual(processNames))
        {
            // Dispose all Process objects if no changes needed
            foreach (var process in processes)
            {
                process.Dispose();
            }
            return [];
        }

        _cachedProcessNames = processNames;

        // Create ProcessInfo objects and dispose Process objects immediately
        var processInfos = processes.Select(p =>
        {
            var processInfo = new ProcessInfo(p.ProcessName, p.MainWindowTitle, p.Id, p.MainWindowHandle);
            p.Dispose();
            return processInfo;
        }).ToList();

        return processInfos;
    }

    /// <summary>
    /// Forces a refresh of the process cache.
    /// </summary>
    public void ClearCache()
    {
        _cachedProcessNames.Clear();
    }
}
