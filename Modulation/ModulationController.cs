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

        private CheckBox _dsscCheckBox;
        private DockPanel _dsscDockPanel;


        // Stored carrier settings when modulation is enabled
        private double _storedCarrierFrequency;
        private double _storedCarrierAmplitude;
        private double _storedCarrierOffset;
        private double _storedCarrierPhase;
        private string _storedCarrierWaveform;


        // ADD THE NEW FIELDS HERE - Stored modulation settings for disable/enable
        private double _lastModulationFrequency = 100; // Default 100 Hz
        private string _lastModulationFrequencyUnit = "kHz";
        private double _lastModulationDepth = 50.0;
        private string _lastModulationType = "AM";
        private string _lastModulatingWaveform = "Sine";
        private bool _lastDSSCEnabled = false;  // ADD THIS FOR DSSC STATE


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

        // Event for status changes
        public event EventHandler ModulationStatusChanged;

        public bool IsEnabled => _isModulationEnabled;

        /// <summary>
        /// Initialize UI controls - called after window is loaded
        /// </summary>
        public void InitializeUI()
        {
            // Find controls in the main window
            _modulationPanel = _mainWindow.FindName("ModulationPanel") as GroupBox;

            // ADD LOGGING to verify the panel is found
            if (_modulationPanel != null)
            {
                Log("ModulationPanel found successfully during initialization");
                Log($"Initial ModulationPanel visibility: {_modulationPanel.Visibility}");
            }
            else
            {
                Log("ERROR: ModulationPanel not found during initialization!");
            }

            _modulationToggleButton = _mainWindow.FindName("ModulationToggleButton") as Button;
            _modulationTypeComboBox = _mainWindow.FindName("ModulationTypeComboBox") as ComboBox;
            _modulatingWaveformComboBox = _mainWindow.FindName("ModulatingWaveformComboBox") as ComboBox;
            _modulationFrequencyTextBox = _mainWindow.FindName("ModulationFrequencyTextBox") as TextBox;
            _modulationFrequencyUnitComboBox = _mainWindow.FindName("ModulationFrequencyUnitComboBox") as ComboBox;
            _modulationDepthTextBox = _mainWindow.FindName("ModulationDepthTextBox") as TextBox;

            // ADD THESE TWO LINES FOR DSSC CONTROLS
            _dsscCheckBox = _mainWindow.FindName("DSSCCheckBox") as CheckBox;
            _dsscDockPanel = _mainWindow.FindName("DSSCDockPanel") as DockPanel;

            // Initialize combo boxes
            InitializeModulationTypes();
            InitializeFrequencyUnits();

            // Set default values
            SetDefaultValues();

            // Initially hide modulation controls
            UpdateModulationVisibility(false);
            Log("ModulationPanel initially hidden");
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
        // Also, ensure the combo box is populated when modulation is enabled:
        // If you want to restore the saved values when re-enabling:
        // Update EnableModulation to clearly separate carrier and modulation:
            public void EnableModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Store current carrier settings FROM UI
                StoreCarrierSettings();

                // Update UI
                _isModulationEnabled = true;
                UpdateModulationVisibility(true);
                UpdateToggleButtonState();

                // Restore saved modulation settings
                if (_modulationFrequencyTextBox != null && _lastModulationFrequency > 0)
                {
                    _modulationFrequencyTextBox.Text = _lastModulationFrequency.ToString();

                    // Restore the unit
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

                // Restore DSSC state if available
                if (_dsscCheckBox != null)
                {
                    _dsscCheckBox.IsChecked = _lastDSSCEnabled;
                }

                // Ensure modulating waveforms are populated
                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    string currentModType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                    UpdateAvailableModulatingWaveforms(currentModType);

                    // Update DSSC visibility based on modulation type
                    if (_dsscDockPanel != null)
                    {
                        _dsscDockPanel.Visibility = (currentModType == "AM") ? Visibility.Visible : Visibility.Collapsed;
                    }
                }

                Log($"Modulation enabled with frequency: {_lastModulationFrequency} {_lastModulationFrequencyUnit}");

                // Log DSSC state if AM modulation
                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    string modType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                    if (modType == "AM")
                    {
                        Log($"DSSC mode: {(_lastDSSCEnabled ? "Enabled" : "Disabled")}");
                    }
                }

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
      
        
        // Update the carrier info display to make separation clear:
        private void UpdateCarrierInfoDisplay()
        {
            if (_mainWindow.FindName("CarrierInfoTextBlock") is TextBlock infoBlock)
            {
                infoBlock.Text = $"Carrier: {_storedCarrierWaveform} @ {_storedCarrierFrequency / 1e6:F3} MHz, {_storedCarrierAmplitude} Vpp\n" +
                                "This is the main signal being modulated by the settings above.";
            }
        }

        /// <summary>
        /// Disable modulation mode
        /// </summary>
        // Update DisableModulation to save the modulation settings:
        public void DisableModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Save current modulation settings before disabling
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

                // Save DSSC state
                if (_dsscCheckBox != null)
                {
                    _lastDSSCEnabled = _dsscCheckBox.IsChecked ?? false;
                }

                Log($"Saving modulation settings: {_lastModulationFrequency} {_lastModulationFrequencyUnit}, {_lastModulationDepth}%, Type: {_lastModulationType}");

                // Log DSSC state if it was AM modulation
                if (_lastModulationType == "AM")
                {
                    Log($"Saving DSSC state: {(_lastDSSCEnabled ? "Enabled" : "Disabled")}");
                }

                // Turn off all modulation types
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
        // 5. Update OnModulationTypeChanged to show/hide DSSC for AM only
        public void OnModulationTypeChanged()
        {
            if (_modulationTypeComboBox?.SelectedItem == null) return;

            string modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();

            // Update available modulating waveforms
            UpdateAvailableModulatingWaveforms(modulationType);

            // Show/hide DSSC control based on modulation type
            if (_dsscDockPanel != null)
            {
                _dsscDockPanel.Visibility = (modulationType == "AM") ? Visibility.Visible : Visibility.Collapsed;
            }

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
        /// Apply specific modulation type to the device (UPDATED WITH FIXES)
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
                case "SINE":
                case "SIN":
                    scpiCarrierWaveform = "SIN";
                    break;
                case "SQUARE":
                case "SQU":
                    scpiCarrierWaveform = "SQU";
                    break;
                case "RAMP":
                    scpiCarrierWaveform = "RAMP";
                    break;
                case "PULSE":
                case "PULS":
                    scpiCarrierWaveform = "PULS";
                    break;
            }

            // Map modulating waveform to SCPI command
            string scpiModulatingWaveform = MapModulatingWaveformToScpi(modulatingWaveform);

            // Log what we're about to apply
            Log($"Applying {modulationType} with carrier {scpiCarrierWaveform} @ {carrierFrequency}Hz");

            // Send SCPI commands based on modulation type
            switch (modulationType)
            {
                case "AM":
                    // Check if we're already in AM mode to avoid resetting carrier
                    string amState = _device.SendQuery($"SOURCE{_activeChannel}:AM:STATE?");
                    bool isAMActive = (amState.Trim() == "ON" || amState.Trim() == "1");

                    if (!isAMActive)
                    {
                        // Only set carrier if AM is not already active
                        Log($"AM not active, setting carrier: {scpiCarrierWaveform} @ {carrierFrequency}Hz, {carrierAmplitude}Vpp");
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:AM:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:AM:SOURCE INT");
                        _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FUNC {scpiModulatingWaveform}");
                    }
                    else
                    {
                        Log("AM already active, only updating modulation parameters");
                    }

                    // Always update these parameters
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:DEPTH {modDepth}");
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:INT:FREQ {modFrequency}");

                    // Use checkbox state for DSSC
                    bool dsscEnabled = _dsscCheckBox?.IsChecked ?? false;
                    _device.SendCommand($"SOURCE{_activeChannel}:AM:DSSC {(dsscEnabled ? "ON" : "OFF")}");

                    Log($"AM DSSC mode: {(dsscEnabled ? "Enabled" : "Disabled")}");
                    break;

                case "FM":
                    // Check if already in FM mode
                    string fmState = _device.SendQuery($"SOURCE{_activeChannel}:FM:STATE?");
                    bool isFMActive = (fmState.Trim() == "ON" || fmState.Trim() == "1");

                    if (!isFMActive)
                    {
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:FM:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:FM:SOURCE INT");
                        _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FUNC {scpiModulatingWaveform}");
                    }

                    _device.SendCommand($"SOURCE{_activeChannel}:FM:INT:FREQ {modFrequency}");
                    double deviation = carrierFrequency * (modDepth / 100.0);
                    _device.SendCommand($"SOURCE{_activeChannel}:FM:DEVIATION {deviation}");
                    break;

                case "PM":
                    // Check if already in PM mode
                    string pmState = _device.SendQuery($"SOURCE{_activeChannel}:PM:STATE?");
                    bool isPMActive = (pmState.Trim() == "ON" || pmState.Trim() == "1");

                    if (!isPMActive)
                    {
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:PM:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:PM:SOURCE INT");
                        _device.SendCommand($"SOURCE{_activeChannel}:PM:INT:FUNC {scpiModulatingWaveform}");
                    }

                    _device.SendCommand($"SOURCE{_activeChannel}:PM:INT:FREQ {modFrequency}");
                    double phaseDeviation = 180 * (modDepth / 100.0); // Convert to degrees
                    _device.SendCommand($"SOURCE{_activeChannel}:PM:DEVIATION {phaseDeviation}");
                    break;

                case "PWM":
                    // PWM requires a pulse carrier
                    string pwmState = _device.SendQuery($"SOURCE{_activeChannel}:PWM:STATE?");
                    bool isPWMActive = (pwmState.Trim() == "ON" || pwmState.Trim() == "1");

                    if (!isPWMActive)
                    {
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:PULS {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:PWM:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:PWM:SOURCE INT");
                        _device.SendCommand($"SOURCE{_activeChannel}:PWM:INT:FUNC {scpiModulatingWaveform}");
                    }

                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:INT:FREQ {modFrequency}");
                    _device.SendCommand($"SOURCE{_activeChannel}:PWM:DEVIATION {modDepth}");
                    break;

                case "ASK":
                    string askState = _device.SendQuery($"SOURCE{_activeChannel}:ASK:STATE?");
                    bool isASKActive = (askState.Trim() == "ON" || askState.Trim() == "1");

                    if (!isASKActive)
                    {
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:ASK:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:ASK:SOURCE INT");
                    }

                    _device.SendCommand($"SOURCE{_activeChannel}:ASK:RATE {modFrequency}");
                    // ASK amplitude is typically a factor (0-1) not percentage
                    double askAmplitude = modDepth / 100.0;
                    _device.SendCommand($"SOURCE{_activeChannel}:ASK:AMPLITUDE {askAmplitude}");
                    break;

                case "FSK":
                    string fskState = _device.SendQuery($"SOURCE{_activeChannel}:FSK:STATE?");
                    bool isFSKActive = (fskState.Trim() == "ON" || fskState.Trim() == "1");

                    if (!isFSKActive)
                    {
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:FSK:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:FSK:SOURCE INT");
                    }

                    _device.SendCommand($"SOURCE{_activeChannel}:FSK:RATE {modFrequency}");
                    // FSK uses hop frequency - calculate based on depth percentage
                    double hopFreq = carrierFrequency * (1 + modDepth / 100.0); // e.g., 10% depth = 1.1x carrier
                    _device.SendCommand($"SOURCE{_activeChannel}:FSK:FREQUENCY {hopFreq}");
                    break;

                case "PSK":
                    string pskState = _device.SendQuery($"SOURCE{_activeChannel}:PSK:STATE?");
                    bool isPSKActive = (pskState.Trim() == "ON" || pskState.Trim() == "1");

                    if (!isPSKActive)
                    {
                        _device.SendCommand($"SOURCE{_activeChannel}:APPLY:{scpiCarrierWaveform} {carrierFrequency},{carrierAmplitude},{carrierOffset},{carrierPhase}");
                        System.Threading.Thread.Sleep(100);

                        _device.SendCommand($"SOURCE{_activeChannel}:PSK:STATE ON");
                        _device.SendCommand($"SOURCE{_activeChannel}:PSK:SOURCE INT");
                    }

                    _device.SendCommand($"SOURCE{_activeChannel}:PSK:RATE {modFrequency}");
                    // PSK phase is in degrees - use depth directly as degrees
                    _device.SendCommand($"SOURCE{_activeChannel}:PSK:PHASE {modDepth}");
                    break;

                default:
                    Log($"Unknown modulation type: {modulationType}");
                    break;
            }
        }
       
        public void OnDSSCChanged(bool isEnabled)
        {
            if (_isModulationEnabled)
            {
                // Apply the DSSC change
                ApplyModulation();
            }
        }


        /// <summary>
        /// Store current carrier settings
        /// </summary>
        private void StoreCarrierSettings()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // PROBLEM: After modulation is applied, GetFrequency might return the modulated frequency
                // SOLUTION: Get the carrier settings BEFORE any modulation is active, or from the UI

                // Option 1: Get from UI controls instead of device
                _storedCarrierFrequency = GetCarrierFrequencyFromUI();
                _storedCarrierAmplitude = GetCarrierAmplitudeFromUI();
                _storedCarrierOffset = GetCarrierOffsetFromUI();
                _storedCarrierPhase = GetCarrierPhaseFromUI();

                // Get waveform type from device (this is usually reliable)
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

        /// <sumary>
        /// SetDefaultValues to match instrument defaults:
        /// </summary>
        private void SetDefaultValues()
        {
            // Set UI defaults to match typical instrument defaults
            if (_modulationFrequencyTextBox != null)
                _modulationFrequencyTextBox.Text = "100"; // 100 Hz default 

            if (_modulationFrequencyUnitComboBox != null)
            {
                // Set to Hz
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
                _modulationDepthTextBox.Text = "100.0"; // 100% default 

            // Ensure modulating waveforms are populated for the current modulation type
            if (_modulationTypeComboBox?.SelectedItem != null)
            {
                string currentModType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                UpdateAvailableModulatingWaveforms(currentModType);
            }
        }


        /// <summary>
        /// Method to read modulation settings from instrument:
        /// </summary>  
        private void ReadModulationSettingsFromDevice()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                Log("Reading modulation settings from device...");

                // Check which modulation type is active
                string activeModType = "";
                string[] modTypes = { "AM", "FM", "PM", "PWM", "ASK", "FSK", "PSK" };

                foreach (var modType in modTypes)
                {
                    string response = _device.SendQuery($"SOURCE{_activeChannel}:{modType}:STATE?");
                    if (response.Trim() == "ON" || response.Trim() == "1")
                    {
                        activeModType = modType;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(activeModType))
                {
                    Log("No modulation currently active on device, using defaults");
                    return;
                }

                Log($"Device has {activeModType} modulation active");

                // Update modulation type in UI
                for (int i = 0; i < _modulationTypeComboBox.Items.Count; i++)
                {
                    var item = _modulationTypeComboBox.Items[i] as ComboBoxItem;
                    if (item?.Content.ToString() == activeModType)
                    {
                        _modulationTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }

                // Read modulation-specific parameters
                switch (activeModType)
                {
                    case "AM":
                        // Get AM frequency
                        string amFreqStr = _device.SendQuery($"SOURCE{_activeChannel}:AM:INT:FREQ?");
                        if (double.TryParse(amFreqStr, out double amFreq))
                        {
                            UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, amFreq);
                            Log($"AM modulation frequency: {amFreq} Hz");
                        }

                        // Get AM depth
                        string amDepthStr = _device.SendQuery($"SOURCE{_activeChannel}:AM:DEPTH?");
                        if (double.TryParse(amDepthStr, out double amDepth))
                        {
                            _modulationDepthTextBox.Text = amDepth.ToString("F1");
                            Log($"AM modulation depth: {amDepth}%");
                        }

                        // Get AM function
                        string amFunc = _device.SendQuery($"SOURCE{_activeChannel}:AM:INT:FUNC?").Trim();
                        UpdateModulatingWaveformFromDevice(amFunc);
                        break;

                    case "FM":
                        // Similar for FM...
                        string fmFreqStr = _device.SendQuery($"SOURCE{_activeChannel}:FM:INT:FREQ?");
                        if (double.TryParse(fmFreqStr, out double fmFreq))
                        {
                            UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, fmFreq);
                        }

                        // FM uses deviation, need to convert to percentage
                        string fmDevStr = _device.SendQuery($"SOURCE{_activeChannel}:FM:DEVIATION?");
                        if (double.TryParse(fmDevStr, out double fmDev))
                        {
                            // Convert deviation to depth percentage based on carrier frequency
                            double carrierFreq = _storedCarrierFrequency;
                            double depth = (fmDev / carrierFreq) * 100.0;
                            _modulationDepthTextBox.Text = depth.ToString("F1");
                        }
                        break;

                        // Add other modulation types as needed...
                }
            }
            catch (Exception ex)
            {
                Log($"Error reading modulation settings from device: {ex.Message}");
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
            {
                _modulationTypeComboBox.SelectedIndex = 0;

                // FIX: Also populate the modulating waveforms for the default selection
                string defaultModType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                UpdateAvailableModulatingWaveforms(defaultModType);
            }
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
        /// Update toggle button state
        /// </summary>
        /// <summary>
        /// Update toggle button state
        /// </summary>
        private void UpdateToggleButtonState()
        {
            if (_modulationToggleButton != null)
            {
                _modulationToggleButton.Content = _isModulationEnabled ? "Disable Modulation" : "Enable Modulation";
                if (_isModulationEnabled)
                {
                    _modulationToggleButton.Background = System.Windows.Media.Brushes.LightCoral;
                }
                else
                {
                    _modulationToggleButton.Background = System.Windows.Media.Brushes.LightGreen;
                }
            }

            // ONE LINE FIX: Find and show the panel whenever button is red
            var panel = _mainWindow?.FindName("ModulationPanel") as GroupBox;
            if (panel != null) panel.Visibility = _isModulationEnabled ? Visibility.Visible : Visibility.Collapsed;
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

        /// <summary>
        /// Sync modulation state from device when modulation is detected
        /// </summary>
        // In ModulationController.cs, update the SyncModulationFromDevice method:

        public void SyncModulationFromDevice(string detectedModType)
        {
            if (!IsDeviceConnected()) return;

            try
            {
                Log($"Syncing {detectedModType} modulation state from device");

                // First, update the modulation type in UI
                for (int i = 0; i < _modulationTypeComboBox.Items.Count; i++)
                {
                    var item = _modulationTypeComboBox.Items[i] as ComboBoxItem;
                    if (item?.Content.ToString() == detectedModType)
                    {
                        _modulationTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }

                // Update available modulating waveforms for this type
                UpdateAvailableModulatingWaveforms(detectedModType);

                // Show DSSC panel if AM
                if (_dsscDockPanel != null)
                {
                    _dsscDockPanel.Visibility = (detectedModType == "AM") ? Visibility.Visible : Visibility.Collapsed;
                }

                // Refresh the specific modulation settings
                switch (detectedModType)
                {
                    case "AM":
                        RefreshAMSettings();
                        RefreshDSSCState();
                        break;
                    case "FM":
                        RefreshFMSettings();
                        break;
                    case "PM":
                        RefreshPMSettings();
                        break;
                    case "PWM":
                        RefreshPWMSettings();
                        break;
                    case "ASK":
                        RefreshASKSettings();
                        break;
                    case "FSK":
                        RefreshFSKSettings();
                        break;
                    case "PSK":
                        RefreshPSKSettings();
                        break;
                }

                // Store current carrier settings from device
                StoreCarrierSettings();

                // Update UI state to show modulation is enabled
                _isModulationEnabled = true;

                // IMPORTANT: Make sure the panel is visible
                UpdateModulationVisibility(true);
                Log($"Setting modulation panel visibility to: Visible");

                // Also try setting it directly as a fallback
                if (_modulationPanel != null)
                {
                    _modulationPanel.Dispatcher.Invoke(() =>
                    {
                        _modulationPanel.Visibility = Visibility.Visible;
                        Log($"Modulation panel visibility is now: {_modulationPanel.Visibility}");
                    });
                }
                else
                {
                    Log("WARNING: _modulationPanel is null!");
                }

                UpdateToggleButtonState();

                // Notify MainWindow to update status display
                if (_mainWindow is MainWindow mainWindow)
                {
                    mainWindow.Dispatcher.Invoke(() =>
                    {
                        // Find and call the UpdateModulationStatus method
                        var updateMethod = mainWindow.GetType().GetMethod("UpdateModulationStatus",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        updateMethod?.Invoke(mainWindow, null);
                    });
                }

                Log($"{detectedModType} modulation synced from device");
            }
            catch (Exception ex)
            {
                Log($"Error syncing modulation from device: {ex.Message}");
            }
        }
        /// <summary>
        /// Force disable modulation UI when device has no modulation
        /// </summary>
        public void ForceDisableUI()
        {
            _isModulationEnabled = false;
            UpdateModulationVisibility(false);
            UpdateToggleButtonState();
            Log("Modulation UI disabled to match device state");
        }

        /// <summary>
        /// Refresh modulation settings when already known to be active
        /// </summary>
        public void RefreshModulationSettings()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Check which modulation is active
                string[] modTypes = { "AM", "FM", "PM", "PWM", "ASK", "FSK", "PSK" };

                foreach (var modType in modTypes)
                {
                    string response = _device.SendQuery($"SOURCE{_activeChannel}:{modType}:STATE?");
                    if (response.Trim() == "ON" || response.Trim() == "1")
                    {
                        // Refresh settings for this modulation type
                        switch (modType)
                        {
                            case "AM":
                                RefreshAMSettings();
                                break;
                            case "FM":
                                RefreshFMSettings();
                                break;
                            case "PM":
                                RefreshPMSettings();
                                break;
                            case "PWM":
                                RefreshPWMSettings();
                                break;
                            case "ASK":
                                RefreshASKSettings();
                                break;
                            case "FSK":
                                RefreshFSKSettings();
                                break;
                            case "PSK":
                                RefreshPSKSettings();
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing modulation settings: {ex.Message}");
            }
        }

        // ===== NEW HELPER METHODS FOR INTEGRATED UI =====
        // Add these helper methods to get values from UI:

        #region Modulation UI Helpers
        private double GetCarrierFrequencyFromUI()
        {
            try
            {
                // Find the frequency textbox and unit combo in the main window
                var freqTextBox = _mainWindow.FindName("ChannelFrequencyTextBox") as TextBox;
                var unitComboBox = _mainWindow.FindName("ChannelFrequencyUnitComboBox") as ComboBox;

                if (freqTextBox != null && unitComboBox != null &&
                    double.TryParse(freqTextBox.Text, out double frequency))
                {
                    string unit = ((ComboBoxItem)unitComboBox.SelectedItem)?.Content.ToString() ?? "Hz";
                    double multiplier = UnitConversionUtility.GetFrequencyMultiplier(unit);

                    Log($"Getting carrier frequency from UI: {frequency} {unit} = {frequency * multiplier} Hz");
                    return frequency * multiplier;
                }
            }
            catch (Exception ex)
            {
                Log($"Error getting frequency from UI: {ex.Message}");
            }

            // Fallback to device if UI fails
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


        // <summary>
        /// Gets the currently selected modulation type
        /// </summary>
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

        /// <summary>
        /// Gets the current DSSC state (for AM modulation)
        /// </summary>
        public bool IsDSSCEnabled
        {
            get
            {
                return _dsscCheckBox?.IsChecked ?? false;
            }
        }

        

        /// <summary>
        /// Helper to update modulating waveform selection from device response
        /// </summary>
        /// <param name="deviceWaveform"></param>

        private void UpdateModulatingWaveformFromDevice(string deviceWaveform)
        {
            // Map device response to UI strings
            string uiWaveform = "";
            switch (deviceWaveform.ToUpper())
            {
                case "SIN": uiWaveform = "Sine"; break;
                case "SQU": uiWaveform = "Square"; break;
                case "TRI": uiWaveform = "Triangle"; break;
                case "RAMP": uiWaveform = "Up Ramp"; break;
                case "NRAM": uiWaveform = "Down Ramp"; break;
                case "NOIS": uiWaveform = "Noise"; break;
                case "ARB": uiWaveform = "Arbitrary Waveform"; break;
            }

            // Find and select in combo box
            for (int i = 0; i < _modulatingWaveformComboBox.Items.Count; i++)
            {
                var item = _modulatingWaveformComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == uiWaveform)
                {
                    _modulatingWaveformComboBox.SelectedIndex = i;
                    Log($"Set modulating waveform to: {uiWaveform}");
                    break;
                }
            }
        }


        #endregion

        #region refresh

        // Add ALL these refresh methods to ModulationController.cs

        /// <summary>
        /// Refresh AM settings from device
        /// </summary>
        private void RefreshAMSettings()
        {
            try
            {
                // Get AM frequency
                string freqResponse = _device.SendQuery($"SOURCE{_activeChannel}:AM:INT:FREQ?");
                if (double.TryParse(freqResponse, out double freq))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, freq);
                }

                // Get AM depth
                string depthResponse = _device.SendQuery($"SOURCE{_activeChannel}:AM:DEPTH?");
                if (double.TryParse(depthResponse, out double depth))
                {
                    if (_modulationDepthTextBox != null)
                        _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
                }

                // Get AM function
                string funcResponse = _device.SendQuery($"SOURCE{_activeChannel}:AM:INT:FUNC?");
                UpdateModulatingWaveformSelection(funcResponse.Trim());

                // ADDED: Get DSSC state for AM
                string dsscResponse = _device.SendQuery($"SOURCE{_activeChannel}:AM:DSSC?");
                if (_dsscCheckBox != null)
                {
                    _dsscCheckBox.IsChecked = (dsscResponse.Trim() == "ON" || dsscResponse.Trim() == "1");
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing AM settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh FM settings from device
        /// </summary>
        private void RefreshFMSettings()
        {
            try
            {
                // Get FM frequency
                string freqResponse = _device.SendQuery($"SOURCE{_activeChannel}:FM:INT:FREQ?");
                if (double.TryParse(freqResponse, out double freq))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, freq);
                }

                // Get FM deviation
                string devResponse = _device.SendQuery($"SOURCE{_activeChannel}:FM:DEVIATION?");
                if (double.TryParse(devResponse, out double deviation))
                {
                    // Convert deviation back to depth percentage
                    double carrierFreq = _device.GetFrequency(_activeChannel);
                    double depth = (deviation / carrierFreq) * 100.0;
                    if (_modulationDepthTextBox != null)
                        _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
                }

                // Get FM function
                string funcResponse = _device.SendQuery($"SOURCE{_activeChannel}:FM:INT:FUNC?");
                UpdateModulatingWaveformSelection(funcResponse.Trim());
            }
            catch (Exception ex)
            {
                Log($"Error refreshing FM settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh PM settings from device
        /// </summary>
        private void RefreshPMSettings()
        {
            try
            {
                // Get PM frequency
                string freqResponse = _device.SendQuery($"SOURCE{_activeChannel}:PM:INT:FREQ?");
                if (double.TryParse(freqResponse, out double freq))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, freq);
                }

                // Get PM deviation
                string devResponse = _device.SendQuery($"SOURCE{_activeChannel}:PM:DEVIATION?");
                if (double.TryParse(devResponse, out double deviation))
                {
                    // Convert phase deviation to depth percentage
                    double depth = (deviation / 180.0) * 100.0;
                    if (_modulationDepthTextBox != null)
                        _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
                }

                // Get PM function
                string funcResponse = _device.SendQuery($"SOURCE{_activeChannel}:PM:INT:FUNC?");
                UpdateModulatingWaveformSelection(funcResponse.Trim());
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PM settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh PWM settings from device
        /// </summary>
        private void RefreshPWMSettings()
        {
            try
            {
                // Get PWM frequency
                string freqResponse = _device.SendQuery($"SOURCE{_activeChannel}:PWM:INT:FREQ?");
                if (double.TryParse(freqResponse, out double freq))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, freq);
                }

                // Get PWM deviation
                string devResponse = _device.SendQuery($"SOURCE{_activeChannel}:PWM:DEVIATION?");
                if (double.TryParse(devResponse, out double deviation))
                {
                    if (_modulationDepthTextBox != null)
                        _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(deviation);
                }

                // Get PWM function
                string funcResponse = _device.SendQuery($"SOURCE{_activeChannel}:PWM:INT:FUNC?");
                UpdateModulatingWaveformSelection(funcResponse.Trim());
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PWM settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh ASK settings from device
        /// </summary>
        private void RefreshASKSettings()
        {
            try
            {
                // Get ASK rate
                string rateResponse = _device.SendQuery($"SOURCE{_activeChannel}:ASK:RATE?");
                if (double.TryParse(rateResponse, out double rate))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, rate);
                }

                // Get ASK amplitude
                string ampResponse = _device.SendQuery($"SOURCE{_activeChannel}:ASK:AMPLITUDE?");
                if (double.TryParse(ampResponse, out double amplitude))
                {
                    // Convert to percentage
                    double depth = amplitude * 100.0;
                    if (_modulationDepthTextBox != null)
                        _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
                }

                // ASK only uses square wave
                UpdateModulatingWaveformSelection("Square");
            }
            catch (Exception ex)
            {
                Log($"Error refreshing ASK settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh FSK settings from device
        /// </summary>
        private void RefreshFSKSettings()
        {
            try
            {
                // Get FSK rate
                string rateResponse = _device.SendQuery($"SOURCE{_activeChannel}:FSK:RATE?");
                if (double.TryParse(rateResponse, out double rate))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, rate);
                }

                // Get FSK hop frequency
                string hopResponse = _device.SendQuery($"SOURCE{_activeChannel}:FSK:FREQUENCY?");
                if (double.TryParse(hopResponse, out double hopFreq))
                {
                    // Convert to depth percentage
                    double carrierFreq = _storedCarrierFrequency;
                    if (carrierFreq > 0)
                    {
                        double depth = ((hopFreq / carrierFreq) - 1) * 100.0;
                        if (_modulationDepthTextBox != null)
                            _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
                    }
                }

                // FSK only uses square wave
                UpdateModulatingWaveformSelection("Square");
            }
            catch (Exception ex)
            {
                Log($"Error refreshing FSK settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh PSK settings from device
        /// </summary>
        private void RefreshPSKSettings()
        {
            try
            {
                // Get PSK rate
                string rateResponse = _device.SendQuery($"SOURCE{_activeChannel}:PSK:RATE?");
                if (double.TryParse(rateResponse, out double rate))
                {
                    UpdateFrequencyDisplay(_modulationFrequencyTextBox, _modulationFrequencyUnitComboBox, rate);
                }

                // Get PSK phase
                string phaseResponse = _device.SendQuery($"SOURCE{_activeChannel}:PSK:PHASE?");
                if (double.TryParse(phaseResponse, out double phase))
                {
                    // Phase is already in degrees, use as depth
                    if (_modulationDepthTextBox != null)
                        _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(phase);
                }

                // PSK only uses square wave
                UpdateModulatingWaveformSelection("Square");
            }
            catch (Exception ex)
            {
                Log($"Error refreshing PSK settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh DSSC state from device
        /// </summary>
        public void RefreshDSSCState()
        {
            if (!IsDeviceConnected() || _dsscCheckBox == null) return;

            try
            {
                // Only query if AM is active
                string amState = _device.SendQuery($"SOURCE{_activeChannel}:AM:STATE?");
                if (amState.Trim() == "ON" || amState.Trim() == "1")
                {
                    string dsscState = _device.SendQuery($"SOURCE{_activeChannel}:AM:DSSC?");
                    bool isDSSCOn = (dsscState.Trim() == "ON" || dsscState.Trim() == "1");
                    _dsscCheckBox.IsChecked = isDSSCOn;
                    Log($"DSSC state from device: {(isDSSCOn ? "Enabled" : "Disabled")}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing DSSC state: {ex.Message}");
            }
        }

        /// <summary>
        /// Update modulating waveform selection helper
        /// </summary>
        private void UpdateModulatingWaveformSelection(string deviceWaveform)
        {
            if (_modulatingWaveformComboBox == null) return;

            // Map device waveform to UI string
            string uiWaveform = "";
            switch (deviceWaveform.ToUpper())
            {
                case "SIN": uiWaveform = "Sine"; break;
                case "SQU": uiWaveform = "Square"; break;
                case "TRI": uiWaveform = "Triangle"; break;
                case "RAMP": uiWaveform = "Up Ramp"; break;
                case "NRAM": uiWaveform = "Down Ramp"; break;
                case "NOIS": uiWaveform = "Noise"; break;
                case "ARB": uiWaveform = "Arbitrary Waveform"; break;
                case "SQUARE": uiWaveform = "Square"; break; // For keying modes
            }

            // Find and select in combo box
            for (int i = 0; i < _modulatingWaveformComboBox.Items.Count; i++)
            {
                var item = _modulatingWaveformComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == uiWaveform)
                {
                    _modulatingWaveformComboBox.SelectedIndex = i;
                    break;
                }
            }
        }




        #endregion


    }
}