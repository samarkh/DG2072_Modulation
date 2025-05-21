using System;
using System.Windows;
using System.Windows.Controls;
using DG2072_USB_Control.Continuous.Square;

namespace DG2072_USB_Control
{
    public partial class MainWindow : Window
    {
        #region Square Generator Fields

        //Square Generator management
        private SquareGen squareGenerator;

        #endregion

        #region Square Generator Event Handlers

        private void ChannelDutyCycleTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (squareGenerator != null)
                squareGenerator.OnDutyCycleTextChanged(sender, e);
        }

        private void ChannelDutyCycleTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (squareGenerator != null)
                squareGenerator.OnDutyCycleLostFocus(sender, e);
        }

        #endregion
    }
}