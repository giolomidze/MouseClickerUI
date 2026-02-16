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

    // Virtual key codes for top-row number keys
    public const ushort VK_0 = 0x30; // Top-row 0 key
    public const ushort VK_1 = 0x31; // Top-row 1 key
    public const ushort VK_6 = 0x36; // Top-row 6 key
    public const ushort VK_7 = 0x37; // Top-row 7 key
    public const ushort VK_8 = 0x38; // Top-row 8 key
    public const ushort VK_9 = 0x39; // Top-row 9 key

    // Virtual key codes for NumPad controls
    public const ushort VK_NUMPAD0 = 0x60; // NumPad 0 key
    public const ushort VK_NUMPAD1 = 0x61; // NumPad 1 key
    public const ushort VK_NUMPAD6 = 0x66; // NumPad 6 key
    public const ushort VK_NUMPAD7 = 0x67; // NumPad 7 key
    public const ushort VK_NUMPAD8 = 0x68; // NumPad 8 key
    public const ushort VK_NUMPAD9 = 0x69; // NumPad 9 key

    // Input type constants
    public const int INPUT_MOUSE = 0;
    public const int INPUT_KEYBOARD = 1;

    // Keyboard event flags
    public const uint KEYEVENTF_KEYUP = 0x0002;
    public const uint KEYEVENTF_SCANCODE = 0x0008;

    // MapVirtualKey mapping types
    public const uint MAPVK_VK_TO_VSC = 0;

#pragma warning restore IDE1006 // Naming Styles
}
