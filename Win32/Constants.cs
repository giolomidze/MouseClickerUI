namespace MouseClickerUI.Win32;

/// <summary>
/// Win32 API constants used throughout the application.
/// </summary>
internal static class Constants
{
#pragma warning disable IDE1006 // Naming Styles - Win32 API constants match Windows API naming

    // Mouse event flags
    public const uint MOUSEEVENTF_LEFTDOWN = 0x02;
    public const uint MOUSEEVENTF_LEFTUP = 0x04;
    public const uint MOUSEEVENTF_MOVE = 0x01;

    // Virtual key codes for WASD keys
    public const ushort VK_W = 0x57; // W key
    public const ushort VK_A = 0x41; // A key
    public const ushort VK_S = 0x53; // S key
    public const ushort VK_D = 0x44; // D key

    // Input type constants
    public const int INPUT_KEYBOARD = 1;

    // Keyboard event flags
    public const uint KEYEVENTF_KEYUP = 0x0002;

#pragma warning restore IDE1006 // Naming Styles
}
