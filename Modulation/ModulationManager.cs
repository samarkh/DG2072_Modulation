using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Modulation
{
    /// <summary>
    /// Manages all modulation-related functionality for the Rigol DG2072
    /// </summary>
    public class ModulationManager
    {
        private readonly RigolDG2072 _device;
        private readonly Window _mainWindow;
        private int _activeChannel;

        // UI Controls - matching the XAML structure
        private ComboBox _carrierWaveformComboBox;
        private TextBox _carrierFrequencyTextBox;
        private ComboBox _carrierFrequencyUnitComboBox;
        private ComboBox _modulationTypeComboBox;
        private ComboBox _modulatingWaveformComboBox;
        private TextBox _modulationFrequencyTextBox;
        private ComboBox _modulationFrequencyUnitComboBox;
        private TextBox _modulationDepthTextBox;

        // Current modulation parameters
        private ModulationType _currentModulationType = ModulationType.AM;
        private double _carrierFrequency = 1000000; // 1 MHz default
        private double _modulationFrequency = 1000; // 1 kHz default
        private double _modulationDepth = 50; // 50% default

        public event EventHandler<string> LogEvent;

        /// <summary>
        /// Supported modulation types
        /// </summary>
        public enum ModulationType
        {
            AM,     // Amplitude Modulation
            FM,     // Frequency Modulation
            PM,     // Phase Modulation
            PWM,    // Pulse Width Modulation
            ASK,    // Amplitude Shift Keying
            FSK,    // Frequency Shift Keying
            PSK     // Phase Shift Keying
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ModulationManager(RigolDG2072 device, int channel, Window mainWindow)
        {
            _device = device;
            _activeChannel = channel;
            _mainWindow = mainWindow;

            InitializeControls();
        }

        /// <summary>
        /// Property for the active channel
        /// </summary>
        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        /// <summary>
        /// Initialize UI control references
        /// </summary>
        private void InitializeControls()
        {
            _carrierWaveformComboBox = _mainWindow.FindName("CarrierWaveformComboBox") as ComboBox;
            _carrierFrequencyTextBox = _mainWindow.FindName("CarrierFrequencyTextBox") as TextBox;
            _carrierFrequencyUnitComboBox = _mainWindow.FindName("CarrierFrequencyUnitComboBox") as ComboBox;
            _modulationTypeComboBox = _mainWindow.FindName("ModulationTypeComboBox") as ComboBox;
            _modulatingWaveformComboBox = _mainWindow.FindName("ModulatingWaveformComboBox") as ComboBox;
            _modulationFrequencyTextBox = _mainWindow.FindName("ModulationFrequencyTextBox") as TextBox;
            _modulationFrequencyUnitComboBox = _mainWindow.FindName("ModulationFrequencyUnitComboBox") as ComboBox;
            _modulationDepthTextBox = _mainWindow.FindName("ModulationDepthTextBox") as TextBox;
        }

        /// <summary>
        /// Initialize modulation type combo box
        /// </summary>
        public void InitializeModulationTypes()
        {
            if (_modulationTypeComboBox == null) return;

            _mainWindow.Dispatcher.Invoke(() =>
            {
                _modulationTypeComboBox.Items.Clear();
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "AM - Amplitude Modulation" });
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "FM - Frequency Modulation" });
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "PM - Phase Modulation" });
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "PWM - Pulse Width Modulation" });
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "ASK - Amplitude Shift Keying" });
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "FSK - Frequency Shift Keying" });
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = "PSK - Phase Shift Keying" });

                _modulationTypeComboBox.SelectedIndex = 0; // Default to AM
            });
        }

        /// <summary>
        /// Initialize modulating waveform combo box based on current carrier and modulation type
        /// </summary>
        public void InitializeModulatingWaveforms()
        {
            if (_modulatingWaveformComboBox == null) return;

            _mainWindow.Dispatcher.Invoke(() =>
            {
                _modulatingWaveformComboBox.Items.Clear();

                // Get current modulation type
                ModulationType modType = GetSelectedModulationType();

                // Populate based on modulation type
                switch (modType)
                {
                    case ModulationType.ASK:
                    case ModulationType.FSK:
                    case ModulationType.PSK:
                        // Digital modulation - only Square with 50% duty
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Square (50% duty)" });
                        break;

                    case ModulationType.AM:
                    case ModulationType.FM:
                    case ModulationType.PM:
                    case ModulationType.PWM:
                        // Analog modulation - multiple options
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Sine" });
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Square (50% duty)" });
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Triangle (50% symmetry)" });
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Up-Ramp (100% symmetry)" });
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Down-Ramp (0% symmetry)" });
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Noise" });
                        _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "Arbitrary" });
                        break;
                }

                // Add External option for all types
                _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = "External (±5V rear input)" });

                // Select default based on carrier waveform
                SelectDefaultModulatingWaveform();
            });
        }

        /// <summary>
        /// Initialize frequency unit combo boxes
        /// </summary>
        public void InitializeFrequencyUnits()
        {
            if (_modulationFrequencyUnitComboBox == null) return;

            _mainWindow.Dispatcher.Invoke(() =>
            {
                // The carrier frequency unit combo box is already populated in XAML
                // Just ensure modulation frequency unit combo box is populated
                if (_modulationFrequencyUnitComboBox.Items.Count == 0)
                {
                    _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = "µHz" });
                    _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = "mHz" });
                    _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = "Hz" });
                    _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = "kHz" });
                    _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = "MHz" });

                    _modulationFrequencyUnitComboBox.SelectedIndex = 3; // Default to kHz
                }
            });
        }

        /// <summary>
        /// Handle carrier waveform selection change
        /// </summary>
        public void OnCarrierWaveformChanged()
        {
            try
            {
                // Get selected carrier waveform
                string carrierWaveform = GetSelectedCarrierWaveform();

                // Automatically select appropriate modulating waveform
                SelectDefaultModulatingWaveform();

                // Copy carrier frequency to modulating frequency
                CopyCarrierFrequencyToModulating();

                // Update modulation amplitude based on carrier
                UpdateModulationAmplitude();

                Log($"Carrier waveform changed to: {carrierWaveform}");
            }
            catch (Exception ex)
            {
                Log($"Error handling carrier waveform change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle modulation type selection change
        /// </summary>
        public void OnModulationTypeChanged()
        {
            try
            {
                _currentModulationType = GetSelectedModulationType();

                // Re-initialize modulating waveforms for new modulation type
                InitializeModulatingWaveforms();

                // Update UI based on modulation type
                UpdateUIForModulationType();

                Log($"Modulation type changed to: {_currentModulationType}");
            }
            catch (Exception ex)
            {
                Log($"Error handling modulation type change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle modulating waveform selection change
        /// </summary>
        public void OnModulatingWaveformChanged()
        {
            try
            {
                string modulatingWaveform = GetSelectedModulatingWaveform();
                Log($"Modulating waveform changed to: {modulatingWaveform}");

                // Apply modulation settings if connected
                if (_device != null && _device.IsConnected)
                {
                    ApplyModulation();
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling modulating waveform change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle modulation frequency text change
        /// </summary>
        public void OnModulationFrequencyChanged()
        {
            try
            {
                if (double.TryParse(_modulationFrequencyTextBox.Text, out double frequency))
                {
                    string unit = UnitConversionUtility.GetFrequencyUnit(_modulationFrequencyUnitComboBox);
                    double multiplier = UnitConversionUtility.GetFrequencyMultiplier(unit);
                    _modulationFrequency = frequency * multiplier;

                    // Format the display
                    _modulationFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(frequency);

                    Log($"Modulation frequency set to: {UnitConversionUtility.FormatWithMinimumDecimals(_modulationFrequency)} Hz");
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling modulation frequency change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle modulation frequency lost focus to format the value
        /// </summary>
        public void OnModulationFrequencyLostFocus()
        {
            if (double.TryParse(_modulationFrequencyTextBox.Text, out double frequency))
            {
                _modulationFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(frequency);
            }
        }

        /// <summary>
        /// Handle modulation depth change
        /// </summary>
        public void OnModulationDepthChanged()
        {
            try
            {
                if (double.TryParse(_modulationDepthTextBox.Text, out double depth))
                {
                    _modulationDepth = Math.Max(0, Math.Min(100, depth)); // Clamp to 0-100%
                    _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(_modulationDepth);

                    Log($"Modulation depth set to: {UnitConversionUtility.FormatWithMinimumDecimals(_modulationDepth)}%");
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling modulation depth change: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle modulation depth lost focus to format the value
        /// </summary>
        public void OnModulationDepthLostFocus()
        {
            if (double.TryParse(_modulationDepthTextBox.Text, out double depth))
            {
                depth = Math.Max(0, Math.Min(100, depth));
                _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
            }
        }

        /// <summary>
        /// Handle carrier frequency change
        /// </summary>
        public void OnCarrierFrequencyChanged()
        {
            try
            {
                if (double.TryParse(_carrierFrequencyTextBox.Text, out double frequency))
                {
                    string unit = UnitConversionUtility.GetFrequencyUnit(_carrierFrequencyUnitComboBox);
                    double multiplier = UnitConversionUtility.GetFrequencyMultiplier(unit);
                    _carrierFrequency = frequency * multiplier;

                    Log($"Carrier frequency set to: {UnitConversionUtility.FormatWithMinimumDecimals(_carrierFrequency)} Hz");
                }
            }
            catch (Exception ex)
            {
                Log($"Error handling carrier frequency change: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply modulation settings to the device
        /// </summary>
        public void ApplyModulation()
        {
            if (!_device.IsConnected) return;

            try
            {
                // Implementation will depend on the specific SCPI commands for modulation
                Log($"Applying {_currentModulationType} modulation to channel {_activeChannel}");

                // Example SCPI commands (adjust based on actual device documentation):
                switch (_currentModulationType)
                {
                    case ModulationType.AM:
                        _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:AM:SOURCE INT");
                        _device.SendCommand($"SOURCE{_activeChannel}:AM:DEPTH {_modulationDepth}");
                        _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FREQ {_modulationFrequency}");
                        break;

                    case ModulationType.FM:
                        _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:FM:SOURCE INT");
                        _device.SendCommand($"SOURCE{_activeChannel}:FM:DEV {_modulationFrequency}");
                        _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FREQ {_modulationFrequency}");
                        break;

                        // Add other modulation types as needed
                }
            }
            catch (Exception ex)
            {
                Log($"Error applying modulation: {ex.Message}");
            }
        }

        /// <summary>
        /// Disable modulation
        /// </summary>
        public void DisableModulation()
        {
            if (!_device.IsConnected) return;

            try
            {
                // Turn off all modulation types
                _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PWM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:ASK:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:FSK:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PSK:STATE OFF");

                Log($"Modulation disabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error disabling modulation: {ex.Message}");
            }
        }

        /// <summary>
        /// Get the selected carrier waveform
        /// </summary>
        private string GetSelectedCarrierWaveform()
        {
            if (_carrierWaveformComboBox?.SelectedItem is ComboBoxItem item)
            {
                return item.Content.ToString();
            }
            return "Sine";
        }

        /// <summary>
        /// Get the selected modulating waveform
        /// </summary>
        private string GetSelectedModulatingWaveform()
        {
            if (_modulatingWaveformComboBox?.SelectedItem is ComboBoxItem item)
            {
                return item.Content.ToString();
            }
            return "Sine";
        }

        /// <summary>
        /// Get the selected modulation type
        /// </summary>
        private ModulationType GetSelectedModulationType()
        {
            if (_modulationTypeComboBox?.SelectedIndex >= 0)
            {
                return (ModulationType)_modulationTypeComboBox.SelectedIndex;
            }
            return ModulationType.AM;
        }

        /// <summary>
        /// Select default modulating waveform based on carrier
        /// </summary>
        private void SelectDefaultModulatingWaveform()
        {
            if (_modulatingWaveformComboBox == null) return;

            string carrier = GetSelectedCarrierWaveform().ToUpper();

            // If carrier is Sine, Square, Ramp, or Arbitrary, use it as modulating source
            if (carrier == "SINE" || carrier == "SQUARE" || carrier == "RAMP" || carrier == "ARBITRARY")
            {
                // Find matching item in modulating waveform combo
                for (int i = 0; i < _modulatingWaveformComboBox.Items.Count; i++)
                {
                    if (_modulatingWaveformComboBox.Items[i] is ComboBoxItem item)
                    {
                        string content = item.Content.ToString().ToUpper();
                        if (content.StartsWith(carrier))
                        {
                            _modulatingWaveformComboBox.SelectedIndex = i;
                            return;
                        }
                    }
                }
            }

            // Default to Sine
            _modulatingWaveformComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Copy carrier frequency to modulating frequency
        /// </summary>
        private void CopyCarrierFrequencyToModulating()
        {
            try
            {
                // Get carrier frequency from the modulation tab's carrier frequency controls
                if (_carrierFrequencyTextBox != null && _carrierFrequencyUnitComboBox != null)
                {
                    if (double.TryParse(_carrierFrequencyTextBox.Text, out double carrierFreq))
                    {
                        // Copy the value
                        _modulationFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(carrierFreq);

                        // Copy the unit selection
                        if (_carrierFrequencyUnitComboBox.SelectedItem is ComboBoxItem sourceItem)
                        {
                            string unit = sourceItem.Content.ToString();
                            for (int i = 0; i < _modulationFrequencyUnitComboBox.Items.Count; i++)
                            {
                                if (_modulationFrequencyUnitComboBox.Items[i] is ComboBoxItem targetItem &&
                                    targetItem.Content.ToString() == unit)
                                {
                                    _modulationFrequencyUnitComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error copying carrier frequency: {ex.Message}");
            }
        }

        /// <summary>
        /// Update modulation amplitude based on carrier amplitude
        /// </summary>
        private void UpdateModulationAmplitude()
        {
            try
            {
                // Get carrier amplitude from the main channel controls
                var ampTextBox = _mainWindow.FindName("ChannelAmplitudeTextBox") as TextBox;
                var ampUnitComboBox = _mainWindow.FindName("ChannelAmplitudeUnitComboBox") as ComboBox;

                if (ampTextBox != null && ampUnitComboBox != null)
                {
                    if (double.TryParse(ampTextBox.Text, out double carrierAmp))
                    {
                        string unit = UnitConversionUtility.GetAmplitudeUnit(ampUnitComboBox);
                        double multiplier = UnitConversionUtility.GetAmplitudeMultiplier(unit);
                        double ampInVpp = carrierAmp * multiplier;

                        // For modulation, we typically use a percentage of carrier amplitude
                        double modulationAmp = ampInVpp * (_modulationDepth / 100.0);

                        Log($"Calculated modulation amplitude: {UnitConversionUtility.FormatWithMinimumDecimals(modulationAmp)} Vpp");
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating modulation amplitude: {ex.Message}");
            }
        }

        /// <summary>
        /// Update UI elements based on modulation type
        /// </summary>
        private void UpdateUIForModulationType()
        {
            // Different modulation types may have different parameter requirements
            switch (_currentModulationType)
            {
                case ModulationType.PWM:
                    // PWM only works with Pulse carrier
                    if (GetSelectedCarrierWaveform().ToUpper() != "PULSE")
                    {
                        Log("Warning: PWM modulation requires Pulse carrier waveform");
                        MessageBox.Show("PWM modulation requires Pulse carrier waveform",
                                      "Waveform Requirement",
                                      MessageBoxButton.OK,
                                      MessageBoxImage.Warning);
                    }
                    break;

                case ModulationType.ASK:
                case ModulationType.FSK:
                case ModulationType.PSK:
                    // Digital modulation types have specific requirements
                    Log($"Digital modulation type selected: {_currentModulationType}");
                    break;

                case ModulationType.FM:
                    // FM uses deviation instead of depth
                    if (_modulationDepthTextBox != null)
                    {
                        var label = _mainWindow.FindName("ModulationDepthLabel") as TextBlock;
                        if (label != null)
                        {
                            label.Text = "Frequency Deviation:";
                        }
                    }
                    break;

                case ModulationType.AM:
                    // AM uses depth percentage
                    if (_modulationDepthTextBox != null)
                    {
                        var label = _mainWindow.FindName("ModulationDepthLabel") as TextBlock;
                        if (label != null)
                        {
                            label.Text = "Modulation Depth (%):";
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Helper method for logging
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}