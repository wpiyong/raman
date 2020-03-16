using System;
using SpinnakerNET;
using SpinnakerNET.GenApi;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media;
using RamanMapping.View;
using System.Windows.Threading;

namespace RamanMapping.Model
{
    enum TriggerType
    {
        Software,
        Hardware,
        Unknown
    };

    enum TriggerMode
    {
        Off,
        On
    };

    class PtGreyCameraImage
    {
        public ulong FrameId { get; set; }
        public ulong TimeStamp { get; set; }
        public System.Drawing.Bitmap Image { get; set; }
    }

    public class ImageEventArgs : EventArgs
    {
        public ImageEventArgs(BitmapSource img)
        {
            image = img;
        }
        public BitmapSource image { get; }
    }

    public class ImageEnqueuedEventArgs : EventArgs
    {
        public ImageEnqueuedEventArgs(int i)
        {
            index = i;
        }
        public int index;
    }

    public delegate void ImageEnqueueHandler(ImageEnqueuedEventArgs e);

    class ImageEventListener : ManagedImageEvent
    {
        Queue<PtGreyCameraImage> _imageQueue = null;
        EventWaitHandle _wh;
        readonly object _locker;
        public bool _measuring;
        public event EventHandler<ImageEventArgs> ImageChanged;
        public event ImageEnqueueHandler ImageEnqueued;
        ManagedImage managedImageConverted;
        public int imgCounter = 0;

        public ImageEventListener(Queue<PtGreyCameraImage> imageQueue, EventWaitHandle wh, object locker)
        {
            _imageQueue = imageQueue;
            _wh = wh;
            _locker = locker;
            managedImageConverted = new ManagedImage();
        }

        private void RaiseImageChangedEvent(ImageEventArgs eventArgs)
        {
            ImageChanged?.Invoke(this, eventArgs);
        }

        private void RaiseImageEnqueuedEvent(ImageEnqueuedEventArgs eventArgs)
        {
            ImageEnqueued?.Invoke(eventArgs);
        }

        override protected void OnImageEvent(ManagedImage image)
        {
            //Debug.WriteLine("OnImageEvent");
            try
            {
                Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
                Dispatcher.Run();

                if (!image.IsIncomplete)
                {
                    // test get image from file
                    //string currentDirectory = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                    //var tempFileNamePath = Path.Combine(currentDirectory, "laser_spot.bmp");
                    //System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(tempFileNamePath);

                    //var bitmapData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
                    //            System.Drawing.Imaging.ImageLockMode.ReadOnly, bmp.PixelFormat);
                    //var img = BitmapSource.Create(bitmapData.Width, bitmapData.Height, bmp.HorizontalResolution, bmp.VerticalResolution,
                    //            PixelFormats.Bgr24, null, bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);
                    //bmp.UnlockBits(bitmapData);

                    // Convert image
                    image.ConvertToBitmapSource(PixelFormatEnums.BGR8, managedImageConverted);
                    BitmapSource img = managedImageConverted.bitmapsource.Clone();
                    img.Freeze();
                    RaiseImageChangedEvent(new ImageEventArgs(img));
                    //img.Dispatcher.InvokeShutdown();
                    if (_measuring && _imageQueue.Count <= 100)
                    {
                        imgCounter++;
                        lock (_locker)
                        {
                            var temp = image.Convert(PixelFormatEnums.BGR8);
                            _imageQueue.Enqueue(new PtGreyCameraImage
                            {
                                FrameId = (ulong)imgCounter,
                                TimeStamp = (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds,
                                Image = new System.Drawing.Bitmap(temp.bitmap)
                                // for testing
                                //Image = new System.Drawing.Bitmap(bmp)
                            }
                            );
                        }
                        RaiseImageEnqueuedEvent(new ImageEnqueuedEventArgs(imgCounter));
                        _wh.Set();
                        Debug.WriteLine("enqueue frame: {0}", imgCounter);
                    }
                    else
                    {
                        //Debug.WriteLine("Dropped frame");
                    }

                }
                image.Release();
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
            }
            catch (Exception ex1)
            {
                Debug.WriteLine("Error: {0}", ex1.Message);
            }
            finally
            {
                GC.Collect();
            }
        }
    }

    class PtGreyCamera : Camera, IDisposable
    {
        ManagedSystem system = null;
        IManagedCamera camera;
        INodeMap nodeMap = null;

        public ImageEventListener imageEventListener = null;
        Queue<PtGreyCameraImage> imageQueue = null;
        EventWaitHandle _wh = new AutoResetEvent(false);
        readonly object _locker = new object();

        bool _capturingImage = false;
        bool _stopWork = false;

        public PtGreyCamera()
        {
            imageQueue = new Queue<PtGreyCameraImage>();

            imageEventListener = new ImageEventListener(imageQueue, _wh, _locker);
        }

        public void Dispose()
        {
            Close();
            _wh.Close();
        }

        public override bool Connect()
        {
            bool result = false;
            try
            {
                system = new ManagedSystem();
                IList<IManagedCamera> camList = system.GetCameras();

                if (camList.Count != 1)
                {
                    int count = camList.Count;
                    foreach (IManagedCamera mc in camList)
                        mc.Dispose();

                    // Clear camera list before releasing system
                    camList.Clear();

                    // Release system
                    system.Dispose();
                    throw new Exception("Only one camera should be connected, but found " + count);
                }

                camera = camList[0];
                // Initialize camera
                camera.Init();

                SerialNumber = Convert.ToUInt32(camera.DeviceSerialNumber);
                FirmwareVersion = camera.DeviceFirmwareVersion;

                // Retrieve GenICam nodemap
                nodeMap = camera.GetNodeMap();

                //initialise settings
                DefaultSettings();

                ImageWidth = camera.Width;
                ImageHeight = camera.Height;

                result = true;
            }
            catch (Exception /*ex*/)
            {
                //App.LogEntry.AddEntry("Failed to Connect to Point Grey Camera : " + ex.Message);
                result = false;
            }

            return result;
        }

        public Queue<PtGreyCameraImage> GetImageQueue()
        {
            return imageQueue;
        }

        public override bool DefaultSettings()
        {
            string sensorType = "CMOS";
            bool result = false;
            try
            {
                StopCapture();
                //if (RestoreDefaultSettings())
                {
                    SetAbsolutePropertyValue("PixelFormat", "RGB8");
                    SetAbsolutePropertyValue("VideoMode", "Continuous");
                    configTrigger(TriggerMode.Off, TriggerType.Unknown);
                    int width = 0, height = 0;
                    if (sensorType == "CCD")
                    {
                        //width = 1920;
                        //height = 1440;
                        SetAbsolutePropertyValue("Binning", "1");
                        width = Convert.ToInt32(GetPropertyValue("WidthMax"));
                        height = Convert.ToInt32(GetPropertyValue("HeightMax"));
                    }
                    else if (sensorType == "CMOS")
                    {
                        // set binning
                        SetAbsolutePropertyValue("Binning", "1");
                        width = Convert.ToInt32(GetPropertyValue("WidthMax"));
                        height = Convert.ToInt32(GetPropertyValue("HeightMax"));

                        // different camare has different contron property names
                        //SetAbsolutePropertyValue("BinningControl", "Average");
                    }
                    SetAbsolutePropertyValue("OffsetX", "0");
                    SetAbsolutePropertyValue("OffsetY", "0");
                    SetAbsolutePropertyValue("Width", width.ToString());// 1920
                    SetAbsolutePropertyValue("Height", height.ToString());//1440
                    Console.WriteLine("image size: {0}x{1}", width, height);

                    //SetCameraVideoModeAndFrameRate();

                    SetStreamBufferCount(1); // test only
                    SetAbsolutePropertyValue("StreamBufferMode", "NewestOnly");

                    result = true;
                }

            }
            catch (Exception ex)
            {
                result = false;
                Console.WriteLine("PtGreyCamera exception: " + ex.Message);
            }

            return result;
        }

        public string GetPropertyValue(string property, bool valueB = false)
        {
            if (property == "Shutter")
            {
                IFloat node = nodeMap.GetNode<IFloat>("ExposureTime");
                return node.Value.ToString();
            }
            else if (property == "DeviceTemperature")
            {
                IFloat node = nodeMap.GetNode<IFloat>("DeviceTemperature");
                return node.Value.ToString();
            }
            else if (property == "WidthMax")
            {
                IInteger node = nodeMap.GetNode<IInteger>("WidthMax");
                return node.Value.ToString();
            }
            else if (property == "HeightMax")
            {
                IInteger node = nodeMap.GetNode<IInteger>("HeightMax");
                return node.Value.ToString();
            }
            else if (property == "FrameRate")
            {
                IFloat node = nodeMap.GetNode<IFloat>("AcquisitionFrameRate");
                return node.Value.ToString();
            }
            else
            {
                IEnum node = nodeMap.GetNode<IEnum>(property);
                return node.Value.ToString();
            }
        }

        public void SetProprtyAutomaticSetting(string property, bool automatic)
        {
            try
            {
                if (property == "Gain")
                {
                    IEnum gainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        gainAuto.Value = "Continuous";
                    }
                    else
                    {
                        gainAuto.Value = "Off";
                    }
                }
                else if (property == "Shutter")
                {
                    IEnum exposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        exposureAuto.Value = "Continuous";
                    }
                    else
                    {
                        exposureAuto.Value = "Off";
                    }
                }
                else if (property == "Sharpness")
                {
                    IEnum sharpnessAuto = nodeMap.GetNode<IEnum>("SharpnessAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        sharpnessAuto.Value = "Continuous";
                    }
                    else
                    {
                        sharpnessAuto.Value = "Off";
                    }
                }
                else if (property == "FrameRate")
                {
                    IEnum framerateAuto = nodeMap.GetNode<IEnum>("AcquisitionFrameRateAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        framerateAuto.Value = "Continuous";
                    }
                    else
                    {
                        framerateAuto.Value = "Off";
                    }
                }
                else if (property == "Saturation")
                {
                    IEnum saturationAuto = nodeMap.GetNode<IEnum>("SaturationAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        saturationAuto.Value = "Continuous";
                    }
                    else
                    {
                        saturationAuto.Value = "Off";
                    }
                }
                else if (property == "WhiteBalance")
                {
                    IEnum whiteBalanceAuto = nodeMap.GetNode<IEnum>("BalanceWhiteAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        IEnumEntry iBalanceWhiteAutoModeContinuous = whiteBalanceAuto.GetEntryByName("Continuous");
                        if (iBalanceWhiteAutoModeContinuous?.IsReadable == true)
                        {
                            whiteBalanceAuto.Value = iBalanceWhiteAutoModeContinuous.Symbolic;
                        }
                    }
                    else
                    {
                        whiteBalanceAuto.Value = "Off";
                    }
                }
                else if (property == "ExposureCompensationAuto")
                {
                    IEnum expoCompAuto = nodeMap.GetNode<IEnum>("pgrExposureCompensationAuto");
                    if (automatic)
                    {
                        // TODO: may have other selection such as "Once"
                        IEnumEntry iExpoCompAutoModeContinuous = expoCompAuto.GetEntryByName("Continuous");
                        if (iExpoCompAutoModeContinuous?.IsReadable == true)
                        {
                            expoCompAuto.Value = iExpoCompAutoModeContinuous.Symbolic;
                        }
                    }
                    else
                    {
                        expoCompAuto.Value = "Off";
                    }
                }
                else
                {
                    Debug.WriteLine("Error: SetPropertyAutomaticSetting for " + property + " not implemented.");
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetPropertyAutomaticSetting for " + property + " exceptoin: " + e.Message);
            }
        }

        public void SetAbsolutePropertyValue(string property, string newValue)
        {
            try
            {
                if (property == "Hue")
                {
                    IFloat hue = nodeMap.GetNode<IFloat>("Hue");
                    hue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "Gamma")
                {
                    IFloat gamma = nodeMap.GetNode<IFloat>("Gamma");
                    gamma.Value = Convert.ToDouble(newValue);
                }
                else if (property == "OffsetX")
                {
                    IInteger x = nodeMap.GetNode<IInteger>("OffsetX");
                    x.Value = Convert.ToInt32(newValue);
                }
                else if (property == "OffsetY")
                {
                    IInteger y = nodeMap.GetNode<IInteger>("OffsetY");
                    y.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Width")
                {
                    IInteger width = nodeMap.GetNode<IInteger>("Width");
                    width.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Height")
                {
                    IInteger height = nodeMap.GetNode<IInteger>("Height");
                    height.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Gain")
                {
                    IEnum gainAuto = nodeMap.GetNode<IEnum>("GainAuto");
                    gainAuto.Value = "Off";

                    IFloat gainValue = nodeMap.GetNode<IFloat>("Gain");
                    gainValue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "Saturation")
                {
                    IEnum saturationAuto = nodeMap.GetNode<IEnum>("SaturationAuto");
                    saturationAuto.Value = "Off";

                    IFloat saturationValue = nodeMap.GetNode<IFloat>("Saturation");
                    saturationValue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "Binning")
                {
                    IInteger binningValue = nodeMap.GetNode<IInteger>("BinningVertical");
                    binningValue.Value = Convert.ToInt32(newValue);
                }
                else if (property == "Shutter")
                {
                    IFloat shutterValue = nodeMap.GetNode<IFloat>("ExposureTime");
                    shutterValue.Value = Convert.ToDouble(newValue) * 1000;
                }
                else if (property == "FrameRate")
                {
                    IEnum frameRateAuto = nodeMap.GetNode<IEnum>("AcquisitionFrameRateAuto");
                    frameRateAuto.Value = "Off";

                    IFloat frameRateValue = nodeMap.GetNode<IFloat>("AcquisitionFrameRate");
                    frameRateValue.Value = Convert.ToDouble(newValue);
                }
                else if (property == "PixelFormat")
                {
                    IEnum pixelFormat = nodeMap.GetNode<IEnum>("PixelFormat");
                    IEnumEntry pixelFormatItem = pixelFormat.GetEntryByName(newValue);

                    if (pixelFormatItem?.IsReadable == true)
                    {
                        pixelFormat.Value = pixelFormatItem.Symbolic;
                    }
                }
                else if (property == "VideoMode")
                {
                    IEnum acquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                    if (acquisitionMode?.IsWritable == true)
                    {
                        IEnumEntry acquisitionModeItem = acquisitionMode.GetEntryByName(newValue);
                        if (acquisitionModeItem?.IsReadable == true)
                        {
                            acquisitionMode.Value = acquisitionModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "BinningControl")
                {
                    IEnum acquisitionMode = nodeMap.GetNode<IEnum>("BinningControl");
                    if (acquisitionMode?.IsWritable == true)
                    {
                        IEnumEntry acquisitionModeItem = acquisitionMode.GetEntryByName(newValue);
                        if (acquisitionModeItem?.IsReadable == true)
                        {
                            acquisitionMode.Value = acquisitionModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "ShutterMode")
                {
                    IEnum exposureMode = nodeMap.GetNode<IEnum>("ExposureMode");
                    if (exposureMode?.IsWritable == true)
                    {
                        IEnumEntry exposureModeItem = exposureMode.GetEntryByName(newValue);
                        if (exposureModeItem?.IsReadable == true)
                        {
                            exposureMode.Value = exposureModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "StreamBufferMode")
                {
                    INodeMap nodeMapStream = camera.GetTLStreamNodeMap();
                    IEnum bufferMode = nodeMapStream.GetNode<IEnum>("StreamBufferHandlingMode");
                    if (bufferMode?.IsWritable == true)
                    {
                        IEnumEntry bufferModeItem = bufferMode.GetEntryByName(newValue);
                        if (bufferModeItem?.IsReadable == true)
                        {
                            bufferMode.Value = bufferModeItem.Symbolic;
                        }
                        else
                        {
                            Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property);
                    }
                }
                else if (property == "ExposureCompensation")
                {
                    IFloat expoCompensation = nodeMap.GetNode<IFloat>("pgrExposureCompensation");
                    expoCompensation.Value = Convert.ToDouble(newValue);
                }
                else
                {
                    Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property + " not implemented.");
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetAbsolutePropertyValue for " + property + " exceptoin: " + e.Message);
            }

        }

        public void SetProprtyEnabledSetting(string property, bool enabled)
        {
            try
            {
                BoolNode boolNode;
                if (property == "Gamma")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("GammaEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "Sharpness")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("SharpnessEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "Hue")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("HueEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "Saturation")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("SaturationEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else if (property == "FrameRate")
                {
                    boolNode = nodeMap.GetNode<BoolNode>("AcquisitionFrameRateEnabled");
                    if (boolNode?.IsReadable == true)
                    {
                        boolNode.Value = enabled;
                    }
                    else
                    {
                        Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not readable", property);
                    }
                }
                else
                {
                    Debug.WriteLine("Error: SetProprtyEnabledSetting {0} not implemented", property);
                }
            }
            catch (SpinnakerException e)
            {
                Debug.WriteLine("Error: SetProprtyEnabledSetting " + property + " exceptoin: " + e.Message);
            }
        }

        bool RestoreDefaultSettings()
        {
            bool result = false;
            try
            {
                #region default_settings
                IEnum iUserSetSelector = nodeMap.GetNode<IEnum>("UserSetSelector");
                if (iUserSetSelector == null || !iUserSetSelector.IsWritable)
                {
                    return false;
                }

                IEnumEntry iUserSetSelectorDefault = iUserSetSelector.GetEntryByName("Default");
                if (iUserSetSelectorDefault == null || !iUserSetSelectorDefault.IsReadable)
                {
                    return false;
                }

                iUserSetSelector.Value = iUserSetSelectorDefault.Symbolic;

                ICommand iUserSetLoad = nodeMap.GetNode<ICommand>("UserSetLoad");
                if (iUserSetLoad == null || !iUserSetLoad.IsWritable)
                {
                    return false;
                }

                iUserSetLoad.Execute();
                #endregion

                result = true;
            }
            catch (Exception ex)
            {
                result = false;
                Console.WriteLine("PtGreyCamera exception: " + ex.Message);
            }

            return result;
        }

        bool SetStreamBufferCount(long count)
        {
            try
            {
                // set to manual
                INodeMap sNodeMap = camera.GetTLStreamNodeMap();
                IEnum sBufferCountSelector = sNodeMap.GetNode<IEnum>("StreamBufferCountMode");
                if (sBufferCountSelector == null || !sBufferCountSelector.IsWritable)
                {
                    return false;
                }
                IEnumEntry iBufferCountManual = sBufferCountSelector.GetEntryByName("Manual");
                if (iBufferCountManual == null || !iBufferCountManual.IsReadable)
                {
                    return false;
                }
                sBufferCountSelector.Value = iBufferCountManual.Symbolic;

                // set the value
                IInteger streamNode = sNodeMap.GetNode<IInteger>("StreamDefaultBufferCount");
                if (streamNode == null || !streamNode.IsWritable)
                {
                    return false;
                }

                streamNode.Value = count;
            }
            catch
            {
                return false;
            }

            return true;
        }

        public bool SetFrameRate(double value)
        {
            bool result = false;

            try
            {
                camera.AcquisitionFrameRate.Value = value;
                result = true;
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
                result = false;
            }


            return result;
        }

        void SetCameraVideoModeAndFrameRate()
        {
            bool restartCapture = _capturingImage;
            StopCapture();
            try
            {
                //camera.SetVideoModeAndFrameRate(newVideoMode, newFrameRate);
                // Set acquisition mode to continuous
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (node retrieval). Aborting...\n");
                    restartCapture = false;
                }

                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionMode.IsReadable)
                {
                    Console.WriteLine("Unable to set acquisition mode to continuous (enum entry retrieval). Aborting...\n");
                    restartCapture = false;
                }

                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                // set framerate 30
                SetProprtyEnabledSetting("FrameRate", true);

                //SetProprtyAutomaticSetting("FrameRate", false);
                SetAbsolutePropertyValue("FrameRate", "25");
            }
            catch (SpinnakerException ex)
            {
                throw ex;
            }

            if (restartCapture)
                StartCapture();
        }

        public override void StartCapture()
        {
            try
            {
                if (camera != null && _capturingImage == false)
                {
                    // Configure image events
                    camera.RegisterEvent(imageEventListener);

                    // Begin acquiring images
                    camera.BeginAcquisition();
                    _capturingImage = true;
                }
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
            }
        }

        public override void StopCapture()
        {
            try
            {
                if (_capturingImage && camera != null)
                {
                    //camera.UnregisterEvent(imageEventListener);
                    camera.EndAcquisition();
                    _capturingImage = false;
                }
            }
            catch (SpinnakerException ex)
            {
                Debug.WriteLine("Error: {0}", ex.Message);
            }
        }

        public override BitmapSource GetImage()
        {
            throw new NotImplementedException();
        }

        public override void EditCameraSettings()
        {
            CameraSettings camDlg = new CameraSettings(camera);
            camDlg.ShowDialog();
        }

        public void Close()
        {
            if(camera != null && _capturingImage)
            {
                DisConnect();
            }

            if(_stopWork == false)
            {
                _stopWork = true;
                _wh.Set();
            }

            if (system != null)
                system.Dispose();
        }

        public override void DisConnect()
        {
            try
            {
                if (camera != null)
                {
                    StopCapture();
                    camera.UnregisterEvent(imageEventListener);
                    camera.DeInit();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("PtGrey Disconnect : " + ex.Message);
            }
        }

        public override void InitCalibrationSettings()
        {
            throw new NotImplementedException();
        }

        public override void ResetSettings()
        {
            throw new NotImplementedException();
        }

        public void Trigger()
        {
            // software trigger the camera
            try
            {
                ICommand iTrigger = nodeMap.GetNode<ICommand>("TriggerSoftware");
                if (iTrigger == null || !iTrigger.IsWritable)
                {
                    return;
                }

                iTrigger.Execute();
            } catch(Exception e)
            {
                Console.WriteLine("PtGreyCamera: " + e.Message);
            }
        }

        public void StartMeasuring(bool refresh)
        {
            if (refresh && imageQueue != null)
            {
                imageQueue.Clear();
                imageEventListener.imgCounter = 0;
            }
            imageEventListener._measuring = true;
        }

        public void StopMeasuring()
        {
            imageEventListener._measuring = false ;
        }

        public bool configTrigger(TriggerMode triggerMode, TriggerType triggerType, uint count = 1)
        {
            bool result = false;
            if (triggerMode == TriggerMode.Off)
            {
                IEnum triMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (triMode == null || !triMode.IsWritable)
                {
                    Console.WriteLine("configTrigger: Unable to disable trigger mode (enum retrieval). Aborting...");
                    return false;
                }

                IEnumEntry iTriggerModeOff = triMode.GetEntryByName("Off");
                if (iTriggerModeOff == null || !iTriggerModeOff.IsReadable)
                {
                    Console.WriteLine("configTrigger: Unable to disable trigger mode (entry retrieval). Aborting...");
                    return false;
                }
                triMode.Value = iTriggerModeOff.Value;
                result = true;
            }
            else if (triggerMode == TriggerMode.On)
            {
                IEnum triMode = nodeMap.GetNode<IEnum>("TriggerMode");
                if (triMode == null || !triMode.IsWritable)
                {
                    Console.WriteLine("configTrigger: Unable to enable trigger mode (enum retrieval). Aborting...");
                    return false;
                }

                IEnumEntry iTriggerModeOn = triMode.GetEntryByName("On");
                if (iTriggerModeOn == null || !iTriggerModeOn.IsReadable)
                {
                    Console.WriteLine("configTrigger: Unable to enable trigger mode (entry retrieval). Aborting...");
                    return false;
                }
                triMode.Value = iTriggerModeOn.Value;

                IEnum triggerSource = nodeMap.GetNode<IEnum>("TriggerSource");
                if (triggerType == TriggerType.Software)
                {
                    // Set trigger mode to software
                    IEnumEntry iTriggerSourceSoftware = triggerSource.GetEntryByName("Software");
                    if (iTriggerSourceSoftware == null || !iTriggerSourceSoftware.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set software trigger mode (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerSource.Value = iTriggerSourceSoftware.Value;

                    Console.WriteLine("configTrigger: Trigger source set to software...");
                }
                else if (triggerType == TriggerType.Hardware)
                {
                    // Set trigger mode to hardware ('Line0')
                    IEnumEntry iTriggerSourceHardware = triggerSource.GetEntryByName("Line0");
                    if (iTriggerSourceHardware == null || !iTriggerSourceHardware.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set hardware trigger mode (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerSource.Value = iTriggerSourceHardware.Value;

                    Console.WriteLine("configTrigger: Trigger source set to hardware...");
                }
                else
                {
                    Console.WriteLine("configTrigger: Trigger source Unknown");
                    return false;
                }

                {
                    IEnum triggerSelector = nodeMap.GetNode<IEnum>("TriggerSelector");
                    IEnumEntry iTriggerSelector = triggerSelector.GetEntryByName("FrameStart");
                    if (iTriggerSelector == null || !iTriggerSelector.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set trigger selector (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerSelector.Value = iTriggerSelector.Value;
                }
                {
                    IEnum triggerActivation = nodeMap.GetNode<IEnum>("TriggerActivation");
                    triggerActivation.Value = "RisingEdge";
                    IEnumEntry iTriggerActivation = triggerActivation.GetEntryByName("RisingEdge");
                    if (iTriggerActivation == null || !iTriggerActivation.IsReadable)
                    {
                        Console.WriteLine("configTrigger: Unable to set trigger activation (entry retrieval). Aborting...");
                        return false;
                    }
                    triggerActivation.Value = iTriggerActivation.Value;
                }
                // multi frame 
                if (count >= 1)
                {
                    IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");

                    IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("MultiFrame");

                    iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;

                    IInteger acquCount = nodeMap.GetNode<IInteger>("AcquisitionFrameCount");
                    acquCount.Value = count;

                    //single frame acquisition mode set to triggered
                    {
                        IEnum iSingleFrameAcquisitionMode = nodeMap.GetNode<IEnum>("SingleFrameAcquisitionMode");

                        IEnumEntry iSingleAcquisitionMode = iSingleFrameAcquisitionMode.GetEntryByName("Triggered");

                        iSingleFrameAcquisitionMode.Value = iSingleAcquisitionMode.Symbolic;
                    }
                }
                else
                {
                    IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");

                    IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");

                    iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;
                }


                result = true;
            }
            return result;
        }
    }
}
