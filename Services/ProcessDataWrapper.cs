using System.Diagnostics;

namespace MouseClickerUI.Services;

/// <summary>
/// Wrapper for System.Diagnostics.Process that implements IProcessData.
/// </summary>
public class ProcessDataWrapper : IProcessData
{
    private readonly Process _process;

    public ProcessDataWrapper(Process process)
    {
        _process = process;
    }

    public string ProcessName => _process.ProcessName;
    public string MainWindowTitle => _process.MainWindowTitle;
    public int Id => _process.Id;
    public IntPtr MainWindowHandle => _process.MainWindowHandle;

    public void Dispose()
    {
        _process.Dispose();
    }
}
