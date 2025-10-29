namespace MouseClickerUI.Models;

/// <summary>
/// Represents information about a running process.
/// </summary>
public class ProcessInfo
{
    public string ProcessName { get; set; }
    public string MainWindowTitle { get; set; }
    public int Id { get; set; }
    public IntPtr MainWindowHandle { get; set; }

    public ProcessInfo(string processName, string mainWindowTitle, int id, IntPtr mainWindowHandle)
    {
        ProcessName = processName;
        MainWindowTitle = mainWindowTitle;
        Id = id;
        MainWindowHandle = mainWindowHandle;
    }
}
