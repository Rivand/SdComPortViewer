using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO.Ports;

namespace SdComPortViewer
{
    [DataContract]
    internal class UartSettings
    {
        [DataMember] public Parity CurrentParity = Parity.None;
        [DataMember] public StopBits CurrentStopBits = StopBits.One;
        [DataMember] public int CurrentBaudRate = 115200;
        [DataMember] public int DataBits = 8;
    }

    internal static class Uart
    {
        public static SerialPort UartPort;
        public static UartSettings CurrentUartSettings = new UartSettings();

        public static void OpenPort(string portName)
        {
            UartPort = new System.IO.Ports.SerialPort(portName, CurrentUartSettings.CurrentBaudRate, CurrentUartSettings.CurrentParity, CurrentUartSettings.DataBits, CurrentUartSettings.CurrentStopBits);
            UartPort.Open();
        }

        public static void ClosePort()
        {
            UartPort.Close();
        }
    }
}
