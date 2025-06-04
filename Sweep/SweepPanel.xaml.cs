using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Sweep
{
    /// <summary>
    /// Self-contained SweepPanel that handles all its own events
    /// THIS REPLACES YOUR CURRENT SweepPanel.xaml.cs FILE COMPLETELY
    /// </summary>
    public partial class SweepPanel : UserControl
    {
        private SweepController _sweepController;
        private bool _isInitializing = false;

        public event EventHandler<string> LogEvent;

        public SweepPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the panel with its controller
        /// </summary>
        public void Initialize(SweepController sweepController)
        {
            _sweepController = sweepController;
            _isInitializing = false;
        }

        // All event handlers work directly with the SweepController
        private void SweepTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnSweepTypeChanged();
        }

        private void SweepTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnSweepTimeChanged();
        }

        private void SweepTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void ReturnTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnReturnTimeChanged();
        }

        private void ReturnTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void FrequencyModeChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;

            if (sender is RadioButton radioButton)
            {
                bool useStartStop = radioButton.Name == "StartStopMode";
                _sweepController.OnFrequencyModeChanged(useStartStop);
            }
        }

        // Start/Stop frequency event handlers
        private void StartFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStartFrequencyChanged();
        }

        private void StartFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void StartFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStartFrequencyChanged();
        }

        private void StopFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStopFrequencyChanged();
        }

        private void StopFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void StopFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStopFrequencyChanged();
        }

        // Center/Span frequency event handlers
        private void CenterFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnCenterFrequencyChanged();
        }

        private void CenterFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void CenterFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnCenterFrequencyChanged();
        }

        private void SpanFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnSpanFrequencyChanged();
        }

        private void SpanFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void SpanFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnSpanFrequencyChanged();
        }

        // Marker frequency event handlers
        private void MarkerFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnMarkerFrequencyChanged();
        }

        private void MarkerFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void MarkerFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnMarkerFrequencyChanged();
        }

        // Hold time event handlers
        private void StartHoldTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStartHoldChanged();
        }

        private void StartHoldTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void StopHoldTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStopHoldChanged();
        }

        private void StopHoldTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        // Step count event handlers
        private void StepCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnStepCountChanged();
        }

        private void StepCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && int.TryParse(textBox.Text, out int value))
            {
                textBox.Text = value.ToString();
            }
        }

        // Trigger event handlers
        private void TriggerSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnTriggerSourceChanged();
        }

        private void TriggerSlopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnTriggerSlopeChanged();
        }

        private void ManualTriggerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sweepController == null) return;
            _sweepController.ExecuteManualTrigger();
        }

        // Helper method to log messages
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}