// ===================================================================
// MainWindow.Sweep.cs - CURRENT CONTENTS (Document 23)
// ===================================================================

// This is what your MainWindow.Sweep.cs file currently contains:

using System.Windows.Controls;

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

// ===================================================================
// WHAT YOU SHOULD DO WITH THIS FILE:
// ===================================================================

// 🗑️ DELETE THIS ENTIRE FILE!
// 
// Reason: In my clean architecture, we don't need these properties on MainWindow.
// The SweepController works directly with the SweepPanel UserControl instead.
// 
// Steps:
// 1. Right-click on "MainWindow.Sweep.cs" in Visual Studio
// 2. Select "Delete" 
// 3. Confirm deletion
//
// This file was causing the compilation errors because:
// - It declares properties that don't exist in the actual MainWindow class
// - The SweepController was trying to access these non-existent properties
// - The properties were never actually connected to the real UI controls
//
// In the clean architecture:
// - SweepController talks directly to SweepPanel
// - No properties needed on MainWindow
// - Much cleaner separation of concerns