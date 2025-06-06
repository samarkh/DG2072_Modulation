﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DG2072_USB_Control.Sweep
{
    public class SweepController
    {
        private readonly RigolDG2072 _device;
        private readonly SweepPanel _sweepPanel;
        private readonly MainWindow _mainWindow;
        private int _activeChannel;
        private bool _isInitialized = false;
        private bool _isSweepEnabled = false;
        private bool _useStartStop = true; // true = Start/Stop, false = Center/Span

        // Timers for debouncing
        private DispatcherTimer _sweepTimeTimer;
        private DispatcherTimer _returnTimeTimer;
        private DispatcherTimer _startFreqTimer;
        private DispatcherTimer _stopFreqTimer;
        private DispatcherTimer _centerFreqTimer;
        private DispatcherTimer _spanFreqTimer;
        private DispatcherTimer _markerFreqTimer;
        private DispatcherTimer _startHoldTimer;
        private DispatcherTimer _stopHoldTimer;
        private DispatcherTimer _stepCountTimer;

        public event EventHandler<string> LogEvent;

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public bool IsEnabled => _isSweepEnabled;

        public SweepController(RigolDG2072 device, int activeChannel, SweepPanel sweepPanel, MainWindow mainWindow)
        {
            _device = device;
            _activeChannel = activeChannel;
            _sweepPanel = sweepPanel;
            _mainWindow = mainWindow;
            InitializeTimers();
        }

        private void InitializeTimers()
        {
            _sweepTimeTimer = CreateDebounceTimer(() => ApplySweepTime());
            _returnTimeTimer = CreateDebounceTimer(() => ApplyReturnTime());
            _startFreqTimer = CreateDebounceTimer(() => ApplyStartFrequency());
            _stopFreqTimer = CreateDebounceTimer(() => ApplyStopFrequency());
            _centerFreqTimer = CreateDebounceTimer(() => ApplyCenterFrequency());
            _spanFreqTimer = CreateDebounceTimer(() => ApplySpanFrequency());
            _markerFreqTimer = CreateDebounceTimer(() => ApplyMarkerFrequency());
            _startHoldTimer = CreateDebounceTimer(() => ApplyStartHold());
            _stopHoldTimer = CreateDebounceTimer(() => ApplyStopHold());
            _stepCountTimer = CreateDebounceTimer(() => ApplyStepCount());
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
                // Initialize sweep type combo box
                if (_sweepPanel.SweepTypeComboBox_Public != null)
                {
                    _sweepPanel.SweepTypeComboBox_Public.Items.Clear();
                    _sweepPanel.SweepTypeComboBox_Public.Items.Add(new ComboBoxItem { Content = "Linear", Tag = "LIN" });
                    _sweepPanel.SweepTypeComboBox_Public.Items.Add(new ComboBoxItem { Content = "Logarithmic", Tag = "LOG" });
                    _sweepPanel.SweepTypeComboBox_Public.Items.Add(new ComboBoxItem { Content = "Step", Tag = "STE" });
                    _sweepPanel.SweepTypeComboBox_Public.SelectedIndex = 0;
                }

                // Initialize trigger source combo box
                if (_sweepPanel.TriggerSourceComboBox_Public != null)
                {
                    _sweepPanel.TriggerSourceComboBox_Public.Items.Clear();
                    _sweepPanel.TriggerSourceComboBox_Public.Items.Add(new ComboBoxItem { Content = "Internal", Tag = "INT" });
                    _sweepPanel.TriggerSourceComboBox_Public.Items.Add(new ComboBoxItem { Content = "External", Tag = "EXT" });
                    _sweepPanel.TriggerSourceComboBox_Public.Items.Add(new ComboBoxItem { Content = "Manual", Tag = "MAN" });
                    _sweepPanel.TriggerSourceComboBox_Public.SelectedIndex = 0;
                }

                // Initialize trigger slope combo box
                if (_sweepPanel.TriggerSlopeComboBox_Public != null)
                {
                    _sweepPanel.TriggerSlopeComboBox_Public.Items.Clear();
                    _sweepPanel.TriggerSlopeComboBox_Public.Items.Add(new ComboBoxItem { Content = "Positive", Tag = "POS" });
                    _sweepPanel.TriggerSlopeComboBox_Public.Items.Add(new ComboBoxItem { Content = "Negative", Tag = "NEG" });
                    _sweepPanel.TriggerSlopeComboBox_Public.SelectedIndex = 0;
                }

                // Set initial visibility
                UpdateFrequencyModeVisibility();

                _isInitialized = true;
                Log("Sweep controller UI initialized");
            }
            catch (Exception ex)
            {
                Log($"Error initializing sweep UI: {ex.Message}");
            }
        }

        public void EnableSweep()
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:SWE:STAT ON");
                _isSweepEnabled = true;
                UpdateSweepUI(true);
                Log($"Sweep enabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error enabling sweep: {ex.Message}");
            }
        }

        public void DisableSweep()
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:SWE:STAT OFF");
                _isSweepEnabled = false;
                UpdateSweepUI(false);
                Log($"Sweep disabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error disabling sweep: {ex.Message}");
            }
        }

        private void UpdateSweepUI(bool enabled)
        {
            // Update the SweepPanel visibility
            _sweepPanel.Dispatcher.Invoke(() =>
            {
                _sweepPanel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;

                // Enable/disable manual trigger button
                if (_sweepPanel.ManualTriggerButton_Public != null)
                {
                    _sweepPanel.ManualTriggerButton_Public.IsEnabled = enabled;
                }
            });

            // Update the main window button
            _mainWindow.Dispatcher.Invoke(() =>
            {
                if (_mainWindow.SweepToggleButton != null)
                {
                    _mainWindow.SweepToggleButton.Content = enabled ? "Disable Sweep" : "Enable Sweep";
                    _mainWindow.SweepToggleButton.Background = enabled ?
                        System.Windows.Media.Brushes.LightCoral :
                        System.Windows.Media.Brushes.LightGreen;
                }
            });
        }

        public void OnFrequencyModeChanged(bool useStartStop)
        {
            _useStartStop = useStartStop;
            UpdateFrequencyModeVisibility();

            if (_isSweepEnabled)
            {
                // Refresh frequency values based on new mode
                RefreshFrequencySettings();
            }
        }

        private void UpdateFrequencyModeVisibility()
        {
            _sweepPanel.Dispatcher.Invoke(() =>
            {
                if (_sweepPanel.StartStopPanel_Public != null && _sweepPanel.CenterSpanPanel_Public != null)
                {
                    _sweepPanel.StartStopPanel_Public.Visibility = _useStartStop ? Visibility.Visible : Visibility.Collapsed;
                    _sweepPanel.CenterSpanPanel_Public.Visibility = _useStartStop ? Visibility.Collapsed : Visibility.Visible;
                }
            });
        }

        public void RefreshSweepSettings()
        {
            if (!_isInitialized || !_device.IsConnected) return;

            try
            {
                // Check if sweep is enabled
                string sweepState = _device.SendQuery($":SOUR{_activeChannel}:SWE:STAT?").Trim();
                _isSweepEnabled = (sweepState == "ON" || sweepState == "1");
                UpdateSweepUI(_isSweepEnabled);

                if (_isSweepEnabled)
                {
                    // Refresh all sweep parameters
                    RefreshSweepType();
                    RefreshSweepTime();
                    RefreshReturnTime();
                    RefreshFrequencySettings();
                    RefreshMarkerFrequency();
                    RefreshHoldTimes();
                    RefreshStepCount();
                    RefreshTriggerSettings();
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing sweep settings: {ex.Message}");
            }
        }

        private void RefreshSweepType()
        {
            try
            {
                string sweepType = _device.SendQuery($":SOUR{_activeChannel}:SWE:SPAC?").Trim().ToUpper();

                _sweepPanel.Dispatcher.Invoke(() =>
                {
                    if (_sweepPanel.SweepTypeComboBox_Public != null)
                    {
                        for (int i = 0; i < _sweepPanel.SweepTypeComboBox_Public.Items.Count; i++)
                        {
                            var item = _sweepPanel.SweepTypeComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == sweepType ||
                                (sweepType.StartsWith("LIN") && item?.Tag?.ToString() == "LIN") ||
                                (sweepType.StartsWith("LOG") && item?.Tag?.ToString() == "LOG") ||
                                (sweepType.StartsWith("STE") && item?.Tag?.ToString() == "STE"))
                            {
                                _sweepPanel.SweepTypeComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing sweep type: {ex.Message}");
            }
        }

        private void RefreshSweepTime()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:SWE:TIME?");
                if (double.TryParse(response, out double sweepTime))
                {
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.SweepTimeTextBox_Public.Text = sweepTime.ToString("F3");
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing sweep time: {ex.Message}");
            }
        }

        private void RefreshReturnTime()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:SWE:RTIM?");
                if (double.TryParse(response, out double returnTime))
                {
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.ReturnTimeTextBox_Public.Text = returnTime.ToString("F3");
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing return time: {ex.Message}");
            }
        }

        private void RefreshFrequencySettings()
        {
            try
            {
                if (_useStartStop)
                {
                    // Refresh start and stop frequencies
                    string startResp = _device.SendQuery($":SOUR{_activeChannel}:FREQ:STAR?");
                    string stopResp = _device.SendQuery($":SOUR{_activeChannel}:FREQ:STOP?");

                    if (double.TryParse(startResp, out double startFreq))
                    {
                        _sweepPanel.Dispatcher.Invoke(() =>
                        {
                            _sweepPanel.StartFrequencyTextBox_Public.Text = FormatFrequency(startFreq);
                        });
                    }

                    if (double.TryParse(stopResp, out double stopFreq))
                    {
                        _sweepPanel.Dispatcher.Invoke(() =>
                        {
                            _sweepPanel.StopFrequencyTextBox_Public.Text = FormatFrequency(stopFreq);
                        });
                    }
                }
                else
                {
                    // Refresh center and span frequencies
                    string centerResp = _device.SendQuery($":SOUR{_activeChannel}:FREQ:CENT?");
                    string spanResp = _device.SendQuery($":SOUR{_activeChannel}:FREQ:SPAN?");

                    if (double.TryParse(centerResp, out double centerFreq))
                    {
                        _sweepPanel.Dispatcher.Invoke(() =>
                        {
                            _sweepPanel.CenterFrequencyTextBox_Public.Text = FormatFrequency(centerFreq);
                        });
                    }

                    if (double.TryParse(spanResp, out double spanFreq))
                    {
                        _sweepPanel.Dispatcher.Invoke(() =>
                        {
                            _sweepPanel.SpanFrequencyTextBox_Public.Text = FormatFrequency(spanFreq);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing frequency settings: {ex.Message}");
            }
        }

        private void RefreshMarkerFrequency()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:MARK:FREQ?");
                if (double.TryParse(response, out double markerFreq))
                {
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.MarkerFrequencyTextBox_Public.Text = FormatFrequency(markerFreq);
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Note: Marker frequency may not be supported: {ex.Message}");
            }
        }

        private void RefreshHoldTimes()
        {
            try
            {
                string startHold = _device.SendQuery($":SOUR{_activeChannel}:SWE:HTIM:STAR?");
                string stopHold = _device.SendQuery($":SOUR{_activeChannel}:SWE:HTIM:STOP?");

                if (double.TryParse(startHold, out double startHoldTime))
                {
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.StartHoldTextBox_Public.Text = startHoldTime.ToString("F3");
                    });
                }

                if (double.TryParse(stopHold, out double stopHoldTime))
                {
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.StopHoldTextBox_Public.Text = stopHoldTime.ToString("F3");
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing hold times: {ex.Message}");
            }
        }

        private void RefreshStepCount()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:SWE:STEP?");
                if (int.TryParse(response, out int stepCount))
                {
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.StepCountTextBox_Public.Text = stepCount.ToString();
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing step count: {ex.Message}");
            }
        }

        private void RefreshTriggerSettings()
        {
            try
            {
                // Refresh trigger source
                string trigSource = _device.SendQuery($":SOUR{_activeChannel}:SWE:TRIG:SOUR?").Trim().ToUpper();
                _sweepPanel.Dispatcher.Invoke(() =>
                {
                    if (_sweepPanel.TriggerSourceComboBox_Public != null)
                    {
                        for (int i = 0; i < _sweepPanel.TriggerSourceComboBox_Public.Items.Count; i++)
                        {
                            var item = _sweepPanel.TriggerSourceComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == trigSource.Substring(0, 3))
                            {
                                _sweepPanel.TriggerSourceComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });

                // Refresh trigger slope
                string trigSlope = _device.SendQuery($":SOUR{_activeChannel}:SWE:TRIG:SLOP?").Trim().ToUpper();
                _sweepPanel.Dispatcher.Invoke(() =>
                {
                    if (_sweepPanel.TriggerSlopeComboBox_Public != null)
                    {
                        for (int i = 0; i < _sweepPanel.TriggerSlopeComboBox_Public.Items.Count; i++)
                        {
                            var item = _sweepPanel.TriggerSlopeComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == trigSlope.Substring(0, 3))
                            {
                                _sweepPanel.TriggerSlopeComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing trigger settings: {ex.Message}");
            }
        }

        // Apply methods for each parameter
        private void ApplySweepTime()
        {
            if (!_isSweepEnabled) return;

            try
            {
                if (double.TryParse(_sweepPanel.SweepTimeTextBox_Public.Text, out double sweepTime))
                {
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:TIME {sweepTime}");
                    Log($"Set sweep time to {sweepTime} seconds");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting sweep time: {ex.Message}");
            }
        }

        private void ApplyReturnTime()
        {
            if (!_isSweepEnabled) return;

            try
            {
                if (double.TryParse(_sweepPanel.ReturnTimeTextBox_Public.Text, out double returnTime))
                {
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:RTIM {returnTime}");
                    Log($"Set return time to {returnTime} seconds");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting return time: {ex.Message}");
            }
        }

        private void ApplyStartFrequency()
        {
            if (!_isSweepEnabled || !_useStartStop) return;

            try
            {
                double freq = ParseFrequencyWithUnit(_sweepPanel.StartFrequencyTextBox_Public.Text,
                    _sweepPanel.StartFrequencyUnitComboBox_Public);
                _device.SendCommand($":SOUR{_activeChannel}:FREQ:STAR {freq}");
                Log($"Set start frequency to {freq} Hz");
            }
            catch (Exception ex)
            {
                Log($"Error setting start frequency: {ex.Message}");
            }
        }

        private void ApplyStopFrequency()
        {
            if (!_isSweepEnabled || !_useStartStop) return;

            try
            {
                double freq = ParseFrequencyWithUnit(_sweepPanel.StopFrequencyTextBox_Public.Text,
                    _sweepPanel.StopFrequencyUnitComboBox_Public);
                _device.SendCommand($":SOUR{_activeChannel}:FREQ:STOP {freq}");
                Log($"Set stop frequency to {freq} Hz");
            }
            catch (Exception ex)
            {
                Log($"Error setting stop frequency: {ex.Message}");
            }
        }

        private void ApplyCenterFrequency()
        {
            if (!_isSweepEnabled || _useStartStop) return;

            try
            {
                double freq = ParseFrequencyWithUnit(_sweepPanel.CenterFrequencyTextBox_Public.Text,
                    _sweepPanel.CenterFrequencyUnitComboBox_Public);
                _device.SendCommand($":SOUR{_activeChannel}:FREQ:CENT {freq}");
                Log($"Set center frequency to {freq} Hz");
            }
            catch (Exception ex)
            {
                Log($"Error setting center frequency: {ex.Message}");
            }
        }

        private void ApplySpanFrequency()
        {
            if (!_isSweepEnabled || _useStartStop) return;

            try
            {
                double freq = ParseFrequencyWithUnit(_sweepPanel.SpanFrequencyTextBox_Public.Text,
                    _sweepPanel.SpanFrequencyUnitComboBox_Public);
                _device.SendCommand($":SOUR{_activeChannel}:FREQ:SPAN {freq}");
                Log($"Set frequency span to {freq} Hz");
            }
            catch (Exception ex)
            {
                Log($"Error setting frequency span: {ex.Message}");
            }
        }

        private void ApplyMarkerFrequency()
        {
            if (!_isSweepEnabled) return;

            try
            {
                double freq = ParseFrequencyWithUnit(_sweepPanel.MarkerFrequencyTextBox_Public.Text,
                    _sweepPanel.MarkerFrequencyUnitComboBox_Public);
                _device.SendCommand($":SOUR{_activeChannel}:MARK:FREQ {freq}");
                Log($"Set marker frequency to {freq} Hz");
            }
            catch (Exception ex)
            {
                Log($"Error setting marker frequency: {ex.Message}");
            }
        }

        private void ApplyStartHold()
        {
            if (!_isSweepEnabled) return;

            try
            {
                if (double.TryParse(_sweepPanel.StartHoldTextBox_Public.Text, out double startHold))
                {
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:HTIM:STAR {startHold}");
                    Log($"Set start hold time to {startHold} seconds");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting start hold time: {ex.Message}");
            }
        }

        private void ApplyStopHold()
        {
            if (!_isSweepEnabled) return;

            try
            {
                if (double.TryParse(_sweepPanel.StopHoldTextBox_Public.Text, out double stopHold))
                {
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:HTIM:STOP {stopHold}");
                    Log($"Set stop hold time to {stopHold} seconds");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting stop hold time: {ex.Message}");
            }
        }

        private void ApplyStepCount()
        {
            if (!_isSweepEnabled) return;

            try
            {
                if (int.TryParse(_sweepPanel.StepCountTextBox_Public.Text, out int stepCount))
                {
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:STEP {stepCount}");
                    Log($"Set step count to {stepCount}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error setting step count: {ex.Message}");
            }
        }

        // Event handlers for UI changes
        public void OnSweepTypeChanged()
        {
            if (!_isSweepEnabled) return;

            try
            {
                var selectedItem = _sweepPanel.SweepTypeComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string sweepType = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:SPAC {sweepType}");
                    Log($"Set sweep type to {selectedItem.Content}");

                    // Enable/disable step count based on sweep type
                    bool isStepMode = (sweepType == "STE");
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.StepCountTextBox_Public.IsEnabled = isStepMode;
                    });
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing sweep type: {ex.Message}");
            }
        }

        public void OnTriggerSourceChanged()
        {
            if (!_isSweepEnabled) return;

            try
            {
                var selectedItem = _sweepPanel.TriggerSourceComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string trigSource = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:TRIG:SOUR {trigSource}");
                    Log($"Set trigger source to {selectedItem.Content}");

                    // Enable manual trigger button only for manual mode
                    bool isManual = (trigSource == "MAN");
                    _sweepPanel.Dispatcher.Invoke(() =>
                    {
                        _sweepPanel.ManualTriggerButton_Public.IsEnabled = _isSweepEnabled && isManual;
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
            if (!_isSweepEnabled) return;

            try
            {
                var selectedItem = _sweepPanel.TriggerSlopeComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string trigSlope = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:SWE:TRIG:SLOP {trigSlope}");
                    Log($"Set trigger slope to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing trigger slope: {ex.Message}");
            }
        }

        public void ExecuteManualTrigger()
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:SWE:TRIG");
                Log("Manual trigger executed");
            }
            catch (Exception ex)
            {
                Log($"Error executing manual trigger: {ex.Message}");
            }
        }

        // Debounced event handlers
        public void OnSweepTimeChanged()
        {
            _sweepTimeTimer.Stop();
            _sweepTimeTimer.Start();
        }

        public void OnReturnTimeChanged()
        {
            _returnTimeTimer.Stop();
            _returnTimeTimer.Start();
        }

        public void OnStartFrequencyChanged()
        {
            _startFreqTimer.Stop();
            _startFreqTimer.Start();
        }

        public void OnStopFrequencyChanged()
        {
            _stopFreqTimer.Stop();
            _stopFreqTimer.Start();
        }

        public void OnCenterFrequencyChanged()
        {
            _centerFreqTimer.Stop();
            _centerFreqTimer.Start();
        }

        public void OnSpanFrequencyChanged()
        {
            _spanFreqTimer.Stop();
            _spanFreqTimer.Start();
        }

        public void OnMarkerFrequencyChanged()
        {
            _markerFreqTimer.Stop();
            _markerFreqTimer.Start();
        }

        public void OnStartHoldChanged()
        {
            _startHoldTimer.Stop();
            _startHoldTimer.Start();
        }

        public void OnStopHoldChanged()
        {
            _stopHoldTimer.Stop();
            _stopHoldTimer.Start();
        }

        public void OnStepCountChanged()
        {
            _stepCountTimer.Stop();
            _stepCountTimer.Start();
        }

        // Helper methods
        private double ParseFrequencyWithUnit(string value, ComboBox unitComboBox)
        {
            if (!double.TryParse(value, out double freq))
                throw new ArgumentException("Invalid frequency value");

            string unit = (unitComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? "Hz";
            double multiplier = unit switch
            {
                "MHz" => 1e6,
                "kHz" => 1e3,
                "Hz" => 1,
                "mHz" => 1e-3,
                "µHz" => 1e-6,
                _ => 1
            };

            return freq * multiplier;
        }

        private string FormatFrequency(double freqHz)
        {
            if (freqHz >= 1e6)
                return (freqHz / 1e6).ToString("F3");
            else if (freqHz >= 1e3)
                return (freqHz / 1e3).ToString("F3");
            else
                return freqHz.ToString("F3");
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}