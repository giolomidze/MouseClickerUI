namespace MouseClickerUI.Models;

/// <summary>
/// Application configuration loaded from external config file.
/// </summary>
public class AppConfig
{
    /// <summary>
    /// Process name to auto-detect and attach to (without .exe extension).
    /// Null or empty means auto-detection is disabled.
    /// </summary>
    public string? TargetProcessName { get; set; }

    /// <summary>
    /// Whether auto-detection is configured with a valid process name.
    /// </summary>
    public bool IsAutoDetectEnabled => !string.IsNullOrWhiteSpace(TargetProcessName);
}
