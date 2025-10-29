using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using MouseClickerUI.Features;
using MouseClickerUI.Models;
using MouseClickerUI.Services;

namespace MouseClickerUI;

public partial class MainWindow
{
    // Application state
    private readonly ApplicationState _state;

    // Services
    private readonly InputSimulator _inputSimulator;
    private readonly WindowManager _windowManager;
    private readonly ProcessManager _processManager;

    // Features
    private readonly MouseClickerFeature _mouseClickerFeature;
    private readonly MouseMovementFeature _mouseMovementFeature;
    private readonly RandomWasdFeature _randomWasdFeature;

    // Timers
    private readonly DispatcherTimer _timer;
    private readonly DispatcherTimer _pollingTimer;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize state
        _state = new ApplicationState();

        // Initialize services
        _inputSimulator = new InputSimulator();
        _windowManager = new WindowManager(_state);
        _processManager = new ProcessManager();

        // Initialize features
        _mouseClickerFeature = new MouseClickerFeature(_inputSimulator);
        _mouseMovementFeature = new MouseMovementFeature(_inputSimulator);
        _randomWasdFeature = new RandomWasdFeature(_inputSimulator, _windowManager);

        // Initialize UI
        LoadProcesses();

        // Setup timers
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

    #region Process Management

    private void ComboBoxProcesses_GotFocus(object sender, RoutedEventArgs e)
    {
        TextBlockValidationMessage.Visibility = Visibility.Collapsed;
    }

    private void LoadProcesses(string? selectedProcessName = null)
    {
        var processInfos = _processManager.LoadProcesses();

        if (processInfos.Count == 0)
        {
            // No changes detected
            return;
        }

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
        if (_state.IsListening && _state.TargetProcessId > 0)
        {
            if (!_windowManager.IsTargetProcessAlive())
            {
                // Target process has terminated
                _state.StopAll();
                LabelStatus.Content = "Target application closed";
                _timer.Stop();
            }
        }
    }

    #endregion

    #region Listening Control

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

        // Set target window
        _windowManager.SetTargetWindow(selectedProcess);

        _state.IsListening = true;
        LabelStatus.Content = $"Listening enabled for {_state.TargetWindowTitle}";
        _timer.Start();
    }

    private void buttonStopListening_Click(object sender, RoutedEventArgs e)
    {
        _state.StopAll();
        LabelStatus.Content = "Listening disabled";
        _timer.Stop();
    }

    #endregion

    #region Delay Configuration

    private void SliderDelay_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextBoxDelay != null)
        {
            _state.ClickDelay = (int)e.NewValue;
            TextBoxDelay.Text = _state.ClickDelay.ToString();

            // Update timer interval based on click delay
            _timer.Interval = TimeSpan.FromMilliseconds(_state.ClickDelay);
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

            _state.ClickDelay = newDelay;
            SliderDelay.Value = newDelay;

            // Update timer interval based on click delay
            _timer.Interval = TimeSpan.FromMilliseconds(_state.ClickDelay);
        }
        else
        {
            TextBoxDelay.Text = _state.ClickDelay.ToString();
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

            _state.ClickDelay = newDelay;
            SliderDelay.Value = newDelay;

            // Update timer interval based on click delay
            _timer.Interval = TimeSpan.FromMilliseconds(_state.ClickDelay);
        }
        else
        {
            TextBoxDelay.Text = _state.ClickDelay.ToString();
        }
    }

    #endregion

    #region Timer and Key State Management

    private void Timer_Tick(object? sender, EventArgs e)
    {
        UpdateKeyStatesAndExecuteFeatures();
    }

    private void UpdateKeyStatesAndExecuteFeatures()
    {
        // Check if target window is in focus
        if (!_windowManager.IsTargetWindow())
        {
            return;
        }

        // Update status if window was re-detected
        if (_state.TargetWindowHandle != IntPtr.Zero && _state.IsListening)
        {
            LabelStatus.Content = $"Target window active at {DateTime.Now:HH:mm:ss}";
        }

        // Check key states
        var isKey1Pressed = _inputSimulator.IsKeyPressed(0x31); // Key '1'
        var isKey0Pressed = _inputSimulator.IsKeyPressed(0x30); // Key '0'
        var isKey8Pressed = _inputSimulator.IsKeyPressed(0x38); // Key '8'
        var isKey9Pressed = _inputSimulator.IsKeyPressed(0x39); // Key '9'
        var isKey7Pressed = _inputSimulator.IsKeyPressed(0x37); // Key '7'
        var isKey6Pressed = _inputSimulator.IsKeyPressed(0x36); // Key '6'

        // Handle key '1' - Enable listening
        if (isKey1Pressed && !_state.PrevEnableListeningState)
        {
            _state.IsListening = true;
            LabelStatus.Content = $"Listening enabled at {DateTime.Now}";
        }
        _state.PrevEnableListeningState = isKey1Pressed;

        // Handle key '0' - Disable listening and all features
        if (isKey0Pressed && !_state.PrevDisableListeningState)
        {
            _state.StopAll();
            LabelStatus.Content = $"Listening disabled at {DateTime.Now}";
        }
        _state.PrevDisableListeningState = isKey0Pressed;

        // Handle key '8' - Enable mouse clicking
        if (_state.IsListening && isKey8Pressed && !_state.PrevEnableClickingState)
        {
            _state.IsClicking = true;
            LabelStatus.Content = $"Mouse clicking enabled at {DateTime.Now}";
        }
        _state.PrevEnableClickingState = isKey8Pressed;

        // Handle key '9' - Disable mouse clicking
        if (_state.IsListening && isKey9Pressed && _state.IsClicking)
        {
            _state.IsClicking = false;
            LabelStatus.Content = $"Mouse clicking disabled at {DateTime.Now}";
        }

        // Handle key '7' - Toggle mouse movement
        if (_state.IsListening && isKey7Pressed && !_state.PrevEnableMouseMovingState)
        {
            _state.IsMouseMoving = !_state.IsMouseMoving;
            if (_state.IsMouseMoving)
            {
                _mouseMovementFeature.Reset();
                LabelStatus.Content = $"Mouse movement enabled at {DateTime.Now}";
            }
            else
            {
                LabelStatus.Content = $"Mouse movement disabled at {DateTime.Now}";
            }
        }
        _state.PrevEnableMouseMovingState = isKey7Pressed;

        // Handle key '6' - Toggle random WASD
        if (_state.IsListening && isKey6Pressed && !_state.PrevEnableRandomWasdState)
        {
            _state.IsRandomWasdEnabled = !_state.IsRandomWasdEnabled;
            if (_state.IsRandomWasdEnabled)
            {
                _randomWasdFeature.Reset();
                LabelStatus.Content = $"Random WASD enabled at {DateTime.Now}";
            }
            else
            {
                LabelStatus.Content = $"Random WASD disabled at {DateTime.Now}";
            }
        }
        _state.PrevEnableRandomWasdState = isKey6Pressed;

        // Execute active features
        if (_state.IsClicking)
        {
            _mouseClickerFeature.Execute();
        }

        if (_state.IsMouseMoving)
        {
            _mouseMovementFeature.Execute();
        }

        if (_state.IsRandomWasdEnabled)
        {
            _randomWasdFeature.Execute();
        }
    }

    #endregion
}
