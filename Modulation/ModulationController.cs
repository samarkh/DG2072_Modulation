using DG2072_USB_Control.Services;
//using Services;
using System;
using System.Windows.Controls;

namespace Modulation
{
    public class ModulationController
    {
        private readonly Action<string> _sendToRigol;

        public ModulationController(Action<string> sendToRigol)
        {
            _sendToRigol = sendToRigol;
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
    }
}
