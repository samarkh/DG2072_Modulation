using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Advanced.RS232
{
    /// <summary>
    /// Self-contained RS232Panel that handles all its own events
    /// </summary>
    public partial class RS232Panel : UserControl
    {
        private RS232Controller _rs232Controller;
        private bool _isInitializing = false;

        public event EventHandler<string> LogEvent;

        // Public properties to expose UI controls to RS232Controller
        public ComboBox BaudRateComboBox_Public => BaudRateComboBox;
        public ComboBox DataBitsComboBox_Public => DataBitsComboBox;
        public ComboBox StopBitsComboBox_Public => StopBitsComboBox;
        public ComboBox ParityComboBox_Public => ParityComboBox;
        public RadioButton ASCIIMode_Public => ASCIIMode;
        public RadioButton HexMode_Public => HexMode;
        public RadioButton SingleByteMode_Public => SingleByteMode;
        public StackPanel ASCIIPanel_Public => ASCIIPanel;
        public StackPanel HexPanel_Public => HexPanel;
        public StackPanel SingleBytePanel_Public => SingleBytePanel;
        public TextBox ASCIITextBox_Public => ASCIITextBox;
        public TextBox HexTextBox_Public => HexTextBox;
        public TextBox SingleByteTextBox_Public => SingleByteTextBox;
        public TextBlock HexEquivalentTextBlock_Public => HexEquivalentTextBlock;
        public TextBlock ASCIIEquivalentTextBlock_Public => ASCIIEquivalentTextBlock;
        public TextBlock LastSentTextBlock_Public => LastSentTextBlock;
        public Button ApplyRS232Button_Public => ApplyRS232Button;

        public RS232Panel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize the panel with its controller
        /// </summary>
        public void Initialize(RS232Controller rs232Controller)
        {
            _rs232Controller = rs232Controller;
            _isInitializing = false;
        }

        // All event handlers work directly with the RS232Controller
        private void BaudRateComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _rs232Controller == null) return;
            _rs232Controller.OnBaudRateChanged();
        }

        private void DataBitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _rs232Controller == null) return;
            _rs232Controller.OnDataBitsChanged();
        }

        private void StopBitsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _rs232Controller == null) return;
            _rs232Controller.OnStopBitsChanged();
        }

        private void ParityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializing || _rs232Controller == null) return;
            _rs232Controller.OnParityChanged();
        }

        private void InputModeChanged(object sender, RoutedEventArgs e)
        {
            if (_isInitializing || _rs232Controller == null) return;

            if (sender is RadioButton radioButton)
            {
                _rs232Controller.OnInputModeChanged(radioButton.Name);
            }
        }

        private void SingleByteTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isInitializing || _rs232Controller == null) return;
            _rs232Controller.OnSingleByteChanged();
        }

        private void SendASCIIButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendASCIIText();
        }

        private void SendHexButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendHexBytes();
        }

        private void SendByteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendSingleByte();
        }

        private void SendCRButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendQuickByte(0x0D, "CR (0x0D)");
        }

        private void SendLFButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendQuickByte(0x0A, "LF (0x0A)");
        }

        private void SendCRLFButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendQuickBytes(new byte[] { 0x0D, 0x0A }, "CRLF (0x0D 0x0A)");
        }

        private void SendNullButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendQuickByte(0x00, "NULL (0x00)");
        }

        private void SendSpaceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.SendQuickByte(0x20, "Space (0x20)");
        }

        private void ApplyRS232Button_Click(object sender, RoutedEventArgs e)
        {
            if (_rs232Controller == null) return;
            _rs232Controller.ApplyRS232Settings();
        }

        // Helper method to log messages
        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}