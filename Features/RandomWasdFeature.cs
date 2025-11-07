using MouseClickerUI.Models;
using MouseClickerUI.Services;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Features;

/// <summary>
/// Feature for simulating random WASD key presses with configurable probability mouse clicks.
/// Each WASD keypress has a configurable chance of also triggering a mouse click.
/// </summary>
public class RandomWasdFeature : IFeature
{
    private const int DelayBetweenKeyAndClickMs = 50; // Small delay to simulate human behavior

    private static readonly ushort[] WasdKeys = { Constants.VK_W, Constants.VK_A, Constants.VK_S, Constants.VK_D };

    private readonly InputSimulator _inputSimulator;
    private readonly WindowManager _windowManager;
    private readonly ApplicationState _state;
    private readonly Random _random = new();

    private DateTime _lastWasdKeyPressTime = DateTime.MinValue;
    private DateTime _lastClickTime = DateTime.MinValue;
    private int _nextWasdIntervalMs;
    private bool _shouldClickAfterDelay;

    public RandomWasdFeature(InputSimulator inputSimulator, WindowManager windowManager, ApplicationState state)
    {
        _inputSimulator = inputSimulator;
        _windowManager = windowManager;
        _state = state;
    }

    public void Execute()
    {
        var now = DateTime.Now;

        // Handle delayed click if pending
        if (_shouldClickAfterDelay && _lastClickTime != DateTime.MinValue)
        {
            var timeSinceKeyPress = (now - _lastClickTime).TotalMilliseconds;
            if (timeSinceKeyPress >= DelayBetweenKeyAndClickMs)
            {
                // Re-validate window is still in focus before clicking
                if (_windowManager.IsTargetWindow())
                {
                    _inputSimulator.SimulateMouseClick();
                }
                _shouldClickAfterDelay = false;
                _lastClickTime = DateTime.MinValue;
            }
        }

        // If first call or enough time has passed, press a key
        bool shouldPress = false;
        if (_lastWasdKeyPressTime == DateTime.MinValue)
        {
            // First call - press immediately
            shouldPress = true;
        }
        else
        {
            var timeSinceLastPress = (now - _lastWasdKeyPressTime).TotalMilliseconds;
            // Check if enough time has passed
            shouldPress = timeSinceLastPress >= _nextWasdIntervalMs;
        }

        if (shouldPress)
        {
            // Verify target window is in focus before sending keys
            if (!_windowManager.IsTargetWindow())
            {
                return; // Don't send keys if target window isn't active
            }

            // Randomly select one of the WASD keys
            ushort selectedKey = WasdKeys[_random.Next(WasdKeys.Length)];
            _inputSimulator.SimulateKeyPress(selectedKey);

            // Check if we should also perform a mouse click (based on configured probability)
            var clickProbability = _state.RandomWasdClickProbability / 100.0;
            if (_random.NextDouble() < clickProbability)
            {
                // Schedule click with small delay to simulate human behavior
                _shouldClickAfterDelay = true;
                _lastClickTime = now;
            }

            // Update last press time and calculate next interval using configured values
            _lastWasdKeyPressTime = now;
            _nextWasdIntervalMs = _random.Next(_state.RandomWasdMinInterval, _state.RandomWasdMaxInterval + 1);
        }
    }

    public void Reset()
    {
        _lastWasdKeyPressTime = DateTime.MinValue;
        _lastClickTime = DateTime.MinValue;
        _nextWasdIntervalMs = 0;
        _shouldClickAfterDelay = false;
    }
}
