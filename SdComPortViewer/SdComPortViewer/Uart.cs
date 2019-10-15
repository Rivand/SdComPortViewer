using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO.Ports;
using System.Windows;
namespace SdComPortViewer
{
    [DataContract]
    internal class UartSettings
    {
        [DataMember] public Parity CurrentParity = Parity.None;
        [DataMember] public StopBits CurrentStopBits = StopBits.One;
        [DataMember] public int CurrentBaudRate = 115200;
        [DataMember] public int DataBits = 8;
        [DataMember] public bool DtrEnable = false;
        [DataMember] public bool RtsEnable = false;
    }

    internal static class Uart
    {
        public static SerialPort UartPort;
        public static UartSettings CurrentUartSettings = new UartSettings();

        public static void OpenPort(string portName) 
        {
            try
            {
                UartPort = new System.IO.Ports.SerialPort(portName, CurrentUartSettings.CurrentBaudRate, CurrentUartSettings.CurrentParity, CurrentUartSettings.DataBits, CurrentUartSettings.CurrentStopBits);
                UartPort.DtrEnable = CurrentUartSettings.DtrEnable;
                UartPort.RtsEnable = CurrentUartSettings.RtsEnable;
                UartPort.Open();
            }
            catch (Exception ex){
                MessageBox.Show(ex.Message);
            }
        }

        public static void ClosePort()
        {
            try {
                UartPort.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
