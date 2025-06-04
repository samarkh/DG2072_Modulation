using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Sweep
{
    /// <summary>
    /// Interaction logic for SweepPanel.xaml
    /// </summary>
    public partial class SweepPanel : UserControl
    {
        private MainWindow _mainWindow;
        private bool _isInitializing = false;

        public SweepPanel()
        {
            InitializeComponent();
        }

        // Property to set the main window reference
        public void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            _isInitializing = false;
        }

        // Pass all events to MainWindow
        private void SweepTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.SweepTypeComboBox_SelectionChanged(sender, e);
        }

        private void SweepTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.SweepTimeTextBox_TextChanged(sender, e);
        }

        private void SweepTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.SweepTimeTextBox_LostFocus(sender, e);
        }

        private void ReturnTimeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.ReturnTimeTextBox_TextChanged(sender, e);
        }

        private void ReturnTimeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.ReturnTimeTextBox_LostFocus(sender, e);
        }

        private void FrequencyModeChanged(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.FrequencyModeChanged(sender, e);
        }

        private void StartFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StartFrequencyTextBox_TextChanged(sender, e);
        }

        private void StartFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.StartFrequencyTextBox_LostFocus(sender, e);
        }

        private void StartFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StartFrequencyUnitComboBox_SelectionChanged(sender, e);
        }

        private void StopFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StopFrequencyTextBox_TextChanged(sender, e);
        }

        private void StopFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.StopFrequencyTextBox_LostFocus(sender, e);
        }

        private void StopFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StopFrequencyUnitComboBox_SelectionChanged(sender, e);
        }

        private void SweepCenterFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.SweepCenterFrequencyTextBox_TextChanged(sender, e);
        }

        private void SweepCenterFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.SweepCenterFrequencyTextBox_LostFocus(sender, e);
        }

        private void SweepCenterFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.SweepCenterFrequencyUnitComboBox_SelectionChanged(sender, e);
        }

        private void SpanFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.SpanFrequencyTextBox_TextChanged(sender, e);
        }

        private void SpanFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.SpanFrequencyTextBox_LostFocus(sender, e);
        }

        private void SpanFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.SpanFrequencyUnitComboBox_SelectionChanged(sender, e);
        }

        private void MarkerFrequencyTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.MarkerFrequencyTextBox_TextChanged(sender, e);
        }

        private void MarkerFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.MarkerFrequencyTextBox_LostFocus(sender, e);
        }

        private void MarkerFrequencyUnitComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.MarkerFrequencyUnitComboBox_SelectionChanged(sender, e);
        }

        private void StartHoldTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StartHoldTextBox_TextChanged(sender, e);
        }

        private void StartHoldTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.StartHoldTextBox_LostFocus(sender, e);
        }

        private void StopHoldTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StopHoldTextBox_TextChanged(sender, e);
        }

        private void StopHoldTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.StopHoldTextBox_LostFocus(sender, e);
        }

        private void StepCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.StepCountTextBox_TextChanged(sender, e);
        }

        private void StepCountTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.StepCountTextBox_LostFocus(sender, e);
        }

        private void TriggerSourceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.TriggerSourceComboBox_SelectionChanged(sender, e);
        }

        private void TriggerSlopeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mainWindow != null && !_isInitializing)
                _mainWindow.TriggerSlopeComboBox_SelectionChanged(sender, e);
        }

        private void ManualTriggerButton_Click(object sender, RoutedEventArgs e)
        {
            if (_mainWindow != null)
                _mainWindow.ManualTriggerButton_Click(sender, e);
        }
    }
}