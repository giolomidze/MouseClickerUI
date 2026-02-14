using MouseClickerUI.Models;

namespace MouseClickerUI.Services;

/// <summary>
/// Handles listening on/off hotkeys, including auto-detect behavior.
/// </summary>
public class ListeningHotkeyHandler
{
    private readonly ApplicationState _state;
    private readonly IClock _clock;

    public ListeningHotkeyHandler(ApplicationState state, IClock? clock = null)
    {
        _state = state;
        _clock = clock ?? new RealTimeClock();
    }

    public ListeningHotkeyResult Handle(
        bool isEnableListeningPressed,
        bool isDisableListeningPressed,
        bool isAutoDetectMode,
        bool isAutoDetectEnabled,
        string? targetProcessName,
        bool autoDetectPaused)
    {
        var result = new ListeningHotkeyResult
        {
            AutoDetectPaused = autoDetectPaused
        };

        // Handle hotkey '1' - Enable listening
        if (isEnableListeningPressed && !_state.PrevEnableListeningState)
        {
            if (isAutoDetectMode && isAutoDetectEnabled)
            {
                result.AutoDetectPaused = false;
                result.StartListeningButtonEnabled = false;
                result.StatusMessage = $"Auto-detect: waiting for {targetProcessName}...";
                result.ShouldTryAutoDetect = true;
            }
            else
            {
                _state.IsListening = true;
                result.StatusMessage = $"Listening enabled at {_clock.Now}";
            }
        }
        _state.PrevEnableListeningState = isEnableListeningPressed;

        // Handle hotkey '0' - Disable listening and all features
        if (isDisableListeningPressed && !_state.PrevDisableListeningState)
        {
            _state.StopAll();
            result.AutoDetectPaused = true;

            if (isAutoDetectMode && isAutoDetectEnabled)
            {
                result.StartListeningButtonEnabled = true;
                result.StatusMessage = "Listening stopped - click Start Listening to resume auto-detect";
            }
            else
            {
                result.StatusMessage = $"Listening disabled at {_clock.Now}";
            }
        }
        _state.PrevDisableListeningState = isDisableListeningPressed;

        return result;
    }
}

public class ListeningHotkeyResult
{
    public bool AutoDetectPaused { get; set; }
    public bool ShouldTryAutoDetect { get; set; }
    public bool? StartListeningButtonEnabled { get; set; }
    public string? StatusMessage { get; set; }
}
