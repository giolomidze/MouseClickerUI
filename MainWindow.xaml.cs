using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace MouseClickerUI
{
    public class ProcessInfo
    {
        public string ProcessName { get; set; }
        public string MainWindowTitle { get; set; }
        public int Id { get; set; }
        public IntPtr MainWindowHandle { get; set; }

        public ProcessInfo(string processName, string mainWindowTitle, int id, IntPtr mainWindowHandle)
        {
            ProcessName = processName;
            MainWindowTitle = mainWindowTitle;
            Id = id;
            MainWindowHandle = mainWindowHandle;
        }
    }

    public partial class MainWindow
    {
        private static bool _clicking;
        private static bool _listening;
        private static bool _prevEnableListeningState;
        private static bool _prevDisableListeningState;
        private static bool _prevEnableClickingState;
        private static bool _mouseMoving;
        private static bool _prevEnableMouseMovingState;
        private static bool _randomWasdEnabled;
        private static bool _prevEnableRandomWasdState;
        private static DateTime _lastWasdKeyPressTime;
        private static int _nextWasdIntervalMs;
        private static int _mouseMovementDirection;
        private static int _mouseMovementStep;
        private static int _currentMovementRange;
#pragma warning disable IDE1006 // Naming Styles - matches existing codebase convention
        private static readonly Random _random = new Random();
#pragma warning restore IDE1006 // Naming Styles
        private static int _targetProcessId;
        private static IntPtr _targetWindowHandle = IntPtr.Zero;
        private static string _targetProcessName = string.Empty;
        private static string _targetWindowTitle = string.Empty;
        private static int _clickDelay = 1;
        private readonly DispatcherTimer _timer;
        private readonly DispatcherTimer _pollingTimer;
        private List<string> _cachedProcessNames = [];

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, uint dwData, UIntPtr dwExtraInfo);

        private const uint MouseEventLetdown = 0x02;
        private const uint MouseEventLeftUp = 0x04;
        private const uint MouseEventMove = 0x01;

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();


        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("kernel32.dll")]
        private static extern uint GetLastError();

#pragma warning disable IDE1006 // Naming Styles - Win32 API constants match Windows API naming
        // Virtual key codes for WASD keys
        private const ushort VK_W = 0x57; // W key
        private const ushort VK_A = 0x41; // A key
        private const ushort VK_S = 0x53; // S key
        private const ushort VK_D = 0x44; // D key

        // INPUT structure for SendInput - must match Windows API exactly
        // Proven pattern: Sequential layout with union wrapper for proper 64-bit alignment
        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public int type; // INPUT_KEYBOARD = 1 (changed to int for Win32 compatibility)
            public InputUnion u; // Union wrapper for keyboard/mouse/hardware input
        }

        // InputUnion uses Explicit layout to represent the Windows API union
        // All members start at offset 0 (overlapping memory)
        // All union members must be defined even if only one is used
        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi; // Mouse input union member
            [FieldOffset(0)]
            public KEYBDINPUT ki; // Keyboard input union member
            [FieldOffset(0)]
            public HARDWAREINPUT hi; // Hardware input union member
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private const int INPUT_KEYBOARD = 1;
        private const uint KEYEVENTF_KEYUP = 0x0002;
#pragma warning restore IDE1006 // Naming Styles

        private static bool IsKeyPressed(int keyCode)
        {
            return (GetKeyState(keyCode) & 0x8000) != 0;
        }

        private static bool IsTargetWindow()
        {
            if (_targetProcessId == 0)
            {
                return false;
            }

            var foregroundWindow = GetForegroundWindow();
            
            if (foregroundWindow == IntPtr.Zero)
            {
                return false; // No foreground window
            }
            
            // Primary check: Verify foreground window handle matches stored handle
            if (_targetWindowHandle != IntPtr.Zero && foregroundWindow == _targetWindowHandle)
            {
                return true;
            }

            // Secondary check: Verify Process ID matches
            if (GetWindowThreadProcessId(foregroundWindow, out uint processId) != 0 && 
                processId == _targetProcessId)
            {
                // Window handle changed but same process - update stored handle
                _targetWindowHandle = foregroundWindow;
                return true;
            }

            // Fallback: Attempt re-detection by Process Name
            if (!string.IsNullOrEmpty(_targetProcessName))
            {
                try
                {
                    var processes = Process.GetProcessesByName(_targetProcessName)
                        .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                        .ToList();

                    if (processes.Count > 0)
                    {
                        // Find the process with matching window title (if available)
                        var matchingProcess = processes.FirstOrDefault(p => 
                            !string.IsNullOrEmpty(_targetWindowTitle) && 
                            p.MainWindowTitle.Contains(_targetWindowTitle)) ?? processes.First();

                        _targetProcessId = matchingProcess.Id;
                        _targetWindowHandle = matchingProcess.MainWindowHandle;
                        _targetWindowTitle = matchingProcess.MainWindowTitle;
                        
                        // Dispose all Process objects
                        foreach (var process in processes)
                        {
                            process.Dispose();
                        }
                        
                        return true;
                    }
                    
                    // Dispose all Process objects if no match found
                    foreach (var process in processes)
                    {
                        process.Dispose();
                    }
                }
                catch
                {
                    // Process no longer exists or access denied
                    return false;
                }
            }

            return false;
        }

        public MainWindow()
        {
            InitializeComponent();
            LoadProcesses();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(10)
            };
            _timer.Tick += Timer_Tick;

            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollingTimer.Tick += PollingTimer_Tick;
            _pollingTimer.Start();

            ComboBoxProcesses.GotFocus += ComboBoxProcesses_GotFocus;
        }

        private void ComboBoxProcesses_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBlockValidationMessage.Visibility = Visibility.Collapsed;
        }

        private void LoadProcesses(string? selectedProcessName = null)
        {
            var processes = Process.GetProcesses()
                .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle))
                .OrderBy(p => p.ProcessName)
                .ToList();

            var processNames = processes.Select(p => p.ProcessName).ToList();

            if (_cachedProcessNames.SequenceEqual(processNames)) 
            {
                // Dispose all Process objects if no changes needed
                foreach (var process in processes)
                {
                    process.Dispose();
                }
                return;
            }

            _cachedProcessNames = processNames;
            
            // Create ProcessInfo objects and dispose Process objects immediately
            var processInfos = processes.Select(p => 
            {
                var processInfo = new ProcessInfo(p.ProcessName, p.MainWindowTitle, p.Id, p.MainWindowHandle);
                p.Dispose();
                return processInfo;
            }).ToList();

            ComboBoxProcesses.ItemsSource = processInfos;
            ComboBoxProcesses.DisplayMemberPath = "MainWindowTitle";
            ComboBoxProcesses.SelectedValuePath = "ProcessName";

            if (!string.IsNullOrEmpty(selectedProcessName))
            {
                ComboBoxProcesses.SelectedValue = selectedProcessName;
            }
        }

        private void PollingTimer_Tick(object? sender, EventArgs e)
        {
            var selectedProcess = ComboBoxProcesses.SelectedItem as ProcessInfo;
            var selectedProcessName = selectedProcess?.ProcessName;
            LoadProcesses(selectedProcessName);

            // Validate target process existence if listening is active
            if (_listening && _targetProcessId > 0)
            {
                try
                {
                    using var targetProcess = Process.GetProcessById(_targetProcessId);
                    if (targetProcess.HasExited)
                    {
                        // Target process has terminated
                        _listening = false;
                        _clicking = false;
                        LabelStatus.Content = "Target application closed";
                        _timer.Stop();
                    }
                }
                catch (ArgumentException)
                {
                    // Process no longer exists
                    _listening = false;
                    _clicking = false;
                    LabelStatus.Content = "Target application closed";
                    _timer.Stop();
                }
            }
        }

        private void buttonStartListening_Click(object sender, RoutedEventArgs e)
        {
            if (ComboBoxProcesses.SelectedItem == null)
            {
                TextBlockValidationMessage.Text = "Please select an application first.";
                TextBlockValidationMessage.Visibility = Visibility.Visible;
                ComboBoxProcesses.Focus();
                MessageBox.Show("Please select an application first.", "Validation", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            TextBlockValidationMessage.Visibility = Visibility.Collapsed;
            var selectedProcess = (ProcessInfo)ComboBoxProcesses.SelectedItem;
            
            // Capture all identifiers for robust targeting
            _targetProcessId = selectedProcess.Id;
            _targetWindowHandle = selectedProcess.MainWindowHandle;
            _targetProcessName = selectedProcess.ProcessName;
            _targetWindowTitle = selectedProcess.MainWindowTitle;
            
            _listening = true;
            LabelStatus.Content = $"Listening enabled for {_targetWindowTitle}";
            _timer.Start();
        }

        private void buttonStopListening_Click(object sender, RoutedEventArgs e)
        {
            _listening = false;
            _clicking = false;
            LabelStatus.Content = "Listening disabled";
            _timer.Stop();
        }

        private void SliderDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (TextBoxDelay != null)
            {
                _clickDelay = (int)e.NewValue;
                TextBoxDelay.Text = _clickDelay.ToString();
                
                // Update timer interval based on click delay
                _timer.Interval = TimeSpan.FromMilliseconds(_clickDelay);
            }
        }

        private void TextBoxDelay_KeyUp(object sender, KeyEventArgs e)
        {
            if (int.TryParse(TextBoxDelay.Text, out int newDelay))
            {
                if (newDelay < (int)SliderDelay.Minimum)
                {
                    newDelay = (int)SliderDelay.Minimum;
                }
                else if (newDelay > (int)SliderDelay.Maximum)
                {
                    newDelay = (int)SliderDelay.Maximum;
                }

                _clickDelay = newDelay;
                SliderDelay.Value = newDelay;
                
                // Update timer interval based on click delay
                _timer.Interval = TimeSpan.FromMilliseconds(_clickDelay);
            }
            else
            {
                TextBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void TextBoxDelay_LostFocus(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TextBoxDelay.Text, out int newDelay))
            {
                if (newDelay < (int)SliderDelay.Minimum)
                {
                    newDelay = (int)SliderDelay.Minimum;
                }
                else if (newDelay > (int)SliderDelay.Maximum)
                {
                    newDelay = (int)SliderDelay.Maximum;
                }

                _clickDelay = newDelay;
                SliderDelay.Value = newDelay;
                
                // Update timer interval based on click delay
                _timer.Interval = TimeSpan.FromMilliseconds(_clickDelay);
            }
            else
            {
                TextBoxDelay.Text = _clickDelay.ToString();
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            UpdateMouseClickingState();
        }

        private void UpdateMouseClickingState()
        {
            var wasTargetWindow = IsTargetWindow();
            if (!wasTargetWindow)
            {
                return;
            }

            // Check if we just re-detected the target window
            if (_targetWindowHandle != IntPtr.Zero)
            {
                var foregroundWindow = GetForegroundWindow();
                if (foregroundWindow == _targetWindowHandle && _listening)
                {
                    // Non-intrusive status update for re-detection
                    LabelStatus.Content = $"Target window re-detected at {DateTime.Now:HH:mm:ss}";
                }
            }

            var isKey1Pressed = IsKeyPressed(0x31); // Key '1'
            var isKey0Pressed = IsKeyPressed(0x30); // Key '0'
            var isKey8Pressed = IsKeyPressed(0x38); // Key '8'
            var isKey9Pressed = IsKeyPressed(0x39); // Key '9'
            var isKey7Pressed = IsKeyPressed(0x37); // Key '7'
            var isKey6Pressed = IsKeyPressed(0x36); // Key '6'

            if (isKey1Pressed && !_prevEnableListeningState)
            {
                _listening = true;
                LabelStatus.Content = $"Listening enabled at {DateTime.Now}";
            }

            _prevEnableListeningState = isKey1Pressed;

            if (isKey0Pressed && !_prevDisableListeningState)
            {
                _listening = false;
                _clicking = false;
                _mouseMoving = false;
                _randomWasdEnabled = false;
                LabelStatus.Content = $"Listening disabled at {DateTime.Now}";
            }

            _prevDisableListeningState = isKey0Pressed;

            if (_listening && isKey8Pressed && !_prevEnableClickingState)
            {
                _clicking = true;
                LabelStatus.Content = $"Mouse clicking enabled at {DateTime.Now}";
            }

            _prevEnableClickingState = isKey8Pressed;

            if (_listening && isKey9Pressed && _clicking)
            {
                _clicking = false;
                LabelStatus.Content = $"Mouse clicking disabled at {DateTime.Now}";
            }

            if (_listening && isKey7Pressed && !_prevEnableMouseMovingState)
            {
                _mouseMoving = !_mouseMoving;
                if (_mouseMoving)
                {
                    // Reset movement state when starting
                    _mouseMovementStep = 0;
                    _mouseMovementDirection = 1;
                    _currentMovementRange = 0; // Will be initialized on first movement step
                    LabelStatus.Content = $"Mouse movement enabled at {DateTime.Now}";
                }
                else
                {
                    LabelStatus.Content = $"Mouse movement disabled at {DateTime.Now}";
                }
            }

            _prevEnableMouseMovingState = isKey7Pressed;

            if (_listening && isKey6Pressed && !_prevEnableRandomWasdState)
            {
                _randomWasdEnabled = !_randomWasdEnabled;
                if (_randomWasdEnabled)
                {
                    // Reset timing state when enabling
                    _lastWasdKeyPressTime = DateTime.MinValue;
                    _nextWasdIntervalMs = 0;
                    LabelStatus.Content = $"Random WASD enabled at {DateTime.Now}";
                }
                else
                {
                    LabelStatus.Content = $"Random WASD disabled at {DateTime.Now}";
                }
            }

            _prevEnableRandomWasdState = isKey6Pressed;

            if (_clicking)
            {
                SimulateMouseClick();
            }

            if (_mouseMoving)
            {
                SimulateMouseMovement();
            }

            if (_randomWasdEnabled)
            {
                SimulateRandomWasd();
            }
        }

        private static void SimulateMouseClick()
        {
            mouse_event(MouseEventLetdown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        }

        private static void SimulateKeyPress(ushort virtualKeyCode)
        {
            // Verify target window is in focus before sending keys
            // This ensures keys are only sent to the target application
            if (!IsTargetWindow())
            {
                return; // Don't send keys if target window isn't active
            }
            
            INPUT[] inputs = new INPUT[2];
            
            // Key down event
            inputs[0] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = 0,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            
            // Key up event
            inputs[1] = new INPUT
            {
                type = INPUT_KEYBOARD,
                u = new InputUnion
                {
                    ki = new KEYBDINPUT
                    {
                        wVk = virtualKeyCode,
                        wScan = 0,
                        dwFlags = KEYEVENTF_KEYUP,
                        time = 0,
                        dwExtraInfo = IntPtr.Zero
                    }
                }
            };
            
            // Send both key down and key up events
            // Marshal.SizeOf correctly calculates size including padding (40 bytes on 64-bit)
            uint result = SendInput(2, inputs, Marshal.SizeOf(typeof(INPUT)));
            
            // SendInput returns the number of events successfully sent
            // Should be 2 (key down + key up). If not, the send failed.
            if (result != 2)
            {
                // SendInput failed - check the error code
                uint errorCode = GetLastError();
                Debug.WriteLine($"[SendInput] Failed to send key press. Expected 2 events, got {result}. Error code: {errorCode} (0x{errorCode:X8})");
                // Common error codes:
                // 5 = ACCESS_DENIED (requires admin or proper permissions)
                // 87 = ERROR_INVALID_PARAMETER (invalid INPUT structure)
                // 578 = ERROR_HOOK_TYPE_INCOMPATIBLE (UIPI blocking)
            }
        }

        private static void SimulateMouseMovement()
        {
            const int baseMovementRangeMin = 25; // Minimum movement range
            const int baseMovementRangeMax = 35; // Maximum movement range
            const int stepsPerDirection = 10; // Number of steps to complete one direction
            const int perStepRandomOffset = 3; // Max random offset per step in pixels
            
            // When we complete one full cycle (both directions), reset and randomize movement range
            if (_mouseMovementStep >= stepsPerDirection * 2)
            {
                _mouseMovementStep = 0;
                _mouseMovementDirection *= -1; // Reverse direction for next cycle
                // Randomize movement range for the new cycle (25-35 pixels)
                _currentMovementRange = _random.Next(baseMovementRangeMin, baseMovementRangeMax + 1);
            }
            
            // Initialize movement range on first call
            if (_mouseMovementStep == 0 && _currentMovementRange == 0)
            {
                _currentMovementRange = _random.Next(baseMovementRangeMin, baseMovementRangeMax + 1);
            }
            
            // Calculate smooth movement using a sine wave pattern
            var progress = (double)_mouseMovementStep / stepsPerDirection;
            var sineValue = Math.Sin(progress * Math.PI); // 0 to π gives smooth 0 to 1 to 0
            var horizontalMovement = (int)(sineValue * _currentMovementRange * _mouseMovementDirection);
            
            // For vertical movement, use a cosine wave (90 degrees out of phase) for smooth up-down motion
            var cosineValue = Math.Cos(progress * Math.PI); // 0 to π gives smooth 1 to -1 to 1
            var verticalMovement = (int)(cosineValue * _currentMovementRange);
            
            // Add small random offset to each step (±perStepRandomOffset pixels)
            var horizontalOffset = _random.Next(-perStepRandomOffset, perStepRandomOffset + 1);
            var verticalOffset = _random.Next(-perStepRandomOffset, perStepRandomOffset + 1);
            horizontalMovement += horizontalOffset;
            verticalMovement += verticalOffset;
            
            // Move mouse both horizontally and vertically
            mouse_event(MouseEventMove, horizontalMovement, verticalMovement, 0, UIntPtr.Zero);
            
            // Update step counter
            _mouseMovementStep++;
        }

        private static void SimulateRandomWasd()
        {
            const int minKeyPressIntervalMs = 200; // Minimum time between key presses (ms)
            const int maxKeyPressIntervalMs = 600; // Maximum time between key presses (ms)
            
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
                // Randomly select one of the WASD keys
                ushort[] wasdKeys = { VK_W, VK_A, VK_S, VK_D };
                ushort selectedKey = wasdKeys[_random.Next(wasdKeys.Length)];
                SimulateKeyPress(selectedKey);
                
                // Update last press time and calculate next interval
                _lastWasdKeyPressTime = now;
                _nextWasdIntervalMs = _random.Next(minKeyPressIntervalMs, maxKeyPressIntervalMs + 1);
            }
        }
    }
}