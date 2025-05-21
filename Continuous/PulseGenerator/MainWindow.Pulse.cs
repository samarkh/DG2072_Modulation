using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Continuous.PulseGenerator;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Pulse Generator Fields

        // pulse generator management
        private PulseGen pulseGenerator;

        #endregion

        #region Pulse Parameter Handling

        private void ChannelPulsePeriodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulsePeriodTextChanged(sender, e);
        }

        private void ChannelPulsePeriodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulsePeriodLostFocus(sender, e);
        }

        private void PulsePeriodUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulsePeriodUnitChanged(sender, e);
        }

        private void PulseRateModeToggle_Click(object sender, RoutedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseRateModeToggleClicked(sender, e);
        }

        private void ChannelPulseRiseTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseRiseTimeTextChanged(sender, e);
        }

        private void ChannelPulseRiseTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseRiseTimeLostFocus(sender, e);
        }

        private void PulseRiseTimeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseRiseTimeUnitChanged(sender, e);
        }

        private void ChannelPulseWidthTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseWidthTextChanged(sender, e);
        }

        private void ChannelPulseWidthTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseWidthLostFocus(sender, e);
        }

        private void PulseWidthUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseWidthUnitChanged(sender, e);
        }

        private void ChannelPulseFallTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseFallTimeTextChanged(sender, e);
        }

        private void ChannelPulseFallTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseFallTimeLostFocus(sender, e);
        }

        private void PulseFallTimeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pulseGenerator != null)
                pulseGenerator.OnPulseFallTimeUnitChanged(sender, e);
        }

        #endregion
    }
}