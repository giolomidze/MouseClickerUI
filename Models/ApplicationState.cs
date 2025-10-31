namespace MouseClickerUI.Models;

/// <summary>
/// Centralized application state management.
/// </summary>
public class ApplicationState
{
    // Feature state flags
    public bool IsListening { get; set; }
    public bool IsClicking { get; set; }
    public bool IsMouseMoving { get; set; }
    public bool IsRandomWasdEnabled { get; set; }

    // Target process information
    public int TargetProcessId { get; set; }
    public IntPtr TargetWindowHandle { get; set; } = IntPtr.Zero;
    public string TargetProcessName { get; set; } = string.Empty;
    public string TargetWindowTitle { get; set; } = string.Empty;

    // Configuration
    public int ClickDelay { get; set; } = 1;

    // Mouse movement configuration
    public int MouseMovementRangeMin { get; set; } = 25;
    public int MouseMovementRangeMax { get; set; } = 35;
    public int MouseMovementStepsPerDirection { get; set; } = 10;
    public int MouseMovementRandomOffset { get; set; } = 3;

    // Previous key states for edge detection
    public bool PrevEnableListeningState { get; set; }
    public bool PrevDisableListeningState { get; set; }
    public bool PrevEnableClickingState { get; set; }
    public bool PrevEnableMouseMovingState { get; set; }
    public bool PrevEnableRandomWasdState { get; set; }

    /// <summary>
    /// Resets all feature states.
    /// </summary>
    public void ResetFeatures()
    {
        IsClicking = false;
        IsMouseMoving = false;
        IsRandomWasdEnabled = false;
    }

    /// <summary>
    /// Stops all activity (listening and features).
    /// </summary>
    public void StopAll()
    {
        IsListening = false;
        ResetFeatures();
    }
}
