using DG2072_USB_Control.Continuous.Harmonics;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using static DG2072_USB_Control.RigolDG2072;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Harmonics Fields

        private ChannelHarmonicController harmonicController;

        // Harmonics management
        private HarmonicsManager _harmonicsManager;
        private HarmonicsUIController _harmonicsUIController;

        #endregion

        #region Harmonics Event Handlers

        private void AmplitudeModeChanged(object sender, RoutedEventArgs e)
        {
            // The event is defined in the XAML file to be handled by MainWindow
            // Forward it to the HarmonicsUIController
            if (_harmonicsUIController != null)
            {
                // Use reflection to call the method since it's private in HarmonicsUIController
                typeof(HarmonicsUIController).GetMethod("AmplitudeModeChanged",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    .Invoke(_harmonicsUIController, new object[] { sender, e });
            }
        }

        private void HarmonicAmplitudeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_harmonicsUIController != null)
            {
                ComboBox comboBox = sender as ComboBox;
                if (comboBox != null && int.TryParse(comboBox.Tag.ToString(), out int harmonicNumber))
                {
                    // Forward the event to the harmonics controller
                    _harmonicsUIController.GetType().GetMethod("HarmonicAmplitudeUnitComboBox_SelectionChanged",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        ?.Invoke(_harmonicsUIController, new object[] { sender, e, harmonicNumber });
                }
            }
        }

        private void HarmonicCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_harmonicsUIController != null)
            {
                // Extract the harmonic number from the Tag property
                CheckBox checkBox = sender as CheckBox;
                if (checkBox != null && int.TryParse(checkBox.Tag.ToString(), out int harmonicNumber))
                {
                    // Use reflection to call the method
                    typeof(HarmonicsUIController).GetMethod("HarmonicCheckBox_Changed",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(_harmonicsUIController, new object[] { sender, e, harmonicNumber });
                }
            }
        }

        private void HarmonicAmplitudeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_harmonicsUIController != null)
            {
                // Extract the harmonic number from the Tag property
                TextBox textBox = sender as TextBox;
                if (textBox != null && int.TryParse(textBox.Tag.ToString(), out int harmonicNumber))
                {
                    // Use reflection to call the method
                    typeof(HarmonicsUIController).GetMethod("HarmonicAmplitudeTextBox_LostFocus",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(_harmonicsUIController, new object[] { sender, e, harmonicNumber });
                }
            }
        }

        private void HarmonicPhaseTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_harmonicsUIController != null)
            {
                // Extract the harmonic number from the Tag property
                TextBox textBox = sender as TextBox;
                if (textBox != null && int.TryParse(textBox.Tag.ToString(), out int harmonicNumber))
                {
                    // Use reflection to call the method
                    typeof(HarmonicsUIController).GetMethod("HarmonicPhaseTextBox_LostFocus",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                        .Invoke(_harmonicsUIController, new object[] { sender, e, harmonicNumber });
                }
            }
        }

        private void RefreshHarmonicSettings()
        {
            if (_harmonicsUIController != null)
            {
                _harmonicsUIController.RefreshHarmonicSettings();
            }
        }

        private void ResetHarmonicValues()
        {
            _harmonicsUIController?.ResetHarmonicValues();
        }

        private void SetHarmonicUIElementsState(bool enabled)
        {
            _harmonicsUIController?.SetHarmonicUIElementsState(enabled);
        }

        private void HarmonicsToggle_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected) return;

            bool isEnabled = HarmonicsToggle.IsChecked == true;
            HarmonicsToggle.Content = isEnabled ? "ENABLED" : "DISABLED";

            try
            {
                if (isEnabled)
                {
                    // Enable harmonics on the device
                    rigolDG2072.SetHarmonicState(activeChannel, true);
                    LogMessage($"Harmonics enabled for Channel {activeChannel}");
                }
                else
                {
                    // Disable harmonics on the device
                    rigolDG2072.SetHarmonicState(activeChannel, false);
                    LogMessage($"Harmonics disabled for Channel {activeChannel}");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error toggling harmonics: {ex.Message}");
            }
        }

        #endregion
    }
}