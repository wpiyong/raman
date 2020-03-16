using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RamanMapping.Model
{
    public abstract class Camera
    {
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }
        public uint SerialNumber { get; set; }
        public string FirmwareVersion { get; set; }

        //abstract must be overriden
        public abstract bool Connect();
        public abstract void StartCapture();
        public abstract void StopCapture();
        public abstract BitmapSource GetImage();
        public abstract void EditCameraSettings();
        public abstract void DisConnect();

        public abstract void InitCalibrationSettings();
        public abstract void ResetSettings();
        public abstract bool DefaultSettings();

        //virtual may be overridden
        public virtual void RestoreNormalSettings() { }
        public virtual void BufferFrames(bool onOff) { }
        public virtual double Framerate { get; set; }
        public virtual void RestartCapture() { }

    }
}
