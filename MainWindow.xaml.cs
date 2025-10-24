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

        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow();


        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        private static bool IsKeyPressed(int keyCode)
        {
            return (GetKeyState(keyCode) & 0x8000) != 0;
        }

        private static bool IsTargetWindow()
        {
            if (_targetProcessId == 0 || _targetWindowHandle == IntPtr.Zero)
            {
                return false;
            }

            var foregroundWindow = GetForegroundWindow();
            
            // Primary check: Verify foreground window handle matches stored handle
            if (foregroundWindow == _targetWindowHandle)
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

            if (_clicking)
            {
                SimulateMouseClick();
            }
        }

        private static void SimulateMouseClick()
        {
            mouse_event(MouseEventLetdown, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
        }
    }
}