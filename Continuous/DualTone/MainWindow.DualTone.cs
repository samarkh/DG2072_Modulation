using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Continuous.DualTone;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region DualTone Fields

        // Dual Tone management
        private DualToneGen dualToneGen;
        private double frequencyRatio = 2.0; // Default frequency ratio (harmonic)

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
    }
}