using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Advanced.Sequence
{
    /// <summary>
    /// Self-contained SequencePanel that handles all its own events
    /// </summary>
    public partial class SequencePanel : UserControl
    {
        private SequenceController _sequenceController;
        private bool _isInitializing = false;

        public event EventHandler<string> LogEvent;

        // Public properties to expose UI controls to SequenceController
        public TextBox SampleRateTextBox_Public => SampleRateTextBox;
        public ComboBox SampleRateUnitComboBox_Public => SampleRateUnitComboBox;
        public ComboBox FilterTypeComboBox_Public => FilterTypeComboBox;
        public DockPanel EdgeTimeDockPanel_Public => EdgeTimeDockPanel;
        public TextBox EdgeTimeTextBox_Public => EdgeTimeTextBox;
        public ComboBox EdgeTimeUnitComboBox_Public => EdgeTimeUnitComboBox;
        public Button ApplySequenceButton_Public => ApplySequenceButton;

        // Slot controls - using arrays for easier management
        public CheckBox[] SlotEnableCheckBoxes_Public => new CheckBox[]
        {
            null, // Index 0 unused to match 1-based slot numbering
            Slot1EnableCheckBox, Slot2EnableCheckBox, Slot3EnableCheckBox, Slot4EnableCheckBox,
            Slot5EnableCheckBox, Slot6EnableCheckBox, Slot7EnableCheckBox, Slot8EnableCheckBox
        };

        public ComboBox[] SlotWaveformComboBoxes_Public => new ComboBox[]
        {
            null, // Index 0 unused to match 1-based slot numbering
            Slot1WaveformComboBox, Slot2WaveformComboBox, Slot3WaveformComboBox, Slot4WaveformComboBox,
            Slot5WaveformComboBox, Slot6WaveformComboBox, Slot7WaveformComboBox, Slot8WaveformComboBox
        };

        public TextBox[] SlotPointsTextBoxes_Public => new TextBox[]
        {
            null, // Index 0 unused to match 1-based slot numbering
            Slot1PointsTextBox, Slot2PointsTextBox, Slot3PointsTextBox, Slot4PointsTextBox,
            Slot5PointsTextBox, Slot6PointsTextBox, Slot7PointsTextBox, Slot8PointsTextBox
        };

        public SequencePanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the panel with its controller
        /// </summary>
        public void Initialize(SequenceController sequenceController)
        {
            _sequenceController = sequenceController;
            _isInitializing = false;
        }

        // All event handlers work directly with the SequenceController
        private void SampleRateTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;
            _sequenceController.OnSampleRateChanged();
        }

        private void SampleRateTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void SampleRateUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;
            _sequenceController.OnSampleRateChanged();
        }

        private void FilterTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;
            _sequenceController.OnFilterTypeChanged();
        }

        private void EdgeTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;
            _sequenceController.OnEdgeTimeChanged();
        }

        private void EdgeTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && double.TryParse(textBox.Text, out double value))
            {
                textBox.Text = UnitConversionUtility.FormatWithMinimumDecimals(value);
            }
        }

        private void EdgeTimeUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;
            _sequenceController.OnEdgeTimeChanged();
        }

        // Slot event handlers
        private void SlotEnableCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;

            if (sender is CheckBox checkBox && int.TryParse(checkBox.Tag?.ToString(), out int slotNumber))
            {
                _sequenceController.OnSlotEnableChanged(slotNumber, checkBox.IsChecked == true);
            }
        }

        private void SlotWaveformComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;

            if (sender is ComboBox comboBox && int.TryParse(comboBox.Tag?.ToString(), out int slotNumber))
            {
                _sequenceController.OnSlotWaveformChanged(slotNumber);
            }
        }

        private void SlotPointsTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _sequenceController == null) return;

            if (sender is TextBox textBox && int.TryParse(textBox.Tag?.ToString(), out int slotNumber))
            {
                _sequenceController.OnSlotPointsChanged(slotNumber);
            }
        }

        private void SlotPointsTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && int.TryParse(textBox.Text, out int value))
            {
                // Clamp to valid range (1-256)
                value = Math.Max(1, Math.Min(256, value));
                textBox.Text = value.ToString();
            }
        }

        private void ApplySequenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sequenceController == null) return;
            _sequenceController.ApplySequenceSettings();
        }

        // Helper method to log messages
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}