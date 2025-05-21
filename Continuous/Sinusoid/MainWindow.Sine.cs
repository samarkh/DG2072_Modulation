using System;
using System.Windows;
using DG2072_USB_Control.Continuous.Sinusoid;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Sine Generator Fields

        //Sinusoid generator management
        private SinGen sineGenerator;

        #endregion

        #region Sine Generator Methods

        // No specific event handlers needed for the sine wave generator
        // since it only uses the standard frequency, amplitude, offset, and phase controls
        // that are already handled in the main window.

        // All specific functionality is encapsulated in the SinGen class.

        #endregion
    }
}