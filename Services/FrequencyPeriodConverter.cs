// Services/FrequencyPeriodConverter.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DG2072_USB_Control.Services
{
    public class FrequencyPeriodConverter
    {
        private bool _isFrequencyMode = true;

        // UI References
        private readonly DockPanel _frequencyDockPanel;
        private readonly DockPanel _periodDockPanel;
        private readonly TextBox _frequencyTextBox;
        private readonly TextBox _periodTextBox;
        private readonly ComboBox _frequencyUnitComboBox;
        private readonly ComboBox _periodUnitComboBox;
        private readonly ToggleButton _modeToggle;

        public event EventHandler<string> LogEvent;

        public bool IsFrequencyMode => _isFrequencyMode;

        public FrequencyPeriodConverter(
            DockPanel frequencyPanel,
            DockPanel periodPanel,
            TextBox frequencyTextBox,
            TextBox periodTextBox,
            ComboBox frequencyUnitComboBox,
            ComboBox periodUnitComboBox,
            ToggleButton modeToggle)
        {
            _frequencyDockPanel = frequencyPanel;
            _periodDockPanel = periodPanel;
            _frequencyTextBox = frequencyTextBox;
            _periodTextBox = periodTextBox;
            _frequencyUnitComboBox = frequencyUnitComboBox;
            _periodUnitComboBox = periodUnitComboBox;
            _modeToggle = modeToggle;
        }

        public void SetMode(bool frequencyMode)
        {
            _isFrequencyMode = frequencyMode;
            UpdateUIVisibility();
            UpdateToggleButton();
            UpdateCalculatedValue();

            Log($"Switched to {(_isFrequencyMode ? "Frequency" : "Period")} mode");
        }

        public void ToggleMode()
        {
            SetMode(!_isFrequencyMode);
        }

        private void UpdateUIVisibility()
        {
            _frequencyDockPanel.Visibility = _isFrequencyMode ? Visibility.Visible : Visibility.Collapsed;
            _periodDockPanel.Visibility = _isFrequencyMode ? Visibility.Collapsed : Visibility.Visible;
        }

        private void UpdateToggleButton()
        {
            _modeToggle.IsChecked = _isFrequencyMode;
            _modeToggle.Content = _isFrequencyMode ? "To Period" : "To Frequency";
        }

        public void UpdateCalculatedValue()
        {
            try
            {
                if (_isFrequencyMode)
                {
                    // Calculate period from frequency
                    if (double.TryParse(_frequencyTextBox.Text, out double frequency))
                    {
                        string freqUnit = UnitConversionUtility.GetFrequencyUnit(_frequencyUnitComboBox);
                        double freqInHz = frequency * UnitConversionUtility.GetFrequencyMultiplier(freqUnit);

                        if (freqInHz > 0)
                        {
                            double periodInSeconds = 1.0 / freqInHz;
                            string periodUnit = UnitConversionUtility.GetPeriodUnit(_periodUnitComboBox);
                            double displayValue = UnitConversionUtility.ConvertFromPicoSeconds(
                                periodInSeconds * 1e12, periodUnit);

                            _periodTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
                        }
                    }
                }
                else
                {
                    // Calculate frequency from period
                    if (double.TryParse(_periodTextBox.Text, out double period))
                    {
                        string periodUnit = UnitConversionUtility.GetPeriodUnit(_periodUnitComboBox);
                        double periodInSeconds = period * UnitConversionUtility.GetPeriodMultiplier(periodUnit);

                        if (periodInSeconds > 0)
                        {
                            double freqInHz = 1.0 / periodInSeconds;
                            string freqUnit = UnitConversionUtility.GetFrequencyUnit(_frequencyUnitComboBox);
                            double displayValue = UnitConversionUtility.ConvertFromMicroHz(
                                freqInHz * 1e6, freqUnit);

                            _frequencyTextBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(displayValue);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Error updating calculated value: {ex.Message}");
            }
        }

        public double GetFrequencyInHz()
        {
            if (_isFrequencyMode)
            {
                if (double.TryParse(_frequencyTextBox.Text, out double frequency))
                {
                    string unit = UnitConversionUtility.GetFrequencyUnit(_frequencyUnitComboBox);
                    return frequency * UnitConversionUtility.GetFrequencyMultiplier(unit);
                }
            }
            else
            {
                if (double.TryParse(_periodTextBox.Text, out double period))
                {
                    string unit = UnitConversionUtility.GetPeriodUnit(_periodUnitComboBox);
                    double periodInSeconds = period * UnitConversionUtility.GetPeriodMultiplier(unit);
                    return periodInSeconds > 0 ? 1.0 / periodInSeconds : 0;
                }
            }
            return 0;
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}