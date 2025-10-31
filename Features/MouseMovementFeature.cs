using MouseClickerUI.Models;
using MouseClickerUI.Services;

namespace MouseClickerUI.Features;

/// <summary>
/// Feature for smooth mouse movement using sine/cosine wave patterns.
/// </summary>
public class MouseMovementFeature : IFeature
{
    private readonly InputSimulator _inputSimulator;
    private readonly ApplicationState _state;
    private readonly Random _random = new();

    private int _mouseMovementDirection = 1;
    private int _mouseMovementStep;
    private int _currentMovementRange;

    public MouseMovementFeature(InputSimulator inputSimulator, ApplicationState state)
    {
        _inputSimulator = inputSimulator;
        _state = state;
    }

    public void Execute()
    {
        // When we complete one full cycle (both directions), reset and randomize movement range
        if (_mouseMovementStep >= _state.MouseMovementStepsPerDirection * 2)
        {
            _mouseMovementStep = 0;
            _mouseMovementDirection *= -1; // Reverse direction for next cycle
            // Randomize movement range for the new cycle
            _currentMovementRange = _random.Next(_state.MouseMovementRangeMin, _state.MouseMovementRangeMax + 1);
        }

        // Initialize movement range on first call
        if (_mouseMovementStep == 0 && _currentMovementRange == 0)
        {
            _currentMovementRange = _random.Next(_state.MouseMovementRangeMin, _state.MouseMovementRangeMax + 1);
        }

        // Calculate smooth movement using a sine wave pattern
        var progress = (double)_mouseMovementStep / _state.MouseMovementStepsPerDirection;
        var sineValue = Math.Sin(progress * Math.PI); // 0 to π gives smooth 0 to 1 to 0
        var horizontalMovement = (int)(sineValue * _currentMovementRange * _mouseMovementDirection);

        // For vertical movement, use a cosine wave (90 degrees out of phase) for smooth up-down motion
        var cosineValue = Math.Cos(progress * Math.PI); // 0 to π gives smooth 1 to -1 to 1
        var verticalMovement = (int)(cosineValue * _currentMovementRange);

        // Add small random offset to each step
        var horizontalOffset = _random.Next(-_state.MouseMovementRandomOffset, _state.MouseMovementRandomOffset + 1);
        var verticalOffset = _random.Next(-_state.MouseMovementRandomOffset, _state.MouseMovementRandomOffset + 1);
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
