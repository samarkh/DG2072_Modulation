using DG2072_USB_Control.Services;
//using Services;
using System;
using System.Windows.Controls;

namespace Modulation
{
    public class ModulationController
    {
        private readonly Action<string> _sendToRigol;
        private readonly Action<string> _updateModulationFrequencyDisplay;
        private readonly Action<string> _updateModulationDepthDisplay;

        //public ModulationController(Action<string> sendToRigol, Action<string> updateModulationFrequencyDisplay, Action<string> updateModulationDepthDisplay)
        public ModulationController(
             Action<string> sendToRigol,
             Action<string> updateModulationTypeDisplay,
             Action<string> updateModulationDepthDisplay,
             Action<string> updateModulationFrequencyDisplay)
        {
            _sendToRigol = sendToRigol;
            _updateModulationFrequencyDisplay = updateModulationFrequencyDisplay;
            _updateModulationDepthDisplay = updateModulationDepthDisplay;
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
