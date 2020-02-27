using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.Serialization;
using System.IO.Ports;
using System.Windows;
namespace SdComPortViewer {
    [DataContract]
    internal class UartSettings {
        [DataMember] public Parity CurrentParity = Parity.None;
        [DataMember] public StopBits CurrentStopBits = StopBits.One;
        [DataMember] public int CurrentBaudRate = 115200;
        [DataMember] public int DataBits = 8;
        [DataMember] public bool DtrEnable = false;
        [DataMember] public bool RtsEnable = false;
    }

    internal static class Uart {
        public static SerialPort serialPort;
        public static UartSettings currentUartSettings = new UartSettings();

        public static bool OpenPort(string portName) {
            try {
                serialPort = new SerialPort();
                lock (serialPort) {
                    serialPort = new System.IO.Ports.SerialPort(portName, currentUartSettings.CurrentBaudRate, currentUartSettings.CurrentParity, currentUartSettings.DataBits, currentUartSettings.CurrentStopBits);
                    try {
                        serialPort.DtrEnable = currentUartSettings.DtrEnable;
                        serialPort.RtsEnable = currentUartSettings.RtsEnable;
                        serialPort.Open();
                        serialPort.ErrorReceived += serialPort_ErrorReceived;
                        return true;
                    } catch (Exception ex) {
                        MessageBox.Show(ex.Message);
                        return false;
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static void Write(byte[] buffer, int offset, int count) {
            try {
                lock (serialPort) {
                    if (serialPort.IsOpen) serialPort.Write(buffer, offset, count);
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public static byte[] Read() {
            try {
                lock (serialPort) {
                    if (serialPort.IsOpen) {
                        int count = serialPort.BytesToRead;
                        byte[] reciveData = new byte[count];
                        serialPort.Read(reciveData, 0, count);
                        return reciveData;
                    }
                    return null;
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return new byte[] { };
            }
        }

        public static void AddReciveEventHandler(SerialDataReceivedEventHandler handler) {
            try {
                lock (serialPort) {
                    serialPort.DataReceived += handler;
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public static void RemoveReciveEventHandler(SerialDataReceivedEventHandler handler) {
            try {
                lock (serialPort) {
                    serialPort.DataReceived -= handler;
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        public static bool ClosePort() {
            try {
                lock (serialPort) {
                    if (serialPort.IsOpen) {
                        Task.Run(() => {
                            Thread th = new Thread(new ThreadStart(() => {
                                try {
                                    serialPort.Dispose();
                                } catch (Exception ex) {
                                    MessageBox.Show(ex.Message);
                                };
                            }));
                            th.IsBackground = true;
                            th.Start();
                            Thread.Sleep(1000);
                            th.Abort();
                        });
                        return true;
                    } else {
                        return false;
                    }
                }
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return false;
            }
        }
        private static void serialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e) {
            MessageBox.Show("serialPort_ErrorReceived");
        }
    }
}
