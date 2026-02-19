namespace MouseClickerUI.Models;

public static class HotkeyInputSources
{
    public const string NumPad = "NumPad";
    public const string NumberRow = "NumberRow";

    public static string Normalize(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return NumPad;
        }

        if (string.Equals(source, NumPad, StringComparison.OrdinalIgnoreCase))
        {
            return NumPad;
        }

        if (string.Equals(source, NumberRow, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(source, "TopRow", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(source, "MainKeyboard", StringComparison.OrdinalIgnoreCase))
        {
            return NumberRow;
        }

        return NumPad;
    }
}
