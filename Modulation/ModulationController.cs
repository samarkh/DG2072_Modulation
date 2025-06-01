using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Modulation
{
    /// <summary>
    /// Refactored modulation controller for integrated UI approach
    /// Preserves all working SCPI backend methods while simplifying UI integration
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

        // Stored carrier settings when modulation is enabled
        private double _storedCarrierFrequency;
        private double _storedCarrierAmplitude;
        private double _storedCarrierOffset;
        private double _storedCarrierPhase;
        private string _storedCarrierWaveform;

        // ===== PRESERVED FROM ORIGINAL ModulationManager =====

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

        public bool IsEnabled => _isModulationEnabled;

        /// <summary>
        /// Initialize UI controls - called after window is loaded
        /// </summary>
        public void InitializeUI()
        {
            // Find controls in the main window
            _modulationPanel = _mainWindow.FindName("ModulationPanel") as GroupBox;
            _modulationToggleButton = _mainWindow.FindName("ModulationToggleButton") as Button;
            _modulationTypeComboBox = _mainWindow.FindName("ModulationTypeComboBox") as ComboBox;
            _modulatingWaveformComboBox = _mainWindow.FindName("ModulatingWaveformComboBox") as ComboBox;
            _modulationFrequencyTextBox = _mainWindow.FindName("ModulationFrequencyTextBox") as TextBox;
            _modulationFrequencyUnitComboBox = _mainWindow.FindName("ModulationFrequencyUnitComboBox") as ComboBox;
            _modulationDepthTextBox = _mainWindow.FindName("ModulationDepthTextBox") as TextBox;

            // Initialize combo boxes
            InitializeModulationTypes();
            InitializeFrequencyUnits();

            // Set default values
            SetDefaultValues();

            // Initially hide modulation controls
            UpdateModulationVisibility(false);
        }

        /// <summary>
        /// Check if the current waveform supports modulation
        /// </summary>
        public bool IsModulationAvailable(string waveformType)
        {
            if (string.IsNullOrEmpty(waveformType)) return false;
            return _modulatableWaveforms.Contains(waveformType.ToUpper());
        }

        /// <summary>
        /// Update visibility of modulation controls based on waveform
        /// </summary>
        public void UpdateModulationAvailability(string waveformType)
        {
            bool isAvailable = IsModulationAvailable(waveformType);

            if (_modulationToggleButton != null)
            {
                _modulationToggleButton.Visibility = isAvailable ? Visibility.Visible : Visibility.Collapsed;
            }

            // If modulation is not available and it's currently enabled, disable it
            if (!isAvailable && _isModulationEnabled)
            {
                DisableModulation();
            }

            // Special handling for PWM - requires pulse carrier
            if (_isModulationEnabled && _modulationTypeComboBox?.SelectedItem != null)
            {
                string modType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                if (modType == "PWM" && waveformType.ToUpper() != "PULSE")
                {
                    Log("PWM modulation requires Pulse carrier - switching waveform");
                    // This would trigger a waveform change in MainWindow
                }
            }
        }

        /// <summary>
        /// Enable modulation mode
        /// </summary>
        public void EnableModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Store current carrier settings
                StoreCarrierSettings();

                // Update UI
                _isModulationEnabled = true;
                UpdateModulationVisibility(true);
                UpdateToggleButtonState();

                // Get current waveform frequency and copy to modulation frequency
                double carrierFreq = _device.GetFrequency(_activeChannel);
                UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, carrierFreq);

                // Apply modulation with current settings
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

        /// <summary>
        /// Disable modulation mode
        /// </summary>
        public void DisableModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Turn off all modulation types (preserved from original)
                _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PWM:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:ASK:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:FSK:STATE OFF");
                _device.SendCommand($"SOURCE{_activeChannel}:PSK:STATE OFF");

                // Update UI
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

        /// <summary>
        /// Called when modulation type changes
        /// </summary>
        public void OnModulationTypeChanged()
        {
            if (_modulationTypeComboBox?.SelectedItem == null) return;

            string modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();

            // Update available modulating waveforms
            UpdateAvailableModulatingWaveforms(modulationType);

            // If modulation is enabled, apply the change
            if (_isModulationEnabled)
            {
                ApplyModulation();
            }

            Log($"Modulation type changed to: {modulationType}");
        }

        /// <summary>
        /// Called when any modulation parameter changes
        /// </summary>
        public void OnModulationParameterChanged()
        {
            if (_isModulationEnabled)
            {
                // Debounce and apply changes
                ApplyModulation();
            }
        }

        // ===== PRESERVED BACKEND METHODS FROM ORIGINAL ModulationManager =====

        /// <summary>
        /// Apply modulation settings to the device (preserved from original)
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

                // Get modulation depth
                double modDepth = 50; // Default 50%
                if (_modulationDepthTextBox != null && double.TryParse(_modulationDepthTextBox.Text, out double depth))
                {
                    modDepth = depth;
                }

                // Log what we're applying
                Log($"Applying {modulationType} modulation:");
                Log($"  Carrier: {carrierWaveform} at {carrierFrequency} Hz, {carrierAmplitude} Vpp");
                Log($"  Modulating: {modulatingWaveform} at {modFrequency} Hz");
                Log($"  Depth: {modDepth}%");

                // Apply modulation (using preserved method)
                ApplyModulationByType(modulationType, carrierWaveform, carrierFrequency, carrierAmplitude,
                    modulatingWaveform, modFrequency, modDepth);

                Log($"Modulation applied successfully");
            }
            catch (Exception ex)
            {
                Log($"Error applying modulation: {ex.Message}");
            }
        }

        // ===== ALL PRESERVED SCPI METHODS FROM ORIGINAL =====

        /// <summary>
        /// Map modulating waveform UI name to SCPI command (preserved)
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
                default: return uiWaveform;
            }
        }

        /// <summary>
        /// Apply specific modulation type to the device (FULLY PRESERVED)
        /// </summary>
        private void ApplyModulationByType(string modulationType, string carrierWaveform,
                                    double carrierFrequency, double carrierAmplitude,
                                    string modulatingWaveform, double modFrequency, double modDepth)
        {
            // Get current offset and phase from stored values
            double carrierOffset = _storedCarrierOffset;
            double carrierPhase = _storedCarrierPhase;

            // Map carrier waveform to SCPI command
            string scpiCarrierWaveform = "SIN"; // Default
            switch (carrierWaveform)
            {
                case "SINE": scpiCarrierWaveform = "SIN"; break;
                case "SQUARE": scpiCarrierWaveform = "SQU"; break;
                case "RAMP": scpiCarrierWaveform = "RAMP"; break;
                case "PULSE": scpiCarrierWaveform = "PULS"; break;
            }

            // Map modulating waveform to SCPI command
            string scpiModulatingWaveform = MapModulatingWaveformToScpi(modulatingWaveform);

            // Send SCPI commands based on modulation type
            switch (modulationType)
            {
                case "AM":
                    // Set carrier waveform with the SPECIFIED frequency and amplitude
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FUNC {scpiModulatingWaveform}");
                    _device.SendCommand($"SOURCE{_activeChannel}:AM {modDepth}");
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FREQ {modFrequency}");
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:DSSC OFF");
                    break;

                case "FM":
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:FM:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FUNC {scpiModulatingWaveform}");
                    _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FREQ {modFrequency}");
                    double deviation = carrierFrequency * (modDepth / 100.0);
                    _device.SendCommand($"SOURCE{_activeChannel}:FM:DEVIATION {deviation}");
                    break;

                case "PM":
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:PM:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:PM:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:PM:INT:FUNC {scpiModulatingWaveform}");
                    _device.SendCommand($"SOURCE{_activeChannel}:PM:INT:FREQ {modFrequency}");
                    double phaseDeviation = 180 * (modDepth / 100.0); // Convert to degrees
                    _device.SendCommand($"SOURCE{_activeChannel}:PM:DEVIATION {phaseDeviation}");
                    break;

                case "PWM":
                    // PWM requires a pulse carrier
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:PULS {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:INT:FUNC {scpiModulatingWaveform}");
                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:INT:FREQ {modFrequency}");
                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:DEVIATION {modDepth}");
                    break;

                case "ASK":
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:ASK:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:ASK:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:ASK:RATE {modFrequency}");
                    // ASK amplitude is typically a factor (0-1) not percentage
                    double askAmplitude = modDepth / 100.0;
                    _device.SendCommand($"SOURCE{_activeChannel}:ASK:AMPLITUDE {askAmplitude}");
                    break;

                case "FSK":
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:FSK:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:FSK:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:FSK:RATE {modFrequency}");
                    // FSK uses hop frequency - calculate based on depth percentage
                    double hopFreq = carrierFrequency * (1 + modDepth / 100.0); // e.g., 10% depth = 1.1x carrier
                    _device.SendCommand($"SOURCE{_activeChannel}:FSK:FREQUENCY {hopFreq}");
                    break;

                case "PSK":
                    _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                    System.Threading.Thread.Sleep(100);

                    _device.SendCommand($"SOURCE{_activeChannel}:PSK:STATE ON");
                    _device.SendCommand($"SOURCE{_activeChannel}:PSK:SOURCE INT");
                    _device.SendCommand($"SOURCE{_activeChannel}:PSK:RATE {modFrequency}");
                    // PSK phase is in degrees - use depth directly as degrees
                    _device.SendCommand($"SOURCE{_activeChannel}:PSK:PHASE {modDepth}");
                    break;

                default:
                    Log($"Unknown modulation type: {modulationType}");
                    break;
            }
        }

        // ===== NEW HELPER METHODS FOR INTEGRATED UI =====

        /// <summary>
        /// Store current carrier settings
        /// </summary>
        private void StoreCarrierSettings()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                _storedCarrierFrequency = _device.GetFrequency(_activeChannel);
                _storedCarrierAmplitude = _device.GetAmplitude(_activeChannel);
                _storedCarrierOffset = _device.GetOffset(_activeChannel);
                _storedCarrierPhase = _device.GetPhase(_activeChannel);

                // Get waveform type from device
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

        /// <summary>
        /// Update modulation panel visibility
        /// </summary>
        private void UpdateModulationVisibility(bool visible)
        {
            if (_modulationPanel != null)
            {
                _modulationPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Update toggle button state
        /// </summary>
        private void UpdateToggleButtonState()
        {
            if (_modulationToggleButton != null)
            {
                _modulationToggleButton.Content = _isModulationEnabled ? "Disable Modulation" : "Enable Modulation";
                // Optional: Change button appearance
                if (_isModulationEnabled)
                {
                    _modulationToggleButton.Background = System.Windows.Media.Brushes.LightCoral;
                }
                else
                {
                    _modulationToggleButton.Background = System.Windows.Media.Brushes.LightGreen;
                }
            }
        }

        /// <summary>
        /// Initialize modulation types combo box
        /// </summary>
        private void InitializeModulationTypes()
        {
            if (_modulationTypeComboBox == null) return;

            _modulationTypeComboBox.Items.Clear();
            foreach (var modType in _modulationTypes.Keys)
            {
                _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = modType });
            }
            if (_modulationTypeComboBox.Items.Count > 0)
                _modulationTypeComboBox.SelectedIndex = 0;
        }

        /// <summary>
        /// Initialize frequency unit combo box
        /// </summary>
        private void InitializeFrequencyUnits()
        {
            if (_modulationFrequencyUnitComboBox == null) return;

            _modulationFrequencyUnitComboBox.Items.Clear();
            string[] units = { "µHz", "mHz", "Hz", "kHz", "MHz" };
            foreach (var unit in units)
            {
                _modulationFrequencyUnitComboBox.Items.Add(new ComboBoxItem { Content = unit });
            }
            _modulationFrequencyUnitComboBox.SelectedIndex = 3; // Default to kHz
        }

        /// <summary>
        /// Set default modulation values
        /// </summary>
        private void SetDefaultValues()
        {
            if (_modulationFrequencyTextBox != null)
                _modulationFrequencyTextBox.Text = "1.0"; // 1 kHz default

            if (_modulationDepthTextBox != null)
                _modulationDepthTextBox.Text = "50.0"; // 50% default
        }

        /// <summary>
        /// Update available modulating waveforms (preserved)
        /// </summary>
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

        /// <summary>
        /// Update frequency display (preserved)
        /// </summary>
        private void UpdateFrequencyDisplay(TextBox textBox, ComboBox unitComboBox, double frequencyHz)
        {
            if (textBox == null || unitComboBox == null) return;

            // Determine best unit
            string unit = "Hz";
            double displayValue = frequencyHz;

            if (frequencyHz >= 1e6)
            {
                unit = "MHz";
                displayValue = frequencyHz / 1e6;
            }
            else if (frequencyHz >= 1e3)
            {
                unit = "kHz";
                displayValue = frequencyHz / 1e3;
            }
            else if (frequencyHz < 1e-3)
            {
                unit = "mHz";
                displayValue = frequencyHz * 1e3;
            }
            else if (frequencyHz < 1e-6)
            {
                unit = "µHz";
                displayValue = frequencyHz * 1e6;
            }

            textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

            // Update unit combo box
            for (int i = 0; i < unitComboBox.Items.Count; i++)
            {
                var item = unitComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == unit)
                {
                    unitComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// Check if device is connected
        /// </summary>
        private bool IsDeviceConnected()
        {
            return _device != null && _device.IsConnected;
        }

        /// <summary>
        /// Log a message
        /// </summary>
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}