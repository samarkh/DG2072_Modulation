using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Burst
{
    /// <summary>
    /// Self-contained BurstPanel that handles all its own events
    /// </summary>
    public partial class BurstPanel : UserControl
    {
        private BurstController _burstController;
        private bool _isInitializing = false;

        public event EventHandler<string> LogEvent;

        public BurstPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the panel with its controller
        /// </summary>
        public void Initialize(BurstController burstController)
        {
            _burstController = burstController;
            _isInitializing = false;
        }

        // Event handlers work directly with the BurstController
        private void BurstModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnBurstModeChanged();
        }

        private void BurstCyclesTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnBurstCyclesChanged();
        }

        private void BurstCyclesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && int.TryParse(textBox.Text, out int value))
            {
                textBox.Text = value.ToString();
            }
        }

        private void BurstPeriodTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnBurstPeriodChanged();
        }

        private void BurstPeriodTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void BurstPeriodUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnBurstPeriodChanged();
        }

        private void TriggerDelayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnTriggerDelayChanged();
        }

        private void TriggerDelayTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void TriggerDelayUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnTriggerDelayChanged();
        }

        private void StartPhaseTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnStartPhaseChanged();
        }

        private void StartPhaseTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void IdleLevelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnIdleLevelChanged();

            // Show/hide user idle level panel
            if (IdleLevelComboBox.SelectedItem is ComboBoxItem item &&
                item.Tag?.ToString() == "USER" && UserIdleLevelPanel != null)
            {
                UserIdleLevelPanel.Visibility = Visibility.Visible;
            }
            else if (UserIdleLevelPanel != null)
            {
                UserIdleLevelPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void UserIdleLevelTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnUserIdleLevelChanged();
        }

        private void UserIdleLevelTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void UserIdleLevelUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnUserIdleLevelChanged();
        }

        private void TriggerSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnTriggerSourceChanged();
        }

        private void TriggerSlopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnTriggerSlopeChanged();
        }

        private void TriggerOutComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnTriggerOutChanged();
        }

        private void GatePolarityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _burstController == null) return;
            _burstController.OnGatePolarityChanged();
        }

        private void ManualTriggerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_burstController == null) return;
            _burstController.ExecuteManualTrigger();
        }

        // Helper method to log messages
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }

        public void SetInitializing(bool isInitializing)
        {
            _isInitializing = isInitializing;
        }
    }
}