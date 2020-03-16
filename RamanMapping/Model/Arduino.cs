using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RamanMapping.Model
{
    static class Arduino
    {
        static SerialPort _serialPort = null;
        static bool _arduinoConnected;
        static Object _commsLock = new Object();

        public static bool Connect()
        {
            bool result = false;
            string[] ports = SerialPort.GetPortNames();
            _arduinoConnected = false;

            foreach (string port in ports)
            {
                try
                {
                    if (_serialPort != null)
                        _serialPort.Close();

                    _serialPort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                    _serialPort.Encoding = System.Text.Encoding.ASCII;
                    _serialPort.NewLine = "\r\n";
                    _serialPort.ReadTimeout = 2000;
                    _serialPort.WriteTimeout = 2000;

                    if (!_serialPort.IsOpen)
                        _serialPort.Open();

                    _serialPort.DiscardInBuffer();
                    _serialPort.DiscardOutBuffer();

                    byte[] buf = new byte[1];
                    buf[0] = (byte)'U';
                    _serialPort.Write(buf, 0, 1);

                    try
                    {
                        string id = _serialPort.ReadLine();
                        if (id == "Arduino")
                        {
                            _arduinoConnected = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex.ToString());
                        if (_serialPort != null)
                            _serialPort.Close();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    if (_serialPort != null)
                        _serialPort.Close();
                }
            }

            if (!_arduinoConnected)
            {
                if (_serialPort != null)
                    _serialPort.Close();
            }
            else
                result = true;

            return result;
        }

        public static bool FluorescenceOn()
        {
            bool result = false;

            try
            {
                if (_arduinoConnected)
                {
                    lock (_commsLock)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            _serialPort.DiscardInBuffer();
                            _serialPort.DiscardOutBuffer();

                            byte[] buf = new byte[1];
                            buf[0] = (byte)'F';
                            _serialPort.Write(buf, 0, 1);
                            string id = _serialPort.ReadLine();
                            result = (id == "A");
                            if (result)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return result;
        }

        public static bool PhosphorescenceOn()
        {
            bool result = false;

            try
            {
                if (_arduinoConnected)
                {
                    lock (_commsLock)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            _serialPort.DiscardInBuffer();
                            _serialPort.DiscardOutBuffer();

                            byte[] buf = new byte[1];
                            buf[0] = (byte)'P';
                            _serialPort.Write(buf, 0, 1);
                            string id = _serialPort.ReadLine();
                            result = (id == "A");
                            if (result)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return result;
        }

        public static bool RoomLightOn()
        {
            bool result = false;

            try
            {
                if (_arduinoConnected)
                {
                    lock (_commsLock)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            _serialPort.DiscardInBuffer();
                            _serialPort.DiscardOutBuffer();

                            byte[] buf = new byte[1];
                            buf[0] = (byte)'R';
                            _serialPort.Write(buf, 0, 1);
                            string id = _serialPort.ReadLine();
                            result = (id == "A");
                            if (result)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return result;
        }

        public static bool PumpOn(bool on)
        {
            bool result = false;
            try
            {
                if (_arduinoConnected)
                {
                    lock (_commsLock)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            _serialPort.DiscardInBuffer();
                            _serialPort.DiscardOutBuffer();

                            byte[] buf = new byte[1];
                            if (on)
                            {
                                buf[0] = (byte)'S';
                            }
                            else
                            {
                                buf[0] = (byte)'T';
                            }
                            _serialPort.Write(buf, 0, 1);
                            string id = _serialPort.ReadLine();
                            result = (id == "A");
                            if (result)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return result;
        }

        public static bool End()
        {
            bool result = false;

            try
            {
                if (_arduinoConnected)
                {
                    lock (_commsLock)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            _serialPort.DiscardInBuffer();
                            _serialPort.DiscardOutBuffer();

                            byte[] buf = new byte[1];
                            buf[0] = (byte)'E';
                            _serialPort.Write(buf, 0, 1);
                            string id = _serialPort.ReadLine();
                            result = (id == "A");
                            if (result)
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return result;
        }
    }
}
