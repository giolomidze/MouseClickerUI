using MouseClickerUI.Services;

namespace MouseClickerUI.Features;

/// <summary>
/// Feature for smooth mouse movement using sine/cosine wave patterns.
/// </summary>
public class MouseMovementFeature : IFeature
{
    private const int BaseMovementRangeMin = 25; // Minimum movement range
    private const int BaseMovementRangeMax = 35; // Maximum movement range
    private const int StepsPerDirection = 10; // Number of steps to complete one direction
    private const int PerStepRandomOffset = 3; // Max random offset per step in pixels

    private readonly InputSimulator _inputSimulator;
    private readonly Random _random = new();

    private int _mouseMovementDirection = 1;
    private int _mouseMovementStep;
    private int _currentMovementRange;

    public MouseMovementFeature(InputSimulator inputSimulator)
    {
        _inputSimulator = inputSimulator;
    }

    public void Execute()
    {
        // When we complete one full cycle (both directions), reset and randomize movement range
        if (_mouseMovementStep >= StepsPerDirection * 2)
        {
            _mouseMovementStep = 0;
            _mouseMovementDirection *= -1; // Reverse direction for next cycle
            // Randomize movement range for the new cycle (25-35 pixels)
            _currentMovementRange = _random.Next(BaseMovementRangeMin, BaseMovementRangeMax + 1);
        }

        // Initialize movement range on first call
        if (_mouseMovementStep == 0 && _currentMovementRange == 0)
        {
            _currentMovementRange = _random.Next(BaseMovementRangeMin, BaseMovementRangeMax + 1);
        }

        // Calculate smooth movement using a sine wave pattern
        var progress = (double)_mouseMovementStep / StepsPerDirection;
        var sineValue = Math.Sin(progress * Math.PI); // 0 to π gives smooth 0 to 1 to 0
        var horizontalMovement = (int)(sineValue * _currentMovementRange * _mouseMovementDirection);

        // For vertical movement, use a cosine wave (90 degrees out of phase) for smooth up-down motion
        var cosineValue = Math.Cos(progress * Math.PI); // 0 to π gives smooth 1 to -1 to 1
        var verticalMovement = (int)(cosineValue * _currentMovementRange);

        // Add small random offset to each step (±perStepRandomOffset pixels)
        var horizontalOffset = _random.Next(-PerStepRandomOffset, PerStepRandomOffset + 1);
        var verticalOffset = _random.Next(-PerStepRandomOffset, PerStepRandomOffset + 1);
        horizontalMovement += horizontalOffset;
        verticalMovement += verticalOffset;

        // Move mouse both horizontally and vertically
        _inputSimulator.SimulateMouseMovement(horizontalMovement, verticalMovement);

        // Update step counter
        _mouseMovementStep++;
    }

    public void Reset()
    {
        _mouseMovementStep = 0;
        _mouseMovementDirection = 1;
        _currentMovementRange = 0;
    }
}
