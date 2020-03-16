using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SettingsLib;

namespace RamanMapping.Model
{
    public class MotorSettings : Settings
    {
        public MotorSettings()
            : base("motorSettings.config")
        {
        }

        public bool UseSimulator { get; set; }
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int HomingBuffer { get; set; }

        public double JogVelX { get; set; }
        public double JogVelY { get; set; }
        public double JogVelZ { get; set; }

        public double Velocity { get; set; }
        public double Acceleration { get; set; }
        public double Deceleration { get; set; }

        public double XPixelInMM { get; set; }
        public double YPixelInMM { get; set; }

        public double XRef { get; set; }
        public double YRef { get; set; }

        public double XStep { get; set; }
        public double YStep { get; set; }

        public double XRefPositionInMM { get; set; }
        public double YRefPositionInMM { get; set; }
        public double ZRefPositionInMM { get; set; }

        public string Preset1Position { get; set; }
        public string Preset2Position { get; set; }
        public string Preset3Position { get; set; }
        public string Preset4Position { get; set; }
        public string Preset5Position { get; set; }

    }
}
