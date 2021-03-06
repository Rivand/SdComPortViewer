﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO.Ports;

namespace SdComPortViewer
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for UartSettingsWindow.xaml
    /// </summary>
    public partial class UartSettingsWindow : Window
    {
        public UartSettingsWindow()
        {
            InitializeComponent();
            textBox_baud_rate.Text = Uart.currentUartSettings.CurrentBaudRate.ToString();
            comboBox_parity.Text = Uart.currentUartSettings.CurrentParity.ToString();
            textBox_data_bits.Text = Uart.currentUartSettings.DataBits.ToString();
            comboBox_stop_bits.Text = Uart.currentUartSettings.CurrentStopBits.ToString();
        }

        private void button_uart_settings_cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void button_uart_settings_ok_Click(object sender, RoutedEventArgs e)
        {
            Uart.currentUartSettings.CurrentBaudRate = Convert.ToInt32(textBox_baud_rate.Text);
            Uart.currentUartSettings.CurrentParity = (Parity)Enum.Parse(typeof(Parity), comboBox_parity.Text);
            Uart.currentUartSettings.DataBits = Convert.ToInt32(textBox_data_bits.Text);
            Uart.currentUartSettings.CurrentStopBits = (StopBits)Enum.Parse(typeof(StopBits), comboBox_stop_bits.Text);
            this.Close();
        }
    }
}
