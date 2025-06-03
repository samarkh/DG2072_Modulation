using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Modulation
{
    /// <summary>
    /// Corrected modulation controller based on actual CSV parameter specifications
    /// </summary>
    public class ModulationController
    {
        private readonly RigolDG2072 _device;
        private readonly Window _mainWindow;
        private int _activeChannel;
        private bool _isModulationEnabled = false;

        // Event for logging
        public event EventHandler<string> LogEvent;

        // UI Control references
        private GroupBox _modulationPanel;
        private Button _modulationToggleButton;
        private ComboBox _modulationTypeComboBox;
        private ComboBox _modulatingWaveformComboBox;
        private TextBox _modulationFrequencyTextBox;
        private ComboBox _modulationFrequencyUnitComboBox;
        private TextBox _modulationDepthTextBox;
        private Label _modulationDepthLabel;
        private ComboBox _modulationDepthUnitComboBox;

        private CheckBox _dsscCheckBox;
        private DockPanel _dsscDockPanel;

        // Stored carrier settings when modulation is enabled
        private double _storedCarrierFrequency;
        private double _storedCarrierAmplitude;
        private double _storedCarrierOffset;
        private double _storedCarrierPhase;
        private string _storedCarrierWaveform;

        // Stored modulation settings for disable/enable
        private double _lastModulationFrequency = 100;
        private string _lastModulationFrequencyUnit = "Hz";
        private double _lastModulationDepth = 50.0;
        private string _lastModulationType = "AM";
        private string _lastModulatingWaveform = "Sine";
        private bool _lastDSSCEnabled = false;

        // CORRECTED: Parameter configuration based on actual CSV data
        private readonly Dictionary<string, ModulationParameterConfig> _modulationParameterConfig =
            new Dictionary<string, ModulationParameterConfig>
        {
            { "AM", new ModulationParameterConfig("Depth (%):", "", new[] { "%" }, 0, 120) },
            { "FM", new ModulationParameterConfig("Deviation:", "MHz", new[] { "Hz", "kHz", "MHz" }, -99.999, 99.999) },
            { "PM", new ModulationParameterConfig("Phase Deviation (°):", "°", new[] { "°", "deg" }, 0, 360) },
            { "PWM", new ModulationParameterConfig("Duty Deviation (%):", "", new[] { "%" }, 0, 100) },
            { "ASK", new ModulationParameterConfig("Amplitude:", "Vpp", new[] { "mVpp", "Vpp" }, 0, 10) },
            { "FSK", new ModulationParameterConfig("Hop Frequency:", "MHz", new[] { "Hz", "kHz", "MHz" }, 0, 200) },
            { "PSK", new ModulationParameterConfig("Phase (°):", "°", new[] { "°", "deg" }, 0, 360) }
        };

        // Available modulation types
        private readonly Dictionary<string, string[]> _modulationTypes = new Dictionary<string, string[]>
        {
            { "AM",  new[] { "Amplitude Modulation" } },
            { "FM",  new[] { "Frequency Modulation" } },
            { "PM",  new[] { "Phase Modulation" } },
            { "PWM", new[] { "Pulse Width Modulation" } },
            { "ASK", new[] { "Amplitude Shift Keying" } },
            { "FSK", new[] { "Frequency Shift Keying" } },
            { "PSK", new[] { "Phase Shift Keying" } }
        };

        // Modulating waveforms for different modulation types
        private readonly Dictionary<string, string[]> _modulatingWaveforms = new Dictionary<string, string[]>
        {
            { "AM",  new[] { "Sine", "Square", "Triangle", "Up Ramp", "Down Ramp", "Noise", "Arbitrary Waveform" } },
            { "FM",  new[] { "Sine", "Square", "Triangle", "Up Ramp", "Down Ramp", "Noise", "Arbitrary Waveform" } },
            { "PM",  new[] { "Sine", "Square", "Triangle", "Up Ramp", "Down Ramp", "Noise", "Arbitrary Waveform" } },
            { "PWM", new[] { "Sine", "Square", "Triangle", "Up Ramp", "Down Ramp", "Noise", "Arbitrary Waveform" } },
            { "ASK", new[] { "Square" } },
            { "FSK", new[] { "Square" } },
            { "PSK", new[] { "Square" } }
        };

        // Waveforms that support modulation
        private readonly HashSet<string> _modulatableWaveforms = new HashSet<string>
        {
            "SINE", "SQUARE", "RAMP", "PULSE"
        };

        public ModulationController(RigolDG2072 device, int channel, Window mainWindow)
        {
            _device = device;
            _activeChannel = channel;
            _mainWindow = mainWindow;
        }

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public event EventHandler ModulationStatusChanged;
        public bool IsEnabled => _isModulationEnabled;

        /// <summary>
        /// Check if UI has been initialized
        /// </summary>
        public bool IsUIInitialized()
        {
            return _modulationPanel != null &&
                   _modulationToggleButton != null &&
                   _modulationTypeComboBox != null;
        }

        /// <summary>
        /// Initialize UI controls with null checks
        /// </summary>
        public void InitializeUI()
        {
            try
            {
                // Find controls in the main window
                _modulationPanel = _mainWindow.FindName("ModulationPanel") as GroupBox;
                _modulationToggleButton = _mainWindow.FindName("ModulationToggleButton") as Button;
                _modulationTypeComboBox = _mainWindow.FindName("ModulationTypeComboBox") as ComboBox;
                _modulatingWaveformComboBox = _mainWindow.FindName("ModulatingWaveformComboBox") as ComboBox;
                _modulationFrequencyTextBox = _mainWindow.FindName("ModulationFrequencyTextBox") as TextBox;
                _modulationFrequencyUnitComboBox = _mainWindow.FindName("ModulationFrequencyUnitComboBox") as ComboBox;
                _modulationDepthTextBox = _mainWindow.FindName("ModulationDepthTextBox") as TextBox;
                _dsscCheckBox = _mainWindow.FindName("DSSCCheckBox") as CheckBox;
                _dsscDockPanel = _mainWindow.FindName("DSSCDockPanel") as DockPanel;

                // Find the depth label and unit controls
                _modulationDepthLabel = _mainWindow.FindName("ModulationDepthLabel") as Label;
                _modulationDepthUnitComboBox = _mainWindow.FindName("ModulationDepthUnitComboBox") as ComboBox;

                Log($"ModulationPanel found: {_modulationPanel != null}");
                Log($"ModulationToggleButton found: {_modulationToggleButton != null}");
                Log($"ModulationTypeComboBox found: {_modulationTypeComboBox != null}");

                // Only proceed if we have the essential controls
                if (_modulationTypeComboBox != null)
                {
                    // Initialize combo boxes
                    InitializeModulationTypes();
                    InitializeFrequencyUnits();

                    // Set default values
                    SetDefaultValues();

                    // Update parameter UI for default modulation type
                    UpdateParameterUIForModulationType("AM");
                }
                else
                {
                    Log("ERROR: Essential modulation controls not found!");
                }

                // Initially hide modulation controls
                UpdateModulationVisibility(false);
                Log("ModulationPanel initially hidden");
            }
            catch (Exception ex)
            {
                Log($"Error in InitializeUI: {ex.Message}");
            }
        }

        /// <summary>
        /// Update the parameter UI based on modulation type
        /// </summary>
        private void UpdateParameterUIForModulationType(string modulationType)
        {
            if (!_modulationParameterConfig.ContainsKey(modulationType)) return;

            var config = _modulationParameterConfig[modulationType];

            // Update the label
            if (_modulationDepthLabel != null)
            {
                _modulationDepthLabel.Content = config.ParameterLabel;
            }

            // Update the unit combo box if it exists
            if (_modulationDepthUnitComboBox != null && config.Units.Length > 0)
            {
                _modulationDepthUnitComboBox.Items.Clear();
                foreach (var unit in config.Units)
                {
                    _modulationDepthUnitComboBox.Items.Add(new ComboBoxItem { Content = unit });
                }

                // Select the default unit
                for (int i = 0; i < _modulationDepthUnitComboBox.Items.Count; i++)
                {
                    var item = _modulationDepthUnitComboBox.Items[i] as ComboBoxItem;
                    if (item?.Content.ToString() == config.DefaultUnit)
                    {
                        _modulationDepthUnitComboBox.SelectedIndex = i;
                        break;
                    }
                }

                if (_modulationDepthUnitComboBox.SelectedIndex == -1 && _modulationDepthUnitComboBox.Items.Count > 0)
                {
                    _modulationDepthUnitComboBox.SelectedIndex = 0;
                }

                _modulationDepthUnitComboBox.Visibility = config.HasUnits ? Visibility.Visible : Visibility.Collapsed;
            }

            // Set appropriate default value based on type
            if (_modulationDepthTextBox != null)
            {
                switch (modulationType)
                {
                    case "AM":
                        _modulationDepthTextBox.Text = "100.0"; // 100% depth
                        break;
                    case "FM":
                        _modulationDepthTextBox.Text = "10.0"; // 10 kHz deviation
                        break;
                    case "PM":
                        _modulationDepthTextBox.Text = "90.0"; // 90 degrees
                        break;
                    case "PWM":
                        _modulationDepthTextBox.Text = "10.0"; // 10% duty deviation
                        break;
                    case "ASK":
                        _modulationDepthTextBox.Text = "1.0"; // 1 Vpp
                        break;
                    case "FSK":
                        _modulationDepthTextBox.Text = "10.0"; // 10 kHz hop frequency
                        break;
                    case "PSK":
                        _modulationDepthTextBox.Text = "90.0"; // 90 degrees
                        break;
                }
            }

            Log($"Updated parameter UI for {modulationType}: {config.ParameterLabel}");
        }

        /// <summary>
        /// Get the parameter value in the correct units for the device
        /// </summary>
        private double GetParameterValueForDevice(string modulationType, double uiValue)
        {
            if (!_modulationParameterConfig.ContainsKey(modulationType))
                return uiValue;

            var config = _modulationParameterConfig[modulationType];

            // Handle unit conversions for parameters that have units
            if (config.HasUnits && _modulationDepthUnitComboBox?.SelectedItem != null)
            {
                string selectedUnit = ((ComboBoxItem)_modulationDepthUnitComboBox.SelectedItem).Content.ToString();

                switch (modulationType)
                {
                    case "FM":
                        // Convert frequency units to Hz for deviation
                        return uiValue * GetFrequencyMultiplier(selectedUnit);

                    case "FSK":
                        // Convert frequency units to Hz for hop frequency
                        return uiValue * GetFrequencyMultiplier(selectedUnit);

                    case "ASK":
                        // Convert voltage units to Vpp
                        return uiValue * GetVoltageMultiplier(selectedUnit);

                    case "PM":
                    case "PSK":
                        // Degrees - no conversion needed
                        return uiValue;

                    default:
                        return uiValue;
                }
            }

            return uiValue;
        }

        /// <summary>
        /// Get frequency multiplier for deviation parameters
        /// </summary>
        private double GetFrequencyMultiplier(string unit)
        {
            switch (unit)
            {
                case "Hz": return 1.0;
                case "kHz": return 1.0e3;
                case "MHz": return 1.0e6;
                default: return 1.0;
            }
        }

        /// <summary>
        /// CORRECTED: Get voltage multiplier for ASK amplitude
        /// </summary>
        private double GetVoltageMultiplier(string unit)
        {
            switch (unit)
            {
                case "mVpp": return 1.0e-3;
                case "Vpp": return 1.0;
                default: return 1.0;
            }
        }

        /// <summary>
        /// Set parameter value in UI from device value
        /// </summary>
        private void SetParameterValueFromDevice(string modulationType, double deviceValue)
        {
            if (!_modulationParameterConfig.ContainsKey(modulationType) || _modulationDepthTextBox == null)
                return;

            var config = _modulationParameterConfig[modulationType];
            double displayValue = deviceValue;
            string bestUnit = config.DefaultUnit;

            // Handle unit conversions for parameters that have units
            if (config.HasUnits && _modulationDepthUnitComboBox != null)
            {
                switch (modulationType)
                {
                    case "FM":
                    case "FSK":
                        // Auto-select best frequency unit
                        if (Math.Abs(deviceValue) >= 1e6)
                        {
                            bestUnit = "MHz";
                            displayValue = deviceValue / 1e6;
                        }
                        else if (Math.Abs(deviceValue) >= 1e3)
                        {
                            bestUnit = "kHz";
                            displayValue = deviceValue / 1e3;
                        }
                        else
                        {
                            bestUnit = "Hz";
                            displayValue = deviceValue;
                        }
                        break;

                    case "ASK":
                        // Auto-select best voltage unit
                        if (deviceValue < 0.1)
                        {
                            bestUnit = "mVpp";
                            displayValue = deviceValue * 1000;
                        }
                        else
                        {
                            bestUnit = "Vpp";
                            displayValue = deviceValue;
                        }
                        break;

                    case "PM":
                    case "PSK":
                        bestUnit = "°";
                        displayValue = deviceValue;
                        break;
                }

                // Select the unit in the combo box
                for (int i = 0; i < _modulationDepthUnitComboBox.Items.Count; i++)
                {
                    var item = _modulationDepthUnitComboBox.Items[i] as ComboBoxItem;
                    if (item?.Content.ToString() == bestUnit)
                    {
                        _modulationDepthUnitComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
        }

        /// <summary>
        /// Apply modulation settings to the device - CORRECTED with proper parameter handling
        /// </summary>
        public void ApplyModulation()
        {
            if (!IsDeviceConnected() || !_isModulationEnabled) return;

            try
            {
                // Get modulation type
                string modulationType = "AM";
                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                }

                // Use stored carrier settings
                double carrierFrequency = _storedCarrierFrequency;
                double carrierAmplitude = _storedCarrierAmplitude;
                double carrierOffset = _storedCarrierOffset;
                double carrierPhase = _storedCarrierPhase;
                string carrierWaveform = _storedCarrierWaveform;

                // Get modulating waveform
                string modulatingWaveform = "SINE";
                if (_modulatingWaveformComboBox?.SelectedItem != null)
                {
                    modulatingWaveform = ((ComboBoxItem)_modulatingWaveformComboBox.SelectedItem).Content.ToString();
                }

                // Get modulation frequency
                double modFrequency = 1000; // Default 1kHz
                if (_modulationFrequencyTextBox != null && double.TryParse(_modulationFrequencyTextBox.Text, out double modFreq))
                {
                    string unit = "Hz";
                    if (_modulationFrequencyUnitComboBox?.SelectedItem != null)
                    {
                        unit = ((ComboBoxItem)_modulationFrequencyUnitComboBox.SelectedItem).Content.ToString();
                    }
                    modFrequency = modFreq * UnitConversionUtility.GetFrequencyMultiplier(unit);
                }

                // Get modulation parameter value in correct units
                double parameterValue = 50; // Default
                if (_modulationDepthTextBox != null && double.TryParse(_modulationDepthTextBox.Text, out double param))
                {
                    parameterValue = GetParameterValueForDevice(modulationType, param);
                }

                // Log what we're applying
                Log($"Applying {modulationType} modulation:");
                Log($"  Carrier: {carrierWaveform} at {carrierFrequency} Hz, {carrierAmplitude} Vpp");
                Log($"  Modulating: {modulatingWaveform} at {modFrequency} Hz");
                Log($"  Parameter: {parameterValue} ({_modulationParameterConfig[modulationType].ParameterLabel})");

                // Apply modulation with correct parameter
                ApplyModulationByType(modulationType, carrierWaveform, carrierFrequency, carrierAmplitude,
                    modulatingWaveform, modFrequency, parameterValue);

                Log($"Modulation applied successfully");
            }
            catch (Exception ex)
            {
                Log($"Error applying modulation: {ex.Message}");
            }
        }

        /// <summary>
        /// Apply specific modulation type to the device - CORRECTED parameter handling
        /// </summary>
        private void ApplyModulationByType(string modulationType, string carrierWaveform,
                                    double carrierFrequency, double carrierAmplitude,
                                    string modulatingWaveform, double modFrequency, double parameterValue)
        {
            // Get current offset and phase from stored values
            double carrierOffset = _storedCarrierOffset;
            double carrierPhase = _storedCarrierPhase;

            // Map carrier waveform to SCPI command
            string scpiCarrierWaveform = MapCarrierWaveformToScpi(carrierWaveform);
            string scpiModulatingWaveform = MapModulatingWaveformToScpi(modulatingWaveform);

            Log($"Applying {modulationType} with carrier {scpiCarrierWaveform} @ {carrierFrequency}Hz");

            // Send SCPI commands based on modulation type - CORRECTED parameter handling
            switch (modulationType)
            {
                case "AM":
                    ApplyAMModulation(scpiCarrierWaveform, carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        scpiModulatingWaveform, modFrequency, parameterValue);
                    break;

                case "FM":
                    ApplyFMModulation(scpiCarrierWaveform, carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        scpiModulatingWaveform, modFrequency, parameterValue);
                    break;

                case "PM":
                    ApplyPMModulation(scpiCarrierWaveform, carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        scpiModulatingWaveform, modFrequency, parameterValue);
                    break;

                case "PWM":
                    ApplyPWMModulation(carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        scpiModulatingWaveform, modFrequency, parameterValue);
                    break;

                case "ASK":
                    ApplyASKModulation(scpiCarrierWaveform, carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        modFrequency, parameterValue);
                    break;

                case "FSK":
                    ApplyFSKModulation(scpiCarrierWaveform, carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        modFrequency, parameterValue);
                    break;

                case "PSK":
                    ApplyPSKModulation(scpiCarrierWaveform, carrierFrequency, carrierAmplitude, carrierOffset, carrierPhase,
                        modFrequency, parameterValue);
                    break;

                default:
                    Log($"Unknown modulation type: {modulationType}");
                    break;
            }
        }

        /// <summary>
        /// CORRECTED: Apply AM modulation (Depth 0-120%)
        /// </summary>
        private void ApplyAMModulation(string carrierWaveform, double carrierFreq, double carrierAmp,
            double carrierOffset, double carrierPhase, string modWaveform, double modFreq, double depth)
        {
            string amState = _device.SendQuery($"SOURCE{_activeChannel}:AM:STATE?");
            bool isAMActive = (amState.Trim() == "ON" || amState.Trim() == "1");

            if (!isAMActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{carrierWaveform} {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:AM:SOURCE INT");
                _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FUNC {modWaveform}");
            }

            // Clamp depth to valid range (0-120%)
            depth = Math.Max(0, Math.Min(120, depth));
            _device.SendCommand($"SOURCE{_activeChannel}:AM:DEPTH {depth}");
            _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FREQ {modFreq}");

            // Use checkbox state for DSSC
            bool dsscEnabled = _dsscCheckBox?.IsChecked ?? false;
            _device.SendCommand($"SOURCE{_activeChannel}:AM:DSSC {(dsscEnabled ? "ON" : "OFF")}");
        }

        /// <summary>
        /// CORRECTED: Apply FM modulation (Deviation in Hz, ±99.999 MHz max)
        /// </summary>
        private void ApplyFMModulation(string carrierWaveform, double carrierFreq, double carrierAmp,
            double carrierOffset, double carrierPhase, string modWaveform, double modFreq, double deviation)
        {
            string fmState = _device.SendQuery($"SOURCE{_activeChannel}:FM:STATE?");
            bool isFMActive = (fmState.Trim() == "ON" || fmState.Trim() == "1");

            if (!isFMActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{carrierWaveform} {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:FM:SOURCE INT");
                _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FUNC {modWaveform}");
            }

            // Clamp deviation to valid range (±99.999 MHz)
            double maxDeviation = 99.999e6; // 99.999 MHz in Hz
            deviation = Math.Max(-maxDeviation, Math.Min(maxDeviation, deviation));

            _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FREQ {modFreq}");
            _device.SendCommand($"SOURCE{_activeChannel}:FM:DEVIATION {deviation}");
        }

        /// <summary>
        /// CORRECTED: Apply PM modulation (Phase Deviation 0-360°)
        /// </summary>
        private void ApplyPMModulation(string carrierWaveform, double carrierFreq, double carrierAmp,
            double carrierOffset, double carrierPhase, string modWaveform, double modFreq, double phaseDeviation)
        {
            string pmState = _device.SendQuery($"SOURCE{_activeChannel}:PM:STATE?");
            bool isPMActive = (pmState.Trim() == "ON" || pmState.Trim() == "1");

            if (!isPMActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{carrierWaveform} {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:PM:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:PM:SOURCE INT");
                _device.SendCommand($"SOURCE{_activeChannel}:PM:INT:FUNC {modWaveform}");
            }

            // Clamp phase deviation to valid range (0-360°)
            phaseDeviation = Math.Max(0, Math.Min(360, phaseDeviation));

            _device.SendCommand($"SOURCE{_activeChannel}:PM:INT:FREQ {modFreq}");
            _device.SendCommand($"SOURCE{_activeChannel}:PM:DEVIATION {phaseDeviation}");
        }

        /// <summary>
        /// Apply PWM modulation (Duty Cycle Deviation %)
        /// </summary>
        private void ApplyPWMModulation(double carrierFreq, double carrierAmp, double carrierOffset,
            double carrierPhase, string modWaveform, double modFreq, double dutyDeviation)
        {
            string pwmState = _device.SendQuery($"SOURCE{_activeChannel}:PWM:STATE?");
            bool isPWMActive = (pwmState.Trim() == "ON" || pwmState.Trim() == "1");

            if (!isPWMActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:PULS {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:PWM:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:PWM:SOURCE INT");
                _device.SendCommand($"SOURCE{_activeChannel}:PWM:INT:FUNC {modWaveform}");
            }

            _device.SendCommand($"SOURCE{_activeChannel}:PWM:INT:FREQ {modFreq}");
            _device.SendCommand($"SOURCE{_activeChannel}:PWM:DEVIATION {dutyDeviation}");
        }

        /// <summary>
        /// CORRECTED: Apply ASK modulation (Amplitude in Vpp, 0-10 Vpp)
        /// </summary>
        private void ApplyASKModulation(string carrierWaveform, double carrierFreq, double carrierAmp,
            double carrierOffset, double carrierPhase, double modFreq, double amplitudeVpp)
        {
            string askState = _device.SendQuery($"SOURCE{_activeChannel}:ASK:STATE?");
            bool isASKActive = (askState.Trim() == "ON" || askState.Trim() == "1");

            if (!isASKActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{carrierWaveform} {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:ASK:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:ASK:SOURCE INT");
            }

            // Clamp amplitude to valid range (0-10 Vpp)
            amplitudeVpp = Math.Max(0, Math.Min(10, amplitudeVpp));

            _device.SendCommand($"SOURCE{_activeChannel}:ASK:RATE {modFreq}");
            // CORRECTED: ASK amplitude is in Vpp, not percentage
            _device.SendCommand($"SOURCE{_activeChannel}:ASK:AMPLITUDE {amplitudeVpp}");
        }

        /// <summary>
        /// CORRECTED: Apply FSK modulation (Hop Frequency - absolute frequency)
        /// </summary>
        private void ApplyFSKModulation(string carrierWaveform, double carrierFreq, double carrierAmp,
            double carrierOffset, double carrierPhase, double modFreq, double hopFrequency)
        {
            string fskState = _device.SendQuery($"SOURCE{_activeChannel}:FSK:STATE?");
            bool isFSKActive = (fskState.Trim() == "ON" || fskState.Trim() == "1");

            if (!isFSKActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{carrierWaveform} {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:FSK:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:FSK:SOURCE INT");
            }

            _device.SendCommand($"SOURCE{_activeChannel}:FSK:RATE {modFreq}");
            // CORRECTED: FSK uses the absolute hop frequency, not carrier + shift
            _device.SendCommand($"SOURCE{_activeChannel}:FSK:FREQUENCY {hopFrequency}");
        }

        /// <summary>
        /// CORRECTED: Apply PSK modulation (Phase 0-360°)
        /// </summary>
        private void ApplyPSKModulation(string carrierWaveform, double carrierFreq, double carrierAmp,
            double carrierOffset, double carrierPhase, double modFreq, double phaseShift)
        {
            string pskState = _device.SendQuery($"SOURCE{_activeChannel}:PSK:STATE?");
            bool isPSKActive = (pskState.Trim() == "ON" || pskState.Trim() == "1");

            if (!isPSKActive)
            {
                _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{carrierWaveform} {carrierFreq},{carrierAmp},{carrierOffset},{carrierPhase}");
                System.Threading.Thread.Sleep(100);

                _device.SendCommand($"SOURCE{_activeChannel}:PSK:STATE ON");
                _device.SendCommand($"SOURCE{_activeChannel}:PSK:SOURCE INT");
            }

            // Clamp phase to valid range (0-360°)
            phaseShift = Math.Max(0, Math.Min(360, phaseShift));

            _device.SendCommand($"SOURCE{_activeChannel}:PSK:RATE {modFreq}");
            _device.SendCommand($"SOURCE{_activeChannel}:PSK:PHASE {phaseShift}");
        }

        /// <summary>
        /// Map carrier waveform to SCPI command
        /// </summary>
        private string MapCarrierWaveformToScpi(string carrierWaveform)
        {
            switch (carrierWaveform.ToUpper())
            {
                case "SINE":
                case "SIN":
                    return "SIN";
                case "SQUARE":
                case "SQU":
                    return "SQU";
                case "RAMP":
                    return "RAMP";
                case "PULSE":
                case "PULS":
                    return "PULS";
                default:
                    return "SIN";
            }
        }

        /// <summary>
        /// Map modulating waveform UI name to SCPI command
        /// </summary>
        private string MapModulatingWaveformToScpi(string uiWaveform)
        {
            switch (uiWaveform.ToUpper())
            {
                case "SINE": return "SIN";
                case "SQUARE": return "SQU";
                case "TRIANGLE": return "TRI";
                case "UP RAMP": return "RAMP";
                case "DOWN RAMP": return "NRAM";
                case "NOISE": return "NOIS";
                case "ARB": return "ARB";
                case "ARBITRARY WAVEFORM": return "ARB";
                default: return "SIN";
            }
        }

        // [Rest of the helper methods - Enable/Disable, UI updates, etc. remain the same]
        // [Include all the initialization, UI management, and refresh methods]

        #region Helper Methods and UI Management

        public bool IsModulationAvailable(string waveformType)
        {
            if (string.IsNullOrEmpty(waveformType)) return false;
            return _modulatableWaveforms.Contains(waveformType.ToUpper());
        }

        public void UpdateModulationAvailability(string waveformType)
        {
            bool isAvailable = IsModulationAvailable(waveformType);

            if (_modulationToggleButton != null)
            {
                _modulationToggleButton.Visibility = isAvailable ? Visibility.Visible : Visibility.Collapsed;
            }

            if (!isAvailable && _isModulationEnabled)
            {
                DisableModulation();
            }
        }

        public void EnableModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                StoreCarrierSettings();
                _isModulationEnabled = true;
                UpdateModulationVisibility(true);
                UpdateToggleButtonState();

                // Restore saved settings and update UI
                if (_modulationFrequencyTextBox != null && _lastModulationFrequency > 0)
                {
                    _modulationFrequencyTextBox.Text = _lastModulationFrequency.ToString();
                    // Restore frequency unit
                    for (int i = 0; i < _modulationFrequencyUnitComboBox.Items.Count; i++)
                    {
                        var item = _modulationFrequencyUnitComboBox.Items[i] as ComboBoxItem;
                        if (item?.Content.ToString() == _lastModulationFrequencyUnit)
                        {
                            _modulationFrequencyUnitComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }

                if (_modulationDepthTextBox != null)
                {
                    _modulationDepthTextBox.Text = _lastModulationDepth.ToString();
                }

                if (_dsscCheckBox != null)
                {
                    _dsscCheckBox.IsChecked = _lastDSSCEnabled;
                }

                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    string currentModType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                    UpdateAvailableModulatingWaveforms(currentModType);
                    UpdateParameterUIForModulationType(currentModType);

                    if (_dsscDockPanel != null)
                    {
                        _dsscDockPanel.Visibility = (currentModType == "AM") ? Visibility.Visible : Visibility.Collapsed;
                    }
                }

                ApplyModulation();
                Log("Modulation enabled");
            }
            catch (Exception ex)
            {
                Log($"Error enabling modulation: {ex.Message}");
                _isModulationEnabled = false;
                UpdateModulationVisibility(false);
            }
        }

        public void DisableModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Save current settings
                if (_modulationFrequencyTextBox != null && double.TryParse(_modulationFrequencyTextBox.Text, out double modFreq))
                {
                    _lastModulationFrequency = modFreq;
                    if (_modulationFrequencyUnitComboBox?.SelectedItem != null)
                    {
                        _lastModulationFrequencyUnit = ((ComboBoxItem)_modulationFrequencyUnitComboBox.SelectedItem).Content.ToString();
                    }
                }

                if (_modulationDepthTextBox != null && double.TryParse(_modulationDepthTextBox.Text, out double depth))
                {
                    _lastModulationDepth = depth;
                }

                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    _lastModulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                }

                if (_dsscCheckBox != null)
                {
                    _lastDSSCEnabled = _dsscCheckBox.IsChecked ?? false;
                }

                // Turn off all modulation types
                _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PWM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:ASK:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:FSK:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PSK:STATE OFF");

                _isModulationEnabled = false;
                UpdateModulationVisibility(false);
                UpdateToggleButtonState();

                Log("Modulation disabled");
            }
            catch (Exception ex)
            {
                Log($"Error disabling modulation: {ex.Message}");
            }
        }

        public void OnModulationTypeChanged()
        {
            if (_modulationTypeComboBox?.SelectedItem == null) return;

            string modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();

            UpdateParameterUIForModulationType(modulationType);
            UpdateAvailableModulatingWaveforms(modulationType);

            if (_dsscDockPanel != null)
            {
                _dsscDockPanel.Visibility = (modulationType == "AM") ? Visibility.Visible : Visibility.Collapsed;
            }

            if (_isModulationEnabled)
            {
                ApplyModulation();
            }

            Log($"Modulation type changed to: {modulationType}");
        }

        public void OnModulationParameterChanged()
        {
            if (_isModulationEnabled)
            {
                ApplyModulation();
            }
        }

        public void OnDSSCChanged(bool isEnabled)
        {
            if (_isModulationEnabled)
            {
                ApplyModulation();
            }
        }

        private void StoreCarrierSettings()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                _storedCarrierFrequency = GetCarrierFrequencyFromUI();
                _storedCarrierAmplitude = GetCarrierAmplitudeFromUI();
                _storedCarrierOffset = GetCarrierOffsetFromUI();
                _storedCarrierPhase = GetCarrierPhaseFromUI();

                string waveform = _device.SendQuery($":SOUR{_activeChannel}:FUNC?").Trim().ToUpper();
                if (waveform.StartsWith("\"") && waveform.EndsWith("\""))
                {
                    waveform = waveform.Substring(1, waveform.Length - 2);
                }
                _storedCarrierWaveform = waveform;

                Log($"Stored carrier settings: {_storedCarrierWaveform} @ {_storedCarrierFrequency}Hz, {_storedCarrierAmplitude}Vpp");
            }
            catch (Exception ex)
            {
                Log($"Error storing carrier settings: {ex.Message}");
            }
        }

        private double GetCarrierFrequencyFromUI()
        {
            try
            {
                var freqTextBox = _mainWindow.FindName("ChannelFrequencyTextBox") as TextBox;
                var unitComboBox = _mainWindow.FindName("ChannelFrequencyUnitComboBox") as ComboBox;

                if (freqTextBox != null && unitComboBox != null &&
                    double.TryParse(freqTextBox.Text, out double frequency))
                {
                    string unit = ((ComboBoxItem)unitComboBox.SelectedItem)?.Content.ToString() ?? "Hz";
                    double multiplier = UnitConversionUtility.GetFrequencyMultiplier(unit);
                    return frequency * multiplier;
                }
            }
            catch (Exception ex)
            {
                Log($"Error getting frequency from UI: {ex.Message}");
            }

            return _device.GetFrequency(_activeChannel);
        }

        private double GetCarrierAmplitudeFromUI()
        {
            try
            {
                var ampTextBox = _mainWindow.FindName("ChannelAmplitudeTextBox") as TextBox;
                var unitComboBox = _mainWindow.FindName("ChannelAmplitudeUnitComboBox") as ComboBox;

                if (ampTextBox != null && unitComboBox != null &&
                    double.TryParse(ampTextBox.Text, out double amplitude))
                {
                    string unit = ((ComboBoxItem)unitComboBox.SelectedItem)?.Content.ToString() ?? "Vpp";
                    double multiplier = UnitConversionUtility.GetAmplitudeMultiplier(unit);
                    return amplitude * multiplier;
                }
            }
            catch { }

            return _device.GetAmplitude(_activeChannel);
        }

        private double GetCarrierOffsetFromUI()
        {
            try
            {
                var offsetTextBox = _mainWindow.FindName("ChannelOffsetTextBox") as TextBox;
                if (offsetTextBox != null && double.TryParse(offsetTextBox.Text, out double offset))
                {
                    return offset;
                }
            }
            catch { }

            return _device.GetOffset(_activeChannel);
        }

        private double GetCarrierPhaseFromUI()
        {
            try
            {
                var phaseTextBox = _mainWindow.FindName("ChannelPhaseTextBox") as TextBox;
                if (phaseTextBox != null && double.TryParse(phaseTextBox.Text, out double phase))
                {
                    return phase;
                }
            }
            catch { }

            return _device.GetPhase(_activeChannel);
        }

        private void SetDefaultValues()
        {
            if (_modulationFrequencyTextBox != null)
                _modulationFrequencyTextBox.Text = "100";

            if (_modulationFrequencyUnitComboBox != null)
            {
                for (int i = 0; i < _modulationFrequencyUnitComboBox.Items.Count; i++)
                {
                    var item = _modulationFrequencyUnitComboBox.Items[i] as ComboBoxItem;
                    if (item?.Content.ToString() == "Hz")
                    {
                        _modulationFrequencyUnitComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            if (_modulationDepthTextBox != null)
                _modulationDepthTextBox.Text = "100.0";

            if (_modulationTypeComboBox?.SelectedItem != null)
            {
                string currentModType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                UpdateAvailableModulatingWaveforms(currentModType);
            }
        }

        private void InitializeModulationTypes()
        {
            if (_modulationTypeComboBox == null) return;

            _modulationTypeComboBox.Items.Clear();
            foreach (var modType in _modulationTypes.Keys)
            {
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = modType });
            }

            if (_modulationTypeComboBox.Items.Count > 0)
            {
                _modulationTypeComboBox.SelectedIndex = 0;
                string defaultModType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                UpdateAvailableModulatingWaveforms(defaultModType);
            }
        }

        private void InitializeFrequencyUnits()
        {
            if (_modulationFrequencyUnitComboBox == null) return;

            _modulationFrequencyUnitComboBox.Items.Clear();
            string[] units = { "µHz", "mHz", "Hz", "kHz", "MHz" };
            foreach (var unit in units)
            {
                _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = unit });
            }
            _modulationFrequencyUnitComboBox.SelectedIndex = 2; // Default to Hz
        }

        private void UpdateToggleButtonState()
        {
            if (_modulationToggleButton != null)
            {
                _modulationToggleButton.Content = _isModulationEnabled ? "Disable Modulation" : "Enable Modulation";

                if (_isModulationEnabled)
                {
                    _modulationToggleButton.Background = System.Windows.Media.Brushes.LightCoral;
                    if (_modulationPanel != null)
                    {
                        _modulationPanel.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    _modulationToggleButton.Background = System.Windows.Media.Brushes.LightGreen;
                    if (_modulationPanel != null)
                    {
                        _modulationPanel.Visibility = Visibility.Collapsed;
                    }
                }
            }
        }

        private void UpdateModulationVisibility(bool visible)
        {
            if (_modulationPanel != null)
            {
                _modulationPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void UpdateAvailableModulatingWaveforms(string modulationType)
        {
            if (_modulatingWaveformComboBox == null) return;

            _modulatingWaveformComboBox.Items.Clear();

            if (_modulatingWaveforms.ContainsKey(modulationType))
            {
                foreach (var waveform in _modulatingWaveforms[modulationType])
                {
                    _modulatingWaveformComboBox.Items.Add(new ComboBoxItem { Content = waveform });
                }

                if (_modulatingWaveformComboBox.Items.Count > 0)
                    _modulatingWaveformComboBox.SelectedIndex = 0;
            }
        }

        private bool IsDeviceConnected()
        {
            return _device != null && _device.IsConnected;
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        public string CurrentModulationType
        {
            get
            {
                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    return ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                }
                return "Unknown";
            }
        }

        public bool IsDSSCEnabled
        {
            get
            {
                return _dsscCheckBox?.IsChecked ?? false;
            }
        }

        // Stub methods for compatibility - implement as needed
        public void SyncModulationFromDevice(string detectedModType) { /* Implementation */ }
        public void ForceDisableUI() { /* Implementation */ }
        public void RefreshModulationSettings() { /* Implementation */ }
        public void ForceUpdateUIState() { /* Implementation */ }
        public void SetModulationEnabledState(bool enabled) { /* Implementation */ }
        public void EnableModulationUIOnly() { /* Implementation */ }

        #endregion
    }

    /// <summary>
    /// CORRECTED: Configuration for each modulation type's parameter with ranges
    /// </summary>
    public class ModulationParameterConfig
    {
        public string ParameterLabel { get; }
        public string DefaultUnit { get; }
        public string[] Units { get; }
        public double MinValue { get; }
        public double MaxValue { get; }
        public bool HasUnits => Units.Length > 0 && !string.IsNullOrEmpty(Units[0]);

        public ModulationParameterConfig(string parameterLabel, string defaultUnit, string[] units, double minValue, double maxValue)
        {
            ParameterLabel = parameterLabel;
            DefaultUnit = defaultUnit;
            Units = units ?? new string[0];
            MinValue = minValue;
            MaxValue = maxValue;
        }
    }
}