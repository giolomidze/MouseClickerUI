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
    private readonly ConfigService _configService;

    // Features
    private readonly MouseClickerFeature _mouseClickerFeature;
    private readonly MouseMovementFeature _mouseMovementFeature;
    private readonly RandomWasdFeature _randomWasdFeature;

    // Configuration
    private readonly AppConfig _config;
    private bool _autoDetectPaused;

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
        _processManager = new ProcessManager(new SystemProcessEnumerator());

        // Load configuration
        _configService = new ConfigService();
        _config = _configService.LoadConfig();

        // Initialize features
        _mouseClickerFeature = new MouseClickerFeature(_inputSimulator);
        _mouseMovementFeature = new MouseMovementFeature(_inputSimulator);
        _randomWasdFeature = new RandomWasdFeature(_inputSimulator, _windowManager, _state);

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

        // Initialize UI
        LoadProcesses();
        InitializeTargetModeControls();

        ComboBoxProcesses.GotFocus += ComboBoxProcesses_GotFocus;
    }

    #region Target Mode Controls

    private void InitializeTargetModeControls()
    {
        if (_config.IsAutoDetectEnabled)
        {
            RadioAutoDetect.IsChecked = true;
        }
        else
        {
            RadioManual.IsChecked = true;
        }
    }

    private void RadioAutoDetect_Checked(object sender, RoutedEventArgs e)
    {
        if (_state == null) return;

        _autoDetectPaused = false;

        if (_state.IsListening && !_state.IsAutoDetected)
        {
            _state.StopAll();
            _timer.Stop();
        }

        if (_config.IsAutoDetectEnabled)
        {
            // Process already configured — disable dropdown and start watching
            ComboBoxProcesses.IsEnabled = false;
            ButtonStartListening.IsEnabled = false;
            TextBlockAutoDetectInfo.Text = $"Target: {_config.TargetProcessName}";
            TextBlockAutoDetectInfo.Visibility = Visibility.Visible;
            LabelStatus.Content = $"Auto-detect: waiting for {_config.TargetProcessName}...";
            TryAutoDetectTargetProcess();
        }
        else
        {
            // No process configured yet — let user pick one from dropdown
            ComboBoxProcesses.IsEnabled = true;
            ButtonStartListening.IsEnabled = true;
            TextBlockAutoDetectInfo.Visibility = Visibility.Collapsed;
            LabelStatus.Content = "Select a process, then click Start Listening to begin auto-detecting";
        }
    }

    private void RadioManual_Checked(object sender, RoutedEventArgs e)
    {
        if (_state == null) return;

        ComboBoxProcesses.IsEnabled = true;
        ButtonStartListening.IsEnabled = true;
        TextBlockAutoDetectInfo.Visibility = Visibility.Collapsed;
        _autoDetectPaused = true;

        if (_state.IsListening && _state.IsAutoDetected)
        {
            _state.StopAll();
            _timer.Stop();
        }

        LabelStatus.Content = "Select an application and click Start Listening";
    }

    #endregion

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
                _timer.Stop();
                _autoDetectPaused = false;

                if (RadioAutoDetect.IsChecked == true)
                {
                    LabelStatus.Content = $"Auto-detect: {_config.TargetProcessName} closed, waiting...";
                }
                else
                {
                    LabelStatus.Content = "Target application closed";
                }
            }
        }
        else if (!_state.IsListening && RadioAutoDetect.IsChecked == true && !_autoDetectPaused)
        {
            TryAutoDetectTargetProcess();
        }
    }

    private void TryAutoDetectTargetProcess()
    {
        var processInfos = ComboBoxProcesses.ItemsSource as List<ProcessInfo>;
        if (processInfos == null || processInfos.Count == 0)
        {
            return;
        }

        var matchingProcess = processInfos.FirstOrDefault(p =>
            string.Equals(p.ProcessName, _config.TargetProcessName, StringComparison.OrdinalIgnoreCase));

        if (matchingProcess == null)
        {
            return;
        }

        // Found the process — select it and start listening
        ComboBoxProcesses.SelectedItem = matchingProcess;
        _windowManager.SetTargetWindow(matchingProcess);
        _state.IsListening = true;
        _state.IsAutoDetected = true;
        LabelStatus.Content = $"Auto-detected: {matchingProcess.MainWindowTitle}";
        _timer.Start();
    }

    #endregion

    #region Listening Control

    private void buttonStartListening_Click(object sender, RoutedEventArgs e)
    {
        // Resume auto-detect if paused
        if (RadioAutoDetect.IsChecked == true && _config.IsAutoDetectEnabled)
        {
            _autoDetectPaused = false;
            ButtonStartListening.IsEnabled = false;
            LabelStatus.Content = $"Auto-detect: waiting for {_config.TargetProcessName}...";
            TryAutoDetectTargetProcess();
            return;
        }

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

        if (RadioAutoDetect.IsChecked == true)
        {
            // Save process name for auto-detect on future runs
            _config.TargetProcessName = selectedProcess.ProcessName;
            _configService.SaveTargetProcessName(selectedProcess.ProcessName);

            _state.IsAutoDetected = true;
            ComboBoxProcesses.IsEnabled = false;
            ButtonStartListening.IsEnabled = false;
            TextBlockAutoDetectInfo.Text = $"Target: {_config.TargetProcessName}";
            TextBlockAutoDetectInfo.Visibility = Visibility.Visible;
            LabelStatus.Content = $"Auto-detected: {_state.TargetWindowTitle}";
        }
        else
        {
            _state.IsAutoDetected = false;
            LabelStatus.Content = $"Listening enabled for {_state.TargetWindowTitle}";
        }

        _timer.Start();
    }

    private void buttonStopListening_Click(object sender, RoutedEventArgs e)
    {
        _state.StopAll();
        _timer.Stop();
        _autoDetectPaused = true;

        if (RadioAutoDetect.IsChecked == true && _config.IsAutoDetectEnabled)
        {
            ButtonStartListening.IsEnabled = true;
            LabelStatus.Content = "Listening stopped — click Start Listening to resume auto-detect";
        }
        else
        {
            LabelStatus.Content = "Listening disabled";
        }
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

    #region Random WASD Configuration

    private void SliderWasdMinInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextBoxWasdMinInterval != null)
        {
            _state.RandomWasdMinInterval = (int)e.NewValue;
            NormalizeRandomWasdIntervals();
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
    }

    private void TextBoxWasdMinInterval_KeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TextBoxWasdMinInterval.Text, out int newValue))
        {
            if (newValue < (int)SliderWasdMinInterval.Minimum)
            {
                newValue = (int)SliderWasdMinInterval.Minimum;
            }
            else if (newValue > (int)SliderWasdMinInterval.Maximum)
            {
                newValue = (int)SliderWasdMinInterval.Maximum;
            }

            _state.RandomWasdMinInterval = newValue;
            NormalizeRandomWasdIntervals();
            SliderWasdMinInterval.Value = _state.RandomWasdMinInterval;
            SliderWasdMaxInterval.Value = _state.RandomWasdMaxInterval;
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
        else
        {
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
        }
    }

    private void TextBoxWasdMinInterval_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TextBoxWasdMinInterval.Text, out int newValue))
        {
            if (newValue < (int)SliderWasdMinInterval.Minimum)
            {
                newValue = (int)SliderWasdMinInterval.Minimum;
            }
            else if (newValue > (int)SliderWasdMinInterval.Maximum)
            {
                newValue = (int)SliderWasdMinInterval.Maximum;
            }

            _state.RandomWasdMinInterval = newValue;
            NormalizeRandomWasdIntervals();
            SliderWasdMinInterval.Value = _state.RandomWasdMinInterval;
            SliderWasdMaxInterval.Value = _state.RandomWasdMaxInterval;
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
        else
        {
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
        }
    }

    private void SliderWasdMaxInterval_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextBoxWasdMaxInterval != null)
        {
            _state.RandomWasdMaxInterval = (int)e.NewValue;
            NormalizeRandomWasdIntervals();
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
    }

    private void TextBoxWasdMaxInterval_KeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TextBoxWasdMaxInterval.Text, out int newValue))
        {
            if (newValue < (int)SliderWasdMaxInterval.Minimum)
            {
                newValue = (int)SliderWasdMaxInterval.Minimum;
            }
            else if (newValue > (int)SliderWasdMaxInterval.Maximum)
            {
                newValue = (int)SliderWasdMaxInterval.Maximum;
            }

            _state.RandomWasdMaxInterval = newValue;
            NormalizeRandomWasdIntervals();
            SliderWasdMinInterval.Value = _state.RandomWasdMinInterval;
            SliderWasdMaxInterval.Value = _state.RandomWasdMaxInterval;
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
        else
        {
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
    }

    private void TextBoxWasdMaxInterval_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TextBoxWasdMaxInterval.Text, out int newValue))
        {
            if (newValue < (int)SliderWasdMaxInterval.Minimum)
            {
                newValue = (int)SliderWasdMaxInterval.Minimum;
            }
            else if (newValue > (int)SliderWasdMaxInterval.Maximum)
            {
                newValue = (int)SliderWasdMaxInterval.Maximum;
            }

            _state.RandomWasdMaxInterval = newValue;
            NormalizeRandomWasdIntervals();
            SliderWasdMinInterval.Value = _state.RandomWasdMinInterval;
            SliderWasdMaxInterval.Value = _state.RandomWasdMaxInterval;
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
        else
        {
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
        }
    }

    private void SliderWasdClickProb_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (TextBoxWasdClickProb != null)
        {
            _state.RandomWasdClickProbability = (int)e.NewValue;
            TextBoxWasdClickProb.Text = _state.RandomWasdClickProbability.ToString();
        }
    }

    private void TextBoxWasdClickProb_KeyUp(object sender, KeyEventArgs e)
    {
        if (int.TryParse(TextBoxWasdClickProb.Text, out int newValue))
        {
            if (newValue < (int)SliderWasdClickProb.Minimum)
            {
                newValue = (int)SliderWasdClickProb.Minimum;
            }
            else if (newValue > (int)SliderWasdClickProb.Maximum)
            {
                newValue = (int)SliderWasdClickProb.Maximum;
            }

            _state.RandomWasdClickProbability = newValue;
            SliderWasdClickProb.Value = newValue;
        }
        else
        {
            TextBoxWasdClickProb.Text = _state.RandomWasdClickProbability.ToString();
        }
    }

    private void TextBoxWasdClickProb_LostFocus(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TextBoxWasdClickProb.Text, out int newValue))
        {
            if (newValue < (int)SliderWasdClickProb.Minimum)
            {
                newValue = (int)SliderWasdClickProb.Minimum;
            }
            else if (newValue > (int)SliderWasdClickProb.Maximum)
            {
                newValue = (int)SliderWasdClickProb.Maximum;
            }

            _state.RandomWasdClickProbability = newValue;
            SliderWasdClickProb.Value = newValue;
        }
        else
        {
            TextBoxWasdClickProb.Text = _state.RandomWasdClickProbability.ToString();
        }
    }

    private void NormalizeRandomWasdIntervals()
    {
        var minInterval = _state.RandomWasdMinInterval;
        var maxInterval = _state.RandomWasdMaxInterval;

        if (minInterval <= maxInterval)
        {
            return;
        }

        // Swap to ensure valid ordering for Random.Next
        _state.RandomWasdMinInterval = maxInterval;
        _state.RandomWasdMaxInterval = minInterval;

        if (SliderWasdMinInterval != null)
        {
            SliderWasdMinInterval.Value = _state.RandomWasdMinInterval;
        }

        if (SliderWasdMaxInterval != null)
        {
            SliderWasdMaxInterval.Value = _state.RandomWasdMaxInterval;
        }

        if (TextBoxWasdMinInterval != null)
        {
            TextBoxWasdMinInterval.Text = _state.RandomWasdMinInterval.ToString();
        }

        if (TextBoxWasdMaxInterval != null)
        {
            TextBoxWasdMaxInterval.Text = _state.RandomWasdMaxInterval.ToString();
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
            if (RadioAutoDetect.IsChecked == true && _config.IsAutoDetectEnabled)
            {
                _autoDetectPaused = false;
                ButtonStartListening.IsEnabled = false;
                LabelStatus.Content = $"Auto-detect: waiting for {_config.TargetProcessName}...";
                TryAutoDetectTargetProcess();
            }
            else
            {
                _state.IsListening = true;
                LabelStatus.Content = $"Listening enabled at {DateTime.Now}";
            }
        }
        _state.PrevEnableListeningState = isKey1Pressed;

        // Handle key '0' - Disable listening and all features
        if (isKey0Pressed && !_state.PrevDisableListeningState)
        {
            _state.StopAll();
            _autoDetectPaused = true;

            if (RadioAutoDetect.IsChecked == true && _config.IsAutoDetectEnabled)
            {
                ButtonStartListening.IsEnabled = true;
                LabelStatus.Content = "Listening stopped — click Start Listening to resume auto-detect";
            }
            else
            {
                LabelStatus.Content = $"Listening disabled at {DateTime.Now}";
            }
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
