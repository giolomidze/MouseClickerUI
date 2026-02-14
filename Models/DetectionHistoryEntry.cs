namespace MouseClickerUI.Models;

/// <summary>
/// History entry for a previously selected or auto-detected process.
/// </summary>
public class DetectionHistoryEntry
{
    public string ProcessName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public DateTime LastSelectedAtUtc { get; set; } = DateTime.UtcNow;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(WindowTitle)
            ? ProcessName
            : $"{ProcessName} - {WindowTitle}";

    public string LastSelectedDisplay => LastSelectedAtUtc.ToLocalTime().ToString("g");
}
