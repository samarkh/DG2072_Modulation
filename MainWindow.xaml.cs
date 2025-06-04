using DG2072_USB_Control.Continuous.ArbitraryWaveform;
using DG2072_USB_Control.Continuous.DC;
using DG2072_USB_Control.Continuous.DualTone;
using DG2072_USB_Control.Continuous.Harmonics;
using DG2072_USB_Control.Continuous.Noise;
using DG2072_USB_Control.Continuous.PulseGenerator;
using DG2072_USB_Control.Continuous.Ramp;
using DG2072_USB_Control.Continuous.Sinusoid;
using DG2072_USB_Control.Continuous.Square;
using DG2072_USB_Control.Modulation;
using DG2072_USB_Control.Services;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using static DG2072_USB_Control.RigolDG2072;


namespace DG2072_USB_Control
{
    public partial class MainWindow : System.Windows.Window
    {
        // VISA instrument handle
        private IntPtr instrumentHandle = IntPtr.Zero;
        private bool isConnected = false;
        private const string InstrumentAddress = "USB0::0x1AB1::0x0644::DG2P224100508::INSTR";
        private RigolDG2072 rigolDG2072;

        // Active channel tracking
        private int activeChannel = 1; // Default to Channel 1

        // Timer for auto-refresh feature
        private DispatcherTimer _autoRefreshTimer;
        private bool _autoRefreshEnabled = false;
        private CheckBox AutoRefreshCheckBox;
        private FrequencyPeriodConverter frequencyPeriodConverter;

        // Update timers
        private DispatcherTimer _frequencyUpdateTimer;
        private DispatcherTimer _amplitudeUpdateTimer;
        private DispatcherTimer _offsetUpdateTimer;
        private DispatcherTimer _phaseUpdateTimer;
        private DispatcherTimer _primaryFrequencyUpdateTimer;
        private DockPanel SymmetryDockPanel;
        //private List<WaveformGenerator> allGenerators;

        // private DispatcherTimer _dutyCycleUpdateTimer;
        private DockPanel DutyCycleDockPanel;

        // Add this with the other timer declarations in MainWindow.xaml.cs:
        // private DispatcherTimer _secondaryFrequencyUpdateTimer;
        private bool _frequencyModeActive = true; // Default to frequency mode
        private DockPanel PulsePeriodDockPanel;
        private DockPanel PhaseDockPanel;
        //private ChannelHarmonicController harmonicController;

        private double frequencyRatio = 2.0; // Default frequency ratio (harmonic)

        private DockPanel DCVoltageDockPanel;

        private DG2072_USB_Control.Modulation.ModulationController _modulationController;

        // Harmonics management
        //private HarmonicsManager _harmonicsManager;
        //private HarmonicsUIController _harmonicsUIController;
        private bool _isInitializing = true;

        // Dual Tone management
        private DualToneGen dualToneGen;

        // Ramp generator management
        private RampGen rampGenerator;

        //Square Generator management
        private SquareGen squareGenerator;

        //Sinusoid generator management
        private SinGen sineGenerator;

        //dc generator management
        private DCGen dcGenerator;

        // Noise generator management
        private NoiseGen noiseGenerator;

        // Arbitrary Waveform Generator Management
        private ArbitraryWaveformGen arbitraryWaveformGen;
        
        private DispatcherTimer _pulsePeriodUpdateTimer;

        private DG2072_USB_Control.Sweep.SweepController _sweepController;


        public GroupBox SweepPanel => SweepPanelControl?.SweepPanelGroupBox;

        // Constructor starts here
        // Constructor starts here
        // Constructor starts here
        // Constructor starts here
        public MainWindow()
        {
            InitializeComponent();

            // Initialize the device communication
            rigolDG2072 = new RigolDG2072();
            rigolDG2072.LogEvent += (s, message) => LogMessage(message);

            // Initialize ComboBoxes
            ChannelWaveformComboBox.SelectedIndex = 0;

            // Initialize auto-refresh feature
            InitializeAutoRefresh();
        }

        private void InitializeFrequencyPeriodConverter()
        {
            frequencyPeriodConverter = new FrequencyPeriodConverter(
                FrequencyDockPanel,
                PeriodDockPanel,
                ChannelFrequencyTextBox,
                PulsePeriod, // Should be renamed to ChannelPeriodTextBox
                ChannelFrequencyUnitComboBox,
                PulsePeriodUnitComboBox, // Should be renamed to ChannelPeriodUnitComboBox
                FrequencyPeriodModeToggle
            );

            frequencyPeriodConverter.LogEvent += (s, msg) => LogMessage(msg);
        }



        //**************** Regions
        //**************** Regions
        //**************** Regions
    #region Channel Output Button Handlers

        private void BtnCH1On_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                LogMessage("Cannot control channel output: Not connected to instrument");
                return;
            }

            try
            {
                // Get current state of Channel 1
                string currentState = rigolDG2072.GetOutputState(1);
                bool isCurrentlyOn = currentState.ToUpper().Contains("ON");

                // Toggle the state
                bool newState = !isCurrentlyOn;
                rigolDG2072.SetOutput(1, newState);

                // Update button appearance
                UpdateChannelButtonState(btnCH1On, 1, newState);

                // Log the action
                LogMessage($"Channel 1 output {(newState ? "enabled" : "disabled")}");

                // If this is the active channel, update the main output toggle too
                if (activeChannel == 1)
                {
                    ChannelOutputToggle.IsChecked = newState;
                    ChannelOutputToggle.Content = newState ? "ON" : "OFF";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error toggling Channel 1 output: {ex.Message}");
            }
        }

        private void BtnCH2On_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                LogMessage("Cannot control channel output: Not connected to instrument");
                return;
            }

            try
            {
                // Get current state of Channel 2
                string currentState = rigolDG2072.GetOutputState(2);
                bool isCurrentlyOn = currentState.ToUpper().Contains("ON");

                // Toggle the state
                bool newState = !isCurrentlyOn;
                rigolDG2072.SetOutput(2, newState);

                // Update button appearance
                UpdateChannelButtonState(btnCH2On, 2, newState);

                // Log the action
                LogMessage($"Channel 2 output {(newState ? "enabled" : "disabled")}");

                // If this is the active channel, update the main output toggle too
                if (activeChannel == 2)
                {
                    ChannelOutputToggle.IsChecked = newState;
                    ChannelOutputToggle.Content = newState ? "ON" : "OFF";
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error toggling Channel 2 output: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the appearance of a channel button based on its state
        /// </summary>
        // If using XAML resources, update UpdateChannelButtonState like this:
        private void UpdateChannelButtonState(Button button, int channel, bool isOn)
        {
            if (button == null) return;

            Dispatcher.Invoke(() =>
            {
                button.Content = $"CH{channel}: {(isOn ? "ON" : "OFF")}";

                if (isOn)
                {
                    // Get brushes from XAML resources
                    string brushKey = channel == 1 ? "Channel1OnBrush" : "Channel2OnBrush";
                    button.Background = (SolidColorBrush)FindResource(brushKey);
                    button.Foreground = (SolidColorBrush)FindResource("ChannelOnForegroundBrush");
                    button.FontWeight = FontWeights.Bold;
                }
                else
                {
                    // Get brushes from XAML resources
                    button.Background = (SolidColorBrush)FindResource("ChannelOffBackgroundBrush");
                    button.Foreground = (SolidColorBrush)FindResource("ChannelOffForegroundBrush");
                    button.FontWeight = FontWeights.Normal;
                }
            });
        }

        /// <summary>
        /// Refreshes the state of both channel output buttons
        /// </summary>
        private void RefreshChannelOutputButtons()
        {
            if (!isConnected) return;

            try
            {
                // Update Channel 1 button
                string ch1State = rigolDG2072.GetOutputState(1);
                bool ch1IsOn = ch1State.ToUpper().Contains("ON");
                UpdateChannelButtonState(btnCH1On, 1, ch1IsOn);

                // Update Channel 2 button
                string ch2State = rigolDG2072.GetOutputState(2);
                bool ch2IsOn = ch2State.ToUpper().Contains("ON");
                UpdateChannelButtonState(btnCH2On, 2, ch2IsOn);
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing channel output buttons: {ex.Message}");
            }
        }

        #endregion

    #region Channel Toggle Methods

        // Update the channel toggle method to update the pulse generator's active channel
        // Alternative approach with helper method
        private void ChannelToggleButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle between Channel 1 and Channel 2
            activeChannel = (ChannelToggleButton.IsChecked == true) ? 1 : 2;

            // Update UI elements
            string channelText = $"Channel {activeChannel}";
            ChannelToggleButton.Content = channelText;
            ActiveChannelTextBlock.Text = channelText;
            ChannelControlsGroupBox.Header = $"{channelText} Controls";

            // Update all waveform generators
            UpdateAllGeneratorsChannel(activeChannel);

            // Refresh the UI to show current settings for the selected channel
            if (isConnected)
            {
                RefreshChannelSettings();
            }
        }

        // Helper method to update all generators
        private void UpdateAllGeneratorsChannel(int channel)
        {
            // Update all waveform generators
            if (pulseGenerator != null)
                pulseGenerator.ActiveChannel = channel;

            if (rampGenerator != null)
                rampGenerator.ActiveChannel = channel;

            if (squareGenerator != null)
                squareGenerator.ActiveChannel = channel;

            if (sineGenerator != null)
                sineGenerator.ActiveChannel = channel;

            if (dcGenerator != null)
                dcGenerator.ActiveChannel = channel;

            if (noiseGenerator != null)
                noiseGenerator.ActiveChannel = channel;

            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.ActiveChannel = channel;

            if (_modulationController != null)
                _modulationController.ActiveChannel = channel;

            if (_harmonicsManager != null)
                _harmonicsManager.SetActiveChannel(channel);

            if (dualToneGen != null)
                dualToneGen.ActiveChannel = channel;

            if (_sweepController != null)
                _sweepController.ActiveChannel = channel;
        }

        private void RefreshChannelSettings()
        {
            try
            {
                // First update the waveform selection - this is critical to do first
                // since other settings depend on the waveform type
                UpdateWaveformSelection(ChannelWaveformComboBox, activeChannel);

                // Now get the currently selected waveform from the UI
                string waveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper();
                if (waveform == "SINE" && sineGenerator != null)
                {
                    sineGenerator.RefreshParameters(); // New way using base class method
                }

                // Special handling for DC waveform
                if (waveform == "DC" && dcGenerator != null)
                {
                    // Delegate to DC generator for handling DC-specific settings
                    dcGenerator.RefreshParameters(); // Updated to use base class method

                    // Update output state
                    UpdateOutputState(ChannelOutputToggle, activeChannel);
                }

                // Special handling for PULSE waveform - delegate to pulseGenerator
                else if (waveform == "PULSE" && pulseGenerator != null)
                {
                    // Update common parameters
                    if (_frequencyModeActive)
                    {
                        UpdateFrequencyValue(ChannelFrequencyTextBox, ChannelFrequencyUnitComboBox, activeChannel);
                    }
                    else
                    {
                        UpdatePeriodValue(PulsePeriod, PulsePeriodUnitComboBox, activeChannel);
                    }

                    // Update amplitude, offset, phase and output state
                    UpdateAmplitudeValue(ChannelAmplitudeTextBox, ChannelAmplitudeUnitComboBox, activeChannel);
                    UpdateOffsetValue(ChannelOffsetTextBox, activeChannel);
                    UpdatePhaseValue(ChannelPhaseTextBox, activeChannel);
                    UpdateOutputState(ChannelOutputToggle, activeChannel);

                    // Delegate pulse-specific parameter updates to the pulse generator
                    pulseGenerator.UpdatePulseParameters(activeChannel);

                    // Ensure pulse-specific controls are visible
                    pulseGenerator.UpdatePulseControls(true);
                }

                // Handle other non-DC waveforms
                else
                {


                    // Update frequency/period based on current mode
                    if (_frequencyModeActive)
                    {
                        UpdateFrequencyValue(ChannelFrequencyTextBox, ChannelFrequencyUnitComboBox, activeChannel);
                    }
                    else
                    {
                        UpdatePeriodValue(PulsePeriod, PulsePeriodUnitComboBox, activeChannel);
                    }

                    // Update common parameters for all non-DC waveforms
                    UpdateAmplitudeValue(ChannelAmplitudeTextBox, ChannelAmplitudeUnitComboBox, activeChannel);
                    UpdateOffsetValue(ChannelOffsetTextBox, activeChannel);
                    UpdatePhaseValue(ChannelPhaseTextBox, activeChannel);
                    UpdateOutputState(ChannelOutputToggle, activeChannel);

                    // Add handling for RAMP waveform
                    if (waveform == "RAMP" && rampGenerator != null)
                    {
                        // Delegate to the ramp generator
                        rampGenerator.ApplyParameters();
                    }

                    else if (waveform == "SQUARE" && squareGenerator != null)
                    {
                        squareGenerator.UpdateDutyCycleValue();
                    }
                    else if (waveform == "HARMONIC" && _harmonicsUIController != null)
                    {
                        _harmonicsUIController.RefreshHarmonicSettings();
                    }
                    else if (waveform == "NOISE" && noiseGenerator != null)
                    {
                        // Delegate to the noise generator for handling NOISE-specific settings
                        noiseGenerator.RefreshParameters();

                        // Update output state
                        UpdateOutputState(ChannelOutputToggle, activeChannel);
                    }
                    else if (waveform == "DUAL TONE")
                    {
                        RefreshDualToneSettings(activeChannel);
                    }
                    else if (waveform == "USER" || waveform == "ARBITRARY WAVEFORM")
                    {
                        RefreshArbitraryWaveformSettings(activeChannel);
                    }
                }

                // Update UI controls visibility based on the selected waveform
                UpdateWaveformSpecificControls(waveform);

                // ADD after refreshing waveform settings:
                if (_modulationController != null && isConnected)
                {
                    // Check if modulation is active on the device
                    string[] modTypes = { "AM", "FM", "PM", "PWM", "ASK", "FSK", "PSK" };
                    bool modulationActive = false;

                    foreach (var modType in modTypes)
                    {
                        string response = rigolDG2072.SendQuery($"SOURCE{activeChannel}:{modType}:STATE?");
                        if (response.Trim() == "ON" || response.Trim() == "1")
                        {
                            modulationActive = true;
                            break;
                        }
                    }

                    // If modulation is active but controller thinks it's off, sync the state
                    if (modulationActive && !_modulationController.IsEnabled)
                    {
                        LogMessage("Device has modulation enabled - syncing UI state");
                        // This would need a method to sync without re-applying
                    }
                }




                LogMessage($"Refreshed Channel {activeChannel} settings");
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing Channel {activeChannel} settings: {ex.Message}");
            }
        }

    #endregion

    #region Auto-Refresh Methods

        private void SetupAutoRefresh()
        {
            // Create the auto-refresh timer
            _autoRefreshTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5) // Default to 5 seconds
            };

            _autoRefreshTimer.Tick += (s, e) =>
            {
                if (isConnected && _autoRefreshEnabled)
                {
                    RefreshInstrumentSettings();
                }
            };
        }

        private void InitializeAutoRefresh()
        {
            SetupAutoRefresh();

            // Create and add auto-refresh checkbox to the UI
            var autoRefreshCheckBox = new CheckBox
            {
                Content = "Auto-Refresh",
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0),
                IsChecked = _autoRefreshEnabled,
                IsEnabled = false // Initially disabled until connected
            };

            autoRefreshCheckBox.Checked += (s, e) =>
            {
                _autoRefreshEnabled = true;
                _autoRefreshTimer.Start();
                LogMessage("Auto-refresh enabled");
            };

            autoRefreshCheckBox.Unchecked += (s, e) =>
            {
                _autoRefreshEnabled = false;
                _autoRefreshTimer.Stop();
                LogMessage("Auto-refresh disabled");
            };

            // Store reference to the checkbox
            AutoRefreshCheckBox = autoRefreshCheckBox;

            // Add to the connection status bar
            var connectionBar = ConnectionToggleButton.Parent as DockPanel;
            if (connectionBar != null)
            {
                connectionBar.Children.Insert(1, autoRefreshCheckBox);
            }
        }

        private void RefreshInstrumentSettings()
        {
            if (!isConnected) return;

            try
            {
                LogMessage("Refreshing all instrument settings...");

                // First refresh basic channel settings - this updates waveform type first
                RefreshChannelSettings();

                // Now get the currently selected waveform from the UI
                string currentWaveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper();

                // For USER/ARBITRARY WAVEFORM, refresh arbitrary settings
                if (currentWaveform == "USER" || currentWaveform == "ARBITRARY WAVEFORM")
                {
                    // Make sure the arbitrary waveform group is visible
                    ArbitraryWaveformGroupBox.Visibility = Visibility.Visible;

                    // Refresh arbitrary waveform specific settings
                    RefreshArbitraryWaveformSettings(activeChannel);
                }
                // For HARMONIC waveform, refresh harmonic settings
                else if (currentWaveform == "HARMONIC")
                {
                    // Initialize harmonicController if needed
                    if (harmonicController == null)
                    {
                        harmonicController = new ChannelHarmonicController(rigolDG2072, activeChannel);
                    }

                    // Make sure the harmonics group is visible
                    if (HarmonicsGroupBox != null)
                    {
                        HarmonicsGroupBox.Visibility = Visibility.Visible;
                    }

                    // Refresh harmonic settings
                    RefreshHarmonicSettings();
                }
                // For DUAL TONE waveform, refresh dual tone settings
                else if (currentWaveform == "DUAL TONE" || currentWaveform == "DUALTONE")
                {
                    // Make sure the dual tone group is visible
                    if (DualToneGroupBox != null)
                    {
                        DualToneGroupBox.Visibility = Visibility.Visible;
                    }

                    // Refresh dual tone specific settings
                    RefreshDualToneSettings(activeChannel);
                }

                // Make sure all waveform-specific controls have proper visibility
                UpdateWaveformSpecificControls(currentWaveform);

                // ADD THIS SINGLE LINE - Refresh the channel output buttons
                RefreshChannelOutputButtons();


                // ADD after refreshing waveform settings:
                if (_modulationController != null && isConnected)
                {
                    // Check if modulation is active on the device
                    string[] modTypes = { "AM", "FM", "PM", "PWM", "ASK", "FSK", "PSK" };
                    bool modulationActive = false;
                    string activeModType = "";

                    foreach (var modType in modTypes)
                    {
                        string response = rigolDG2072.SendQuery($"SOURCE{activeChannel}:{modType}:STATE?");
                        if (response.Trim() == "ON" || response.Trim() == "1")
                        {
                            modulationActive = true;
                            activeModType = modType;
                            break;
                        }
                    }

                    // If modulation is active but controller thinks it's off, sync the state
                    if (modulationActive && !_modulationController.IsEnabled)
                    {
                        LogMessage($"Device has {activeModType} modulation enabled - syncing UI state");
                        _modulationController.SyncModulationFromDevice(activeModType);
                    }
                    else if (!modulationActive && _modulationController.IsEnabled)
                    {
                        // Device has no modulation but UI thinks it's on
                        LogMessage("Device has no modulation enabled - updating UI");
                        _modulationController.ForceDisableUI();
                    }
                    else if (modulationActive && _modulationController.IsEnabled)
                    {
                        // Both agree modulation is on - just refresh the settings
                        _modulationController.RefreshModulationSettings();
                    }
                }

                if (_sweepController != null && isConnected)
                    _sweepController.RefreshSweepSettings();



                LogMessage("Instrument settings refreshed successfully");
            }
            catch (Exception ex)
            {
                LogMessage($"Error refreshing instrument settings: {ex.Message}");
            }
        }

        private void UpdateAutoRefreshState(bool connected)
        {
            if (AutoRefreshCheckBox != null)
            {
                AutoRefreshCheckBox.IsEnabled = connected;

                if (!connected && _autoRefreshEnabled)
                {
                    _autoRefreshEnabled = false;
                    _autoRefreshTimer.Stop();
                    AutoRefreshCheckBox.IsChecked = false;
                }
            }
        }

     #endregion

    #region Instrument Settings Update Methods

        private void UpdatePeriodValue(TextBox periodTextBox, ComboBox unitComboBox, int channel)
        {
            try
            {
                // Get current waveform
                string currentWaveform = rigolDG2072.SendQuery($":SOUR{channel}:FUNC?").Trim().ToUpper();
                double period;

                // For pulse, use the specific method
                if (currentWaveform.Contains("PULS"))
                {
                    period = rigolDG2072.GetPulsePeriod(channel);
                }
                else
                {
                    // For other waveforms, query the period directly
                    string response = rigolDG2072.SendQuery($"SOURCE{channel}:PERiod?");
                    if (!double.TryParse(response, out period))
                    {
                        // If direct query fails, calculate from frequency
                        double frequency = rigolDG2072.GetFrequency(channel);
                        if (frequency > 0)
                        {
                            period = 1.0 / frequency;
                        }
                        else
                        {
                            period = 0.001; // Default 1ms
                        }
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    // Store current unit to preserve it if possible
                    string currentUnit = Services.UnitConversionUtility.GetPeriodUnit(unitComboBox);

                    // Convert to picoseconds for internal representation
                    double psValue = period * 1e12; // Convert seconds to picoseconds

                    // Calculate the display value based on the current unit
                    double displayValue = Services.UnitConversionUtility.ConvertFromPicoSeconds(psValue, currentUnit);

                    // If the value would display poorly in the current unit, find a better unit
                    if (displayValue > 9999 || displayValue < 0.1)
                    {
                        string[] units = { "ps", "ns", "µs", "ms", "s" };
                        int bestUnitIndex = 2; // Default to µs

                        for (int i = 0; i < units.Length; i++)
                        {
                            double testValue = Services.UnitConversionUtility.ConvertFromPicoSeconds(psValue, units[i]);
                            if (testValue >= 0.1 && testValue < 10000)
                            {
                                bestUnitIndex = i;
                                break;
                            }
                        }

                        // Update the display value and select the best unit
                        displayValue = Services.UnitConversionUtility.ConvertFromPicoSeconds(psValue, units[bestUnitIndex]);

                        // Find and select the unit in the combo box
                        for (int i = 0; i < unitComboBox.Items.Count; i++)
                        {
                            ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                            if (item != null && item.Content.ToString() == units[bestUnitIndex])
                            {
                                unitComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    periodTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating period for channel {channel}: {ex.Message}");
            }
        }

        private void UpdateOutputState(ToggleButton outputToggle, int channel)
        {
            string state = rigolDG2072.GetOutputState(channel);
            bool isOn = state.ToUpper().Contains("ON");

            Dispatcher.Invoke(() =>
            {
                outputToggle.IsChecked = isOn;
                outputToggle.Content = isOn ? "ON" : "OFF";
            });
        }

        private void UpdateWaveformSelection(ComboBox waveformComboBox, int channel)
        {
            try
            {
                string currentWaveform = rigolDG2072.SendQuery($":SOUR{channel}:FUNC?").Trim().ToUpper();
                LogMessage($"Updating waveform selection - Device reports: {currentWaveform}");

                // Remove any SCPI response delimiters if present
                if (currentWaveform.StartsWith("\"") && currentWaveform.EndsWith("\""))
                {
                    currentWaveform = currentWaveform.Substring(1, currentWaveform.Length - 2);
                }

                // Map common abbreviations to full names
                if (currentWaveform == "SIN") currentWaveform = "SINE";
                if (currentWaveform == "SQU") currentWaveform = "SQUARE";
                if (currentWaveform == "PULS") currentWaveform = "PULSE";
                if (currentWaveform == "RAMP") currentWaveform = "RAMP";
                if (currentWaveform == "NOIS") currentWaveform = "NOISE";
                if (currentWaveform == "USER") currentWaveform = "ARBITRARY WAVEFORM";
                if (currentWaveform == "HARM") currentWaveform = "HARMONIC";
                if (currentWaveform == "DUAL") currentWaveform = "DUAL TONE";

                // Check if this is an arbitrary waveform
                bool isArbitraryWaveform = false;
                var waveformInfo = rigolDG2072.FindArbitraryWaveformByScpiCommand(currentWaveform);

                if (waveformInfo.HasValue)
                {
                    isArbitraryWaveform = true;
                    LogMessage($"Detected arbitrary waveform: {waveformInfo.Value.FriendlyName} from category {waveformInfo.Value.Category}");

                    // Store the detected waveform info for later use in ArbitraryWaveformGen
                    rigolDG2072.LastDetectedArbitraryWaveform = waveformInfo.Value;
                }

                // If it's an arbitrary waveform but not already recognized as USER/ARBITRARY WAVEFORM, 
                // treat it as ARBITRARY WAVEFORM for UI selection
                if (isArbitraryWaveform && currentWaveform != "ARBITRARY WAVEFORM" && currentWaveform != "USER")
                {
                    currentWaveform = "ARBITRARY WAVEFORM";
                }

                Dispatcher.Invoke(() =>
                {
                    bool found = false;

                    // First try exact match
                    foreach (ComboBoxItem item in waveformComboBox.Items)
                    {
                        if (item.Content.ToString().ToUpper() == currentWaveform)
                        {
                            waveformComboBox.SelectedItem = item;
                            found = true;
                            LogMessage($"Selected waveform: {item.Content}");
                            break;
                        }
                    }

                    // If not found, try with partial match
                    if (!found)
                    {
                        foreach (ComboBoxItem item in waveformComboBox.Items)
                        {
                            if (currentWaveform.Contains(item.Content.ToString().ToUpper()) ||
                                item.Content.ToString().ToUpper().Contains(currentWaveform))
                            {
                                waveformComboBox.SelectedItem = item;
                                LogMessage($"Selected waveform by partial match: {item.Content}");
                                found = true;
                                break;
                            }
                        }
                    }

                    // Special handling for USER/ARBITRARY WAVEFORM
                    // If currentWaveform is an arbitrary waveform and we haven't found it yet,
                    // try to select the "Arbitrary Waveform" option
                    if (!found && isArbitraryWaveform)
                    {
                        foreach (ComboBoxItem item in waveformComboBox.Items)
                        {
                            if (item.Content.ToString().ToUpper() == "ARBITRARY WAVEFORM")
                            {
                                waveformComboBox.SelectedItem = item;
                                found = true;
                                LogMessage($"Selected Arbitrary Waveform for {currentWaveform}");
                                break;
                            }
                        }
                    }

                    // If still not found, log a warning
                    if (!found)
                    {
                        LogMessage($"Warning: Could not find matching waveform for '{currentWaveform}' in UI");
                        // Default to first item as a fallback
                        if (waveformComboBox.Items.Count > 0)
                        {
                            waveformComboBox.SelectedIndex = 0;
                            LogMessage($"Defaulted to {((ComboBoxItem)waveformComboBox.SelectedItem).Content}");
                        }
                    }

                    // Make sure to update waveform-specific controls based on selection
                    UpdateWaveformSpecificControls(((ComboBoxItem)waveformComboBox.SelectedItem).Content.ToString());

                    // If it's an arbitrary waveform, refresh the arbitrary waveform settings
                    // This must happen AFTER the main waveform type is selected in the UI
                    if (currentWaveform == "ARBITRARY WAVEFORM" || isArbitraryWaveform)
                    {
                        LogMessage("Refreshing arbitrary waveform settings based on detected waveform");
                        RefreshArbitraryWaveformSettings(channel);
                    }
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating waveform selection for channel {channel}: {ex.Message}");
            }
        }

        private void UpdateFrequencyValue(TextBox freqTextBox, ComboBox unitComboBox, int channel)
        {
            try
            {
                double frequency = rigolDG2072.GetFrequency(channel);

                Dispatcher.Invoke(() =>
                {
                    // Store current unit to preserve it if possible
                    string currentUnit = UnitConversionUtility.GetFrequencyUnit(unitComboBox);

                    // Calculate the display value based on the current unit
                    double displayValue = UnitConversionUtility.ConvertFromMicroHz(frequency * 1e6, currentUnit);

                    // If the value would display poorly in the current unit, find a better unit
                    if (displayValue > 9999 || displayValue < 0.1)
                    {
                        string[] units = { "µHz", "mHz", "Hz", "kHz", "MHz" };
                        int bestUnitIndex = 2; // Default to Hz

                        for (int i = 0; i < units.Length; i++)
                        {
                            double testValue = UnitConversionUtility.ConvertFromMicroHz(frequency * 1e6, units[i]);
                            if (testValue >= 0.1 && testValue < 10000)
                            {
                                bestUnitIndex = i;
                                break;
                            }
                        }

                        // Update the display value and select the best unit
                        displayValue = UnitConversionUtility.ConvertFromMicroHz(frequency * 1e6, units[bestUnitIndex]);

                        // Find and select the unit in the combo box
                        for (int i = 0; i < unitComboBox.Items.Count; i++)
                        {
                            ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                            if (item != null && item.Content.ToString() == units[bestUnitIndex])
                            {
                                unitComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    freqTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating frequency for channel {channel}: {ex.Message}");
            }
        }

        private void UpdateAmplitudeValue(TextBox ampTextBox, ComboBox unitComboBox, int channel)
        {
            try
            {
                double amplitude = rigolDG2072.GetAmplitude(channel);

                Dispatcher.Invoke(() =>
                {
                    // Store current unit to preserve it if possible
                    string currentUnit = UnitConversionUtility.GetAmplitudeUnit(unitComboBox);
                    bool isRms = currentUnit.Contains("rms");

                    // Calculate the scale factor based on the current unit
                    double scaleFactor = 1.0;
                    if (isRms)
                    {
                        // Convert Vpp to Vrms (for sine waves)
                        scaleFactor = 1.0 / (2.0 * Math.Sqrt(2.0));
                    }

                    // Apply milli prefix if needed
                    bool useMilliPrefix = currentUnit.StartsWith("m");
                    if (useMilliPrefix)
                    {
                        scaleFactor *= 1000.0;
                    }

                    double displayValue = amplitude * scaleFactor;

                    // If the value would display poorly in the current unit, find a better unit
                    if (displayValue > 9999 || displayValue < 0.1)
                    {
                        // Toggle between base unit and milli prefix
                        useMilliPrefix = !useMilliPrefix;

                        if (useMilliPrefix)
                        {
                            displayValue = amplitude * scaleFactor * 1000.0;
                        }
                        else
                        {
                            displayValue = amplitude * scaleFactor / 1000.0;
                        }

                        // Determine the new unit string
                        string newUnit;
                        if (isRms)
                        {
                            newUnit = useMilliPrefix ? "mVrms" : "Vrms";
                        }
                        else
                        {
                            newUnit = useMilliPrefix ? "mVpp" : "Vpp";
                        }

                        // Find and select the unit in the combo box
                        for (int i = 0; i < unitComboBox.Items.Count; i++)
                        {
                            ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                            if (item != null && item.Content.ToString() == newUnit)
                            {
                                unitComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                    }

                    ampTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating amplitude for channel {channel}: {ex.Message}");
            }
        }

        private void UpdateOffsetValue(TextBox offsetTextBox, int channel)
        {
            try
            {
                double offset = rigolDG2072.GetOffset(channel);

                Dispatcher.Invoke(() =>
                {
                    offsetTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(offset);
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating offset for channel {channel}: {ex.Message}");
            }
        }

        private void UpdatePhaseValue(TextBox phaseTextBox, int channel)
        {
            try
            {
                double phase = rigolDG2072.GetPhase(channel);

                Dispatcher.Invoke(() =>
                {
                    phaseTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(phase);
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating phase for channel {channel}: {ex.Message}");
            }
        }

        #endregion

    #region Connection Methods

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (isConnected)
            {
                RefreshInstrumentSettings();
                LogMessage("Settings refreshed from instrument");
            }
            else
            {
                LogMessage("Cannot refresh settings: Instrument not connected");
            }
        }

        private bool Connect()
        {
            try
            {
                LogMessage("Starting connection process...");
                var sw = System.Diagnostics.Stopwatch.StartNew();

                LogMessage("Creating VISA connection...");
                bool result = rigolDG2072.Connect();

                LogMessage($"VISA Connect returned in {sw.ElapsedMilliseconds}ms");

                if (result)
                {
                    isConnected = true;
                    LogMessage("Connected to Rigol DG2072");
                    RefreshInstrumentSettings();
                }
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"Connection error: {ex.Message}");
                return false;
            }
        }

        //private bool Connect()
        //{
        //    try
        //    {
        //        bool result = rigolDG2072.Connect();
        //        if (result)
        //        {
        //            isConnected = true;
        //            LogMessage("Connected to Rigol DG2072");

        //            // Refresh all settings from the instrument
        //            RefreshInstrumentSettings();
        //        }
        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        LogMessage($"Connection error: {ex.Message}");
        //        return false;
        //    }
        //}

        private bool Disconnect()
        {
            try
            {
                bool result = rigolDG2072.Disconnect();
                if (result)
                {
                    isConnected = false;
                    LogMessage("Disconnected from Rigol DG2072");
                }
                return result;
            }
            catch (Exception ex)
            {
                LogMessage($"Disconnection error: {ex.Message}");
                return false;
            }
        }

        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                CommandLogTextBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
                CommandLogTextBox.ScrollToEnd();
            });
        }

        #endregion

    #region Event Handlers - Window and Connection

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initialization flag at the very start
            _isInitializing = true;

            // Find and store reference to symmetry dock panel
            SymmetryDockPanel = FindVisualParent<DockPanel>(Symm);

            // Find and store reference to duty cycle dock panel
            DutyCycleDockPanel = FindVisualParent<DockPanel>(DutyCycle);

            // Find and store references to pulse panels
            PulseWidthDockPanel = FindVisualParent<DockPanel>(PulseWidth);

            // Since PulsePeriod is now inside FrequencyDockPanel, we set this to FrequencyDockPanel
            // This maintains compatibility with existing code that references PulsePeriodDockPanel
            PulsePeriodDockPanel = FindVisualParent<DockPanel>(PulsePeriod);

            PulseRiseTimeDockPanel = FindVisualParent<DockPanel>(PulseRiseTime);
            PulseFallTimeDockPanel = FindVisualParent<DockPanel>(PulseFallTime);

            // Get references to the main frequency panel and calculated rate panel
            FrequencyDockPanel = FindVisualParent<DockPanel>(ChannelFrequencyTextBox);

            // Find and store reference to phase panel
            PhaseDockPanel = FindVisualParent<DockPanel>(ChannelPhaseTextBox);

            // In Window_Loaded method, add this to find and store the DC panel
            DCVoltageDockPanel = FindVisualParent<DockPanel>(DCVoltageTextBox);

            // Initialize frequency/period mode with frequency mode active by default
            _frequencyModeActive = true;
            FrequencyPeriodModeToggle.IsChecked = true;
            FrequencyPeriodModeToggle.Content = "To Period";

            // Initialize the pulse generator after UI references are set up
            pulseGenerator = new PulseGen(rigolDG2072, activeChannel, this);
            pulseGenerator.LogEvent += (s, message) => LogMessage(message);

            // Initialize the dual tone generator after UI references are set up
            dualToneGen = new DualToneGen(rigolDG2072, activeChannel, this);
            dualToneGen.LogEvent += (s, message) => LogMessage(message);

            // Initialize the ramp generator after UI references are set up
            rampGenerator = new RampGen(rigolDG2072, activeChannel, this);
            rampGenerator.LogEvent += (s, message) => LogMessage(message);

            // Initialize the square generator after UI references are set up
            squareGenerator = new SquareGen(rigolDG2072, activeChannel, this);
            squareGenerator.LogEvent += (s, message) => LogMessage(message);

            // Initialize the sine generator after UI references are set up
            sineGenerator = new SinGen(rigolDG2072, activeChannel, this);
            sineGenerator.LogEvent += (s, message) => LogMessage(message);

            // Initialize the DC generator after UI references are set up
            dcGenerator = new DCGen(rigolDG2072, activeChannel, this);
            dcGenerator.LogEvent += (s, message) => LogMessage(message);

            // initialize the noise generator after UI references are set up
            noiseGenerator = new NoiseGen(rigolDG2072, activeChannel, this);
            noiseGenerator.LogEvent += (s, message) => LogMessage(message);

            // Initialize the arbitrary waveform generator
            arbitraryWaveformGen = new ArbitraryWaveformGen(rigolDG2072, activeChannel, this);
            arbitraryWaveformGen.LogEvent += (s, message) => LogMessage(message);

            // Initialize harmonics management
            _harmonicsManager = new HarmonicsManager(rigolDG2072, activeChannel);
            _harmonicsManager.LogEvent += (s, message) => LogMessage(message);

            _harmonicsUIController = new HarmonicsUIController(_harmonicsManager, this);
            _harmonicsUIController.LogEvent += (s, message) => LogMessage(message);

            _modulationController = new ModulationController(rigolDG2072, activeChannel, this);
            _modulationController.LogEvent += (s, message) => LogMessage(message);

            //_sweepController = new DG2072_USB_Control.Sweep.SweepController(rigolDG2072, activeChannel, this);
            //_sweepController.LogEvent += (s, message) => LogMessage(message);

            _sweepController = new DG2072_USB_Control.Sweep.SweepController(rigolDG2072, activeChannel, SweepPanelControl);
            _sweepController.LogEvent += (s, message) => LogMessage(message);


            // Don't initialize UI here - wait until after connection
            // After window initialization, use a small delay before auto-connecting
            // This gives the UI time to fully render before connecting
            DispatcherTimer startupTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500)  // 500ms delay
            };

            startupTimer.Tick += (s, args) =>
            {
                startupTimer.Stop();

                // Auto-connect only if not already connected
                if (!isConnected) 
                {
                    LogMessage("Auto-connecting to instrument...");
                    // Call the connection method to establish connection
                    if (Connect())
                    {
                        // Initialize AFTER connection
                        InitializeFrequencyPeriodConverter();

                        // Update UI to reflect connected state
                        ConnectionStatusTextBlock.Text = "Connected";
                        ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                        ConnectionToggleButton.Content = "Disconnect";
                        IdentifyButton.IsEnabled = true;
                        RefreshButton.IsEnabled = true;
                        
                        
                        UpdateAutoRefreshState(true);

                        if (_modulationController != null)
                        {
                            _modulationController.InitializeUI();

                            // ADD THIS: Check if device already has modulation enabled
                            System.Windows.Threading.DispatcherTimer delayedModCheck = new System.Windows.Threading.DispatcherTimer
                            {
                                Interval = TimeSpan.FromMilliseconds(500)
                            };
                            delayedModCheck.Tick += (sender, e) =>
                            {
                                delayedModCheck.Stop();

                                // IMPORTANT: Ensure modulation controller is initialized
                                if (_modulationController != null && !_modulationController.IsUIInitialized())
                                {
                                    _modulationController.InitializeUI();
                                    LogMessage("Initialized modulation controller UI before sync");
                                }

                                // Check for active modulation after everything is initialized
                                string[] modTypes = { "AM", "FM", "PM", "PWM", "ASK", "FSK", "PSK" };
                                foreach (var modType in modTypes)
                                {
                                    try
                                    {
                                        string response = rigolDG2072.SendQuery($"SOURCE{activeChannel}:{modType}:STATE?");
                                        if (response.Trim() == "ON" || response.Trim() == "1")
                                        {
                                            LogMessage($"Detected {modType} modulation active on device during startup");

                                            // Set the internal state first
                                            _modulationController.SetModulationEnabledState(true);

                                            // Then sync the settings
                                            _modulationController.SyncModulationFromDevice(modType);

                                            // Force UI state update after sync
                                            _modulationController.ForceUpdateUIState();

                                            break;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        LogMessage($"Error checking {modType}: {ex.Message}");
                                    }
                                }
                            };
                            delayedModCheck.Start();
                        }


                        if (_sweepController != null)
                            _sweepController.InitializeUI();

                        // NOW we're ready - clear the initialization flag
                        _isInitializing = false;

                        // Refresh the UI with current device settings
                        RefreshInstrumentSettings();
                        LogMessage("Auto-connection successful");
                    }
                    else
                    {
                        // Even on failure, clear the flag so manual connection can work
                        _isInitializing = false;
                        LogMessage("Auto-connection failed - please connect manually");
                    }
                }
                else
                {
                    // If somehow already connected, just clear the flag
                    _isInitializing = false;
                }
            };
            startupTimer.Start();
        }

        private void ModulationDepthUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
                _modulationController.OnModulationParameterChanged();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Disconnect on window closing
            if (isConnected)
            {
                LogMessage("Application closing - performing safe disconnect...");

                try
                {
                    // If we're in harmonic mode, disable it before disconnecting
                    // to ensure the device is left in a standard state
                    string currentWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                    if (currentWaveform.Contains("HARM"))
                    {
                        LogMessage("Disabling harmonic mode before closing...");
                        rigolDG2072.SendCommand($":SOUR{activeChannel}:HARM:STAT OFF");
                        System.Threading.Thread.Sleep(50);
                    }


                    // Ensure harmonics are disabled when closing
                    if (isConnected && _harmonicsManager != null)
                    {

                        //string currentWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                        string deviceWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                        if (currentWaveform.Contains("HARM"))
                        {
                            LogMessage("Disabling harmonic mode before closing...");
                            _harmonicsManager.SetHarmonicState(false);
                            System.Threading.Thread.Sleep(50);
                        }
                    }


                    // Set the channel back to a standard waveform (sine) for safety
                    LogMessage("Setting device to standard sine wave state...");
                    rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:SIN 1000,1,0,0");
                    System.Threading.Thread.Sleep(50);

                    // Disconnect from the instrument
                    bool result = rigolDG2072.Disconnect();
                    if (result)
                    {
                        isConnected = false;
                        LogMessage("Successfully disconnected from Rigol DG2072");

                        // Send any additional commands needed to put the device back into local control
                        // Note: For most VISA devices, disconnecting naturally returns local control
                        // but if specific commands are needed, add them here
                    }
                    else
                    {
                        LogMessage("WARNING: Disconnect may not have completed successfully");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error during disconnect: {ex.Message}");
                }

                // Final safety - if we have direct access to the VISA handle, ensure it's closed
                try
                {
                    if (instrumentHandle != IntPtr.Zero)
                    {
                        LogMessage("Closing VISA handle directly...");
                        // Assuming there's a close method in your VISA interface
                        // Add the appropriate code for your specific implementation
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error closing VISA handle: {ex.Message}");
                }
            }
        }

        // Helper method to find parent control
        private T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;

            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        private void ConnectionToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                // Set initializing flag before attempting connection
                _isInitializing = true;

                // Try to connect
                if (Connect())
                {
                    // Update UI to show connected state
                    ConnectionStatusTextBlock.Text = "Connected";
                    ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;
                    ConnectionToggleButton.Content = "Disconnect";
                    IdentifyButton.IsEnabled = true;
                    RefreshButton.IsEnabled = true;
                    UpdateAutoRefreshState(true);

                    // Initialize frequency/period converter after successful connection
                    if (frequencyPeriodConverter == null)
                    {
                        InitializeFrequencyPeriodConverter();
                    }

                    if (_modulationController != null)
                    {
                        _modulationController.InitializeUI();
                    }

                    if (_sweepController != null)
                    {
                        _sweepController.InitializeUI();
                    }

                    // Clear the initialization flag now that we're connected
                    _isInitializing = false;

                    // Refresh settings from the instrument
                    RefreshInstrumentSettings();
                    LogMessage("Manual connection successful");
                }
                else
                {
                    // Connection failed - clear the flag and show error state
                    _isInitializing = false;

                    MessageBox.Show("Failed to connect to instrument. Please check connections and try again.",
                                  "Connection Failed",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                    LogMessage("Manual connection failed");
                }
            }
            else
            {
                // Try to disconnect
                if (Disconnect())
                {
                    // Update UI to show disconnected state
                    ConnectionStatusTextBlock.Text = "Disconnected";
                    ConnectionStatusTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                    ConnectionToggleButton.Content = "Connect";
                    IdentifyButton.IsEnabled = false;
                    RefreshButton.IsEnabled = false;
                    UpdateAutoRefreshState(false);

                    LogMessage("Manually disconnected from instrument");
                }
                else
                {
                    MessageBox.Show("Failed to disconnect properly. You may need to restart the application.",
                                  "Disconnect Failed",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Warning);
                    LogMessage("Manual disconnect failed");
                }
            }
        }

        private void IdentifyButton_Click(object sender, RoutedEventArgs e)
        {
            string response = rigolDG2072.GetIdentification();
            if (!string.IsNullOrEmpty(response))
            {
                MessageBox.Show($"Instrument Identification:\n{response}", "Instrument Identification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            CommandLogTextBox.Clear();
        }

        private void PrimaryFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isConnected) return;
            if (!double.TryParse(PrimaryFrequencyTextBox.Text, out double frequency)) return;

            // Use a timer to debounce rapid changes
            if (_primaryFrequencyUpdateTimer == null)
            {
                _primaryFrequencyUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _primaryFrequencyUpdateTimer.Tick += (s, args) =>
                {
                    _primaryFrequencyUpdateTimer.Stop();
                    if (double.TryParse(PrimaryFrequencyTextBox.Text, out double freq))
                    {
                        // Only update if in dual tone mode
                        if (((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper() == "DUAL TONE")
                        {
                            // Update SecondaryFrequencyTextBox if auto-sync is enabled
                            if (SynchronizeFrequenciesCheckBox.IsChecked == true)
                            {
                                double secondaryFreq = freq * frequencyRatio;
                                SecondaryFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(secondaryFreq);
                            }

                            // Apply the dual tone settings
                            if (dualToneGen != null)
                                dualToneGen.ApplyDualToneParameters();
                        }
                    }
                };
            }

            _primaryFrequencyUpdateTimer.Stop();
            _primaryFrequencyUpdateTimer.Start();
        }

        private void PrimaryFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected) return;

            if (double.TryParse(PrimaryFrequencyTextBox.Text, out double frequency))
            {
                // Only update if in dual tone mode
                if (((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper() == "DUAL TONE")
                {
                    if (dualToneGen != null)
                        dualToneGen.ApplyDualToneParameters();
                }
            }
        }

        #endregion

    #region Channel Basic Controls Event Handlers

        private void ChannelOutputToggle_Click(object sender, RoutedEventArgs e)
        {
            if (ChannelOutputToggle.IsChecked == true)
            {
                rigolDG2072.SetOutput(activeChannel, true);
                ChannelOutputToggle.Content = "ON";
            }
            else
            {
                rigolDG2072.SetOutput(activeChannel, false);
                ChannelOutputToggle.Content = "OFF";
            }
        }

        private void ChannelOffsetUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected) return;

            if (double.TryParse(ChannelOffsetTextBox.Text, out double offset))
            {
                ApplyOffset(offset);
            }
        }

        private void ApplyOffset(double offset)
        {
            if (!isConnected) return;

            try
            {
                string unit = UnitConversionUtility.GetOffsetUnit(ChannelOffsetUnitComboBox);
                double multiplier = UnitConversionUtility.GetOffsetMultiplier(unit);
                double actualOffset = offset * multiplier;

                rigolDG2072.SetOffset(activeChannel, actualOffset);
                LogMessage($"Set CH{activeChannel} offset to {offset} {unit} ({actualOffset} V)");
            }
            catch (Exception ex)
            {
                LogMessage($"Error applying offset: {ex.Message}");
            }
        }

        private void AdjustOffsetAndUnit(TextBox textBox, ComboBox unitComboBox)
        {
            if (!double.TryParse(textBox.Text, out double value))
                return;

            string currentUnit = ((ComboBoxItem)unitComboBox.SelectedItem).Content.ToString();

            // Convert to appropriate unit
            if (Math.Abs(value) < 0.1 && currentUnit == "V")
            {
                // Switch to mV for small values
                value *= 1000.0;
                for (int i = 0; i < unitComboBox.Items.Count; i++)
                {
                    ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                    if (item != null && item.Content.ToString() == "mV")
                    {
                        unitComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }
            else if (Math.Abs(value) > 1000.0 && currentUnit == "mV")
            {
                // Switch to V for large values
                value /= 1000.0;
                for (int i = 0; i < unitComboBox.Items.Count; i++)
                {
                    ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                    if (item != null && item.Content.ToString() == "V")
                    {
                        unitComboBox.SelectedIndex = i;
                        break;
                    }
                }
            }

            // Format with minimum decimals
            textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value, 1); // 1 decimal for frequency
        }

        private void ChannelWaveformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            // Get previous waveform type (if available)
            string previousWaveform = string.Empty;
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ComboBoxItem)
            {
                previousWaveform = ((ComboBoxItem)e.RemovedItems[0]).Content.ToString().ToUpper();
            }

            string waveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper();
            string selectedArbWaveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString();


            if (_modulationController != null)
            {
                _modulationController.UpdateModulationAvailability(waveform);
            }

            // If leaving Dual Tone mode, set frequency to F1 instead of center
            if (previousWaveform == "DUAL TONE" && waveform != "DUAL TONE")
            {
                try
                {
                    // If we can get center and offset, we can calculate F1
                    if (CenterFrequencyTextBox != null && OffsetFrequencyTextBox != null &&
                        double.TryParse(CenterFrequencyTextBox.Text, out double center) &&
                        double.TryParse(OffsetFrequencyTextBox.Text, out double offset))
                    {
                        // Calculate F1 using: F1 = Center - Offset
                        double f1 = center - offset;

                        // Update the frequency setting
                        ChannelFrequencyTextBox.Text = f1.ToString();

                        // Log for debugging
                        LogMessage($"Switching from Dual Tone: Set frequency to F1 ({f1} Hz) instead of center ({center} Hz)");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting F1 frequency when switching from Dual Tone: {ex.Message}");
                }
            }

            // Special handling for HARMONIC waveform
            if (waveform == "HARMONIC")
            {
                LogMessage("Switching to HARMONIC waveform mode...");

                try
                {
                    // Then enable harmonic mode
                    rigolDG2072.SendCommand($":SOUR{activeChannel}:HARM:STAT ON");
                    System.Threading.Thread.Sleep(100);

                    // Get current parameters
                    double frequency = rigolDG2072.GetFrequency(activeChannel);
                    double amplitude = rigolDG2072.GetAmplitude(activeChannel);

                    // Use the current SINE settings
                    rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:SIN {frequency},{amplitude},{0},{0}");
                    System.Threading.Thread.Sleep(100);  // Give device time to process

                    // Initialize harmonicController if needed
                    if (harmonicController == null)
                    {
                        harmonicController = new ChannelHarmonicController(rigolDG2072, activeChannel);
                        LogMessage($"Initialized harmonic controller for Channel {activeChannel}");
                    }

                    // Reset all harmonic values to zero for a clean starting state
                    ResetHarmonicValues();

                    // Set harmonic toggle to ENABLED in UI
                    HarmonicsToggle.IsChecked = true;
                    HarmonicsToggle.Content = "ENABLED";

                    // Make sure harmonic UI elements are enabled
                    SetHarmonicUIElementsState(true);

                    // Verify the waveform was set correctly
                    string verifyWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                    LogMessage($"Verification - Device waveform now: {verifyWaveform}");

                    LogMessage("Ready for harmonic editing. Changes will be applied automatically.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting {waveform} mode: {ex.Message}");
                    MessageBox.Show($"Error setting {waveform} mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Special handling for DUAL TONE waveform
            else if (waveform == "DUAL TONE")
            {
                LogMessage("Switching to dual tone waveform mode...");
                try
                {
                    // Get current parameters
                    double frequency = rigolDG2072.GetFrequency(activeChannel);
                    double amplitude = rigolDG2072.GetAmplitude(activeChannel);
                    double offset = rigolDG2072.GetOffset(activeChannel);
                    double phase = rigolDG2072.GetPhase(activeChannel);

                    // Set secondary frequency based on primary frequency and ratio
                    if (SecondaryFrequencyTextBox != null)
                    {
                        double secondaryFreq = frequency * frequencyRatio;
                        SecondaryFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(secondaryFreq);
                    }

                    // Update UI for direct frequency mode by default
                    if (DirectFrequencyMode != null)
                    {
                        DirectFrequencyMode.IsChecked = true;
                    }

                    if (CenterOffsetMode != null)
                    {
                        CenterOffsetMode.IsChecked = false;
                    }

                    // First set sine wave as base, then change to dual tone
                    rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:SIN {frequency},{amplitude},{offset},{phase}");
                    System.Threading.Thread.Sleep(100);

                    // Set the waveform on the device
                    rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:DUAL {frequency},{amplitude},{offset},{phase}");
                    System.Threading.Thread.Sleep(100);

                    // Apply dual tone parameters 
                    ApplyDualToneParameters();

                    // Verify the waveform was set correctly
                    string verifyWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                    LogMessage($"Verification - Device waveform now: {verifyWaveform}");

                    LogMessage("Dual tone mode ready. Adjust frequencies as needed.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting {waveform} mode: {ex.Message}");
                    MessageBox.Show($"Error setting {waveform} mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (waveform == "ARBITRARY WAVEFORM")
            {
                LogMessage("Switching to arbitrary waveform mode...");
                try
                {
                    // Get current parameters - just like the sine wave implementation
                    double frequency = rigolDG2072.GetFrequency(activeChannel);
                    double amplitude = rigolDG2072.GetAmplitude(activeChannel);
                    double offset = rigolDG2072.GetOffset(activeChannel);
                    double phase = rigolDG2072.GetPhase(activeChannel);

                    // Set the waveform on the device using current parameters instead of hardcoded values
                    rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:USER {frequency},{amplitude},{offset},{phase}");
                    System.Threading.Thread.Sleep(100);

                    // Initialize the arbitrary waveform UI
                    if (ArbitraryWaveformComboBox.SelectedItem == null)
                    {
                        // Refresh the arbitrary waveform settings 
                        RefreshArbitraryWaveformSettings(activeChannel);
                    }

                    // Only access SelectedItem if it's not null and is the correct type
                    if (ArbitraryWaveformComboBox.SelectedItem != null && ArbitraryWaveformComboBox.SelectedItem is ComboBoxItem)
                    {
                        string selectedWaveformName = ((ComboBoxItem)ArbitraryWaveformComboBox.SelectedItem).Content.ToString();
                        LogMessage($"Arbitrary waveform mode ready. Selected: {selectedWaveformName}");
                    }
                    else
                    {
                        LogMessage("Arbitrary waveform mode ready. Select a waveform type and click Apply to set it.");
                    }

                    // Verify the waveform was set correctly
                    string verifyWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                    LogMessage($"Verification - Device waveform now: {verifyWaveform}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting {waveform} mode: {ex.Message}");
                    MessageBox.Show($"Error setting {waveform} mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else if (waveform == "DC" && dcGenerator != null)
            {
                LogMessage("Switching to DC waveform mode...");
                try
                {
                    // Delegate to the DC generator
                    dcGenerator.ApplyParameters();

                    // Verify the waveform was set correctly
                    string verifyWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                    LogMessage($"Verification - Device waveform now: {verifyWaveform}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error setting {waveform} mode: {ex.Message}");
                    MessageBox.Show($"Error setting {waveform} mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // Add the sine generator code here
            else if (waveform == "SINE" && sineGenerator != null)
            {
                sineGenerator.ApplyParameters(); // New way using base class method
            }

            // Add the Noise generator code here
            else if (waveform == "NOISE" && noiseGenerator != null)
            {
                // Delegate to the noise generator
                noiseGenerator.ApplyParameters();
            }
            else
            {
                // For all other waveforms, use the standard APPLY command to ensure it's set properly
                try
                {
                    // Get current parameters
                    double frequency = rigolDG2072.GetFrequency(activeChannel);
                    double amplitude = rigolDG2072.GetAmplitude(activeChannel);
                    double offset = rigolDG2072.GetOffset(activeChannel);
                    double phase = rigolDG2072.GetPhase(activeChannel);

                    // Map upper case waveform name to the correct apply command
                    string applyWaveform = waveform;
                    if (waveform == "SINE") applyWaveform = "SIN";
                    if (waveform == "SQUARE") applyWaveform = "SQU";
                    if (waveform == "PULSE") applyWaveform = "PULS";
                    if (waveform == "NOISE") applyWaveform = "NOIS";

                    // Special handling for NOISE waveform
                    if (waveform == "NOISE")
                    {
                        // NOISE waveform doesn't have frequency and phase parameters
                        rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:NOIS {amplitude},{offset}");
                        LogMessage($"Applied NOISE waveform with parameters: amp={amplitude}Vpp, offset={offset}V");
                    }
                    else
                    {
                        // For all other standard waveforms, use all parameters
                        rigolDG2072.SendCommand($":SOURCE{activeChannel}:APPLY:{applyWaveform} {frequency},{amplitude},{offset},{phase}");
                        LogMessage($"Applied {waveform} waveform with parameters: f={frequency}Hz, amp={amplitude}Vpp, offset={offset}V, phase={phase}°");
                    }

                    // Verify the waveform was set correctly
                    string verifyWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                    LogMessage($"Verification - Device waveform now: {verifyWaveform}");
                }
                catch (Exception ex)
                {
                    LogMessage($"Error applying waveform {waveform}: {ex.Message}");
                    // Fallback to basic method if apply command fails
                    rigolDG2072.SetWaveform(activeChannel, waveform);
                }
            }

            // Update waveform-specific UI elements visibility
            UpdateWaveformSpecificControls(waveform);
        }

        private void ChannelFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (frequencyPeriodConverter?.IsFrequencyMode == true)
            {
                frequencyPeriodConverter.UpdateCalculatedValue();
            }

            if (!isConnected) return;
            if (!double.TryParse(ChannelFrequencyTextBox.Text, out double frequency)) return;

            // Don't update the instrument for every keystroke
            // Instead, use a timer to delay the update
            if (_frequencyUpdateTimer == null)
            {
                _frequencyUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _frequencyUpdateTimer.Tick += (s, args) =>
                {
                    _frequencyUpdateTimer.Stop();
                    if (double.TryParse(ChannelFrequencyTextBox.Text, out double freq))
                    {
                        ApplyFrequency(freq);

                        // Add this line to update the period when frequency changes
                        if (_frequencyModeActive)
                        {
                            string currentWaveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper();
                            if (currentWaveform == "PULSE")
                                UpdateCalculatedRateValue();
                        }
                    }
                };
            }

            _frequencyUpdateTimer.Stop();
            _frequencyUpdateTimer.Start();

            // Check if this is in DUAL TONE mode with Center/Offset mode active
            if (isConnected &&
                ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper() == "DUAL TONE")
            {
                if (CenterOffsetMode.IsChecked == true)
                {
                    // In Center/Offset mode, update offset calculations directly
                    // This will trigger the UpdateFrequenciesFromCenterOffset method
                    if (dualToneGen != null)
                    {
                        dualToneGen.UpdateFrequenciesFromCenterOffset();
                    }
                }
                else if (SynchronizeFrequenciesCheckBox.IsChecked == true &&
                        double.TryParse(ChannelFrequencyTextBox.Text, out double primaryFreq))
                {
                    // Update secondary frequency to maintain the ratio
                    double secondaryFreq = primaryFreq * frequencyRatio;
                    SecondaryFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(secondaryFreq);
                }
            }
        }

        private void ChannelAmplitudeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isConnected) return;
            if (!double.TryParse(ChannelAmplitudeTextBox.Text, out double amplitude)) return;

            if (_amplitudeUpdateTimer == null)
            {
                _amplitudeUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _amplitudeUpdateTimer.Tick += (s, args) =>
                {
                    _amplitudeUpdateTimer.Stop();
                    if (double.TryParse(ChannelAmplitudeTextBox.Text, out double amp))
                    {
                        ApplyAmplitude(amp);
                    }
                };
            }

            _amplitudeUpdateTimer.Stop();
            _amplitudeUpdateTimer.Start();
        }

        private void ChannelOffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isConnected) return;
            if (!double.TryParse(ChannelOffsetTextBox.Text, out double offset)) return;

            if (_offsetUpdateTimer == null)
            {
                _offsetUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _offsetUpdateTimer.Tick += (s, args) =>
                {
                    _offsetUpdateTimer.Stop();
                    if (double.TryParse(ChannelOffsetTextBox.Text, out double off))
                    {
                        ApplyOffset(off);
                    }
                };
            }

            _offsetUpdateTimer.Stop();
            _offsetUpdateTimer.Start();
        }

        private void ChannelPhaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isConnected) return;
            if (!double.TryParse(ChannelPhaseTextBox.Text, out double phase)) return;

            if (_phaseUpdateTimer == null)
            {
                _phaseUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _phaseUpdateTimer.Tick += (s, args) =>
                {
                    _phaseUpdateTimer.Stop();
                    if (double.TryParse(ChannelPhaseTextBox.Text, out double ph))
                    {
                        rigolDG2072.SetPhase(activeChannel, ph);
                    }
                };
            }

            _phaseUpdateTimer.Stop();
            _phaseUpdateTimer.Start();
        }

        private void ChannelSymmetryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (rampGenerator != null)
                rampGenerator.OnSymmetryTextChanged(sender, e);
        }

        private void ChannelSymmetryTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (rampGenerator != null)
                rampGenerator.OnSymmetryLostFocus(sender, e);
        }

        private void ChannelApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            try
            {
                // Get current waveform type
                string waveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper();

                // Handle waveform-specific parameters first
                if (waveform == "PULSE" && pulseGenerator != null)
                {
                    pulseGenerator.ApplyParameters();
                }
                else
                {
                    // Apply common parameters
                    ApplyCommonParameters();

                    // Apply waveform-specific parameters
                    ApplyWaveformSpecificParameters(waveform);
                }

                // Refresh the UI to show the actual values from the device
                RefreshChannelSettings();
                LogMessage($"Applied settings to CH{activeChannel}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error applying settings: {ex.Message}");
                MessageBox.Show($"Error applying settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyCommonParameters()
        {
            // First adjust the values and units for better display
            if (_frequencyModeActive)
            {
                AdjustFrequencyAndUnit(ChannelFrequencyTextBox, ChannelFrequencyUnitComboBox);
            }
            else if (pulseGenerator != null)
            {
                pulseGenerator.AdjustPulseTimeAndUnit(PulsePeriod, PulsePeriodUnitComboBox);
            }

            AdjustAmplitudeAndUnit(ChannelAmplitudeTextBox, ChannelAmplitudeUnitComboBox);

            // Apply frequency or period
            ApplyFrequencyOrPeriod();

            // Apply amplitude, offset, phase
            ApplyAmplitudeOffsetPhase();
        }

        private void ApplyFrequencyOrPeriod()
        {
            if (_frequencyModeActive)
            {
                if (!double.TryParse(ChannelFrequencyTextBox.Text, out double frequency))
                {
                    LogMessage($"Invalid frequency value for CH{activeChannel}");
                    return;
                }

                string freqUnit = UnitConversionUtility.GetFrequencyUnit(ChannelFrequencyUnitComboBox);
                double freqMultiplier = UnitConversionUtility.GetFrequencyMultiplier(freqUnit);
                double actualFrequency = frequency * freqMultiplier;

                rigolDG2072.SetFrequency(activeChannel, actualFrequency);
            }
            else
            {
                if (!double.TryParse(PulsePeriod.Text, out double period))
                {
                    LogMessage($"Invalid period value for CH{activeChannel}");
                    return;
                }

                string periodUnit = UnitConversionUtility.GetPeriodUnit(PulsePeriodUnitComboBox);
                double periodMultiplier = UnitConversionUtility.GetPeriodMultiplier(periodUnit);
                double actualPeriod = period * periodMultiplier;

                rigolDG2072.SendCommand($"SOURCE{activeChannel}:PERiod {actualPeriod}");
            }
        }

        private void ApplyAmplitudeOffsetPhase()
        {
            if (!double.TryParse(ChannelAmplitudeTextBox.Text, out double amplitude) ||
                !double.TryParse(ChannelOffsetTextBox.Text, out double offset) ||
                !double.TryParse(ChannelPhaseTextBox.Text, out double phase))
            {
                LogMessage("Invalid amplitude, offset, or phase values");
                return;
            }

            string ampUnit = UnitConversionUtility.GetAmplitudeUnit(ChannelAmplitudeUnitComboBox);
            double ampMultiplier = UnitConversionUtility.GetAmplitudeMultiplier(ampUnit);
            double actualAmplitude = amplitude * ampMultiplier;

            rigolDG2072.SetAmplitude(activeChannel, actualAmplitude);
            rigolDG2072.SetOffset(activeChannel, offset);
            rigolDG2072.SetPhase(activeChannel, phase);
        }

        private void ApplyWaveformSpecificParameters(string waveform)
        {
            switch (waveform)
            {
                case "RAMP":
                    if (rampGenerator != null)
                        rampGenerator.ApplyParameters();
                    break;

                case "SQUARE":
                    if (squareGenerator != null)
                        squareGenerator.ApplySquareParameters();
                    break;
            }
        }

        #endregion

    #region To Period - To Frequency Toggle Handlers


        private void FrequencyPeriodModeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (frequencyPeriodConverter != null)
            {
                frequencyPeriodConverter.ToggleMode();

                // Synchronize with local state
                _frequencyModeActive = frequencyPeriodConverter.IsFrequencyMode;

                // Update dependent components
                if (pulseGenerator != null)
                {
                    pulseGenerator.SetFrequencyMode(_frequencyModeActive);
                }

                // Update UI based on current waveform
                string currentWaveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem)?.Content.ToString().ToUpper();
                UpdateWaveformSpecificControls(currentWaveform);
            }
            else
            {
                // Fallback if converter not initialized
                LogMessage("Warning: FrequencyPeriodConverter not initialized");
            }
        }



        private void ChannelPulsePeriodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(PulsePeriod.Text, out double period))
            {
                PulsePeriod.Text = UnitConversionUtility.FormatWithMinimumDecimals(period);

                // Apply the period to the device if in period mode
                if (!_frequencyModeActive)
                {
                    string periodUnit = UnitConversionUtility.GetPeriodUnit(PulsePeriodUnitComboBox);
                    double periodInSeconds = period * UnitConversionUtility.GetPeriodMultiplier(periodUnit);

                    // Convert to frequency for the device
                    if (periodInSeconds > 0)
                    {
                        double freqInHz = 1.0 / periodInSeconds;
                        rigolDG2072.SetFrequency(activeChannel, freqInHz);
                        LogMessage($"Set frequency via period: {freqInHz} Hz (Period: {period} {periodUnit})");
                    }
                }
            }
        }

        private void PulsePeriodUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected || _frequencyModeActive) return;

            if (double.TryParse(PulsePeriod.Text, out double period))
            {
                string periodUnit = UnitConversionUtility.GetPeriodUnit(PulsePeriodUnitComboBox);
                double periodInSeconds = period * UnitConversionUtility.GetPeriodMultiplier(periodUnit);

                if (periodInSeconds > 0)
                {
                    double freqInHz = 1.0 / periodInSeconds;
                    rigolDG2072.SetFrequency(activeChannel, freqInHz);
                    LogMessage($"Set frequency via period: {freqInHz} Hz (Period: {period} {periodUnit})");

                    // Update frequency display
                    UpdateCalculatedRateValue();
                }
            }
        }




        #endregion

    #region Unit Selection Handlers


        private void ChannelFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected) return;

            if (double.TryParse(ChannelFrequencyTextBox.Text, out double frequency))
            {
                ApplyFrequency(frequency);
                if (_frequencyModeActive && ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper() == "PULSE")
                    UpdateCalculatedRateValue();
            }
        }

        private void ChannelAmplitudeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isConnected) return;

            if (double.TryParse(ChannelAmplitudeTextBox.Text, out double amplitude))
            {
                ApplyAmplitude(amplitude);
            }
        }

        #endregion

    #region Apply Value Methods

        // Make sure frequency changes use the direct frequency command
        private void ApplyFrequency(double frequency)
        {
            if (!isConnected) return;

            try
            {
                // Only use this direct frequency method in Frequency mode
                if (_frequencyModeActive)
                {
                    string unit = UnitConversionUtility.GetFrequencyUnit(ChannelFrequencyUnitComboBox);
                    double actualFrequency = frequency * UnitConversionUtility.GetFrequencyMultiplier(unit);

                    // Send frequency command directly
                    rigolDG2072.SetFrequency(activeChannel, actualFrequency);
                    LogMessage($"Set CH{activeChannel} frequency to {frequency} {unit} ({actualFrequency} Hz)");

                    // Update period display but don't send to device
                    if (((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper() == "PULSE")
                        UpdateCalculatedRateValue();
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error applying frequency: {ex.Message}");
            }
        }

        private void ApplyAmplitude(double amplitude)
        {
            string unit = UnitConversionUtility.GetAmplitudeUnit(ChannelAmplitudeUnitComboBox);
            double multiplier = UnitConversionUtility.GetAmplitudeMultiplier(unit);
            double actualAmplitude = amplitude * multiplier;

            rigolDG2072.SetAmplitude(activeChannel, actualAmplitude);
            LogMessage($"Set CH{activeChannel} amplitude to {amplitude} {unit} ({actualAmplitude} Vpp)");

            // If harmonics are enabled and we're on the harmonic waveform, update harmonics for the new fundamental amplitude
            if (isConnected && _harmonicsUIController != null &&
                ChannelWaveformComboBox.SelectedItem != null &&
                ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper() == "HARMONIC")
            {
                // Update harmonics for the new amplitude
                _harmonicsUIController.UpdateHarmonicsForFundamentalChange(actualAmplitude);
            }
        }

        #endregion

    #region UI Formatting and Adjustment Methods

        // Update the UpdateWaveformSpecificControls method to use the pulse generator
        private void UpdateWaveformSpecificControls(string waveformType)
        {
            // Convert to uppercase for case-insensitive comparison
            string waveform = waveformType.ToUpper();

            // Handle both "USER" and "ARBITRARY WAVEFORM" as the same thing
            if (waveform == "USER" || waveform == "ARBITRARY WAVEFORM")
                waveform = "ARBITRARY WAVEFORMS";

            bool isPulse = (waveform == "PULSE");
            bool isNoise = (waveform == "NOISE");
            bool isDualTone = (waveform == "DUAL TONE");
            bool isHarmonic = (waveform == "HARMONIC");
            bool isDC = (waveform == "DC");
            bool isArbitraryWaveform = (waveform == "ARBITRARY WAVEFORMS");

            // Use the pulse generator to update pulse controls
            if (pulseGenerator != null)
            {
                pulseGenerator.UpdatePulseControls(isPulse);
            }

            // Handle symmetry control visibility (for Ramp waveform)
            if (SymmetryDockPanel != null)
            {
                SymmetryDockPanel.Visibility = (waveform == "RAMP") ? Visibility.Visible : Visibility.Collapsed;
            }

            // Handle duty cycle control visibility (for Square waveform only)
            if (DutyCycleDockPanel != null)
            {
                DutyCycleDockPanel.Visibility = (waveform == "SQUARE") ? Visibility.Visible : Visibility.Collapsed;
            }

            // Handle frequency/period control visibility (hide for Noise)
            if (FrequencyDockPanel != null && PeriodDockPanel != null)
            {
                bool showFrequency = !isNoise && !isDC && !isDualTone;

                if (_frequencyModeActive)
                {
                    FrequencyDockPanel.Visibility = showFrequency ? Visibility.Visible : Visibility.Collapsed;
                    PeriodDockPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    FrequencyDockPanel.Visibility = Visibility.Collapsed;
                    PeriodDockPanel.Visibility = showFrequency ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            // Handle pulse-specific controls
            if (PulseWidthDockPanel != null &&
                PulseRiseTimeDockPanel != null &&
                PulseFallTimeDockPanel != null)
            {
                Visibility pulseVisibility = isPulse ? Visibility.Visible : Visibility.Collapsed;

                // Show/hide pulse-specific controls
                PulseWidthDockPanel.Visibility = pulseVisibility;
                PulseRiseTimeDockPanel.Visibility = pulseVisibility;
                PulseFallTimeDockPanel.Visibility = pulseVisibility;

                // REMOVED: References to PulseRateModeDockPanel - we use the general toggle instead
            }

            // Handle the GENERAL frequency/period toggle for ALL waveforms
            if (FrequencyPeriodModeToggle != null)
            {
                bool showToggle = !isNoise && !isDC && !isDualTone;
                FrequencyPeriodModeToggle.Visibility = showToggle ? Visibility.Visible : Visibility.Collapsed;
                FrequencyPeriodModeToggle.IsEnabled = showToggle;

                LogMessage($"Setting FrequencyPeriodModeToggle visibility to {(showToggle ? "Visible" : "Collapsed")} for waveform {waveform}");
            }

            // Handle phase control visibility - hide for noise waveform
            if (PhaseDockPanel != null)
            {
                PhaseDockPanel.Visibility = (isNoise || isDC) ? Visibility.Collapsed : Visibility.Visible;
            }

            // Handle fall time control visibility - hide for noise waveform (already handled for non-pulse waveforms)
            if (PulseFallTimeDockPanel != null)
            {
                // For noise waveform, always hide
                if (isNoise)
                {
                    PulseFallTimeDockPanel.Visibility = Visibility.Collapsed;
                    LogMessage($"Setting PulseFallTimeDockPanel visibility to Collapsed for noise waveform");
                }
                // For non-noise, show only if it's pulse waveform
                else
                {
                    PulseFallTimeDockPanel.Visibility = isPulse ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            // Hide Apply Settings button for dual tone waveform
            if (ChannelApplyButton != null)
            {
                // Hide button for dual tone
                if (isDualTone)
                {
                    // Hide main frequency controls when in dual tone mode
                    if (FrequencyDockPanel != null)
                        FrequencyDockPanel.Visibility = Visibility.Collapsed;

                    if (PeriodDockPanel != null)
                        PeriodDockPanel.Visibility = Visibility.Collapsed;

                    // Make sure dual tone controls are visible
                    if (DualToneGroupBox != null)
                        DualToneGroupBox.Visibility = Visibility.Visible;
                }
            }

            // Handle dual tone-specific controls
            if (DualToneGroupBox != null)
            {
                DualToneGroupBox.Visibility = isDualTone ? Visibility.Visible : Visibility.Collapsed;

                // Also manage the secondary frequency controls' visibility
                if (SecondaryFrequencyDockPanel != null)
                {
                    SecondaryFrequencyDockPanel.Visibility = isDualTone ? Visibility.Visible : Visibility.Collapsed;
                }

                // Reference to FrequencyRatioComboBox instead of FrequencyRatioDockPanel
                if (FrequencyRatioComboBox != null)
                {
                    FrequencyRatioComboBox.Visibility = isDualTone ? Visibility.Visible : Visibility.Collapsed;
                }

                if (CenterOffsetPanel != null && DirectFrequencyPanel != null)
                {
                    // Respect the current dual tone mode
                    if (CenterOffsetMode != null && DirectFrequencyMode != null)
                    {
                        bool isDirectMode = DirectFrequencyMode.IsChecked == true;
                        DirectFrequencyPanel.Visibility = (isDualTone && isDirectMode) ? Visibility.Visible : Visibility.Collapsed;
                        CenterOffsetPanel.Visibility = (isDualTone && !isDirectMode) ? Visibility.Visible : Visibility.Collapsed;
                    }
                }

                if (SynchronizeFrequenciesCheckBox != null)
                {
                    SynchronizeFrequenciesCheckBox.Visibility = isDualTone ? Visibility.Visible : Visibility.Collapsed;
                }
            }

            // Handle harmonic-specific controls
            if (HarmonicsGroupBox != null)
            {
                HarmonicsGroupBox.Visibility = isHarmonic ? Visibility.Visible : Visibility.Collapsed;
            }

            // Handle arbitrary waveform controls visibility
            if (ArbitraryWaveformGroupBox != null)
            {
                ArbitraryWaveformGroupBox.Visibility = isArbitraryWaveform ? Visibility.Visible : Visibility.Collapsed;

                // Hide the Apply button since changes are auto-applied
                if (ApplyArbitraryWaveformButton != null)
                {
                    ApplyArbitraryWaveformButton.Visibility = Visibility.Collapsed;
                }
            }

            // DC group box visibility
            if (DCGroupBox != null)
            {
                DCGroupBox.Visibility = isDC ? Visibility.Visible : Visibility.Collapsed;
            }

            // Hide all standard controls and show only DC controls for DC waveform
            if (isDC)
            {
                // Hide frequency/period, amplitude, phase controls
                if (FrequencyDockPanel != null) FrequencyDockPanel.Visibility = Visibility.Collapsed;
                if (PeriodDockPanel != null) PeriodDockPanel.Visibility = Visibility.Collapsed;

                // Toggle buttons should be hidden for DC
                if (FrequencyPeriodModeToggle != null) FrequencyPeriodModeToggle.Visibility = Visibility.Collapsed;

                // Amplitude should be hidden for DC
                if (FindVisualParent<DockPanel>(ChannelAmplitudeTextBox) != null)
                    FindVisualParent<DockPanel>(ChannelAmplitudeTextBox).Visibility = Visibility.Collapsed;

                // Phase should be hidden for DC
                if (PhaseDockPanel != null) PhaseDockPanel.Visibility = Visibility.Collapsed;

                // Offset control is redundant with DC voltage - hide it
                if (FindVisualParent<DockPanel>(ChannelOffsetTextBox) != null)
                    FindVisualParent<DockPanel>(ChannelOffsetTextBox).Visibility = Visibility.Collapsed;
            }
            else
            {
                // Show amplitude control for non-DC waveforms
                if (FindVisualParent<DockPanel>(ChannelAmplitudeTextBox) != null)
                    FindVisualParent<DockPanel>(ChannelAmplitudeTextBox).Visibility = Visibility.Visible;

                // Show offset control for non-DC waveforms
                if (FindVisualParent<DockPanel>(ChannelOffsetTextBox) != null)
                    FindVisualParent<DockPanel>(ChannelOffsetTextBox).Visibility = Visibility.Visible;
            }
        }

        private void ChannelDutyCycleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (squareGenerator != null)
                squareGenerator.OnDutyCycleTextChanged(sender, e);
        }

        private void ChannelDutyCycleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (squareGenerator != null)
                squareGenerator.OnDutyCycleLostFocus(sender, e);
        }

        /// <summary>
        /// Updates UI elements based on the selected frequency/period mode
        /// </summary>
        private void UpdateFrequencyPeriodMode()
        {
            if (!isConnected) return;

            string currentWaveform = ((ComboBoxItem)ChannelWaveformComboBox.SelectedItem).Content.ToString().ToUpper();
            bool isNoise = (currentWaveform == "NOISE");
            bool isDualTone = (currentWaveform == "DUAL TONE");
            bool isDC = (currentWaveform == "DC");

            // Hide frequency/period controls for waveforms that don't use them
            if (isDualTone || isDC)
            {
                FrequencyDockPanel.Visibility = Visibility.Collapsed;
                PeriodDockPanel.Visibility = Visibility.Collapsed;
                return;
            }

            // For all other waveforms (including PULSE)
            if (_frequencyModeActive)
            {
                // Show frequency, hide period
                FrequencyDockPanel.Visibility = isNoise ? Visibility.Collapsed : Visibility.Visible;
                PeriodDockPanel.Visibility = Visibility.Collapsed;

                // Update the main frequency label
                if (FrequencyDockPanel != null && FrequencyDockPanel.Children.Count > 0)
                {
                    var label = FrequencyDockPanel.Children[0] as Label;
                    if (label != null) label.Content = "Frequency:";
                }
            }
            else
            {
                // Show period, hide frequency
                FrequencyDockPanel.Visibility = Visibility.Collapsed;
                PeriodDockPanel.Visibility = isNoise ? Visibility.Collapsed : Visibility.Visible;

                // Update the period label
                if (PeriodDockPanel != null && PeriodDockPanel.Children.Count > 0)
                {
                    var label = PeriodDockPanel.Children[0] as Label;
                    if (label != null) label.Content = "Period:";
                }
            }
        }

        /// <summary>
        /// Calculates and updates the complementary value (frequency or period) based on current mode
        /// </summary>
        private void UpdateCalculatedRateValue()
        {
            if (!isConnected) return;

            try
            {
                if (_frequencyModeActive)
                {
                    // We're showing frequency, so calculate and store the period value
                    if (double.TryParse(ChannelFrequencyTextBox.Text, out double frequency))
                    {
                        string freqUnit = UnitConversionUtility.GetFrequencyUnit(ChannelFrequencyUnitComboBox);
                        double freqInHz = frequency * UnitConversionUtility.GetFrequencyMultiplier(freqUnit);

                        if (freqInHz > 0)
                        {
                            double periodInSeconds = 1.0 / freqInHz;

                            // Auto-select appropriate time unit
                            string[] timeUnits = { "ps", "ns", "µs", "ms", "s" };
                            string bestUnit = "s";
                            double bestValue = periodInSeconds;

                            // Find the best unit for display
                            foreach (string unit in timeUnits)
                            {
                                double testValue = UnitConversionUtility.ConvertFromPicoSeconds(periodInSeconds * 1e12, unit);
                                if (testValue >= 0.1 && testValue < 1000)
                                {
                                    bestUnit = unit;
                                    bestValue = testValue;
                                    break;
                                }
                            }

                            // Update the period textbox and unit
                            PulsePeriod.Text = UnitConversionUtility.FormatWithMinimumDecimals(bestValue);

                            // Update the unit combo box
                            for (int i = 0; i < PulsePeriodUnitComboBox.Items.Count; i++)
                            {
                                ComboBoxItem item = PulsePeriodUnitComboBox.Items[i] as ComboBoxItem;
                                if (item != null && item.Content.ToString() == bestUnit)
                                {
                                    PulsePeriodUnitComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // We're showing period, so calculate and store the frequency value
                    if (double.TryParse(PulsePeriod.Text, out double period))
                    {
                        string periodUnit = UnitConversionUtility.GetPeriodUnit(PulsePeriodUnitComboBox);
                        double periodInSeconds = period * UnitConversionUtility.GetPeriodMultiplier(periodUnit);

                        if (periodInSeconds > 0)
                        {
                            double freqInHz = 1.0 / periodInSeconds;

                            // Auto-select appropriate frequency unit
                            string[] freqUnits = { "µHz", "mHz", "Hz", "kHz", "MHz" };
                            string bestUnit = "Hz";
                            double bestValue = freqInHz;

                            // Find the best unit for display
                            foreach (string unit in freqUnits)
                            {
                                double testValue = UnitConversionUtility.ConvertFromMicroHz(freqInHz * 1e6, unit);
                                if (testValue >= 0.1 && testValue < 1000)
                                {
                                    bestUnit = unit;
                                    bestValue = testValue;
                                    break;
                                }
                            }

                            // Update the frequency textbox and unit
                            ChannelFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(bestValue);

                            // Update the unit combo box
                            for (int i = 0; i < ChannelFrequencyUnitComboBox.Items.Count; i++)
                            {
                                ComboBoxItem item = ChannelFrequencyUnitComboBox.Items[i] as ComboBoxItem;
                                if (item != null && item.Content.ToString() == bestUnit)
                                {
                                    ChannelFrequencyUnitComboBox.SelectedIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error updating calculated rate value: {ex.Message}");
            }
        }


        private void PulsePeriod_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isConnected || _frequencyModeActive) return;

            if (!double.TryParse(PulsePeriod.Text, out double period)) return;

            // Debounce the update
            if (_pulsePeriodUpdateTimer == null)
            {
                _pulsePeriodUpdateTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(500)
                };
                _pulsePeriodUpdateTimer.Tick += (s, args) =>
                {
                    _pulsePeriodUpdateTimer.Stop();

                    // Apply the period by converting to frequency
                    if (double.TryParse(PulsePeriod.Text, out double p))
                    {
                        string periodUnit = UnitConversionUtility.GetPeriodUnit(PulsePeriodUnitComboBox);
                        double periodInSeconds = p * UnitConversionUtility.GetPeriodMultiplier(periodUnit);

                        if (periodInSeconds > 0)
                        {
                            double freqInHz = 1.0 / periodInSeconds;
                            rigolDG2072.SetFrequency(activeChannel, freqInHz);
                            LogMessage($"Set frequency via period: {freqInHz} Hz (Period: {p} {periodUnit})");

                            // Update the frequency display
                            UpdateCalculatedRateValue();
                        }
                    }
                };
            }

            _pulsePeriodUpdateTimer.Stop();
            _pulsePeriodUpdateTimer.Start();
        }


        private void PulsePeriod_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(PulsePeriod.Text, out double period))
            {
                PulsePeriod.Text = UnitConversionUtility.FormatWithMinimumDecimals(period);

                // Apply the period to the device if in period mode
                if (!_frequencyModeActive)
                {
                    string periodUnit = UnitConversionUtility.GetPeriodUnit(PulsePeriodUnitComboBox);
                    double periodInSeconds = period * UnitConversionUtility.GetPeriodMultiplier(periodUnit);

                    // Convert to frequency for the device
                    if (periodInSeconds > 0)
                    {
                        double freqInHz = 1.0 / periodInSeconds;
                        rigolDG2072.SetFrequency(activeChannel, freqInHz);
                        LogMessage($"Set frequency via period: {freqInHz} Hz (Period: {period} {periodUnit})");
                    }
                }
            }
        }



        private void AdjustFrequencyAndUnit(TextBox textBox, ComboBox unitComboBox)
        {
            if (!double.TryParse(textBox.Text, out double value))
                return;

            string currentUnit = ((ComboBoxItem)unitComboBox.SelectedItem).Content.ToString();

            // Convert current value to µHz to maintain precision
            double microHzValue = UnitConversionUtility.ConvertToMicroHz(value, currentUnit);

            // Define units in order from smallest to largest
            string[] frequencyUnits = { "µHz", "mHz", "Hz", "kHz", "MHz" };

            // Map the combo box selection to our array index
            int unitIndex = 0;
            for (int i = 0; i < frequencyUnits.Length; i++)
            {
                if (frequencyUnits[i] == currentUnit)
                {
                    unitIndex = i;
                    break;
                }
            }

            // Get the current value in the selected unit
            double displayValue = UnitConversionUtility.ConvertFromMicroHz(microHzValue, frequencyUnits[unitIndex]);

            // Handle values that are too large (> 9999)
            while (displayValue > 9999 && unitIndex < frequencyUnits.Length - 1)
            {
                unitIndex++;
                displayValue = UnitConversionUtility.ConvertFromMicroHz(microHzValue, frequencyUnits[unitIndex]);
            }

            // Handle values that are too small (< 0.1)
            while (displayValue < 0.1 && unitIndex > 0)
            {
                unitIndex--;
                displayValue = UnitConversionUtility.ConvertFromMicroHz(microHzValue, frequencyUnits[unitIndex]);
            }

            // Update the textbox with formatted value
            textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);

            // Find and select the unit in the combo box
            for (int i = 0; i < unitComboBox.Items.Count; i++)
            {
                ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                if (item != null && item.Content.ToString() == frequencyUnits[unitIndex])
                {
                    unitComboBox.SelectedIndex = i;
                    break;
                }
            }
        }

        private void AdjustAmplitudeAndUnit(TextBox textBox, ComboBox unitComboBox)
        {
            if (!double.TryParse(textBox.Text, out double value))
                return;

            string currentUnit = ((ComboBoxItem)unitComboBox.SelectedItem).Content.ToString();
            bool isRms = currentUnit.Contains("rms");

            string[] amplitudeUnits;
            if (isRms)
            {
                amplitudeUnits = new[] { "mVrms", "Vrms" };
            }
            else
            {
                amplitudeUnits = new[] { "mVpp", "Vpp" };
            }

            // Map the current unit to our array index
            int unitIndex = currentUnit.StartsWith("m") ? 0 : 1;

            // Handle values that are too large (> 9999)
            if (value > 9999 && unitIndex == 0)
            {
                value /= 1000;
                unitIndex = 1;
            }

            // Handle values that are too small (< 0.1)
            if (value < 0.1 && unitIndex == 1)
            {
                value *= 1000;
                unitIndex = 0;
            }

            // Find and select the unit in the combo box
            for (int i = 0; i < unitComboBox.Items.Count; i++)
            {
                ComboBoxItem item = unitComboBox.Items[i] as ComboBoxItem;
                if (item != null && item.Content.ToString() == amplitudeUnits[unitIndex])
                {
                    unitComboBox.SelectedIndex = i;
                    break;
                }
            }
            textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value, 1); // 1 decimal for frequency
        }

        #endregion

    #region TextBox LostFocus Handlers

        private void ChannelFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ChannelFrequencyTextBox.Text, out double frequency))
            {
                string currentUnit = UnitConversionUtility.GetFrequencyUnit(ChannelFrequencyUnitComboBox);
                double microHzValue = UnitConversionUtility.ConvertToMicroHz(frequency, currentUnit);
                double displayValue = UnitConversionUtility.ConvertFromMicroHz(microHzValue, currentUnit);
                ChannelFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
            }
        }

        private void ChannelAmplitudeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ChannelAmplitudeTextBox.Text, out double amplitude))
            {
                ChannelAmplitudeTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(amplitude);
            }
        }

        private void ChannelOffsetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ChannelOffsetTextBox.Text, out double offset))
            {
                AdjustOffsetAndUnit(ChannelOffsetTextBox, ChannelOffsetUnitComboBox);
            }
        }

        private void ChannelPhaseTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ChannelPhaseTextBox.Text, out double phase))
            {
                ChannelPhaseTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(phase);
            }
        }

        #endregion

    #region DualTone Event Handlers

        private void DualToneModeChanged(object sender, RoutedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnDualToneModeChanged(sender, e);
        }

        private void SynchronizeFrequenciesCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnSynchronizeFrequenciesCheckChanged(sender, e);
        }

        private void FrequencyRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnFrequencyRatioSelectionChanged(sender, e);
        }

        private void SecondaryFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnSecondaryFrequencyTextChanged(sender, e);
        }

        private void SecondaryFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnSecondaryFrequencyLostFocus(sender, e);
        }

        private void SecondaryFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnSecondaryFrequencyUnitChanged(sender, e);
        }

        private void CenterFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnCenterFrequencyTextChanged(sender, e);
        }

        private void CenterFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnCenterFrequencyLostFocus(sender, e);
        }

        private void CenterFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnCenterFrequencyUnitChanged(sender, e);
        }

        private void OffsetFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnOffsetFrequencyTextChanged(sender, e);
        }

        private void OffsetFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnOffsetFrequencyLostFocus(sender, e);
        }

        private void OffsetFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dualToneGen != null)
                dualToneGen.OnOffsetFrequencyUnitChanged(sender, e);
        }


        private void RefreshDualToneSettings(int channel)
        {
            if (dualToneGen != null)
            {
                dualToneGen.ActiveChannel = channel;
                dualToneGen.RefreshDualToneSettings();
            }
        }

        // This method delegates to the DualToneGen instance
        private void ApplyDualToneParameters()
        {
            if (dualToneGen != null)
                dualToneGen.ApplyDualToneParameters();
        }

        #endregion

    #region Harmonics Event Handlers

        #endregion

    #region Arbitrary Waveform Handlers

        // Event handler for parameter text changes
        private void ArbitraryParamTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnParameterTextChanged(sender, e);
        }

        // Event handler for parameter text box lost focus
        private void ArbitraryParamTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnParameterLostFocus(sender, e);
        }

        // Event handler for when the arbitrary waveform category changes
        private void ArbitraryWaveformCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnCategorySelectionChanged(sender, e);
        }

        // Update the arbitrary waveform info text when a waveform is selected
        private void ArbitraryWaveformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnWaveformSelectionChanged(sender, e);
        }

        private void RefreshArbitraryWaveformSettings(int channel)
        {
            if (arbitraryWaveformGen != null)
            {
                arbitraryWaveformGen.ActiveChannel = channel;
                arbitraryWaveformGen.RefreshParameters(); // Now uses the base class method
            }
        }

        private void ApplyArbitraryWaveformButton_Click(object sender, RoutedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.ApplyParameters(); // Now uses the base class method
        }

        #endregion

    #region DC Mode Controls

        // All DC mode-specific methods and handlers
        // Add this method to handle DC voltage changes
        private void DCVoltageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCVoltageTextChanged(sender, e);
        }

        private void DCVoltageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCVoltageLostFocus(sender, e);
        }

        private void DCVoltageUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCVoltageUnitChanged(sender, e);
        }

        private void DCImpedanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCImpedanceChanged(sender, e);
        }




        #endregion


        // Add this new region for Modulation Event Handlers:
        #region Modulation Event Handlers

        private void ModulationToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_modulationController == null) return;

            if (_modulationController.IsEnabled)
            {
                _modulationController.DisableModulation();
            }
            else
            {
                _modulationController.EnableModulation();
            }

            // Update status
            UpdateModulationStatus();
        }

        private void ModulationTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
                _modulationController.OnModulationTypeChanged();
        }

        private void ModulatingWaveformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
                _modulationController.OnModulationParameterChanged();
        }

        private void ModulationFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            // Debounce with timer if needed
            if (_modulationController != null)
                _modulationController.OnModulationParameterChanged();
        }

        private void ModulationFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ModulationFrequencyTextBox.Text, out double freq))
            {
                ModulationFrequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(freq);
            }
        }

        private void ModulationFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
                _modulationController.OnModulationParameterChanged();
        }

        private void ModulationDepthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
                _modulationController.OnModulationParameterChanged();
        }

        private void ModulationDepthTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (double.TryParse(ModulationDepthTextBox.Text, out double depth))
            {
                ModulationDepthTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(depth);
            }
        }

        // Update the UpdateModulationStatus method in MainWindow.xaml.cs:

        private void UpdateModulationStatus()
        {
            if (ModulationStatusTextBlock != null && _modulationController != null)
            {
                if (_modulationController.IsEnabled)
                {
                    // Get current modulation type
                    string modType = "Unknown";
                    if (ModulationTypeComboBox?.SelectedItem != null)
                    {
                        modType = ((ComboBoxItem)ModulationTypeComboBox.SelectedItem).Content.ToString();
                    }

                    // Check for DSSC if AM
                    string dsscStatus = "";
                    if (modType == "AM" && DSSCCheckBox != null && DSSCCheckBox.IsChecked == true)
                    {
                        dsscStatus = " (DSSC ON)";
                    }

                    ModulationStatusTextBlock.Text = $"Modulation: {modType}{dsscStatus} Enabled";
                    ModulationStatusTextBlock.Foreground = System.Windows.Media.Brushes.Green;

                    // Also update the button
                    if (ModulationToggleButton != null)
                    {
                        ModulationToggleButton.Background = System.Windows.Media.Brushes.LightCoral;
                    }
                }
                else
                {
                    ModulationStatusTextBlock.Text = "Modulation: Disabled";
                    ModulationStatusTextBlock.Foreground = System.Windows.Media.Brushes.Black;

                    // Also update the button
                    if (ModulationToggleButton != null)
                    {
                        ModulationToggleButton.Background = System.Windows.Media.Brushes.LightGreen;
                    }
                }
            }
        }

        private void DSSCCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
            {
                _modulationController.OnDSSCChanged(true);
                LogMessage("DSSC (Double-Sideband Suppressed Carrier) enabled");
            }
        }

        private void DSSCCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || !isConnected) return;

            if (_modulationController != null)
            {
                _modulationController.OnDSSCChanged(false);
                LogMessage("DSSC (Double-Sideband Suppressed Carrier) disabled");
            }
        }


        private void EnableModulationUIOnly()
        {
            if (_modulationController != null)
            {
                _modulationController.EnableModulationUIOnly();

                // Update the modulation status display
                UpdateModulationStatus();
            }
        }




        #endregion


        #region Sweep

        // ===================================================================
        // ADDITIONS TO YOUR MAIN MainWindow.xaml.cs FILE
        // ===================================================================

        // Add this field declaration with your other private fields:
       // private DG2072_USB_Control.Sweep.SweepController _sweepController;

        // ===================================================================
        // Add this ONE event handler method:
        // ===================================================================

        private void SweepToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sweepController == null) return;

            if (_sweepController.IsEnabled)
            {
                _sweepController.DisableSweep();
            }
            else
            {
                _sweepController.EnableSweep();
            }
        }

        #endregion
    }
}


