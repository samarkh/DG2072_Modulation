using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Continuous.ArbitraryWaveform;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Arbitrary Waveform Fields

        // Arbitrary Waveform Generator Management
        private ArbitraryWaveformGen arbitraryWaveformGen;

        #endregion

        #region Arbitrary Waveform Handlers

        // Event handler for parameter text changes
        private void ArbitraryParamTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnParameterTextChanged(sender, e);
        }

        // Event handler for parameter text box lost focus
        private void ArbitraryParamTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnParameterLostFocus(sender, e);
        }

        // Event handler for when the arbitrary waveform category changes
        private void ArbitraryWaveformCategoryComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnCategorySelectionChanged(sender, e);
        }

        // Update the arbitrary waveform info text when a waveform is selected
        private void ArbitraryWaveformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.OnWaveformSelectionChanged(sender, e);
        }

        private void RefreshArbitraryWaveformSettings(int channel)
        {
            if (arbitraryWaveformGen != null)
            {
                arbitraryWaveformGen.ActiveChannel = channel;
                arbitraryWaveformGen.RefreshParameters(); // Now uses the base class method
            }
        }

        private void ApplyArbitraryWaveformButton_Click(object sender, RoutedEventArgs e)
        {
            if (arbitraryWaveformGen != null)
                arbitraryWaveformGen.ApplyParameters(); // Now uses the base class method
        }

        #endregion
    }
}