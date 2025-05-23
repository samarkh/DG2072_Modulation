using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Modulation.FM
{
    public class FMModulation
    {
        private readonly RigolDG2072 _device;
        private readonly int _channel;

        public FMModulation(RigolDG2072 device, int channel)
        {
            _device = device;
            _channel = channel;
        }

        /// <summary>
        /// Applies FM modulation using internal source with specified frequency, deviation, and waveform shape.
        /// </summary>
        /// <param name="modFreqHz">Modulation frequency in Hz</param>
        /// <param name="deviationHz">Frequency deviation in Hz</param>
        /// <param name="modWaveform">Modulation waveform shape (e.g., SINusoid, SQUare, TRIangle, etc.)</param>
        public void Apply(double modFreqHz, double deviationHz, string modWaveform = "SINusoid")
        {
            _device.SendCommand($":SOUR{_channel}:MOD:TYPE FM");
            _device.SendCommand($":SOUR{_channel}:MOD:SOUR INT");
            _device.SendCommand($":SOUR{_channel}:FM:INT:FREQ {modFreqHz}");
            _device.SendCommand($":SOUR{_channel}:FM:INT:FUNC {modWaveform.ToUpper()}");
            _device.SendCommand($":SOUR{_channel}:FM:DEVI {deviationHz}");
            _device.SendCommand($":SOUR{_channel}:MOD:STAT ON");
        }

        /// <summary>
        /// Disables FM modulation on the current channel.
        /// </summary>
        public void Disable()
        {
            _device.SendCommand($":SOUR{_channel}:MOD:STAT OFF");
        }
    }
}