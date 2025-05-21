using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Continuous.Ramp;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Ramp Generator Fields

        // Ramp generator management
        private RampGen rampGenerator;
        private DockPanel SymmetryDockPanel;

        #endregion

        #region Ramp Generator Event Handlers

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

        #endregion
    }
}