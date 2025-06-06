using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Services;

namespace DG2072_USB_Control.Advanced.RS232
{
    public class RS232Controller
    {
        private readonly RigolDG2072 _device;
        private readonly RS232Panel _rs232Panel;
        private readonly MainWindow _mainWindow;
        private int _activeChannel;
        private bool _isInitialized = false;
        private bool _isRS232Enabled = false;

        public event EventHandler<string> LogEvent;

        public int ActiveChannel
        {
            get => _activeChannel;
            set => _activeChannel = value;
        }

        public bool IsEnabled => _isRS232Enabled;

        public RS232Controller(RigolDG2072 device, int activeChannel, RS232Panel rs232Panel, MainWindow mainWindow)
        {
            _device = device;
            _activeChannel = activeChannel;
            _rs232Panel = rs232Panel;
            _mainWindow = mainWindow;
        }

        public void InitializeUI()
        {
            if (_isInitialized) return;

            try
            {
                // Update input mode visibility
                UpdateInputModeVisibility();

                // Update single byte equivalents
                UpdateSingleByteEquivalents();

                _isInitialized = true;
                Log("RS232 controller UI initialized");
            }
            catch (Exception ex)
            {
                Log($"Error initializing RS232 UI: {ex.Message}");
            }
        }

        public void EnableRS232()
        {
            try
            {
                // Apply RS232 settings to enable it
                ApplyRS232Settings();
                _isRS232Enabled = true;
                UpdateRS232UI(true);
                Log($"RS232 enabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error enabling RS232: {ex.Message}");
            }
        }

        public void DisableRS232()
        {
            try
            {
                // Switch back to a standard waveform to disable RS232
                _device.SendCommand($":SOUR{_activeChannel}:FUNC SIN");
                _isRS232Enabled = false;
                UpdateRS232UI(false);
                Log($"RS232 disabled on channel {_activeChannel}");
            }
            catch (Exception ex)
            {
                Log($"Error disabling RS232: {ex.Message}");
            }
        }

        private void UpdateRS232UI(bool enabled)
        {
            // Update the RS232Panel visibility
            _rs232Panel.Dispatcher.Invoke(() =>
            {
                _rs232Panel.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
            });

            // Log the state change
            Log($"RS232 UI updated: {(enabled ? "Enabled" : "Disabled")}");
        }

        public void RefreshRS232Settings()
        {
            if (!_isInitialized || !_device.IsConnected) return;

            try
            {
                // Check if current waveform is RS232
                string currentFunc = _device.SendQuery($":SOUR{_activeChannel}:FUNC?").Trim().ToUpper();
                _isRS232Enabled = currentFunc.Contains("RS232");

                UpdateRS232UI(_isRS232Enabled);

                if (_isRS232Enabled)
                {
                    // Refresh all RS232 parameters
                    RefreshBaudRate();
                    RefreshDataBits();
                    RefreshStopBits();
                    RefreshParity();
                }
            }
            catch (Exception ex)
            {
                Log($"Error refreshing RS232 settings: {ex.Message}");
            }
        }

        private void RefreshBaudRate()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:RS232:BAUD?");

                _rs232Panel.Dispatcher.Invoke(() =>
                {
                    if (_rs232Panel.BaudRateComboBox_Public != null)
                    {
                        for (int i = 0; i < _rs232Panel.BaudRateComboBox_Public.Items.Count; i++)
                        {
                            var item = _rs232Panel.BaudRateComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == response.Trim())
                            {
                                _rs232Panel.BaudRateComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing RS232 baud rate: {ex.Message}");
            }
        }

        private void RefreshDataBits()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:RS232:DATAB?");

                _rs232Panel.Dispatcher.Invoke(() =>
                {
                    if (_rs232Panel.DataBitsComboBox_Public != null)
                    {
                        for (int i = 0; i < _rs232Panel.DataBitsComboBox_Public.Items.Count; i++)
                        {
                            var item = _rs232Panel.DataBitsComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == response.Trim())
                            {
                                _rs232Panel.DataBitsComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing RS232 data bits: {ex.Message}");
            }
        }

        private void RefreshStopBits()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:RS232:STOPB?");

                _rs232Panel.Dispatcher.Invoke(() =>
                {
                    if (_rs232Panel.StopBitsComboBox_Public != null)
                    {
                        for (int i = 0; i < _rs232Panel.StopBitsComboBox_Public.Items.Count; i++)
                        {
                            var item = _rs232Panel.StopBitsComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == response.Trim())
                            {
                                _rs232Panel.StopBitsComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing RS232 stop bits: {ex.Message}");
            }
        }

        private void RefreshParity()
        {
            try
            {
                string response = _device.SendQuery($":SOUR{_activeChannel}:FUNC:RS232:CHECKB?").Trim().ToUpper();

                _rs232Panel.Dispatcher.Invoke(() =>
                {
                    if (_rs232Panel.ParityComboBox_Public != null)
                    {
                        for (int i = 0; i < _rs232Panel.ParityComboBox_Public.Items.Count; i++)
                        {
                            var item = _rs232Panel.ParityComboBox_Public.Items[i] as ComboBoxItem;
                            if (item?.Tag?.ToString() == response)
                            {
                                _rs232Panel.ParityComboBox_Public.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log($"Error refreshing RS232 parity: {ex.Message}");
            }
        }

        // Event handlers for UI changes
        public void OnBaudRateChanged()
        {
            if (!_isRS232Enabled) return;

            try
            {
                var selectedItem = _rs232Panel.BaudRateComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string baudRate = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:BAUD {baudRate}");
                    Log($"Set RS232 baud rate to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing RS232 baud rate: {ex.Message}");
            }
        }

        public void OnDataBitsChanged()
        {
            if (!_isRS232Enabled) return;

            try
            {
                var selectedItem = _rs232Panel.DataBitsComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string dataBits = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:DATAB {dataBits}");
                    Log($"Set RS232 data bits to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing RS232 data bits: {ex.Message}");
            }
        }

        public void OnStopBitsChanged()
        {
            if (!_isRS232Enabled) return;

            try
            {
                var selectedItem = _rs232Panel.StopBitsComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string stopBits = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:STOPB {stopBits}");
                    Log($"Set RS232 stop bits to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing RS232 stop bits: {ex.Message}");
            }
        }

        public void OnParityChanged()
        {
            if (!_isRS232Enabled) return;

            try
            {
                var selectedItem = _rs232Panel.ParityComboBox_Public.SelectedItem as ComboBoxItem;
                if (selectedItem != null)
                {
                    string parity = selectedItem.Tag.ToString();
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:CHECKB {parity}");
                    Log($"Set RS232 parity to {selectedItem.Content}");
                }
            }
            catch (Exception ex)
            {
                Log($"Error changing RS232 parity: {ex.Message}");
            }
        }

        public void OnInputModeChanged(string modeName)
        {
            UpdateInputModeVisibility();
        }

        public void OnSingleByteChanged()
        {
            UpdateSingleByteEquivalents();
        }

        private void UpdateInputModeVisibility()
        {
            _rs232Panel.Dispatcher.Invoke(() =>
            {
                bool isASCII = _rs232Panel.ASCIIMode_Public.IsChecked == true;
                bool isHex = _rs232Panel.HexMode_Public.IsChecked == true;
                bool isSingleByte = _rs232Panel.SingleByteMode_Public.IsChecked == true;

                _rs232Panel.ASCIIPanel_Public.Visibility = isASCII ? Visibility.Visible : Visibility.Collapsed;
                _rs232Panel.HexPanel_Public.Visibility = isHex ? Visibility.Visible : Visibility.Collapsed;
                _rs232Panel.SingleBytePanel_Public.Visibility = isSingleByte ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private void UpdateSingleByteEquivalents()
        {
            _rs232Panel.Dispatcher.Invoke(() =>
            {
                if (int.TryParse(_rs232Panel.SingleByteTextBox_Public.Text, out int byteValue))
                {
                    if (byteValue >= 0 && byteValue <= 255)
                    {
                        _rs232Panel.HexEquivalentTextBlock_Public.Text = $"0x{byteValue:X2}";

                        // Show ASCII equivalent if printable
                        if (byteValue >= 32 && byteValue <= 126)
                        {
                            _rs232Panel.ASCIIEquivalentTextBlock_Public.Text = $"'{(char)byteValue}'";
                        }
                        else
                        {
                            _rs232Panel.ASCIIEquivalentTextBlock_Public.Text = "[non-printable]";
                        }
                    }
                    else
                    {
                        _rs232Panel.HexEquivalentTextBlock_Public.Text = "Invalid";
                        _rs232Panel.ASCIIEquivalentTextBlock_Public.Text = "Invalid";
                    }
                }
                else
                {
                    _rs232Panel.HexEquivalentTextBlock_Public.Text = "Invalid";
                    _rs232Panel.ASCIIEquivalentTextBlock_Public.Text = "Invalid";
                }
            });
        }

        public void SendASCIIText()
        {
            try
            {
                string text = _rs232Panel.ASCIITextBox_Public.Text;
                if (string.IsNullOrEmpty(text)) return;

                StringBuilder logBuilder = new StringBuilder();
                foreach (char c in text)
                {
                    int byteValue = (int)c;
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:DATA {byteValue}");
                    logBuilder.Append($"0x{byteValue:X2} ");
                }

                string logMessage = $"ASCII: \"{text}\" → {logBuilder.ToString().TrimEnd()}";
                UpdateLastSent(logMessage);
                Log($"Sent ASCII text: {text}");
            }
            catch (Exception ex)
            {
                Log($"Error sending ASCII text: {ex.Message}");
            }
        }

        public void SendHexBytes()
        {
            try
            {
                string hexText = _rs232Panel.HexTextBox_Public.Text;
                if (string.IsNullOrWhiteSpace(hexText)) return;

                string[] hexValues = hexText.Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder logBuilder = new StringBuilder();

                foreach (string hexValue in hexValues)
                {
                    if (int.TryParse(hexValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int byteValue))
                    {
                        if (byteValue >= 0 && byteValue <= 255)
                        {
                            _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:DATA {byteValue}");
                            logBuilder.Append($"0x{byteValue:X2} ");
                        }
                        else
                        {
                            Log($"Invalid byte value: {hexValue} (must be 0-255)");
                            return;
                        }
                    }
                    else
                    {
                        Log($"Invalid hex value: {hexValue}");
                        return;
                    }
                }

                string logMessage = $"Hex: {logBuilder.ToString().TrimEnd()}";
                UpdateLastSent(logMessage);
                Log($"Sent hex bytes: {hexText}");
            }
            catch (Exception ex)
            {
                Log($"Error sending hex bytes: {ex.Message}");
            }
        }

        public void SendSingleByte()
        {
            try
            {
                if (int.TryParse(_rs232Panel.SingleByteTextBox_Public.Text, out int byteValue))
                {
                    if (byteValue >= 0 && byteValue <= 255)
                    {
                        _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:DATA {byteValue}");

                        string logMessage = $"Byte: {byteValue} (0x{byteValue:X2})";
                        UpdateLastSent(logMessage);
                        Log($"Sent single byte: {byteValue}");
                    }
                    else
                    {
                        Log("Byte value must be between 0 and 255");
                    }
                }
                else
                {
                    Log("Invalid byte value");
                }
            }
            catch (Exception ex)
            {
                Log($"Error sending single byte: {ex.Message}");
            }
        }

        public void SendQuickByte(byte byteValue, string description)
        {
            try
            {
                _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:DATA {byteValue}");
                UpdateLastSent(description);
                Log($"Sent {description}");
            }
            catch (Exception ex)
            {
                Log($"Error sending {description}: {ex.Message}");
            }
        }

        public void SendQuickBytes(byte[] bytes, string description)
        {
            try
            {
                foreach (byte b in bytes)
                {
                    _device.SendCommand($":SOUR{_activeChannel}:FUNC:RS232:DATA {b}");
                }
                UpdateLastSent(description);
                Log($"Sent {description}");
            }
            catch (Exception ex)
            {
                Log($"Error sending {description}: {ex.Message}");
            }
        }

        private void UpdateLastSent(string message)
        {
            _rs232Panel.Dispatcher.Invoke(() =>
            {
                _rs232Panel.LastSentTextBlock_Public.Text = message;
            });
        }

        public void ApplyRS232Settings()
        {
            try
            {
                // Get current amplitude and offset from main window controls
                double amplitude = 1.0; // Default value
                double offset = 2.0;     // Default value for RS232 (typically needs positive offset)

                // Try to get values from main window if available
                if (_mainWindow != null)
                {
                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        try
                        {
                            if (double.TryParse(_mainWindow.ChannelAmplitudeTextBox.Text, out double amp))
                            {
                                string ampUnit = UnitConversionUtility.GetAmplitudeUnit(_mainWindow.ChannelAmplitudeUnitComboBox);
                                amplitude = amp * UnitConversionUtility.GetAmplitudeMultiplier(ampUnit);
                            }

                            if (double.TryParse(_mainWindow.ChannelOffsetTextBox.Text, out double off))
                            {
                                offset = off;
                            }
                        }
                        catch (Exception)
                        {
                            // Use default values if can't get from UI
                        }
                    });
                }

                // Apply RS232 waveform with current amplitude and offset
                _device.SendCommand($":SOUR{_activeChannel}:APPL:RS232 {amplitude},{offset}");

                // Apply all communication settings
                OnBaudRateChanged();
                OnDataBitsChanged();
                OnStopBitsChanged();
                OnParityChanged();

                _isRS232Enabled = true;

                Log($"Applied RS232 settings: {amplitude} Vpp, {offset} V offset");
            }
            catch (Exception ex)
            {
                Log($"Error applying RS232 settings: {ex.Message}");
            }
        }

        private void Log(string message)
        {
            LogEvent?.Invoke(this, message);
        }
    }
}