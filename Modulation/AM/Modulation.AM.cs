using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Modulation.AM
{
    public class AMModulation
    {
        private readonly RigolDG2072 _device;
        private readonly int _channel;

        public AMModulation(RigolDG2072 device, int channel)
        {
            _device = device;
            _channel = channel;
        }

        /// <summary>
        /// Applies AM modulation with specified internal frequency and depth.
        /// </summary>
        /// <param name="frequencyHz">Modulation frequency in Hz</param>
        /// <param name="depthPercent">Modulation depth in percent (0 to 120%)</param>
        public void Apply(double frequencyHz, double depthPercent)
        {
            _device.SendCommand($":SOUR{_channel}:MOD:TYPE AM");
            _device.SendCommand($":SOUR{_channel}:MOD:SOUR INT");
            _device.SendCommand($":SOUR{_channel}:AM:INT:FREQ {frequencyHz}");
            _device.SendCommand($":SOUR{_channel}:AM:DEPT {depthPercent}");
            _device.SendCommand($":SOUR{_channel}:MOD:STAT ON");
        }

        /// <summary>
        /// Disables modulation on the current channel.
        /// </summary>
        public void Disable()
        {
            _device.SendCommand($":SOUR{_channel}:MOD:STAT OFF");
        }
    }
}


