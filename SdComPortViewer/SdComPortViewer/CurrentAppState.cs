using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows;
using System.Windows.Media;

namespace SdComPortViewer
{
    [DataContract]
    internal class AppConfig
    {
        [DataMember] public double WindowsWeight = 1599;
        [DataMember] public double WindowsHeight = 852;
        [DataMember] public double WindowsLocationX = 10;
        [DataMember] public double WindowsLocationY = 10;
        [DataMember] public WindowState WindowsState = WindowState.Normal;
        [DataMember] public int ThemeNumber = 0;
        [DataMember] public int FontSize = 12;
        [DataMember] public int ComboBoxFontSizeIndex = 6;
        [DataMember] public FontWeight FontWeights = System.Windows.FontWeights.Normal;
        [DataMember] public int ComboBoxFontStyleIndex = 0;
        [DataMember] public string LogsPathsHex = @"logs_hex.txt";
        [DataMember] public string LogsPathsAscii = @"logs_ascii.txt";
        [DataMember] public bool LogsPathsHexIsListen = false;
        [DataMember] public bool LogsPathsAsciiIsListen = false;
        [DataMember] public UartSettings UartSettings = new UartSettings();
        [DataMember] public bool? CheckBoxAutoscrollIsChecked = true;
        [DataMember] public bool RightMenuIsCollapsed = false;
        [DataMember] public bool DownMenuIsCollapsed = false;
        [DataMember] public string TextBoxCommandText = "";
        [DataMember] public bool? CheckBoxHexCommandIsChecked = false;
    }

    internal static class CurrentAppState
    {
        public static AppConfig CurrentAppConfig = new AppConfig();
        public static StreamWriter LogsHexStream;
        public static StreamWriter LogsAsciiStream;
        private static MainWindow _mainWindow;
        public static void InitConfig(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            try
            {
                FileStream fileStream = new FileStream("AppConfig.json", FileMode.Open);
                DataContractJsonSerializer jsonSerializer = new DataContractJsonSerializer(typeof(AppConfig));
                CurrentAppConfig = (AppConfig)jsonSerializer.ReadObject(fileStream);
                //Настройка окна
                _mainWindow.text_box_main_1.FontSize = CurrentAppState.CurrentAppConfig.FontSize;
                _mainWindow.text_box_main_2.FontSize = CurrentAppState.CurrentAppConfig.FontSize;
                _mainWindow.text_box_main_3.FontSize = CurrentAppState.CurrentAppConfig.FontSize;
                _mainWindow.text_box_main_1.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                _mainWindow.text_box_main_2.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                _mainWindow.text_box_main_3.FontWeight = CurrentAppState.CurrentAppConfig.FontWeights;
                ChangeTheme(CurrentAppConfig.ThemeNumber);
                _mainWindow.comboBox_font_size.SelectedIndex = CurrentAppConfig.ComboBoxFontSizeIndex;
                _mainWindow.comboBox_font_style.SelectedIndex = CurrentAppConfig.ComboBoxFontStyleIndex;
                _mainWindow.Left = CurrentAppState.CurrentAppConfig.WindowsLocationX;
                _mainWindow.Top = CurrentAppState.CurrentAppConfig.WindowsLocationY;
                mainWindow.Width = CurrentAppConfig.WindowsWeight;
                mainWindow.Height = CurrentAppConfig.WindowsHeight;
                mainWindow.WindowState = CurrentAppConfig.WindowsState;
                Uart.CurrentUartSettings = CurrentAppConfig.UartSettings;
                mainWindow.checkBox_autoscroll.IsChecked = CurrentAppState.CurrentAppConfig.CheckBoxAutoscrollIsChecked;
                mainWindow.textBox_command.Text = CurrentAppState.CurrentAppConfig.TextBoxCommandText;
                mainWindow.checkBox_hex_command.IsChecked = CurrentAppState.CurrentAppConfig.CheckBoxHexCommandIsChecked;

                if (CurrentAppConfig.RightMenuIsCollapsed == true) mainWindow.HideRightMenu();
                if (CurrentAppConfig.DownMenuIsCollapsed == true) mainWindow.HideDownMenu();


            }
            catch
            {
                MessageBox.Show("Не удалось загрузить файл с настройками. Были установлены настройки по умолчанию.");
            }
        }

        public static void ChangeTheme(int themeNumber)
        {
            switch (themeNumber)
            {
                case 1:
                    CurrentAppConfig.ThemeNumber = 1;
                    _mainWindow.Resources["Color1"] = Color.FromArgb(255, 255, 255, 255);
                    _mainWindow.Resources["Color2"] = Color.FromArgb(255, 64, 64, 64);
                    _mainWindow.Resources["Color3"] = Color.FromArgb(255, 32, 32, 32);
                    _mainWindow.Resources["Color4"] = Color.FromArgb(255, 0, 220, 0);
                    _mainWindow.Resources["Color5"] = Color.FromArgb(255, 0x30, 0x30, 0x30);
                    break;
                case 2:
                    CurrentAppConfig.ThemeNumber = 2;
                    _mainWindow.Resources["Color1"] = Color.FromArgb(255, 0, 0, 0);
                    _mainWindow.Resources["Color2"] = Color.FromArgb(255, 0xD3, 0xD3, 0xD3);
                    _mainWindow.Resources["Color3"] = Color.FromArgb(255, 0xFF, 0xFF, 0xFF);
                    _mainWindow.Resources["Color4"] = Color.FromArgb(255, 0, 0, 0);
                    _mainWindow.Resources["Color5"] = Color.FromArgb(255, 0xc3, 0xc3, 0xc3);
                    break;
                case 3:
                    CurrentAppConfig.ThemeNumber = 3;
                    _mainWindow.Resources["Color1"] = Color.FromArgb(255, 0, 0, 0);
                    _mainWindow.Resources["Color2"] = Color.FromArgb(255, 0xFF, 0xC0, 0xCB);
                    _mainWindow.Resources["Color3"] = Color.FromArgb(255, 0xFF, 0xB6, 0xC1);
                    _mainWindow.Resources["Color4"] = Color.FromArgb(255, 0, 0, 0);
                    _mainWindow.Resources["Color5"] = Color.FromArgb(255, 0xFF - 10, 0xB6 - 10, 0xC1 - 10);
                    break;
                case 4:
                    CurrentAppConfig.ThemeNumber = 4;
                    _mainWindow.Resources["Color1"] = Color.FromArgb(255, 0, 0, 0);
                    _mainWindow.Resources["Color2"] = Color.FromArgb(255, 0xF5, 0xF5, 0xF5);
                    _mainWindow.Resources["Color3"] = Color.FromArgb(255, 0xFF, 0xFF, 0xFF);
                    _mainWindow.Resources["Color4"] = Color.FromArgb(255, 0, 0, 0);
                    _mainWindow.Resources["Color5"] = Color.FromArgb(255, 0xD3, 0xD3, 0xD3);
                    break;
            }
        }
    }

}
