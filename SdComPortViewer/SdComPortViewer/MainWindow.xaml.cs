using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.IO.Ports;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

namespace SdComPortViewer
{
    /// <inheritdoc />
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            CurrentAppState.InitConfig(this);
            comboBox_uart_ports.Items.Clear();
            foreach (var c in System.IO.Ports.SerialPort.GetPortNames()) comboBox_uart_ports.Items.Add(c);
            if (comboBox_uart_ports.Items.Count != 0)
            {
                comboBox_uart_ports.Text = comboBox_uart_ports.Items[0].ToString();
                button_listen.IsEnabled = true;
            }
            else
            {
                button_listen.IsEnabled = false;
                comboBox_uart_ports.Text = "";
            }
            text_box_main_2.Visibility = Visibility.Hidden;
            text_box_main_3.Visibility = Visibility.Hidden;

            if (CurrentAppState.CurrentAppConfig.LogsPathsHexIsListen) CurrentAppState.LogsHexStream = new StreamWriter(CurrentAppState.CurrentAppConfig.LogsPathsHex, true, System.Text.Encoding.Default);
            if (CurrentAppState.CurrentAppConfig.LogsPathsAsciiIsListen) CurrentAppState.LogsAsciiStream = new StreamWriter(CurrentAppState.CurrentAppConfig.LogsPathsAscii, true, System.Text.Encoding.Default);

            this.Closed += (object sender, EventArgs e) =>
            {

                CurrentAppState.CurrentAppConfig.UartSettings = Uart.CurrentUartSettings;
                CurrentAppState.CurrentAppConfig.WindowsState = this.WindowState;
                CurrentAppState.CurrentAppConfig.WindowsWeight = this.Width;
                CurrentAppState.CurrentAppConfig.WindowsHeight = this.Height;
                CurrentAppState.CurrentAppConfig.WindowsLocationX = this.Left;
                CurrentAppState.CurrentAppConfig.WindowsLocationY = this.Top;
                CurrentAppState.CurrentAppConfig.CheckBoxAutoscrollIsChecked = checkBox_autoscroll.IsChecked;

                CurrentAppState.CurrentAppConfig.TextBoxCommandText = this.textBox_command.Text;
                CurrentAppState.CurrentAppConfig.CheckBoxHexCommandIsChecked = this.checkBox_hex_command.IsChecked;


                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(AppConfig));
                FileStream fileStream = new FileStream("AppConfig.json", FileMode.OpenOrCreate);
                jsonSerializer.WriteObject(fileStream, CurrentAppState.CurrentAppConfig);
                fileStream.Close();
            };

            // Автоскролин TextBox'ов
            if (checkBox_autoscroll.IsChecked == true)
            {
                text_box_main_1.TextChanged += TextBoxMain1TextChangeedHandler;
                text_box_main_2.TextChanged += TextBoxMain2TextChangeedHandler;
                text_box_main_3.TextChanged += TextBoxMain3TextChangeedHandler;
            }

            checkBox_autoscroll.Click += (object sender, RoutedEventArgs e) =>
            {
                switch (checkBox_autoscroll.IsChecked)
                {
                    case true:
                        text_box_main_1.TextChanged += TextBoxMain1TextChangeedHandler;
                        text_box_main_2.TextChanged += TextBoxMain2TextChangeedHandler;
                        text_box_main_3.TextChanged += TextBoxMain3TextChangeedHandler;
                        break;
                    case false:
                        text_box_main_1.TextChanged -= TextBoxMain1TextChangeedHandler;
                        text_box_main_2.TextChanged -= TextBoxMain2TextChangeedHandler;
                        text_box_main_3.TextChanged -= TextBoxMain3TextChangeedHandler;
                        break;
                }
            };

            // Запуск основных часов
            StartWatch();
        }
        private void TextBoxMain1TextChangeedHandler(object sender, TextChangedEventArgs e)
        {
            if (text_box_main_1.IsVisible) text_box_main_1.ScrollToEnd();
        }
        private void TextBoxMain2TextChangeedHandler(object sender, TextChangedEventArgs e)
        {
            if (text_box_main_2.IsVisible) text_box_main_2.ScrollToEnd();
        }
        private void TextBoxMain3TextChangeedHandler(object sender, TextChangedEventArgs e)
        {
            if (text_box_main_3.IsVisible) text_box_main_3.ScrollToEnd();
        }

        private readonly List<byte> _uartData = new List<byte>(); // Тут собираеться пакет с данными 
        private byte _lastByte = 0;
        private void GetDataFromComPort(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                SerialPort sp = (SerialPort)sender;
                //Thread.Sleep(100);
                //string data = sp.ReadExisting();
                //listBox_COM.Invoke(new Action(() => listBox_COM.Items.Add(data)));
                int count = Uart.UartPort.BytesToRead;
                byte[] listenerData = new byte[count];
                Uart.UartPort.Read(listenerData, 0, count); // TODO Вставить обработчик исключения (которое выскакивает если БС не работает)
                string strMessage = "";
                strMessage = listenerData.Aggregate("", (current, c) => current + (char)c);
                string hexMsg = "";
                for (int i = 0; i < count; i++)
                {
                    _uartData.Add(listenerData[i]);
                    if (listenerData[i] < 0X10) hexMsg += "0";
                    hexMsg += listenerData[i].ToString("X") + " ";
                    _lastByte = listenerData[i];

                }
                this.Dispatcher.Invoke(new Action(() =>
                {

                    text_box_main_1.AppendText(strMessage);
                    text_box_main_2.AppendText(strMessage);
                    text_box_main_3.AppendText(hexMsg);
                    if (CurrentAppState.CurrentAppConfig.LogsPathsHexIsListen)
                    {
                        CurrentAppState.LogsHexStream.Write(hexMsg);
                        CurrentAppState.LogsHexStream.Flush();
                    }
                    if (CurrentAppState.CurrentAppConfig.LogsPathsAsciiIsListen)
                    {
                        CurrentAppState.LogsAsciiStream.Write(strMessage);
                        CurrentAppState.LogsAsciiStream.Flush();
                    }
                }));
            }
            catch (Exception ex)
            {
                //var a = ex as EndOfStreamException;
                MessageBox.Show("Исключение в потоке получения данных UART: " + ex.Message + "(" + ex.Source + ")");
            }
        }

        public void StartWatch()
        {
            // Таймер 
            Thread timerThread = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(1000); // Необходима задержка, иначе будет крашиться на старой системе. 
                while (true)
                {
                    DateTime dt = DateTime.Now;
                    this.Dispatcher.Invoke(() =>
                    {
                        label_current_time.Content = (dt.Hour).ToString("00.") + ":" + (dt.Minute).ToString("00.") + ":" + (dt.Second).ToString("00."); // Текущее время 
                    });
                    Thread.Sleep(1000);
                }
            }));
            timerThread.IsBackground = true;
            timerThread.Start();
        }

        public static void CheckCrc16S(int lenght, byte[] data, out byte highCrCbyte, out byte lowCrCbyte)
        {
            int checkSum = 0xFFFF;
            int j = 0;
            int q = 0;
            for (j = 0; j < lenght; j++)
            {
                checkSum = checkSum ^ (int)data[j];
                for (q = 8; q > 0; q--)
                    if ((checkSum & 0x0001) != 0)
                        checkSum = (checkSum >> 1) ^ 0xA001;
                    else
                        checkSum >>= 1;
            }
            highCrCbyte = (byte)(checkSum >> 8);
            checkSum <<= 8;
            lowCrCbyte = (byte)(checkSum >> 8);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            UartSettingsWindow uartSettingsWindow = new UartSettingsWindow();
            uartSettingsWindow.Show();

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            comboBox_uart_ports.Items.Clear();
            foreach (var c in System.IO.Ports.SerialPort.GetPortNames()) comboBox_uart_ports.Items.Add(c);
            if (comboBox_uart_ports.Items.Count != 0)
            {
                comboBox_uart_ports.Text = comboBox_uart_ports.Items[0].ToString();
                button_listen.IsEnabled = true;
            }
            else
            {
                button_listen.IsEnabled = false;
                comboBox_uart_ports.Text = "";
            }
        }



        private void button_listen_Click(object sender, RoutedEventArgs e)
        {
            comboBox_uart_ports.IsEnabled = false;
            button_rescan.IsEnabled = false;
            button_stop_listen.IsEnabled = true;
            button_listen.IsEnabled = false;
            button_send_command.IsEnabled = true;
            Uart.OpenPort(comboBox_uart_ports.Text);
            Uart.UartPort.DataReceived += GetDataFromComPort;
        }

        public bool FlagHexFieldIsOpened = false;
        private void button_add_hex_Click(object sender, RoutedEventArgs e)
        {
            if (!FlagHexFieldIsOpened)
            {
                button_add_hex.Content = "-> Hide HEX";
                FlagHexFieldIsOpened = true;

                text_box_main_1.Visibility = Visibility.Hidden;
                text_box_main_2.Visibility = Visibility.Visible;
                text_box_main_3.Visibility = Visibility.Visible;
                GridSplitter_m2_m3.Visibility = Visibility.Visible;
            }
            else
            {

                button_add_hex.Content = "<- Add HEX";
                FlagHexFieldIsOpened = false;

                text_box_main_1.Visibility = Visibility.Visible;
                text_box_main_2.Visibility = Visibility.Hidden;
                text_box_main_3.Visibility = Visibility.Hidden;
                GridSplitter_m2_m3.Visibility = Visibility.Hidden;
            }

        }

        private void button_clean_Click(object sender, RoutedEventArgs e)
        {
            text_box_main_1.Clear();
            text_box_main_2.Clear();
            text_box_main_3.Clear();
        }

        private void button_stop_listen_Click(object sender, RoutedEventArgs e)
        {
            comboBox_uart_ports.IsEnabled = true;
            button_rescan.IsEnabled = true;
            button_stop_listen.IsEnabled = false;
            button_listen.IsEnabled = true;
            button_send_command.IsEnabled = false;
            Uart.ClosePort();
            Uart.UartPort.DataReceived -= GetDataFromComPort;
        }

        private void comboBox_uart_ports_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void comboBox_font_size_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Thread th = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(100);
                this.Dispatcher.Invoke(() =>
                {
                    CurrentAppState.CurrentAppConfig.FontSize = Convert.ToInt32(comboBox_font_size.Text);
                    text_box_main_1.FontSize = CurrentAppState.CurrentAppConfig.FontSize;
                    text_box_main_2.FontSize = CurrentAppState.CurrentAppConfig.FontSize;
                    text_box_main_3.FontSize = CurrentAppState.CurrentAppConfig.FontSize;
                    CurrentAppState.CurrentAppConfig.ComboBoxFontSizeIndex = comboBox_font_size.SelectedIndex;
                });
            }));
            th.IsBackground = true;
            th.Start();
        }

        private void comboBox_font_style_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Thread th = new Thread(new ThreadStart(() =>
            {
                Thread.Sleep(100);
                this.Dispatcher.Invoke(() =>
                {
                    CurrentAppState.CurrentAppConfig.FontSize = Convert.ToInt32(comboBox_font_size.Text);
                    CurrentAppState.CurrentAppConfig.ComboBoxFontStyleIndex = comboBox_font_style.SelectedIndex;
                    if (comboBox_font_style.SelectedIndex == 0)
                    {
                        CurrentAppState.CurrentAppConfig.FontWeights = FontWeights.Normal;
                        text_box_main_1.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                        text_box_main_2.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                        text_box_main_3.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                    }
                    else
                    {
                        CurrentAppState.CurrentAppConfig.FontWeights = FontWeights.Bold;
                        text_box_main_1.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                        text_box_main_2.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                        text_box_main_3.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                    }

                });
            }));
            th.IsBackground = true;
            th.Start();
        }


        private void Button_Click_T1(object sender, RoutedEventArgs e)
        {
            CurrentAppState.ChangeTheme(1);
        }
        private void Button_Click_T2(object sender, RoutedEventArgs e)
        {
            CurrentAppState.ChangeTheme(2);

        }
        private void Button_Click_T3(object sender, RoutedEventArgs e)
        {
            CurrentAppState.ChangeTheme(3);

        }
        private void Button_Click_T4(object sender, RoutedEventArgs e)
        {
            CurrentAppState.ChangeTheme(4);
        }

        private void button_send_command_Click(object sender, RoutedEventArgs e)
        {
            if (checkBox_hex_command.IsChecked == true)
            {
                var bytes = textBox_command.Text.Split(' ').Select(_ => int.Parse(_, NumberStyles.HexNumber));

                int[] int_array = bytes.ToArray();
                byte[] byte_array = new byte[int_array.Length];
                for (int i = 0; i < int_array.Length; i++)
                {
                    byte_array[i] = (byte)int_array[i];
                }
                Uart.UartPort.Write(byte_array, 0, byte_array.Length);
            }
            else
            {
                var msg = textBox_command.Text.ToCharArray();
                Uart.UartPort.Write(msg, 0, msg.Length);
            }
        }

        private void button_browse_Click(object sender, RoutedEventArgs e)
        {

        }

        private void button_browse_Click_1(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.ShowDialog();
            switch (comboBox_logs_path.SelectedIndex)
            {
                case 0:
                    CurrentAppState.CurrentAppConfig.LogsPathsHex = openFileDialog.FileName;
                    textBox_logs_path.Text = CurrentAppState.CurrentAppConfig.LogsPathsHex;
                    break;
                case 1:
                    CurrentAppState.CurrentAppConfig.LogsPathsAscii = openFileDialog.FileName;
                    textBox_logs_path.Text = CurrentAppState.CurrentAppConfig.LogsPathsAscii;
                    break;
            }
        }

        private void button_logs_start_Click_1(object sender, RoutedEventArgs e)
        {
            switch (comboBox_logs_path.SelectedIndex)
            {

                case 0:
                    if (CurrentAppState.CurrentAppConfig.LogsPathsHexIsListen == false)
                    {
                        try
                        {
                            CurrentAppState.CurrentAppConfig.LogsPathsHex = textBox_logs_path.Text;
                            CurrentAppState.LogsHexStream = new StreamWriter(CurrentAppState.CurrentAppConfig.LogsPathsHex, true, System.Text.Encoding.Default);
                            CurrentAppState.CurrentAppConfig.LogsPathsHexIsListen = true;
                            button_logs_start.Content = "Stop";
                        }
                        catch
                        {
                            MessageBox.Show("Неудалось открыть файл логирования.");
                        }
                    }
                    else
                    {
                        CurrentAppState.CurrentAppConfig.LogsPathsHexIsListen = false;
                        CurrentAppState.LogsHexStream.Close();
                        button_logs_start.Content = "Start";
                    }

                    break;
                case 1:
                    if (CurrentAppState.CurrentAppConfig.LogsPathsAsciiIsListen == false)
                    {
                        try
                        {
                            CurrentAppState.CurrentAppConfig.LogsPathsAscii = textBox_logs_path.Text;
                            CurrentAppState.LogsAsciiStream = new StreamWriter(CurrentAppState.CurrentAppConfig.LogsPathsAscii, true, System.Text.Encoding.Default);
                            CurrentAppState.CurrentAppConfig.LogsPathsAsciiIsListen = true;
                            button_logs_start.Content = "Stop";
                        }
                        catch
                        {
                            MessageBox.Show("Неудалось открыть файл логирования.");
                        }
                    }
                    else
                    {
                        CurrentAppState.CurrentAppConfig.LogsPathsAsciiIsListen = false;
                        CurrentAppState.LogsAsciiStream.Close();
                        button_logs_start.Content = "Start";
                    }

                    break;
            }
        }

        private void comboBox_logs_path_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (comboBox_logs_path.SelectedIndex)
            {
                case 0:
                    textBox_logs_path.Text = CurrentAppState.CurrentAppConfig.LogsPathsHex;
                    if (CurrentAppState.CurrentAppConfig.LogsPathsHexIsListen == true) button_logs_start.Content = "Stop";
                    else button_logs_start.Content = "Start";
                    break;
                case 1:
                    textBox_logs_path.Text = CurrentAppState.CurrentAppConfig.LogsPathsAscii;
                    if (CurrentAppState.CurrentAppConfig.LogsPathsAsciiIsListen == true) button_logs_start.Content = "Stop";
                    else button_logs_start.Content = "Start";
                    break;
            }
        }




        public void HideRightMenu()
        {
            button_folding_right_menu.Margin = new Thickness(button_folding_right_menu.Margin.Left, button_folding_right_menu.Margin.Top, button_folding_right_menu.Margin.Right - groupBoxUart.Width - 10, button_folding_right_menu.Margin.Bottom);
            button_folding_down_menu.Margin = new Thickness(button_folding_down_menu.Margin.Left, button_folding_down_menu.Margin.Top, button_folding_down_menu.Margin.Right - groupBoxUart.Width - 10, button_folding_down_menu.Margin.Bottom);
            text_box_main_1.Margin = new Thickness(text_box_main_1.Margin.Left, text_box_main_1.Margin.Top, text_box_main_1.Margin.Right - groupBoxUart.Width - 10, text_box_main_1.Margin.Bottom);
            MainTextBoxesGrid.Margin = new Thickness(MainTextBoxesGrid.Margin.Left, MainTextBoxesGrid.Margin.Top, MainTextBoxesGrid.Margin.Right - groupBoxUart.Width - 10, MainTextBoxesGrid.Margin.Bottom);
            label_current_time.Visibility = Visibility.Hidden;
            groupBoxUart.Visibility = Visibility.Hidden;
            groupBoxStyle.Visibility = Visibility.Hidden;
            groupBoxUart_DtrRts.Visibility = Visibility.Hidden;
            button_folding_right_menu.Content = "<";
            CurrentAppState.CurrentAppConfig.RightMenuIsCollapsed = true;
        }

        public void ShowRightMenu()
        {
            button_folding_right_menu.Margin = new Thickness(button_folding_right_menu.Margin.Left, button_folding_right_menu.Margin.Top, button_folding_right_menu.Margin.Right + groupBoxUart.Width + 10, button_folding_right_menu.Margin.Bottom);
            button_folding_down_menu.Margin = new Thickness(button_folding_down_menu.Margin.Left, button_folding_down_menu.Margin.Top, button_folding_down_menu.Margin.Right + groupBoxUart.Width + 10, button_folding_down_menu.Margin.Bottom);
            text_box_main_1.Margin = new Thickness(text_box_main_1.Margin.Left, text_box_main_1.Margin.Top, text_box_main_1.Margin.Right + groupBoxUart.Width + 10, text_box_main_1.Margin.Bottom);
            MainTextBoxesGrid.Margin = new Thickness(MainTextBoxesGrid.Margin.Left, MainTextBoxesGrid.Margin.Top, MainTextBoxesGrid.Margin.Right + groupBoxUart.Width + 10, MainTextBoxesGrid.Margin.Bottom);
            label_current_time.Visibility = Visibility.Visible;
            groupBoxUart.Visibility = Visibility.Visible;
            groupBoxStyle.Visibility = Visibility.Visible;
            groupBoxUart_DtrRts.Visibility = Visibility.Visible;
            button_folding_right_menu.Content = ">";
            CurrentAppState.CurrentAppConfig.RightMenuIsCollapsed = false;
        }

        public void HideDownMenu()
        {
            label_command.Visibility = Visibility.Hidden;
            textBox_command.Visibility = Visibility.Hidden;
            checkBox_hex_command.Visibility = Visibility.Hidden;
            button_send_command.Visibility = Visibility.Hidden;
            label_logs_path.Visibility = Visibility.Hidden;
            textBox_logs_path.Visibility = Visibility.Hidden;
            comboBox_logs_path.Visibility = Visibility.Hidden;
            button_logs_start.Visibility = Visibility.Hidden;
            button_browse.Visibility = Visibility.Hidden;
            button_folding_down_menu.Margin = new Thickness(button_folding_down_menu.Margin.Left, button_folding_down_menu.Margin.Top, button_folding_down_menu.Margin.Right, button_folding_down_menu.Margin.Bottom - 30);
            text_box_main_1.Margin = new Thickness(text_box_main_1.Margin.Left, text_box_main_1.Margin.Top, text_box_main_1.Margin.Right, text_box_main_1.Margin.Bottom - 30);
            MainTextBoxesGrid.Margin = new Thickness(MainTextBoxesGrid.Margin.Left, MainTextBoxesGrid.Margin.Top, MainTextBoxesGrid.Margin.Right, MainTextBoxesGrid.Margin.Bottom - 30);
            button_folding_right_menu.Margin = new Thickness(button_folding_right_menu.Margin.Left, button_folding_right_menu.Margin.Top, button_folding_right_menu.Margin.Right, button_folding_right_menu.Margin.Bottom - 30);
            CurrentAppState.CurrentAppConfig.DownMenuIsCollapsed = true;
        }

        public void ShowDownMenu()
        {
            label_command.Visibility = Visibility.Visible;
            textBox_command.Visibility = Visibility.Visible;
            checkBox_hex_command.Visibility = Visibility.Visible;
            button_send_command.Visibility = Visibility.Visible;
            label_logs_path.Visibility = Visibility.Visible;
            textBox_logs_path.Visibility = Visibility.Visible;
            comboBox_logs_path.Visibility = Visibility.Visible;
            button_logs_start.Visibility = Visibility.Visible;
            button_browse.Visibility = Visibility.Visible;
            button_folding_down_menu.Margin = new Thickness(button_folding_down_menu.Margin.Left, button_folding_down_menu.Margin.Top, button_folding_down_menu.Margin.Right, button_folding_down_menu.Margin.Bottom + 30);
            text_box_main_1.Margin = new Thickness(text_box_main_1.Margin.Left, text_box_main_1.Margin.Top, text_box_main_1.Margin.Right, text_box_main_1.Margin.Bottom + 30);
            MainTextBoxesGrid.Margin = new Thickness(MainTextBoxesGrid.Margin.Left, MainTextBoxesGrid.Margin.Top, MainTextBoxesGrid.Margin.Right, MainTextBoxesGrid.Margin.Bottom + 30);
            button_folding_right_menu.Margin = new Thickness(button_folding_right_menu.Margin.Left, button_folding_right_menu.Margin.Top, button_folding_right_menu.Margin.Right, button_folding_right_menu.Margin.Bottom + 30);
            CurrentAppState.CurrentAppConfig.DownMenuIsCollapsed = false;
        }
        private void button_folding_right_menu_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentAppState.CurrentAppConfig.RightMenuIsCollapsed)
            {
                case false:
                    HideRightMenu();
                    return;
                case true:
                    ShowRightMenu();
                    return;
            }
        }

        private void button_folding_down_menu_Click(object sender, RoutedEventArgs e)
        {
            switch (CurrentAppState.CurrentAppConfig.DownMenuIsCollapsed)
            {
                case false:
                    HideDownMenu();
                    return;
                case true:
                    ShowDownMenu();
                    return;
            }
        }

        private void Button_Click_Dtr(object sender, RoutedEventArgs e)
        {
            if (Uart.CurrentUartSettings.DtrEnable)
            {
                Uart.CurrentUartSettings.DtrEnable = false;
                if (Uart.UartPort != null) Uart.UartPort.DtrEnable = Uart.CurrentUartSettings.DtrEnable;
                button_dtr.Content = "DTR now off";
            }
            else
            {
                Uart.CurrentUartSettings.DtrEnable = true;
                if (Uart.UartPort != null) Uart.UartPort.DtrEnable = Uart.CurrentUartSettings.DtrEnable;
                button_dtr.Content = "DTR now on";
            }
        }

        private void Button_Click_Rts(object sender, RoutedEventArgs e)
        {
            if (Uart.CurrentUartSettings.RtsEnable)
            {
                Uart.CurrentUartSettings.RtsEnable = false;
                if (Uart.UartPort != null) Uart.UartPort.RtsEnable = Uart.CurrentUartSettings.RtsEnable;
                button_rts.Content = "RTS now off";
            }
            else
            {
                Uart.CurrentUartSettings.RtsEnable = true;
                if (Uart.UartPort != null) Uart.UartPort.RtsEnable = Uart.CurrentUartSettings.RtsEnable;
                button_rts.Content = "RTS now on";
            }
        }
    }
}
