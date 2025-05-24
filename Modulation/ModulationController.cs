using DG2072_USB_Control.Services;
//using Services;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;

namespace Modulation
{
    public class ModulationController
    {
        private readonly Action<string> _sendToRigol;
        private readonly Func<string, string> _queryRigol;
        private readonly Action<string> _updateModulationTypeDisplay;
        private readonly Action<string> _updateModulationDepthDisplay;
        private readonly Action<string> _updateModulationFrequencyDisplay;
        private ComboBox CarrierWaveformComboBox;

        public ModulationController(
                Action<string> sendToRigol,
                Func<string, string> queryRigol,
                Action<string> updateModulationTypeDisplay,
                Action<string> updateModulationDepthDisplay,
                Action<string> updateModulationFrequencyDisplay,
                ComboBox carrierWaveformComboBox)
        {
            _sendToRigol = sendToRigol;
            _queryRigol = queryRigol;
            _updateModulationTypeDisplay = updateModulationTypeDisplay;
            _updateModulationDepthDisplay = updateModulationDepthDisplay;
            _updateModulationFrequencyDisplay = updateModulationFrequencyDisplay;
            CarrierWaveformComboBox = carrierWaveformComboBox;
        }



        private void CarrierWaveformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CarrierWaveformComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string waveform = selectedItem.Content.ToString().ToUpper();

                // Only send if valid selection
                if (!string.IsNullOrWhiteSpace(waveform))
                {
                    _sendToRigol($"SOURCE1:FUNC {waveform}");
                    Debug.WriteLine($"Carrier waveform set to {waveform}");
                }
            }
        }


        public void InitializeAndLoadModulation()
        {
            InitializeModulation();
            LoadCurrentModulationSettings();
        }


        public void InitializeModulation()
        {
            try
            {
                // Set valid carrier for modulation
                _sendToRigol("SOURCE1:FUNC SIN");
                Thread.Sleep(100);

                // Enable AM modulation cleanly (no need to use MODulation:TYPE/STATE)
                _sendToRigol(":SOUR1:AM:STAT ON");
                Thread.Sleep(300);

                Debug.WriteLine("Modulation initialized to AM.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error initializing modulation: " + ex.Message);
            }
        }


        public void LoadCurrentModulationSettings()
        {
            try
            {
                string modState = _queryRigol("SOURCE1:MODulation:STATE?");

                if (modState == null)
                {
                    Debug.WriteLine("Could not determine modulation state. Aborting.");
                    return;
                }

                if (modState.Trim().ToUpper() != "ON")
                {
                    Debug.WriteLine("Modulation is OFF — enabling and setting default to AM.");

                    _sendToRigol("SOURCE1:MODulation:TYPE AM");
                    Thread.Sleep(50);
                    _sendToRigol("SOURCE1:MODulation:STATE ON");
                    Thread.Sleep(100); // allow the device to catch up
                }

                string type = _queryRigol("SOURCE1:MODulation:TYPE?");
                Thread.Sleep(50);
                string freq = _queryRigol("SOURCE1:MODulation:FREQuency?");
                Thread.Sleep(50);
                string depth = _queryRigol("SOURCE1:MODulation:DEPTh?");

                if (!string.IsNullOrWhiteSpace(type)) _updateModulationTypeDisplay(type);
                if (!string.IsNullOrWhiteSpace(freq)) _updateModulationFrequencyDisplay(freq);
                if (!string.IsNullOrWhiteSpace(depth)) _updateModulationDepthDisplay(depth);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception in LoadCurrentModulationSettings: " + ex.Message);
            }
        }


        public void DisableCurrentModulation()
        {
            try
            {
                string modType = _queryRigol("SOURCE1:MODulation:TYPE?")?.Trim().ToUpper();

                if (string.IsNullOrWhiteSpace(modType))
                {
                    Debug.WriteLine("Could not determine modulation type to disable.");
                    return;
                }

                string command = modType switch
                {
                    "AM" => ":SOUR1:AM:STAT OFF",
                    "FM" => ":SOUR1:FM:STAT OFF",
                    "PM" => ":SOUR1:PM:STAT OFF",
                    "FSK" => ":SOUR1:FSK:STAT OFF",
                    "PSK" => ":SOUR1:PSK:STAT OFF",
                    "PWM" => ":SOUR1:PWM:STAT OFF",
                    _ => null
                };

                if (command != null)
                {
                    _sendToRigol(command);
                    Debug.WriteLine($"Disabled modulation type: {modType}");
                }
                else
                {
                    Debug.WriteLine($"Unknown modulation type: {modType}. Nothing disabled.");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error disabling current modulation: " + ex.Message);
            }
        }


        public void ApplyModulationSettings(string modulationType, string depth, string frequency, string freqUnit)
        {
            if (!double.TryParse(depth, out double parsedDepth) || !double.TryParse(frequency, out double parsedFreq))
                return;

            string formattedDepth = UnitConversionUtility.FormatWithMinimumDecimals(parsedDepth);
            string formattedFreq = UnitConversionUtility.FormatWithMinimumDecimals(parsedFreq);

            string cmdType = $":SOUR1:MOD:TYPE {modulationType}";
            string cmdDepth = $":SOUR1:{modulationType}:DEPTH {formattedDepth}";
            string cmdFreq = $":SOUR1:{modulationType}:FREQ {formattedFreq}{freqUnit}";

            _sendToRigol(cmdType);
            _sendToRigol(cmdDepth);
            _sendToRigol(cmdFreq);
        }

        public void OnModulationTypeChanged(double currentOutputFrequency, string unit)
        {
            // Convert frequency to match unit (Hz/kHz/MHz)
            double displayFreq = currentOutputFrequency;
            switch (unit)
            {
                case "kHz": displayFreq /= 1e3; break;
                case "MHz": displayFreq /= 1e6; break;
            }

            // Format values and update modulation settings (via public properties, bindings, or call-backs)
            _updateModulationFrequencyDisplay(UnitConversionUtility.FormatWithMinimumDecimals(displayFreq));
            _updateModulationDepthDisplay("25.0");
        }




    }
}
