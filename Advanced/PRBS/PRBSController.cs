using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Advanced.PRBS
{
    public class PRBSController
    {
        private readonly RigolDG2072 _device;
        private readonly PRBSPanel _prbsPanel;
        private readonly MainWindow _mainWindow;
        private int _activeChannel;
        private bool _isInitialized = false;
        private bool _isPRBSEnabled = false;

        // Timers for debouncing
        private DispatcherTimer _bitRateTimer;
        private DispatcherTimer _amplitudeTimer;
        private DispatcherTimer _offsetTimer;

        public event EventHandler<string> LogEvent;

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public bool IsEnabled => _isPRBSEnabled;

        public PRBSController(RigolDG2072 device, int activeChannel, PRBSPanel prbsPanel, MainWindow mainWindow)
        {
            _device = device;
            _activeChannel = activeChannel;
            _prbsPanel = prbsPanel;
            _mainWindow = mainWindow;
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _bitRateTimer = CreateDebounceTimer(() => ApplyBitRate());
            _amplitudeTimer = CreateDebounceTimer(() => ApplyAmplitude());
            _offsetTimer = CreateDebounceTimer(() => ApplyOffset());
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
                // Update sequence information based on default selection
                UpdateSequenceInformation();

                _isInitialized = true;
                Log("PRBS controller UI initialized");
            }
            catch (Exception ex)
            {
                Log($"Error initializing PRBS UI: {ex.Message}");
            }
        }

        public void EnablePRBS()
        {
            try
            {
                // Apply PRBS settings to enable it
                ApplyPRBSSettings();
                _isPRBSEnabled = true;
                UpdatePRBSUI(true);
                Log($"PRBS enabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error enabling PRBS: {ex.Message}");
            }
        }

        public void DisablePRBS()
        {
            try
            {
                // Switch back to a standard waveform to disable PRBS
                _device.SendCommand($":SOUR{_activeChannel}:FUNC SIN");
                _isPRBSEnabled = false;
                UpdatePRBSUI(false);
                Log($"PRBS disabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error disabling PRBS: {ex.Message}");
            }
        }

        private void UpdatePRBSUI(bool enabled)
        {
            // Update the PRBSPanel visibility
            _prbsPanel.Dispatcher.Invoke(() =>
            {
                _prbsPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            });

            // Update the main window button if it exists
            _mainWindow.Dispatcher.Invoke(() =>
            {
                // Note: You'll need to add a PRBS toggle button to MainWindow similar to Sweep/Burst
                // For now, just log the state change
                Log($"PRBS UI updated: {(enabled ? "Enabled" : "Disabled")}");
            });
        }

        public void RefreshPRBSSettings()
        {
            if (!_isInitialized || !_device.IsConnected) return;

            try
            {
                // Check if current waveform is PRBS
                string currentFunc = _device.SendQuery($":SOUR{_activeChannel}:FUNC?").Trim().ToUpper();
                _isPRBSEnabled = currentFunc.Contains("PRBS");

                UpdatePRBSUI(_isPRBSEnabled);

                if (_isPRBSEnabled)
                {
                    // Refresh all PRBS parameters
                    RefreshDataType();
                    RefreshBitRate();
                    RefreshAmplitude();
                    RefreshOffset();
                    UpdateSequenceInformation();
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PRBS settings: {ex.Message}");
            }
        }

        private void RefreshDataType()
        {
            try
            {
                string dataType = _device.SendQuery($":SOUR{_activeChannel}:FUNC:PRBS:DATA?").Trim().ToUpper();

                _prbsPanel.Dispatcher.Invoke(() =>
                {
                    if (_prbsPanel.PRBSDataTypeComboBox_Public != null)
                    {
                        for (int i = 0; i < _prbsPanel.PRBSDataTypeComboBox_Public.Items.Count; i++)
                        {
                            var item = _prbsPanel.PRBSDataTypeComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == dataType)
                            {
                                _prbsPanel.PRBSDataTypeComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PRBS data type: {ex.Message}");
            }
        }

        private void RefreshBitRate()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:PRBS:BRAT?");
                if (double.TryParse(response, out double bitRate))
                {
                    _prbsPanel.Dispatcher.Invoke(() =>
                    {
                        // Convert to appropriate unit and update display
                        string unit;
                        double displayValue;

                        if (bitRate >= 1000000)
                        {
                            unit = "Mbps";
                            displayValue = bitRate / 1000000.0;
                        }
                        else if (bitRate >= 1000)
                        {
                            unit = "kbps";
                            displayValue = bitRate / 1000.0;
                        }
                        else
                        {
                            unit = "bps";
                            displayValue = bitRate;
                        }

                        _prbsPanel.PRBSBitRateTextBox_Public.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

                        // Update unit combo box
                        for (int i = 0; i < _prbsPanel.PRBSBitRateUnitComboBox_Public.Items.Count; i++)
                        {
                            var item = _prbsPanel.PRBSBitRateUnitComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Content?.ToString() == unit)
                            {
                                _prbsPanel.PRBSBitRateUnitComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PRBS bit rate: {ex.Message}");
            }
        }

        private void RefreshAmplitude()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:VOLT?");
                if (double.TryParse(response, out double amplitude))
                {
                    _prbsPanel.Dispatcher.Invoke(() =>
                    {
                        _prbsPanel.PRBSAmplitudeTextBox_Public.Text = UnitConversionUtility.FormatWithMinimumDecimals(amplitude);
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PRBS amplitude: {ex.Message}");
            }
        }

        private void RefreshOffset()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:VOLT:OFFS?");
                if (double.TryParse(response, out double offset))
                {
                    _prbsPanel.Dispatcher.Invoke(() =>
                    {
                        _prbsPanel.PRBSOffsetTextBox_Public.Text = UnitConversionUtility.FormatWithMinimumDecimals(offset);
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PRBS offset: {ex.Message}");
            }
        }

        // Apply methods for each parameter
        private void ApplyBitRate()
        {
            if (!_isPRBSEnabled) return;

            try
            {
                double bitRate = ParseBitRateWithUnit(_prbsPanel.PRBSBitRateTextBox_Public.Text,
                    _prbsPanel.PRBSBitRateUnitComboBox_Public);

                _device.SendCommand($":SOUR{_activeChannel}:FUNC:PRBS:BRAT {bitRate}");
                Log($"Set PRBS bit rate to {bitRate} bps");

                // Update sequence information
                UpdateSequenceInformation();
            }
            catch (Exception ex)
            {
                Log($"Error setting PRBS bit rate: {ex.Message}");
            }
        }

        private void ApplyAmplitude()
        {
            if (!_isPRBSEnabled) return;

            try
            {
                double amplitude = ParseAmplitudeWithUnit(_prbsPanel.PRBSAmplitudeTextBox_Public.Text,
                    _prbsPanel.PRBSAmplitudeUnitComboBox_Public);

                _device.SendCommand($":SOUR{_activeChannel}:VOLT {amplitude}");
                Log($"Set PRBS amplitude to {amplitude} Vpp");
            }
            catch (Exception ex)
            {
                Log($"Error setting PRBS amplitude: {ex.Message}");
            }
        }

        private void ApplyOffset()
        {
            if (!_isPRBSEnabled) return;

            try
            {
                double offset = ParseOffsetWithUnit(_prbsPanel.PRBSOffsetTextBox_Public.Text,
                    _prbsPanel.PRBSOffsetUnitComboBox_Public);

                _device.SendCommand($":SOUR{_activeChannel}:VOLT:OFFS {offset}");
                Log($"Set PRBS offset to {offset} V");
            }
            catch (Exception ex)
            {
                Log($"Error setting PRBS offset: {ex.Message}");
            }
        }

        // Event handlers for UI changes
        public void OnDataTypeChanged()
        {
            try
            {
                var selectedItem = _prbsPanel.PRBSDataTypeComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string dataType = selectedItem.Tag.ToString();

                    if (_isPRBSEnabled)
                    {
                        _device.SendCommand($":SOUR{_activeChannel}:FUNC:PRBS:DATA {dataType}");
                        Log($"Set PRBS data type to {selectedItem.Content}");
                    }

                    // Update sequence information
                    UpdateSequenceInformation();
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing PRBS data type: {ex.Message}");
            }
        }

        public void OnBitRateChanged()
        {
            _bitRateTimer.Stop();
            _bitRateTimer.Start();

            // Also update sequence information immediately for UI feedback
            UpdateSequenceInformation();
        }

        public void OnAmplitudeChanged()
        {
            _amplitudeTimer.Stop();
            _amplitudeTimer.Start();
        }

        public void OnOffsetChanged()
        {
            _offsetTimer.Stop();
            _offsetTimer.Start();
        }

        public void ApplyPRBSSettings()
        {
            try
            {
                // Get all parameters
                double bitRate = ParseBitRateWithUnit(_prbsPanel.PRBSBitRateTextBox_Public.Text,
                    _prbsPanel.PRBSBitRateUnitComboBox_Public);
                double amplitude = ParseAmplitudeWithUnit(_prbsPanel.PRBSAmplitudeTextBox_Public.Text,
                    _prbsPanel.PRBSAmplitudeUnitComboBox_Public);
                double offset = ParseOffsetWithUnit(_prbsPanel.PRBSOffsetTextBox_Public.Text,
                    _prbsPanel.PRBSOffsetUnitComboBox_Public);

                var selectedDataType = _prbsPanel.PRBSDataTypeComboBox_Public.SelectedItem as ComboBoxItem;
                string dataType = selectedDataType?.Tag?.ToString() ?? "PN7";

                // Apply PRBS waveform
                _device.SendCommand($":SOUR{_activeChannel}:APPL:PRBS {bitRate},{amplitude},{offset}");

                // Set data type
                _device.SendCommand($":SOUR{_activeChannel}:FUNC:PRBS:DATA {dataType}");

                _isPRBSEnabled = true;
                UpdateSequenceInformation();

                Log($"Applied PRBS settings: {dataType}, {bitRate} bps, {amplitude} Vpp, {offset} V offset");
            }
            catch (Exception ex)
            {
                Log($"Error applying PRBS settings: {ex.Message}");
            }
        }

        private void UpdateSequenceInformation()
        {
            try
            {
                _prbsPanel.Dispatcher.Invoke(() =>
                {
                    // Get current data type
                    var selectedDataType = _prbsPanel.PRBSDataTypeComboBox_Public.SelectedItem as ComboBoxItem;
                    string dataType = selectedDataType?.Tag?.ToString() ?? "PN7";

                    // Calculate sequence length
                    int sequenceLength = GetSequenceLength(dataType);
                    _prbsPanel.SequenceLengthTextBlock_Public.Text = $"{sequenceLength} bits";

                    // Get bit rate and calculate timing
                    if (double.TryParse(_prbsPanel.PRBSBitRateTextBox_Public.Text, out double bitRateValue))
                    {
                        double bitRateInBps = ConvertBitRateToBase(bitRateValue, _prbsPanel.PRBSBitRateUnitComboBox_Public);

                        if (bitRateInBps > 0)
                        {
                            // Calculate sequence period (time for one complete sequence)
                            double sequencePeriodSeconds = sequenceLength / bitRateInBps;
                            _prbsPanel.SequencePeriodTextBlock_Public.Text = FormatTime(sequencePeriodSeconds);

                            // Calculate repetition rate (how often sequence repeats)
                            double repetitionRate = 1.0 / sequencePeriodSeconds;
                            _prbsPanel.RepetitionRateTextBlock_Public.Text = FormatFrequency(repetitionRate);
                        }
                        else
                        {
                            _prbsPanel.SequencePeriodTextBlock_Public.Text = "-- ms";
                            _prbsPanel.RepetitionRateTextBlock_Public.Text = "-- Hz";
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error updating sequence information: {ex.Message}");
            }
        }

        // Helper methods
        private int GetSequenceLength(string dataType)
        {
            return dataType switch
            {
                "PN7" => (1 << 7) - 1,    // 2^7 - 1 = 127
                "PN9" => (1 << 9) - 1,    // 2^9 - 1 = 511
                "PN11" => (1 << 11) - 1,  // 2^11 - 1 = 2047
                _ => 127
            };
        }

        private double ParseBitRateWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double bitRate))
                throw new ArgumentException("Invalid bit rate value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "kbps";
            double multiplier = unit switch
            {
                "Mbps" => 1e6,
                "kbps" => 1e3,
                "bps" => 1,
                _ => 1e3
            };

            return bitRate * multiplier;
        }

        private double ParseAmplitudeWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double amplitude))
                throw new ArgumentException("Invalid amplitude value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Vpp";
            double multiplier = unit switch
            {
                "Vpp" => 1.0,
                "mVpp" => 1e-3,
                "Vrms" => 1.0 / Math.Sqrt(2.0),  // Convert RMS to peak for square wave
                "mVrms" => 1e-3 / Math.Sqrt(2.0),
                _ => 1.0
            };

            return amplitude * multiplier;
        }

        private double ParseOffsetWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double offset))
                throw new ArgumentException("Invalid offset value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "V";
            double multiplier = unit switch
            {
                "V" => 1.0,
                "mV" => 1e-3,
                _ => 1.0
            };

            return offset * multiplier;
        }

        private double ConvertBitRateToBase(double value, ComboBox unitComboBox)
        {
            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "kbps";
            double multiplier = unit switch
            {
                "Mbps" => 1e6,
                "kbps" => 1e3,
                "bps" => 1,
                _ => 1e3
            };

            return value * multiplier;
        }

        private string FormatTime(double seconds)
        {
            if (seconds >= 1.0)
                return $"{seconds:F2} s";
            else if (seconds >= 1e-3)
                return $"{seconds * 1e3:F2} ms";
            else if (seconds >= 1e-6)
                return $"{seconds * 1e6:F2} µs";
            else
                return $"{seconds * 1e9:F2} ns";
        }

        private string FormatFrequency(double frequency)
        {
            if (frequency >= 1e6)
                return $"{frequency / 1e6:F2} MHz";
            else if (frequency >= 1e3)
                return $"{frequency / 1e3:F2} kHz";
            else
                return $"{frequency:F2} Hz";
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}