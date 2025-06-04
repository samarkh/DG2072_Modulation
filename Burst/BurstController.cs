using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DG2072_USB_Control.Burst
{
    public class BurstController
    {
        private readonly RigolDG2072 _device;
        private readonly MainWindow _mainWindow;
        private int _activeChannel;
        private bool _isInitialized = false;
        private bool _isBurstEnabled = false;

        // Timers for debouncing
        private DispatcherTimer _burstCyclesTimer;
        private DispatcherTimer _burstPeriodTimer;
        private DispatcherTimer _triggerDelayTimer;
        private DispatcherTimer _startPhaseTimer;
        private DispatcherTimer _userIdleLevelTimer;

        public event EventHandler<string> LogEvent;

        // Helper method to get the burst panel
        private BurstPanel GetBurstPanel()
        {
            return _mainWindow.BurstPanelControl;
        }

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public bool IsEnabled => _isBurstEnabled;

        public BurstController(RigolDG2072 device, int activeChannel, MainWindow mainWindow)
        {
            _device = device;
            _activeChannel = activeChannel;
            _mainWindow = mainWindow;
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _burstCyclesTimer = CreateDebounceTimer(() => ApplyBurstCycles());
            _burstPeriodTimer = CreateDebounceTimer(() => ApplyBurstPeriod());
            _triggerDelayTimer = CreateDebounceTimer(() => ApplyTriggerDelay());
            _startPhaseTimer = CreateDebounceTimer(() => ApplyStartPhase());
            _userIdleLevelTimer = CreateDebounceTimer(() => ApplyUserIdleLevel());
        }

        private DispatcherTimer CreateDebounceTimer(Action action)
        {
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            timer.Tick += (s, e) =>
            {
                timer.Stop();
                action();
            };
            return timer;
        }

        public void InitializeUI()
        {
            if (_isInitialized) return;

            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel == null)
                {
                    Log("BurstPanel UserControl not found");
                    return;
                }

                // Initialize the panel with reference to this controller
                burstPanel.Initialize(this);
                burstPanel.SetInitializing(true);

                // Initialize burst mode combo box
                if (burstPanel.BurstModeComboBox != null)
                {
                    burstPanel.BurstModeComboBox.Items.Clear();
                    burstPanel.BurstModeComboBox.Items.Add(new ComboBoxItem { Content = "N-Cycle", Tag = "TRIG" });
                    burstPanel.BurstModeComboBox.Items.Add(new ComboBoxItem { Content = "Infinite", Tag = "INF" });
                    burstPanel.BurstModeComboBox.Items.Add(new ComboBoxItem { Content = "Gated", Tag = "GAT" });
                    burstPanel.BurstModeComboBox.SelectedIndex = 0;
                }

                // Initialize idle level combo box
                if (burstPanel.IdleLevelComboBox != null)
                {
                    burstPanel.IdleLevelComboBox.Items.Clear();
                    burstPanel.IdleLevelComboBox.Items.Add(new ComboBoxItem { Content = "First Point", Tag = "FPT" });
                    burstPanel.IdleLevelComboBox.Items.Add(new ComboBoxItem { Content = "Bottom", Tag = "BOTTOM" });
                    burstPanel.IdleLevelComboBox.Items.Add(new ComboBoxItem { Content = "Top", Tag = "TOP" });
                    burstPanel.IdleLevelComboBox.Items.Add(new ComboBoxItem { Content = "Center", Tag = "CENTER" });
                    burstPanel.IdleLevelComboBox.Items.Add(new ComboBoxItem { Content = "User", Tag = "USER" });
                    burstPanel.IdleLevelComboBox.SelectedIndex = 0;
                }

                // Initialize trigger source combo box
                if (burstPanel.TriggerSourceComboBox != null)
                {
                    burstPanel.TriggerSourceComboBox.Items.Clear();
                    burstPanel.TriggerSourceComboBox.Items.Add(new ComboBoxItem { Content = "Internal", Tag = "INT" });
                    burstPanel.TriggerSourceComboBox.Items.Add(new ComboBoxItem { Content = "External", Tag = "EXT" });
                    burstPanel.TriggerSourceComboBox.Items.Add(new ComboBoxItem { Content = "Manual", Tag = "MAN" });
                    burstPanel.TriggerSourceComboBox.SelectedIndex = 0;
                }

                // Initialize trigger slope combo box
                if (burstPanel.TriggerSlopeComboBox != null)
                {
                    burstPanel.TriggerSlopeComboBox.Items.Clear();
                    burstPanel.TriggerSlopeComboBox.Items.Add(new ComboBoxItem { Content = "Positive", Tag = "POS" });
                    burstPanel.TriggerSlopeComboBox.Items.Add(new ComboBoxItem { Content = "Negative", Tag = "NEG" });
                    burstPanel.TriggerSlopeComboBox.SelectedIndex = 0;
                }

                // Initialize trigger out combo box
                if (burstPanel.TriggerOutComboBox != null)
                {
                    burstPanel.TriggerOutComboBox.Items.Clear();
                    burstPanel.TriggerOutComboBox.Items.Add(new ComboBoxItem { Content = "Off", Tag = "OFF" });
                    burstPanel.TriggerOutComboBox.Items.Add(new ComboBoxItem { Content = "Rising Edge", Tag = "POS" });
                    burstPanel.TriggerOutComboBox.Items.Add(new ComboBoxItem { Content = "Falling Edge", Tag = "NEG" });
                    burstPanel.TriggerOutComboBox.SelectedIndex = 0;
                }

                // Initialize gate polarity combo box
                if (burstPanel.GatePolarityComboBox != null)
                {
                    burstPanel.GatePolarityComboBox.Items.Clear();
                    burstPanel.GatePolarityComboBox.Items.Add(new ComboBoxItem { Content = "Normal", Tag = "NORM" });
                    burstPanel.GatePolarityComboBox.Items.Add(new ComboBoxItem { Content = "Inverted", Tag = "INV" });
                    burstPanel.GatePolarityComboBox.SelectedIndex = 0;
                }

                burstPanel.SetInitializing(false);
                _isInitialized = true;
                Log("Burst controller UI initialized");
            }
            catch (Exception ex)
            {
                Log($"Error initializing burst UI: {ex.Message}");
            }
        }

        public void EnableBurst()
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:BURS:STAT ON");
                _isBurstEnabled = true;
                UpdateBurstUI(true);
                Log($"Burst enabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error enabling burst: {ex.Message}");
            }
        }

        public void DisableBurst()
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:BURS:STAT OFF");
                _isBurstEnabled = false;
                UpdateBurstUI(false);
                Log($"Burst disabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error disabling burst: {ex.Message}");
            }
        }

        private void UpdateBurstUI(bool enabled)
        {
            _mainWindow.Dispatcher.Invoke(() =>
            {
                // Update toggle button
                if (_mainWindow.BurstToggleButton != null)
                {
                    _mainWindow.BurstToggleButton.Content = enabled ? "Disable Burst" : "Enable Burst";
                    _mainWindow.BurstToggleButton.Background = enabled ?
                        System.Windows.Media.Brushes.LightCoral :
                        System.Windows.Media.Brushes.LightGreen;
                }

                // Show/hide burst panel
                if (_mainWindow.BurstPanelControl?.Visibility != null)
                {
                    _mainWindow.BurstPanelControl.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
                }

                // Enable/disable manual trigger button based on trigger source
                var burstPanel = GetBurstPanel();
                if (burstPanel?.ManualTriggerButton != null && burstPanel?.TriggerSourceComboBox != null)
                {
                    var selectedItem = burstPanel.TriggerSourceComboBox.SelectedItem as ComboBoxItem;
                    bool isManual = selectedItem?.Tag?.ToString() == "MAN";
                    burstPanel.ManualTriggerButton.IsEnabled = enabled && isManual;
                }
            });
        }

        public void RefreshBurstSettings()
        {
            if (!_isInitialized || !_device.IsConnected) return;

            try
            {
                // Check if burst is enabled
                string burstState = _device.SendQuery($":SOUR{_activeChannel}:BURS:STAT?").Trim();
                _isBurstEnabled = (burstState == "ON" || burstState == "1");
                UpdateBurstUI(_isBurstEnabled);

                if (_isBurstEnabled)
                {
                    var burstPanel = GetBurstPanel();
                    if (burstPanel != null)
                    {
                        burstPanel.SetInitializing(true);
                    }

                    // Refresh all burst parameters
                    RefreshBurstMode();
                    RefreshBurstCycles();
                    RefreshBurstPeriod();
                    RefreshTriggerDelay();
                    RefreshStartPhase();
                    RefreshIdleLevel();
                    RefreshTriggerSettings();

                    if (burstPanel != null)
                    {
                        burstPanel.SetInitializing(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing burst settings: {ex.Message}");
            }
        }

        private void RefreshBurstMode()
        {
            try
            {
                string burstMode = _device.SendQuery($":SOUR{_activeChannel}:BURS:MODE?").Trim().ToUpper();

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    var burstPanel = GetBurstPanel();
                    if (burstPanel?.BurstModeComboBox != null)
                    {
                        for (int i = 0; i < burstPanel.BurstModeComboBox.Items.Count; i++)
                        {
                            var item = burstPanel.BurstModeComboBox.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == burstMode ||
                                (burstMode.StartsWith("TRIG") && item?.Tag?.ToString() == "TRIG") ||
                                (burstMode.StartsWith("INF") && item?.Tag?.ToString() == "INF") ||
                                (burstMode.StartsWith("GAT") && item?.Tag?.ToString() == "GAT"))
                            {
                                burstPanel.BurstModeComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing burst mode: {ex.Message}");
            }
        }

        private void RefreshBurstCycles()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:BURS:NCYC?");
                if (int.TryParse(response, out int cycles))
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        var burstPanel = GetBurstPanel();
                        if (burstPanel?.BurstCyclesTextBox != null)
                            burstPanel.BurstCyclesTextBox.Text = cycles.ToString();
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing burst cycles: {ex.Message}");
            }
        }

        private void RefreshBurstPeriod()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:BURS:INT:PER?");
                if (double.TryParse(response, out double period))
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        var burstPanel = GetBurstPanel();
                        if (burstPanel?.BurstPeriodTextBox != null)
                        {
                            // Convert to appropriate unit
                            ConvertAndDisplayTime(period, burstPanel.BurstPeriodTextBox, burstPanel.BurstPeriodUnitComboBox);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing burst period: {ex.Message}");
            }
        }

        private void RefreshTriggerDelay()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:BURS:TDEL?");
                if (double.TryParse(response, out double delay))
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        var burstPanel = GetBurstPanel();
                        if (burstPanel?.TriggerDelayTextBox != null)
                        {
                            ConvertAndDisplayTime(delay, burstPanel.TriggerDelayTextBox, burstPanel.TriggerDelayUnitComboBox);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing trigger delay: {ex.Message}");
            }
        }

        private void RefreshStartPhase()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:BURS:PHAS?");
                if (double.TryParse(response, out double phase))
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        var burstPanel = GetBurstPanel();
                        if (burstPanel?.StartPhaseTextBox != null)
                            burstPanel.StartPhaseTextBox.Text = phase.ToString("F1");
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing start phase: {ex.Message}");
            }
        }

        private void RefreshIdleLevel()
        {
            try
            {
                string idleLevel = _device.SendQuery($":SOUR{_activeChannel}:BURS:IDLE?").Trim().ToUpper();

                _mainWindow.Dispatcher.Invoke(() =>
                {
                    var burstPanel = GetBurstPanel();
                    if (burstPanel?.IdleLevelComboBox != null)
                    {
                        for (int i = 0; i < burstPanel.IdleLevelComboBox.Items.Count; i++)
                        {
                            var item = burstPanel.IdleLevelComboBox.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == idleLevel)
                            {
                                burstPanel.IdleLevelComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    // If USER, also refresh the user idle level value
                    if (idleLevel == "USER")
                    {
                        RefreshUserIdleLevel();
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing idle level: {ex.Message}");
            }
        }

        private void RefreshUserIdleLevel()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:BURS:IDLE:LEV?");
                if (double.TryParse(response, out double level))
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        var burstPanel = GetBurstPanel();
                        if (burstPanel?.UserIdleLevelTextBox != null)
                        {
                            ConvertAndDisplayVoltage(level, burstPanel.UserIdleLevelTextBox, burstPanel.UserIdleLevelUnitComboBox);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing user idle level: {ex.Message}");
            }
        }

        private void RefreshTriggerSettings()
        {
            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel == null) return;

                // Refresh trigger source
                string trigSource = _device.SendQuery($":SOUR{_activeChannel}:BURS:TRIG:SOUR?").Trim().ToUpper();
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    if (burstPanel.TriggerSourceComboBox != null)
                    {
                        for (int i = 0; i < burstPanel.TriggerSourceComboBox.Items.Count; i++)
                        {
                            var item = burstPanel.TriggerSourceComboBox.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == trigSource.Substring(0, 3))
                            {
                                burstPanel.TriggerSourceComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });

                // Refresh trigger slope
                string trigSlope = _device.SendQuery($":SOUR{_activeChannel}:BURS:TRIG:SLOP?").Trim().ToUpper();
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    if (burstPanel.TriggerSlopeComboBox != null)
                    {
                        for (int i = 0; i < burstPanel.TriggerSlopeComboBox.Items.Count; i++)
                        {
                            var item = burstPanel.TriggerSlopeComboBox.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == trigSlope.Substring(0, 3))
                            {
                                burstPanel.TriggerSlopeComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });

                // Refresh trigger out
                string trigOut = _device.SendQuery($":SOUR{_activeChannel}:BURS:TRIG:TRIGO?").Trim().ToUpper();
                _mainWindow.Dispatcher.Invoke(() =>
                {
                    if (burstPanel.TriggerOutComboBox != null)
                    {
                        for (int i = 0; i < burstPanel.TriggerOutComboBox.Items.Count; i++)
                        {
                            var item = burstPanel.TriggerOutComboBox.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == trigOut)
                            {
                                burstPanel.TriggerOutComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });

                // Refresh gate polarity if in gated mode
                string burstMode = _device.SendQuery($":SOUR{_activeChannel}:BURS:MODE?").Trim().ToUpper();
                if (burstMode.StartsWith("GAT"))
                {
                    string gatePolarity = _device.SendQuery($":SOUR{_activeChannel}:BURS:GATE:POL?").Trim().ToUpper();
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        if (burstPanel.GatePolarityComboBox != null)
                        {
                            for (int i = 0; i < burstPanel.GatePolarityComboBox.Items.Count; i++)
                            {
                                var item = burstPanel.GatePolarityComboBox.Items[i] as ComboBoxItem;
                                if (item?.Tag?.ToString() == gatePolarity.Substring(0, 3))
                                {
                                    burstPanel.GatePolarityComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing trigger settings: {ex.Message}");
            }
        }

        // Apply methods for each parameter
        private void ApplyBurstCycles()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel?.BurstCyclesTextBox != null &&
                    int.TryParse(burstPanel.BurstCyclesTextBox.Text, out int cycles))
                {
                    // Validate range (1-1000000)
                    cycles = Math.Max(1, Math.Min(1000000, cycles));
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:NCYC {cycles}");
                    Log($"Set burst cycles to {cycles}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting burst cycles: {ex.Message}");
            }
        }

        private void ApplyBurstPeriod()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel == null) return;

                double period = ParseTimeWithUnit(burstPanel.BurstPeriodTextBox.Text,
                    burstPanel.BurstPeriodUnitComboBox);

                // Validate range (2us to 8000s)
                period = Math.Max(0.000002, Math.Min(8000, period));
                _device.SendCommand($":SOUR{_activeChannel}:BURS:INT:PER {period}");
                Log($"Set burst period to {period} seconds");
            }
            catch (Exception ex)
            {
                Log($"Error setting burst period: {ex.Message}");
            }
        }

        private void ApplyTriggerDelay()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel == null) return;

                double delay = ParseTimeWithUnit(burstPanel.TriggerDelayTextBox.Text,
                    burstPanel.TriggerDelayUnitComboBox);

                // Validate range (0 to 100s)
                delay = Math.Max(0, Math.Min(100, delay));
                _device.SendCommand($":SOUR{_activeChannel}:BURS:TDEL {delay}");
                Log($"Set trigger delay to {delay} seconds");
            }
            catch (Exception ex)
            {
                Log($"Error setting trigger delay: {ex.Message}");
            }
        }

        private void ApplyStartPhase()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel?.StartPhaseTextBox != null &&
                    double.TryParse(burstPanel.StartPhaseTextBox.Text, out double phase))
                {
                    // Validate range (0-360)
                    phase = Math.Max(0, Math.Min(360, phase));
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:PHAS {phase}");
                    Log($"Set start phase to {phase} degrees");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting start phase: {ex.Message}");
            }
        }

        private void ApplyUserIdleLevel()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                if (burstPanel == null) return;

                double level = ParseVoltageWithUnit(burstPanel.UserIdleLevelTextBox.Text,
                    burstPanel.UserIdleLevelUnitComboBox);

                _device.SendCommand($":SOUR{_activeChannel}:BURS:IDLE:LEV {level}");
                Log($"Set user idle level to {level} V");
            }
            catch (Exception ex)
            {
                Log($"Error setting user idle level: {ex.Message}");
            }
        }

        // Event handlers for UI changes
        public void OnBurstModeChanged()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                var selectedItem = burstPanel?.BurstModeComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string burstMode = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:MODE {burstMode}");
                    Log($"Set burst mode to {selectedItem.Content}");

                    // Update UI visibility based on mode
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        bool isNCycle = (burstMode == "TRIG");
                        bool isGated = (burstMode == "GAT");

                        // Cycles are only for N-Cycle mode
                        if (burstPanel?.BurstCyclesTextBox != null)
                            burstPanel.BurstCyclesTextBox.IsEnabled = isNCycle;

                        // Period is only for internal trigger (infinite mode)
                        if (burstPanel?.BurstPeriodTextBox != null)
                            burstPanel.BurstPeriodTextBox.IsEnabled = (burstMode == "INF");

                        // Gate settings visibility
                        if (burstPanel?.GateSettingsGroupBox != null)
                            burstPanel.GateSettingsGroupBox.Visibility = isGated ? Visibility.Visible : Visibility.Collapsed;
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing burst mode: {ex.Message}");
            }
        }

        public void OnIdleLevelChanged()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                var selectedItem = burstPanel?.IdleLevelComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string idleLevel = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:IDLE {idleLevel}");
                    Log($"Set idle level to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing idle level: {ex.Message}");
            }
        }

        public void OnTriggerSourceChanged()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                var selectedItem = burstPanel?.TriggerSourceComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string trigSource = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:TRIG:SOUR {trigSource}");
                    Log($"Set trigger source to {selectedItem.Content}");

                    // Enable manual trigger button only for manual mode
                    bool isManual = (trigSource == "MAN");
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        if (burstPanel?.ManualTriggerButton != null)
                            burstPanel.ManualTriggerButton.IsEnabled = _isBurstEnabled && isManual;
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing trigger source: {ex.Message}");
            }
        }

        public void OnTriggerSlopeChanged()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                var selectedItem = burstPanel?.TriggerSlopeComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string trigSlope = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:TRIG:SLOP {trigSlope}");
                    Log($"Set trigger slope to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing trigger slope: {ex.Message}");
            }
        }

        public void OnTriggerOutChanged()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                var selectedItem = burstPanel?.TriggerOutComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string trigOut = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:TRIG:TRIGO {trigOut}");
                    Log($"Set trigger output to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing trigger output: {ex.Message}");
            }
        }

        public void OnGatePolarityChanged()
        {
            if (!_isBurstEnabled) return;

            try
            {
                var burstPanel = GetBurstPanel();
                var selectedItem = burstPanel?.GatePolarityComboBox.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string gatePolarity = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:BURS:GATE:POL {gatePolarity}");
                    Log($"Set gate polarity to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing gate polarity: {ex.Message}");
            }
        }

        public void ExecuteManualTrigger()
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:BURS:TRIG");
                Log("Manual burst trigger executed");
            }
            catch (Exception ex)
            {
                Log($"Error executing manual trigger: {ex.Message}");
            }
        }

        // Debounced event handlers
        public void OnBurstCyclesChanged() { _burstCyclesTimer.Stop(); _burstCyclesTimer.Start(); }
        public void OnBurstPeriodChanged() { _burstPeriodTimer.Stop(); _burstPeriodTimer.Start(); }
        public void OnTriggerDelayChanged() { _triggerDelayTimer.Stop(); _triggerDelayTimer.Start(); }
        public void OnStartPhaseChanged() { _startPhaseTimer.Stop(); _startPhaseTimer.Start(); }
        public void OnUserIdleLevelChanged() { _userIdleLevelTimer.Stop(); _userIdleLevelTimer.Start(); }

        // Helper methods
        private double ParseTimeWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double time))
                throw new ArgumentException("Invalid time value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "s";
            double multiplier = unit switch
            {
                "s" => 1,
                "ms" => 1e-3,
                "µs" => 1e-6,
                "ns" => 1e-9,
                _ => 1
            };

            return time * multiplier;
        }

        private double ParseVoltageWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double voltage))
                throw new ArgumentException("Invalid voltage value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "V";
            double multiplier = unit switch
            {
                "V" => 1,
                "mV" => 1e-3,
                _ => 1
            };

            return voltage * multiplier;
        }

        private void ConvertAndDisplayTime(double timeInSeconds, TextBox textBox, ComboBox unitComboBox)
        {
            // Select best unit
            string bestUnit;
            double displayValue;

            if (timeInSeconds >= 1)
            {
                bestUnit = "s";
                displayValue = timeInSeconds;
            }
            else if (timeInSeconds >= 1e-3)
            {
                bestUnit = "ms";
                displayValue = timeInSeconds * 1e3;
            }
            else if (timeInSeconds >= 1e-6)
            {
                bestUnit = "µs";
                displayValue = timeInSeconds * 1e6;
            }
            else
            {
                bestUnit = "ns";
                displayValue = timeInSeconds * 1e9;
            }

            textBox.Text = displayValue.ToString("F3");

            // Select unit in combo box
            for (int i = 0; i < unitComboBox.Items.Count; i++)
            {
                var item = unitComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == bestUnit)
                {
                    unitComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void ConvertAndDisplayVoltage(double voltageInVolts, TextBox textBox, ComboBox unitComboBox)
        {
            // Select best unit
            string bestUnit;
            double displayValue;

            if (Math.Abs(voltageInVolts) >= 0.1)
            {
                bestUnit = "V";
                displayValue = voltageInVolts;
            }
        }
    }
}
