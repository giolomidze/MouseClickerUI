namespace MouseClickerUI.Models;

/// <summary>
/// Application configuration loaded from external config file.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Which keyboard section to use for hotkeys.
    /// Supported values: NumPad, NumberRow.
    /// Defaults to NumPad.
    /// </summary>
    public string HotkeyInputSource { get; set; } = HotkeyInputSources.NumPad;

    /// <summary>
    /// Process name to auto-detect and attach to (without .exe extension).
    /// Null or empty means auto-detection is disabled.
    /// </summary>
    public string? TargetProcessName { get; set; }

    /// <summary>
    /// History of selected or auto-detected processes.
    /// </summary>
    public List<DetectionHistoryEntry> DetectionHistory { get; set; } = new();

    /// <summary>
    /// Whether auto-detection is configured with a valid process name.
    /// </summary>
    public bool IsAutoDetectEnabled => !string.IsNullOrWhiteSpace(TargetProcessName);
}
