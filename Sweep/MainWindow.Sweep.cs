using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace DG2072_USB_Control
{
    /// <summary>
    /// Partial class containing sweep-related UI element declarations
    /// This file should be placed in the Sweep folder
    /// </summary>
    public partial class MainWindow
    {
        // Sweep UI Controls
        // Main toggle button
        public Button SweepToggleButton;

        // Main panel
        public GroupBox SweepPanel;

        // Sweep configuration
        public ComboBox SweepTypeComboBox;
        public TextBox SweepTimeTextBox;
        public TextBox ReturnTimeTextBox;

        // Frequency mode controls
        public RadioButton StartStopMode;
        public RadioButton CenterSpanMode;

        // Start/Stop frequency panels and controls
        public StackPanel StartStopPanel;
        public TextBox StartFrequencyTextBox;
        public ComboBox StartFrequencyUnitComboBox;
        public TextBox StopFrequencyTextBox;
        public ComboBox StopFrequencyUnitComboBox;

        // Center/Span frequency panels and controls
        public StackPanel CenterSpanPanel;
        public TextBox CenterFrequencyTextBox;
        public ComboBox CenterFrequencyUnitComboBox;
        public TextBox SpanFrequencyTextBox;
        public ComboBox SpanFrequencyUnitComboBox;

        // Marker frequency
        public TextBox MarkerFrequencyTextBox;
        public ComboBox MarkerFrequencyUnitComboBox;

        // Hold times
        public TextBox StartHoldTextBox;
        public TextBox StopHoldTextBox;

        // Step count
        public TextBox StepCountTextBox;

        // Trigger controls
        public ComboBox TriggerSourceComboBox;
        public ComboBox TriggerSlopeComboBox;
        public Button ManualTriggerButton;
    }
}