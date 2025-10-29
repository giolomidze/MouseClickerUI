namespace MouseClickerUI.Services;

/// <summary>
/// Interface for enumerating system processes.
/// Allows for testability by abstracting Process.GetProcesses() calls.
/// </summary>
public interface IProcessEnumerator
{
    /// <summary>
    /// Gets all processes running on the system.
    /// </summary>
    /// <returns>Array of IProcessData objects</returns>
    IProcessData[] GetProcesses();
}
