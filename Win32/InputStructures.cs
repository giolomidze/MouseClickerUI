using System.Runtime.InteropServices;

namespace MouseClickerUI.Win32;

/// <summary>
/// Win32 input structures for SendInput API.
/// These must match the Windows API exactly for proper marshaling.
/// </summary>
#pragma warning disable IDE1006 // Naming Styles - Win32 structures match Windows API naming

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public int type; // INPUT_KEYBOARD = 1
    public InputUnion u; // Union wrapper for keyboard/mouse/hardware input
}

/// <summary>
/// InputUnion uses Explicit layout to represent the Windows API union.
/// All members start at offset 0 (overlapping memory).
/// All union members must be defined even if only one is used.
/// </summary>
[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)]
    public MOUSEINPUT mi; // Mouse input union member
    [FieldOffset(0)]
    public KEYBDINPUT ki; // Keyboard input union member
    [FieldOffset(0)]
    public HARDWAREINPUT hi; // Hardware input union member
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    public int dx;
    public int dy;
    public uint mouseData;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct HARDWAREINPUT
{
    public uint uMsg;
    public ushort wParamL;
    public ushort wParamH;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    public ushort wVk;
    public ushort wScan;
    public uint dwFlags;
    public uint time;
    public IntPtr dwExtraInfo;
}

#pragma warning restore IDE1006 // Naming Styles
