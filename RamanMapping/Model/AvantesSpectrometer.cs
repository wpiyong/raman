using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace RamanMapping.Model
{
    class AvantesSpectrometer
    {
        long _deviceHandle = Avaspec.INVALID_AVS_HANDLE_VALUE;
        readonly ushort StartPixel = 0;
        readonly ushort EndPixel = 2047;

        public string SerialNumber { get; set; }
        public string Name { get; set; }

        public bool Connect()
        {
            bool connected = false;

            int l_Port = Avaspec.AVS_Init(0);
            if (l_Port > 0)
            {
                uint l_Size = 0;
                uint l_RequiredSize = 0;
                int l_NrDevices = 0;

                l_NrDevices = Avaspec.AVS_GetNrOfDevices();
                Avaspec.AvsIdentityType[] l_Id = new Avaspec.AvsIdentityType[l_NrDevices];
                l_RequiredSize = ((uint)l_NrDevices) * (uint)Marshal.SizeOf(typeof(Avaspec.AvsIdentityType));

                if (l_RequiredSize > 0)
                {
                    l_Size = l_RequiredSize;
                    l_NrDevices = Avaspec.AVS_GetList(l_Size, ref l_RequiredSize, l_Id);
                    long l_hDevice = (long)Avaspec.AVS_Activate(ref l_Id[0]);
                    if (Avaspec.INVALID_AVS_HANDLE_VALUE != l_hDevice)
                    {
                        _deviceHandle = l_hDevice;
                        SerialNumber = l_Id[0].m_SerialNumber;
                        Name = l_Id[0].m_UserFriendlyName;
                        connected = true;
                    }

                    if (Avaspec.AVS_UseHighResAdc((IntPtr)l_hDevice, true) != Avaspec.ERR_SUCCESS)
                    {
                        App.Current.Dispatcher.Invoke(new Action(() =>
                            System.Windows.MessageBox.Show("Could not set to use 16 bit resolution", "Maximum count will be 16383")));
                    }
                }
            }

            if (!connected)
            {
                Avaspec.AVS_Done();
            }

            return connected;
        }

        public void Disconnect()
        {
            if (_deviceHandle != Avaspec.INVALID_AVS_HANDLE_VALUE)
            {
                int l_Res = (int)Avaspec.AVS_StopMeasure((IntPtr)_deviceHandle);

                if (Avaspec.ERR_SUCCESS != l_Res)
                {
                    //MessageBox.Show("Error in AVS_StopMeasure, code: " + l_Res.ToString(), "Avantes",
                    //    MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            Avaspec.AVS_Done();
        }

        public bool StartMeasurement(IntPtr windowHandle, double integrationTime,
            uint numAverages, short numMeasurements, uint delayMs = 0,
            bool externalTrigger = false)
        {
            bool result = false;

            //Prepare Measurement
            Avaspec.MeasConfigType l_PrepareMeasData = new Avaspec.MeasConfigType();

            l_PrepareMeasData.m_StartPixel = StartPixel;
            l_PrepareMeasData.m_StopPixel = EndPixel;
            l_PrepareMeasData.m_IntegrationTime = (float)integrationTime;
            //double l_NanoSec = System.Convert.ToDouble(edtIntegrationDelay.Text);
            //uint l_FPGAClkCycles = (uint)(6.0 * (l_NanoSec + 20.84) / 125.0);
            l_PrepareMeasData.m_IntegrationDelay = delayMs == 0 ? 1 : (uint)(delayMs * 1000000 / 20.83);//each unit = 20.83ns
            l_PrepareMeasData.m_NrAverages = numAverages;
            l_PrepareMeasData.m_Trigger.m_Mode = externalTrigger == false ? (byte)0 : (byte)1;//1=hardware
            l_PrepareMeasData.m_Trigger.m_Source = (byte)0;//0=external trigger
            l_PrepareMeasData.m_Trigger.m_SourceType = (byte)0;//0=edge
            l_PrepareMeasData.m_SaturationDetection = 1;
            l_PrepareMeasData.m_CorDynDark.m_Enable = 1;
            l_PrepareMeasData.m_CorDynDark.m_ForgetPercentage = 100;
            l_PrepareMeasData.m_Smoothing.m_SmoothPix = 0;
            l_PrepareMeasData.m_Smoothing.m_SmoothModel = 0;
            l_PrepareMeasData.m_Control.m_StrobeControl = 0;
            //l_NanoSec = System.Convert.ToDouble(edtLaserDelay.Text);
            //l_FPGAClkCycles = (uint)(6.0 * l_NanoSec / 125.0);
            l_PrepareMeasData.m_Control.m_LaserDelay = 0;// l_FPGAClkCycles;
            //l_NanoSec = System.Convert.ToDouble(edtLaserWidth.Text);
            //l_FPGAClkCycles = (uint)(6.0 * l_NanoSec / 125.0);
            l_PrepareMeasData.m_Control.m_LaserWidth = 0;// l_FPGAClkCycles;
            l_PrepareMeasData.m_Control.m_LaserWaveLength = 0;
            l_PrepareMeasData.m_Control.m_StoreToRam = 0;

            int l_Res = (int)Avaspec.AVS_PrepareMeasure((IntPtr)_deviceHandle, ref l_PrepareMeasData);
            if (Avaspec.ERR_SUCCESS == l_Res)
            {
                //Start Measurement

                l_Res = (int)Avaspec.AVS_Measure((IntPtr)_deviceHandle, windowHandle, numMeasurements);
                result = (Avaspec.ERR_SUCCESS == l_Res);
            }
            return result;
        }

        public void StopMeasurement()
        {
            Avaspec.AVS_StopMeasure((IntPtr)_deviceHandle);
        }

        public bool GetWavelengths(out double[] wavelengths)
        {
            bool result = false;
            wavelengths = new double[1];

            Avaspec.PixelArrayType m_Lambda = new Avaspec.PixelArrayType();

            if (0 == (int)Avaspec.AVS_GetLambda((IntPtr)_deviceHandle, ref m_Lambda))
            {
                wavelengths = m_Lambda.Value.ToArray();
                result = true;
            }
            return result;
        }

        public bool GetSpectrum(out bool isSaturated, out double[] spectrumData, out uint timestamp)
        {
            bool result = false;
            isSaturated = true;
            spectrumData = new double[1];

            Avaspec.PixelArrayType l_pSpectrum = new Avaspec.PixelArrayType();
            Avaspec.SaturatedArrayType l_pSaturated = new Avaspec.SaturatedArrayType();

            timestamp = 0;
            if (Avaspec.ERR_SUCCESS == (int)Avaspec.AVS_GetScopeData((IntPtr)_deviceHandle, ref timestamp,
                ref l_pSpectrum))
            {
                spectrumData = l_pSpectrum.Value.ToArray();

                if (Avaspec.ERR_SUCCESS ==
                    (int)Avaspec.AVS_GetSaturatedPixels((IntPtr)_deviceHandle, ref l_pSaturated))
                {
                    isSaturated = false;
                    for (int i = 0; i < l_pSaturated.Value.Length; i++)
                        if (l_pSaturated.Value[i] == 1)
                        {
                            isSaturated = true;
                            break;
                        }
                    result = true;
                }
            }
            return result;
        }

        public uint GetTimestamp()
        {
            Avaspec.PixelArrayType l_pSpectrum = new Avaspec.PixelArrayType();
            uint timestamp = 0;
            if (Avaspec.ERR_SUCCESS == (int)Avaspec.AVS_GetScopeData((IntPtr)_deviceHandle, ref timestamp,
                ref l_pSpectrum))
            {
                return timestamp;
            }
            return 0;
        }

    }
}
