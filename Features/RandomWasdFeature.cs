using MouseClickerUI.Services;
using MouseClickerUI.Win32;

namespace MouseClickerUI.Features;

/// <summary>
/// Feature for simulating random WASD key presses.
/// </summary>
public class RandomWasdFeature : IFeature
{
    private const int MinKeyPressIntervalMs = 200; // Minimum time between key presses (ms)
    private const int MaxKeyPressIntervalMs = 600; // Maximum time between key presses (ms)

    private static readonly ushort[] WasdKeys = { Constants.VK_W, Constants.VK_A, Constants.VK_S, Constants.VK_D };

    private readonly InputSimulator _inputSimulator;
    private readonly WindowManager _windowManager;
    private readonly Random _random = new();

    private DateTime _lastWasdKeyPressTime = DateTime.MinValue;
    private int _nextWasdIntervalMs;

    public RandomWasdFeature(InputSimulator inputSimulator, WindowManager windowManager)
    {
        _inputSimulator = inputSimulator;
        _windowManager = windowManager;
    }

    public void Execute()
    {
        var now = DateTime.Now;

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

            // Update last press time and calculate next interval
            _lastWasdKeyPressTime = now;
            _nextWasdIntervalMs = _random.Next(MinKeyPressIntervalMs, MaxKeyPressIntervalMs + 1);
        }
    }

    public void Reset()
    {
        _lastWasdKeyPressTime = DateTime.MinValue;
        _nextWasdIntervalMs = 0;
    }
}
