using System;
using System.Windows;
using DG2072_USB_Control.Continuous.Noise;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Noise Generator Fields

        // Noise generator management
        private NoiseGen noiseGenerator;

        #endregion

        #region Noise Generator Methods

        // Noise waveform only uses amplitude and offset controls 
        // that are shared with other waveforms, so no specific event handlers are needed here.
        // All noise-specific functionality is encapsulated in the NoiseGen class.

        #endregion
    }
}