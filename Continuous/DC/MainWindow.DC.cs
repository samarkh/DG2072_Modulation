using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Continuous.DC;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        // DC-specific fields
        private DCGen dcGenerator;
        private DockPanel DCVoltageDockPanel;

        #region DC Initialization

        /// <summary>
        /// Initialize DC mode controls and handlers
        /// </summary>
        private void InitializeDCControls()
        {
            // Find and store reference to DC panel
            DCVoltageDockPanel = FindVisualParent<DockPanel>(DCVoltageTextBox);

            // Initialize the DC generator
            dcGenerator = new DCGen(rigolDG2072, activeChannel, this);
            dcGenerator.LogEvent += (s, message) => LogMessage(message);
        }

        #endregion

        #region DC Mode Controls

        /// <summary>
        /// Event handler for DC voltage changes
        /// </summary>
        private void DCVoltageTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCVoltageTextChanged(sender, e);
        }

        /// <summary>
        /// Event handler for DC voltage text box losing focus
        /// </summary>
        private void DCVoltageTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCVoltageLostFocus(sender, e);
        }

        /// <summary>
        /// Event handler for DC voltage unit selection changes
        /// </summary>
        private void DCVoltageUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCVoltageUnitChanged(sender, e);
        }

        /// <summary>
        /// Event handler for DC impedance selection changes
        /// </summary>
        private void DCImpedanceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dcGenerator != null)
                dcGenerator.OnDCImpedanceChanged(sender, e);
        }

        #endregion

        #region DC UI Management

        /// <summary>
        /// Show or hide DC-specific controls based on the selected waveform
        /// </summary>
        private void UpdateDCControls(bool isDCWaveform)
        {
            if (DCGroupBox != null)
            {
                DCGroupBox.Visibility = isDCWaveform ? Visibility.Visible : Visibility.Collapsed;
            }

            if (isDCWaveform)
            {
                // Hide frequency/period, amplitude, phase controls
                if (FrequencyDockPanel != null) FrequencyDockPanel.Visibility = Visibility.Collapsed;
                if (PeriodDockPanel != null) PeriodDockPanel.Visibility = Visibility.Collapsed;

                // Toggle buttons should be hidden for DC
                if (FrequencyPeriodModeToggle != null) FrequencyPeriodModeToggle.Visibility = Visibility.Collapsed;
                if (PulseRateModeToggle != null) PulseRateModeToggle.Visibility = Visibility.Collapsed;

                // Amplitude should be hidden for DC
                if (FindVisualParent<DockPanel>(ChannelAmplitudeTextBox) != null)
                    FindVisualParent<DockPanel>(ChannelAmplitudeTextBox).Visibility = Visibility.Collapsed;

                // Phase should be hidden for DC
                if (PhaseDockPanel != null) PhaseDockPanel.Visibility = Visibility.Collapsed;

                // Offset control is redundant with DC voltage - hide it
                if (FindVisualParent<DockPanel>(ChannelOffsetTextBox) != null)
                    FindVisualParent<DockPanel>(ChannelOffsetTextBox).Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Handle waveform switch to DC mode
        /// </summary>
        private void SwitchToDCWaveform()
        {
            LogMessage("Switching to DC waveform mode...");
            try
            {
                // Delegate to the DC generator
                if (dcGenerator != null)
                {
                    dcGenerator.ApplyParameters();
                }

                // Verify the waveform was set correctly
                string verifyWaveform = rigolDG2072.SendQuery($":SOUR{activeChannel}:FUNC?").Trim().ToUpper();
                LogMessage($"Verification - Device waveform now: {verifyWaveform}");
            }
            catch (Exception ex)
            {
                LogMessage($"Error setting DC mode: {ex.Message}");
                MessageBox.Show($"Error setting DC mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refresh DC settings from the device
        /// </summary>
        private void RefreshDCSettings()
        {
            if (dcGenerator != null)
            {
                dcGenerator.RefreshParameters();
            }
        }

        #endregion
    }
}