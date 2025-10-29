namespace MouseClickerUI.Services;

/// <summary>
/// Interface representing process information needed for enumeration.
/// Allows for testability by abstracting Process object properties.
/// </summary>
public interface IProcessData
{
    string ProcessName { get; }
    string MainWindowTitle { get; }
    int Id { get; }
    IntPtr MainWindowHandle { get; }
    void Dispose();
}
