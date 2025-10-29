using System.Diagnostics;

namespace MouseClickerUI.Services;

/// <summary>
/// Default implementation of IProcessEnumerator that uses the real Process.GetProcesses() API.
/// </summary>
public class SystemProcessEnumerator : IProcessEnumerator
{
    public IProcessData[] GetProcesses()
    {
        var processes = Process.GetProcesses();
        return processes.Select(p => new ProcessDataWrapper(p) as IProcessData).ToArray();
    }
}
