using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Advanced.Sequence
{
    public class SequenceController
    {
        private readonly RigolDG2072 _device;
        private readonly SequencePanel _sequencePanel;
        private readonly MainWindow _mainWindow;
        private int _activeChannel;
        private bool _isInitialized = false;
        private bool _isSequenceEnabled = false;

        // Timers for debouncing
        private DispatcherTimer _sampleRateTimer;
        private DispatcherTimer _edgeTimeTimer;
        private DispatcherTimer[] _slotPointsTimers;

        public event EventHandler<string> LogEvent;

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public bool IsEnabled => _isSequenceEnabled;

        public SequenceController(RigolDG2072 device, int activeChannel, SequencePanel sequencePanel, MainWindow mainWindow)
        {
            _device = device;
            _activeChannel = activeChannel;
            _sequencePanel = sequencePanel;
            _mainWindow = mainWindow;
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _sampleRateTimer = CreateDebounceTimer(() => ApplySampleRate());
            _edgeTimeTimer = CreateDebounceTimer(() => ApplyEdgeTime());

            // Initialize slot points timers (index 0 unused for 1-based slot numbering)
            _slotPointsTimers = new DispatcherTimer[9];
            for (int i = 1; i <= 8; i++)
            {
                int slotIndex = i; // Capture for closure
                _slotPointsTimers[i] = CreateDebounceTimer(() => ApplySlotPoints(slotIndex));
            }
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
                // Update edge time visibility based on filter type
                UpdateEdgeTimeVisibility();

                _isInitialized = true;
                Log("Sequence controller UI initialized");
            }
            catch (Exception ex)
            {
                Log($"Error initializing Sequence UI: {ex.Message}");
            }
        }

        public void EnableSequence()
        {
            try
            {
                // Apply sequence settings to enable it
                ApplySequenceSettings();
                _isSequenceEnabled = true;
                UpdateSequenceUI(true);
                Log($"Sequence enabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error enabling Sequence: {ex.Message}");
            }
        }

        public void DisableSequence()
        {
            try
            {
                // Switch back to a standard waveform to disable sequence
                _device.SendCommand($":SOUR{_activeChannel}:FUNC SIN");
                _isSequenceEnabled = false;
                UpdateSequenceUI(false);
                Log($"Sequence disabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error disabling Sequence: {ex.Message}");
            }
        }

        private void UpdateSequenceUI(bool enabled)
        {
            // Update the SequencePanel visibility
            _sequencePanel.Dispatcher.Invoke(() =>
            {
                _sequencePanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            });

            // Log the state change
            Log($"Sequence UI updated: {(enabled ? "Enabled" : "Disabled")}");
        }

        public void RefreshSequenceSettings()
        {
            if (!_isInitialized || !_device.IsConnected) return;

            try
            {
                // Check if current waveform is sequence
                string currentFunc = _device.SendQuery($":SOUR{_activeChannel}:FUNC?").Trim().ToUpper();
                _isSequenceEnabled = currentFunc.Contains("SEQ");

                UpdateSequenceUI(_isSequenceEnabled);

                if (_isSequenceEnabled)
                {
                    // Check if sequence function is enabled
                    string sequenceState = _device.SendQuery($":SOUR{_activeChannel}:FUNC:SEQ:STAT?").Trim();
                    bool isSeqOn = (sequenceState == "ON" || sequenceState == "1");

                    if (isSeqOn)
                    {
                        // Refresh all sequence parameters
                        RefreshSampleRate();
                        RefreshFilterType();
                        RefreshEdgeTime();
                        RefreshSlotSettings();
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing Sequence settings: {ex.Message}");
            }
        }

        private void RefreshSampleRate()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:SEQ:SRAT?");
                if (double.TryParse(response, out double sampleRate))
                {
                    _sequencePanel.Dispatcher.Invoke(() =>
                    {
                        // Convert to appropriate unit and update display
                        string unit;
                        double displayValue;

                        if (sampleRate >= 1e6)
                        {
                            unit = "MSa/s";
                            displayValue = sampleRate / 1e6;
                        }
                        else if (sampleRate >= 1e3)
                        {
                            unit = "kSa/s";
                            displayValue = sampleRate / 1e3;
                        }
                        else
                        {
                            unit = "Sa/s";
                            displayValue = sampleRate;
                        }

                        _sequencePanel.SampleRateTextBox_Public.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

                        // Update unit combo box
                        for (int i = 0; i < _sequencePanel.SampleRateUnitComboBox_Public.Items.Count; i++)
                        {
                            var item = _sequencePanel.SampleRateUnitComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Content?.ToString() == unit)
                            {
                                _sequencePanel.SampleRateUnitComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing sequence sample rate: {ex.Message}");
            }
        }

        private void RefreshFilterType()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:SEQ:FILT?").Trim().ToUpper();

                _sequencePanel.Dispatcher.Invoke(() =>
                {
                    if (_sequencePanel.FilterTypeComboBox_Public != null)
                    {
                        for (int i = 0; i < _sequencePanel.FilterTypeComboBox_Public.Items.Count; i++)
                        {
                            var item = _sequencePanel.FilterTypeComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == response.Substring(0, 4)) // First 4 characters
                            {
                                _sequencePanel.FilterTypeComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });

                // Update edge time visibility
                UpdateEdgeTimeVisibility();
            }
            catch (Exception ex)
            {
                Log($"Error refreshing sequence filter type: {ex.Message}");
            }
        }

        private void RefreshEdgeTime()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:SEQ:EDGET?");
                if (double.TryParse(response, out double edgeTime))
                {
                    _sequencePanel.Dispatcher.Invoke(() =>
                    {
                        // Convert to appropriate unit
                        string unit;
                        double displayValue;

                        if (edgeTime >= 1.0)
                        {
                            unit = "s";
                            displayValue = edgeTime;
                        }
                        else if (edgeTime >= 1e-3)
                        {
                            unit = "ms";
                            displayValue = edgeTime * 1e3;
                        }
                        else if (edgeTime >= 1e-6)
                        {
                            unit = "µs";
                            displayValue = edgeTime * 1e6;
                        }
                        else
                        {
                            unit = "ns";
                            displayValue = edgeTime * 1e9;
                        }

                        _sequencePanel.EdgeTimeTextBox_Public.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

                        // Update unit combo box
                        for (int i = 0; i < _sequencePanel.EdgeTimeUnitComboBox_Public.Items.Count; i++)
                        {
                            var item = _sequencePanel.EdgeTimeUnitComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Content?.ToString() == unit)
                            {
                                _sequencePanel.EdgeTimeUnitComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing sequence edge time: {ex.Message}");
            }
        }

        private void RefreshSlotSettings()
        {
            try
            {
                for (int slot = 1; slot <= 8; slot++)
                {
                    // Get slot waveform
                    string waveResponse = _device.SendQuery($":SOUR{_activeChannel}:FUNC:SEQ:WAVE? {slot}");
                    string waveform = waveResponse.Trim().ToUpper();

                    // Get slot period (points)
                    string pointsResponse = _device.SendQuery($":SOUR{_activeChannel}:FUNC:SEQ:PER? {slot}");

                    _sequencePanel.Dispatcher.Invoke(() =>
                    {
                        // Update waveform combo box
                        var waveformComboBox = _sequencePanel.SlotWaveformComboBoxes_Public[slot];
                        if (waveformComboBox != null)
                        {
                            for (int i = 0; i < waveformComboBox.Items.Count; i++)
                            {
                                var item = waveformComboBox.Items[i] as ComboBoxItem;
                                if (item?.Tag?.ToString() == waveform)
                                {
                                    waveformComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }

                        // Update points textbox
                        var pointsTextBox = _sequencePanel.SlotPointsTextBoxes_Public[slot];
                        if (pointsTextBox != null && int.TryParse(pointsResponse, out int points))
                        {
                            pointsTextBox.Text = points.ToString();
                        }

                        // Enable checkbox if slot has valid settings
                        var enableCheckBox = _sequencePanel.SlotEnableCheckBoxes_Public[slot];
                        if (enableCheckBox != null)
                        {
                            // For now, enable if points > 0
                            enableCheckBox.IsChecked = int.TryParse(pointsResponse, out int p) && p > 0;
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing slot settings: {ex.Message}");
            }
        }

        // Apply methods for each parameter
        private void ApplySampleRate()
        {
            if (!_isSequenceEnabled) return;

            try
            {
                double sampleRate = ParseSampleRateWithUnit(_sequencePanel.SampleRateTextBox_Public.Text,
                    _sequencePanel.SampleRateUnitComboBox_Public);

                _device.SendCommand($":SOUR{_activeChannel}:FUNC:SEQ:SRAT {sampleRate}");
                Log($"Set sequence sample rate to {sampleRate} Sa/s");
            }
            catch (Exception ex)
            {
                Log($"Error setting sequence sample rate: {ex.Message}");
            }
        }

        private void ApplyEdgeTime()
        {
            if (!_isSequenceEnabled) return;

            try
            {
                double edgeTime = ParseTimeWithUnit(_sequencePanel.EdgeTimeTextBox_Public.Text,
                    _sequencePanel.EdgeTimeUnitComboBox_Public);

                _device.SendCommand($":SOUR{_activeChannel}:FUNC:SEQ:EDGET {edgeTime}");
                Log($"Set sequence edge time to {edgeTime} seconds");
            }
            catch (Exception ex)
            {
                Log($"Error setting sequence edge time: {ex.Message}");
            }
        }

        private void ApplySlotPoints(int slotNumber)
        {
            if (!_isSequenceEnabled) return;

            try
            {
                var pointsTextBox = _sequencePanel.SlotPointsTextBoxes_Public[slotNumber];
                if (pointsTextBox != null && int.TryParse(pointsTextBox.Text, out int points))
                {
                    // Clamp to valid range
                    points = Math.Max(1, Math.Min(256, points));
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:SEQ:PER {slotNumber},{points}");
                    Log($"Set slot {slotNumber} points to {points}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting slot {slotNumber} points: {ex.Message}");
            }
        }

        // Event handlers for UI changes
        public void OnSampleRateChanged()
        {
            _sampleRateTimer.Stop();
            _sampleRateTimer.Start();
        }

        public void OnFilterTypeChanged()
        {
            if (!_isSequenceEnabled) return;

            try
            {
                var selectedItem = _sequencePanel.FilterTypeComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string filterType = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:SEQ:FILT {filterType}");
                    Log($"Set sequence filter type to {selectedItem.Content}");

                    // Update edge time visibility
                    UpdateEdgeTimeVisibility();
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing sequence filter type: {ex.Message}");
            }
        }

        public void OnEdgeTimeChanged()
        {
            _edgeTimeTimer.Stop();
            _edgeTimeTimer.Start();
        }

        public void OnSlotEnableChanged(int slotNumber, bool enabled)
        {
            // Note: The SCPI commands don't seem to have individual slot enable/disable
            // This is more for UI organization. We'll apply the slot when the sequence is applied.
            Log($"Slot {slotNumber} {(enabled ? "enabled" : "disabled")} in UI");
        }

        public void OnSlotWaveformChanged(int slotNumber)
        {
            if (!_isSequenceEnabled) return;

            try
            {
                var waveformComboBox = _sequencePanel.SlotWaveformComboBoxes_Public[slotNumber];
                var selectedItem = waveformComboBox?.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string waveform = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:SEQ:WAVE {slotNumber},{waveform}");
                    Log($"Set slot {slotNumber} waveform to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing slot {slotNumber} waveform: {ex.Message}");
            }
        }

        public void OnSlotPointsChanged(int slotNumber)
        {
            _slotPointsTimers[slotNumber].Stop();
            _slotPointsTimers[slotNumber].Start();
        }

        public void ApplySequenceSettings()
        {
            try
            {
                // Get current amplitude and offset from main window controls
                double amplitude = 1.0; // Default value
                double offset = 0.0;    // Default value

                // Try to get values from main window if available
                if (_mainWindow != null)
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (double.TryParse(_mainWindow.ChannelAmplitudeTextBox.Text, out double amp))
                            {
                                string ampUnit = UnitConversionUtility.GetAmplitudeUnit(_mainWindow.ChannelAmplitudeUnitComboBox);
                                amplitude = amp * UnitConversionUtility.GetAmplitudeMultiplier(ampUnit);
                            }

                            if (double.TryParse(_mainWindow.ChannelOffsetTextBox.Text, out double off))
                            {
                                offset = off;
                            }
                        }
                        catch (Exception)
                        {
                            // Use default values if can't get from UI
                        }
                    });
                }

                // Get sample rate
                double sampleRate = ParseSampleRateWithUnit(_sequencePanel.SampleRateTextBox_Public.Text,
                    _sequencePanel.SampleRateUnitComboBox_Public);

                // Apply sequence waveform
                _device.SendCommand($":SOUR{_activeChannel}:APPL:SEQ {sampleRate},{amplitude},{offset},0");

                // Enable sequence function
                _device.SendCommand($":SOUR{_activeChannel}:FUNC:SEQ ON");

                // Apply filter type
                OnFilterTypeChanged();

                // Apply edge time if applicable
                string filterType = (_sequencePanel.FilterTypeComboBox_Public.SelectedItem as ComboBoxItem)?.Tag?.ToString();
                if (filterType == "SMOO" || filterType == "INSE")
                {
                    ApplyEdgeTime();
                }

                // Apply all enabled slots
                for (int slot = 1; slot <= 8; slot++)
                {
                    var enableCheckBox = _sequencePanel.SlotEnableCheckBoxes_Public[slot];
                    if (enableCheckBox?.IsChecked == true)
                    {
                        OnSlotWaveformChanged(slot);
                        ApplySlotPoints(slot);
                    }
                }

                _isSequenceEnabled = true;

                Log($"Applied sequence settings: {sampleRate} Sa/s, {amplitude} Vpp, {offset} V offset");
            }
            catch (Exception ex)
            {
                Log($"Error applying sequence settings: {ex.Message}");
            }
        }

        private void UpdateEdgeTimeVisibility()
        {
            _sequencePanel.Dispatcher.Invoke(() =>
            {
                var selectedItem = _sequencePanel.FilterTypeComboBox_Public.SelectedItem as ComboBoxItem;
                string filterType = selectedItem?.Tag?.ToString();

                // Show edge time only for SMOOTH and INSERT modes
                bool showEdgeTime = (filterType == "SMOO" || filterType == "INSE");
                _sequencePanel.EdgeTimeDockPanel_Public.Visibility = showEdgeTime ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        // Helper methods
        private double ParseSampleRateWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double rate))
                throw new ArgumentException("Invalid sample rate value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "kSa/s";
            double multiplier = unit switch
            {
                "MSa/s" => 1e6,
                "kSa/s" => 1e3,
                "Sa/s" => 1,
                _ => 1e3
            };

            return rate * multiplier;
        }

        private double ParseTimeWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double time))
                throw new ArgumentException("Invalid time value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "µs";
            double multiplier = unit switch
            {
                "s" => 1.0,
                "ms" => 1e-3,
                "µs" => 1e-6,
                "ns" => 1e-9,
                _ => 1e-6
            };

            return time * multiplier;
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}