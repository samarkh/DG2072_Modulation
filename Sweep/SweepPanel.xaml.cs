using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Sweep
{
    /// <summary>
    /// Self-contained SweepPanel that handles all its own events
    /// </summary>
    public partial class SweepPanel : UserControl
    {
        private SweepController _sweepController;
        private bool _isInitializing = false;

        public event EventHandler<string> LogEvent;

        // Public properties to expose UI controls to SweepController
        public ComboBox SweepTypeComboBox_Public => SweepTypeComboBox;
        public ComboBox TriggerSourceComboBox_Public => TriggerSourceComboBox;
        public ComboBox TriggerSlopeComboBox_Public => TriggerSlopeComboBox;
        public TextBox SweepTimeTextBox_Public => SweepTimeTextBox;
        public TextBox ReturnTimeTextBox_Public => ReturnTimeTextBox;
        public TextBox StartFrequencyTextBox_Public => StartFrequencyTextBox;
        public ComboBox StartFrequencyUnitComboBox_Public => StartFrequencyUnitComboBox;
        public TextBox StopFrequencyTextBox_Public => StopFrequencyTextBox;
        public ComboBox StopFrequencyUnitComboBox_Public => StopFrequencyUnitComboBox;
        public TextBox CenterFrequencyTextBox_Public => SweepCenterFrequencyTextBox;
        public ComboBox CenterFrequencyUnitComboBox_Public => SweepCenterFrequencyUnitComboBox;
        public TextBox SpanFrequencyTextBox_Public => SpanFrequencyTextBox;
        public ComboBox SpanFrequencyUnitComboBox_Public => SpanFrequencyUnitComboBox;
        public TextBox MarkerFrequencyTextBox_Public => MarkerFrequencyTextBox;
        public ComboBox MarkerFrequencyUnitComboBox_Public => MarkerFrequencyUnitComboBox;
        public TextBox StartHoldTextBox_Public => StartHoldTextBox;
        public TextBox StopHoldTextBox_Public => StopHoldTextBox;
        public TextBox StepCountTextBox_Public => StepCountTextBox;
        public Button ManualTriggerButton_Public => ManualTriggerButton;
        public StackPanel StartStopPanel_Public => StartStopPanel;
        public StackPanel CenterSpanPanel_Public => CenterSpanPanel;

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

        // Center/Span frequency event handlers - these match the XAML names
        private void SweepCenterFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sweepController == null) return;
            _sweepController.OnCenterFrequencyChanged();
        }

        private void SweepCenterFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void SweepCenterFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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
