namespace MouseClickerUI.Services;

/// <summary>
/// Abstraction over time to make time-dependent logic testable.
/// </summary>
public interface IClock
{
    DateTime Now { get; }
}

public class RealTimeClock : IClock
{
    public DateTime Now => DateTime.Now;
}
