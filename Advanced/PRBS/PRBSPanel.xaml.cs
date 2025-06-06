using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Advanced.PRBS
{
    /// <summary>
    /// Self-contained PRBSPanel that handles all its own events
    /// </summary>
    public partial class PRBSPanel : UserControl
    {
        private PRBSController _prbsController;
        private bool _isInitializing = false;

        public event EventHandler<string> LogEvent;

        // Public properties to expose UI controls to PRBSController
        public ComboBox PRBSDataTypeComboBox_Public => PRBSDataTypeComboBox;
        public TextBox PRBSBitRateTextBox_Public => PRBSBitRateTextBox;
        public ComboBox PRBSBitRateUnitComboBox_Public => PRBSBitRateUnitComboBox;
        public TextBox PRBSAmplitudeTextBox_Public => PRBSAmplitudeTextBox;
        public ComboBox PRBSAmplitudeUnitComboBox_Public => PRBSAmplitudeUnitComboBox;
        public TextBox PRBSOffsetTextBox_Public => PRBSOffsetTextBox;
        public ComboBox PRBSOffsetUnitComboBox_Public => PRBSOffsetUnitComboBox;
        public TextBlock SequenceLengthTextBlock_Public => SequenceLengthTextBlock;
        public TextBlock SequencePeriodTextBlock_Public => SequencePeriodTextBlock;
        public TextBlock RepetitionRateTextBlock_Public => RepetitionRateTextBlock;
        public Button ApplyPRBSButton_Public => ApplyPRBSButton;

        public PRBSPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the panel with its controller
        /// </summary>
        public void Initialize(PRBSController prbsController)
        {
            _prbsController = prbsController;
            _isInitializing = false;
        }

        // All event handlers work directly with the PRBSController
        private void PRBSDataTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnDataTypeChanged();
        }

        private void PRBSBitRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnBitRateChanged();
        }

        private void PRBSBitRateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void PRBSBitRateUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnBitRateChanged();
        }

        private void PRBSAmplitudeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnAmplitudeChanged();
        }

        private void PRBSAmplitudeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void PRBSAmplitudeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnAmplitudeChanged();
        }

        private void PRBSOffsetTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnOffsetChanged();
        }

        private void PRBSOffsetTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void PRBSOffsetUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _prbsController == null) return;
            _prbsController.OnOffsetChanged();
        }

        private void ApplyPRBSButton_Click(object sender, RoutedEventArgs e)
        {
            if (_prbsController == null) return;
            _prbsController.ApplyPRBSSettings();
        }

        // Helper method to log messages
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}