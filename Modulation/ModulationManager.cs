using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Modulation
{
    /// <summary>
    /// Main controller for modulation functionality
    /// </summary>
    public class ModulationManager
    {
        private readonly RigolDG2072 _device;
        private readonly Window _mainWindow;
        private int _activeChannel;

        // Event for logging
        public event EventHandler<string> LogEvent;

        // UI Controls references
        private ComboBox   _carrierWaveformComboBox;
        private TextBox   _carrierFrequencyTextBox;
        private ComboBox  _carrierFrequencyUnitComboBox;
        private TextBox   _carrierAmplitudeTextBox;
        private ComboBox  _carrierAmplitudeUnitComboBox;
        private TextBox   _modulationFrequencyTextBox;
        private ComboBox  _modulationFrequencyUnitComboBox;
        private ComboBox  _modulationTypeComboBox;
        private ComboBox  _modulatingWaveformComboBox;
        private TextBox   _modulationDepthTextBox;


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

        public ModulationManager(RigolDG2072 device, int channel, Window mainWindow)
        {
            _device = device;
            _activeChannel = channel;
            _mainWindow = mainWindow;

            //InitializeControls();
        }

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public void InitializeUI()
        {
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Find controls in the main window
            _carrierWaveformComboBox =       _mainWindow.FindName("CarrierWaveformComboBox") as ComboBox;
            _carrierFrequencyTextBox =       _mainWindow.FindName("CarrierFrequencyTextBox") as TextBox;
            _carrierFrequencyUnitComboBox =  _mainWindow.FindName("CarrierFrequencyUnitComboBox") as ComboBox;
            _carrierAmplitudeTextBox =       _mainWindow.FindName("CarrierAmplitudeTextBox") as TextBox;
            _carrierAmplitudeUnitComboBox =  _mainWindow.FindName("CarrierAmplitudeUnitComboBox") as ComboBox;
            _modulatingWaveformComboBox =    _mainWindow.FindName("ModulatingWaveformComboBox") as ComboBox;
            _modulationTypeComboBox =        _mainWindow.FindName("ModulationTypeComboBox") as ComboBox;
            _modulationFrequencyTextBox =    _mainWindow.FindName("ModulationFrequencyTextBox") as TextBox;
            _modulationFrequencyUnitComboBox = _mainWindow.FindName("ModulationFrequencyUnitComboBox") as ComboBox;
            _modulationDepthTextBox =        _mainWindow.FindName("ModulationDepthTextBox") as TextBox;


            // Initialize modulation type combo box
            if (_modulationTypeComboBox != null)
            {
                _modulationTypeComboBox.Items.Clear();
                foreach (var modType in _modulationTypes.Keys)
                {
                    _modulationTypeComboBox.Items.Add(new ComboBoxItem { Content = modType });
                }
                if (_modulationTypeComboBox.Items.Count > 0)
                    _modulationTypeComboBox.SelectedIndex = 0;
            }

            // Initialize frequency unit combo boxes
            InitializeFrequencyUnitComboBox(_carrierFrequencyUnitComboBox, 3);
            InitializeFrequencyUnitComboBox(_modulationFrequencyUnitComboBox, 2);

            // Set default values
            if (_carrierFrequencyTextBox != null)
                _carrierFrequencyTextBox.Text = "100";  // Default 100 kHz for carrier

            // Set default carrier amplitude
            if (_carrierAmplitudeTextBox != null)
                _carrierAmplitudeTextBox.Text = "3.0";  // Default 3 Vpp

            if (_modulationFrequencyTextBox != null)
                _modulationFrequencyTextBox.Text = "500";  // Default 500 Hz for modulating

            if (_modulationDepthTextBox != null)
                _modulationDepthTextBox.Text = "25.0";  // Default 25% depth
        }

        private void InitializeFrequencyUnitComboBox(ComboBox comboBox, int defaultIndex)
        {
            if (comboBox == null) return;

            comboBox.Items.Clear();
            string[] units = { "µHz", "mHz", "Hz", "kHz", "MHz" };
            foreach (var unit in units)
            {
                comboBox.Items.Add(new ComboBoxItem { Content = unit });
            }
            comboBox.SelectedIndex = defaultIndex;
        }

        /// <summary>
        /// Called when carrier waveform selection changes
        /// </summary>
        public void OnCarrierWaveformChanged()
        {
            if (_carrierWaveformComboBox?.SelectedItem == null) return;

            string carrierWaveform = ((ComboBoxItem)_carrierWaveformComboBox.SelectedItem).Content.ToString().ToUpper();

            // Update modulating waveform based on carrier
            UpdateModulatingWaveformFromCarrier(carrierWaveform);

            // REMOVE OR COMMENT OUT THIS LINE:
            // CopyCarrierFrequencyToModulating();  // Don't copy carrier freq to modulating!

            // Calculate and set modulating amplitude
            UpdateModulatingAmplitude();

            Log($"Carrier waveform changed to: {carrierWaveform}");
        }

        /// <summary>
        /// Called when carrier amplitude text changes
        /// </summary>
        public void OnCarrierAmplitudeTextChanged()
        {
            // Update modulating amplitude if needed based on new carrier amplitude
            UpdateModulatingAmplitude();
            Log("Carrier amplitude changed");
        }

        /// <summary>
        /// Called when carrier amplitude unit changes
        /// </summary>
        public void OnCarrierAmplitudeUnitChanged()
        {
            Log("Carrier amplitude unit changed");
        }

        /// <summary>
        /// Called when modulation type changes
        /// </summary>
        public void OnModulationTypeChanged()
        {
            if (!IsDeviceConnected())
            {
                Log("Skipping modulation change - device not connected");
                return;
            }

            if (_modulationTypeComboBox?.SelectedItem == null) return;

            string modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();

            // Update available modulating waveforms based on modulation type
            UpdateAvailableModulatingWaveforms(modulationType);

            Log($"Modulation type changed to: {modulationType}");
        }

        /// <summary>
        /// Update modulating waveform based on carrier waveform
        /// </summary>
        private void UpdateModulatingWaveformFromCarrier(string carrierWaveform)
        {
            if (_modulatingWaveformComboBox == null) return;

            // Check if carrier waveform is one of the supported types
            string modulatingWaveform = "SINE"; // Default

            if (carrierWaveform == "SINE" || carrierWaveform == "SQUARE" ||
                carrierWaveform == "RAMP" || carrierWaveform == "ARBITRARY")
            {
                modulatingWaveform = carrierWaveform;
            }

            // Update the modulating waveform combo box
            _modulatingWaveformComboBox.Items.Clear();

            // Get current modulation type
            string modulationType = "AM"; // Default
            if (_modulationTypeComboBox?.SelectedItem != null)
            {
                modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
            }

            // Add available waveforms for this modulation type
            if (_modulatingWaveforms.ContainsKey(modulationType))
            {
                foreach (var waveform in _modulatingWaveforms[modulationType])
                {
                    var item = new ComboBoxItem { Content = waveform };
                    _modulatingWaveformComboBox.Items.Add(item);

                    // Select the matching waveform
                    if (waveform == modulatingWaveform ||
                        (modulatingWaveform == "RAMP" && (waveform == "TRIANGLE" || waveform == "Up Ramp")))
                    {
                        _modulatingWaveformComboBox.SelectedItem = item;
                    }
                }
            }

            // If nothing selected, select first item
            if (_modulatingWaveformComboBox.SelectedItem == null && _modulatingWaveformComboBox.Items.Count > 0)
            {
                _modulatingWaveformComboBox.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Update available modulating waveforms based on modulation type
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
        /// Copy carrier frequency to modulating frequency
        /// </summary>
        private void CopyCarrierFrequencyToModulating()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Get carrier frequency from device
                double carrierFrequency = _device.GetFrequency(_activeChannel);

                // Update modulating frequency textbox
                if (_modulationFrequencyTextBox != null)
                {
                    // Find appropriate unit
                    string unit = "Hz";
                    double displayValue = carrierFrequency;

                    if (carrierFrequency >= 1e6)
                    {
                        unit = "MHz";
                        displayValue = carrierFrequency / 1e6;
                    }
                    else if (carrierFrequency >= 1e3)
                    {
                        unit = "kHz";
                        displayValue = carrierFrequency / 1e3;
                    }
                    else if (carrierFrequency < 1e-3)
                    {
                        unit = "mHz";
                        displayValue = carrierFrequency * 1e3;
                    }

                    _modulationFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

                    // Update unit combo box
                    if (_modulationFrequencyUnitComboBox != null)
                    {
                        for (int i = 0; i < _modulationFrequencyUnitComboBox.Items.Count; i++)
                        {
                            var item = _modulationFrequencyUnitComboBox.Items[i] as ComboBoxItem;
                            if (item?.Content.ToString() == unit)
                            {
                                _modulationFrequencyUnitComboBox.SelectedIndex = i;
                                break;
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
        /// Calculate and update modulating amplitude based on carrier amplitude
        /// </summary>
        private void UpdateModulatingAmplitude()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Get carrier amplitude from device
                double carrierAmplitude = _device.GetAmplitude(_activeChannel);

                // Calculate modulating amplitude using formula
                // This is a placeholder - replace with your actual formula
                double modulatingAmplitude = CalculateModulatingAmplitude(carrierAmplitude);

                // For AM modulation, this would typically be the modulation depth
                if (_modulationDepthTextBox != null)
                {
                    _modulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(modulatingAmplitude);
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating modulating amplitude: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculate modulating amplitude from carrier amplitude
        /// </summary>
        private double CalculateModulatingAmplitude(double carrierAmplitude)
        {
            // Placeholder formula - replace with your actual calculation
            // For AM, this might be a percentage (e.g., 50% modulation depth)
            return 50.0; // Default 50% modulation depth
        }

        /// <summary>
        /// Apply modulation settings to the device
        /// </summary>
        public void ApplyModulation()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Get modulation type
                string modulationType = "AM";
                if (_modulationTypeComboBox?.SelectedItem != null)
                {
                    modulationType = ((ComboBoxItem)_modulationTypeComboBox.SelectedItem).Content.ToString();
                }

                // Get CARRIER frequency from the carrier frequency textbox (NOT from device)
                double carrierFrequency = 100000; // Default 100kHz
                if (_carrierFrequencyTextBox != null && double.TryParse(_carrierFrequencyTextBox.Text, out double carrierFreq))
                {
                    string unit = "Hz";
                    if (_carrierFrequencyUnitComboBox?.SelectedItem != null)
                    {
                        unit = ((ComboBoxItem)_carrierFrequencyUnitComboBox.SelectedItem).Content.ToString();
                    }
                    carrierFrequency = carrierFreq * UnitConversionUtility.GetFrequencyMultiplier(unit);
                }

                // Get carrier waveform
                string carrierWaveform = "SINE";
                if (_carrierWaveformComboBox?.SelectedItem != null)
                {
                    carrierWaveform = ((ComboBoxItem)_carrierWaveformComboBox.SelectedItem).Content.ToString().ToUpper();
                }


                // Get CARRIER amplitude from the carrier amplitude textbox
                double carrierAmplitude = 1.0; // Default 1 Vpp
                if (_carrierAmplitudeTextBox != null && double.TryParse(_carrierAmplitudeTextBox.Text, out double carrierAmp))
                {
                    string unit = "Vpp";
                    if (_carrierAmplitudeUnitComboBox?.SelectedItem != null)
                    {
                        unit = ((ComboBoxItem)_carrierAmplitudeUnitComboBox.SelectedItem).Content.ToString();
                    }
                    carrierAmplitude = carrierAmp * UnitConversionUtility.GetAmplitudeMultiplier(unit);
                }



                // Get modulating waveform
                string modulatingWaveform = "SINE";
                if (_modulatingWaveformComboBox?.SelectedItem != null)
                {
                    modulatingWaveform = ((ComboBoxItem)_modulatingWaveformComboBox.SelectedItem).Content.ToString();
                }

                // Get modulation frequency
                double modFrequency = 500; // Default 500Hz
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
                Log($"  Carrier: {carrierWaveform} at {carrierFrequency} Hz, {carrierAmplitude} Vpp from UI");
                Log($"  Modulating: {modulatingWaveform} at {modFrequency} Hz");
                Log($"  Depth: {modDepth}%");

                // Apply modulation based on type - pass carrier frequency explicitly
                ApplyModulationByType(modulationType, carrierWaveform, carrierFrequency, carrierAmplitude,
                    modulatingWaveform, modFrequency, modDepth);

                Log($"Modulation applied successfully");
            }
            catch (Exception ex)
            {
                Log($"Error applying modulation: {ex.Message}");
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
                default: return uiWaveform;
            }
        }

        /// <summary>
        /// Apply specific modulation type to the device
        /// </summary>
        private void ApplyModulationByType(string modulationType, string carrierWaveform,
                                    double carrierFrequency, double carrierAmplitude,
                                    string modulatingWaveform, double modFrequency, double modDepth)
        {
            // Get current offset and phase from device
            double carrierOffset = _device.GetOffset(_activeChannel);
            double carrierPhase = _device.GetPhase(_activeChannel);

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


        /// <summary>
        /// Refresh carrier amplitude from device
        /// </summary>
        public void RefreshCarrierAmplitude()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                double amplitude = _device.GetAmplitude(_activeChannel);

                // Update the carrier amplitude display
                if (_carrierAmplitudeTextBox != null && _carrierAmplitudeUnitComboBox != null)
                {
                    // Determine best unit
                    string unit = "Vpp";
                    double displayValue = amplitude;

                    if (amplitude < 0.1)
                    {
                        unit = "mVpp";
                        displayValue = amplitude * 1000;
                    }

                    _carrierAmplitudeTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

                    // Update unit combo box
                    for (int i = 0; i < _carrierAmplitudeUnitComboBox.Items.Count; i++)
                    {
                        var item = _carrierAmplitudeUnitComboBox.Items[i] as ComboBoxItem;
                        if (item?.Content.ToString() == unit)
                        {
                            _carrierAmplitudeUnitComboBox.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing carrier amplitude: {ex.Message}");
            }
        }







        /// <summary>
        /// Disable all modulation
        /// </summary>
        public void DisableModulation()
        {
            if (!IsDeviceConnected()) return;

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

                Log("All modulation disabled");
            }
            catch (Exception ex)
            {
                Log($"Error disabling modulation: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh modulation settings from device
        /// </summary>
        public void RefreshSettings()
        {
            if (!IsDeviceConnected()) return;

            try
            {
                // Check which modulation is active
                string[] modTypes = { "AM", "FM", "PM", "PWM", "ASK", "FSK", "PSK" };

                foreach (var modType in modTypes)
                {
                    string response = _device.SendQuery($"SOURCE{_activeChannel}:{modType}:STATE?");
                    if (response.Trim() == "ON")
                    {
                        // This modulation type is active
                        RefreshModulationSettings(modType);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing modulation settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Refresh settings for specific modulation type
        /// </summary>
        private void RefreshModulationSettings(string modulationType)
        {
            // Update modulation type combo box
            if (_modulationTypeComboBox != null)
            {
                for (int i = 0; i < _modulationTypeComboBox.Items.Count; i++)
                {
                    var item = _modulationTypeComboBox.Items[i] as ComboBoxItem;
                    if (item?.Content.ToString() == modulationType)
                    {
                        _modulationTypeComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Get modulation parameters based on type
            switch (modulationType)
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
                    // Add other modulation types as needed
            }
        }

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
        /// Update frequency display with appropriate unit
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
        /// Update modulating waveform selection
        /// </summary>
        private void UpdateModulatingWaveformSelection(string waveform)
        {
            if (_modulatingWaveformComboBox == null) return;

            for (int i = 0; i < _modulatingWaveformComboBox.Items.Count; i++)
            {
                var item = _modulatingWaveformComboBox.Items[i] as ComboBoxItem;
                if (item?.Content.ToString() == waveform)
                {
                    _modulatingWaveformComboBox.SelectedIndex = i;
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