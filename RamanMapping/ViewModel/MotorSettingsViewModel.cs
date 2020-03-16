using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ViewModelLib;

namespace RamanMapping.ViewModel
{
    public delegate void MotorSettingsChangedHandler();
    public delegate void RefPointChangedHandler();

    class MotorSettingsViewModel : ViewModelBase
    {
        public event MotorSettingsChangedHandler MotorSettingsChanged;
        public event RefPointChangedHandler RefPointChanged;

        public MotorSettingsViewModel()
        {
            base.DisplayName = "SettingsViewModel";
            Velocity = GlobalVariables.motorSettings.Velocity;
            Acceleration = GlobalVariables.motorSettings.Acceleration;
            Deceleration = GlobalVariables.motorSettings.Deceleration;
            JogX = GlobalVariables.motorSettings.JogVelX;
            JogY = GlobalVariables.motorSettings.JogVelY;
            JogZ = GlobalVariables.motorSettings.JogVelZ;
            XPixelInMM = GlobalVariables.motorSettings.XPixelInMM;
            YPixelInMM = GlobalVariables.motorSettings.YPixelInMM;
            XRef = GlobalVariables.motorSettings.XRef;
            YRef = GlobalVariables.motorSettings.YRef;

            CommandUpdateSettings = new RelayCommand(param => UpdateSettings(param));
            CommandCancelSettings = new RelayCommand(param => CancelSettings(param));
        }

        public RelayCommand CommandUpdateSettings { get; set; }
        public RelayCommand CommandCancelSettings { get; set; }

        double _velocity;
        public double Velocity
        {
            get
            {
                return _velocity;
            }
            set
            {
                if(_velocity == value)
                {
                    return;
                } else
                {
                    _velocity = value;
                    OnPropertyChanged("Velocity");
                }
            }
        }

        double _acceleration;
        public double Acceleration
        {
            get
            {
                return _acceleration;
            }
            set
            {
                if (_acceleration == value)
                {
                    return;
                }
                else
                {
                    _acceleration = value;
                    OnPropertyChanged("Acceleration");
                }
            }
        }

        double _deceleration;
        public double Deceleration
        {
            get
            {
                return _deceleration;
            }
            set
            {
                if (_deceleration == value)
                {
                    return;
                }
                else
                {
                    _deceleration = value;
                    OnPropertyChanged("Deceleration");
                }
            }
        }

        double _jogX;
        public double JogX
        {
            get
            {
                return _jogX;
            }
            set
            {
                if (_jogX == value)
                {
                    return;
                }
                else
                {
                    _jogX = value;
                    OnPropertyChanged("JogX");
                }
            }
        }

        double _jogY;
        public double JogY
        {
            get
            {
                return _jogY;
            }
            set
            {
                if (_jogY == value)
                {
                    return;
                }
                else
                {
                    _jogY = value;
                    OnPropertyChanged("JogY");
                }
            }
        }

        double _jogZ;
        public double JogZ
        {
            get
            {
                return _jogZ;
            }
            set
            {
                if (_jogZ == value)
                {
                    return;
                }
                else
                {
                    _jogZ = value;
                    OnPropertyChanged("JogZ");
                }
            }
        }

        double _xPixelInMM;
        public double XPixelInMM
        {
            get
            {
                return _xPixelInMM;
            }
            set
            {
                if(_xPixelInMM == value)
                {
                    return;
                }
                _xPixelInMM = value;
                OnPropertyChanged("XPixelInMM");
            }
        }

        double _yPixelInMM;
        public double YPixelInMM
        {
            get
            {
                return _yPixelInMM;
            }
            set
            {
                if (_yPixelInMM == value)
                {
                    return;
                }
                _yPixelInMM = value;
                OnPropertyChanged("YPixelInMM");
            }
        }

        double _xRef;
        public double XRef
        {
            get
            {
                return _xRef;
            }
            set
            {
                if(_xRef == value)
                {
                    return;
                }
                _xRef = value;
                OnPropertyChanged("XRef");
            }
        }

        double _yRef;
        public double YRef
        {
            get
            {
                return _yRef;
            }
            set
            {
                if(_yRef == value)
                {
                    return;
                }
                _yRef = value;
                OnPropertyChanged("YRef");
            }
        }

        double _xStep;
        public double XStep
        {
            get
            {
                return _xStep;
            }
            set
            {
                if (_xStep == value)
                {
                    return;
                }
                _xStep = value;
                OnPropertyChanged("XStep");
            }
        }

        double _yStep;
        public double YStep
        {
            get
            {
                return _yStep;
            }
            set
            {
                if (_yStep == value)
                {
                    return;
                }
                _yStep = value;
                OnPropertyChanged("YStep");
            }
        }

        void CancelSettings(object param)
        {
            ((Window)param).Close();
        }

        void UpdateSettings(object param)
        {
            bool settingsChanged = false;
            if(GlobalVariables.motorSettings.Velocity != Velocity)
            {
                settingsChanged = true;
                GlobalVariables.motorSettings.Velocity = Velocity;
            }

            if (GlobalVariables.motorSettings.Acceleration != Acceleration)
            {
                settingsChanged = true;
                GlobalVariables.motorSettings.Acceleration = Acceleration;
            }

            if (GlobalVariables.motorSettings.Deceleration != Deceleration)
            {
                settingsChanged = true;
                GlobalVariables.motorSettings.Deceleration = Deceleration;
            }

            if (GlobalVariables.motorSettings.JogVelX != JogX)
            {
                GlobalVariables.motorSettings.JogVelX = JogX;
            }

            if (GlobalVariables.motorSettings.JogVelY != JogY)
            {
                GlobalVariables.motorSettings.JogVelY = JogY;
            }

            if (GlobalVariables.motorSettings.JogVelZ != JogZ)
            {
                GlobalVariables.motorSettings.JogVelZ = JogZ;
            }

            if(GlobalVariables.motorSettings.XPixelInMM != XPixelInMM)
            {
                GlobalVariables.motorSettings.XPixelInMM = XPixelInMM;
            }

            if(GlobalVariables.motorSettings.YPixelInMM != YPixelInMM)
            {
                GlobalVariables.motorSettings.YPixelInMM = YPixelInMM;
            }

            if (GlobalVariables.motorSettings.XStep != XStep)
            {
                GlobalVariables.motorSettings.XStep = XStep;
            }

            if (GlobalVariables.motorSettings.YStep != YStep)
            {
                GlobalVariables.motorSettings.YStep = YStep;
            }

            if (GlobalVariables.motorSettings.XRef != XRef || GlobalVariables.motorSettings.YRef != YRef)
            {
                GlobalVariables.motorSettings.XRef = XRef;
                GlobalVariables.motorSettings.YRef = YRef;
                RaiseRefPointChangedEvent();
            }

            if (settingsChanged)
            {
                RaiseMotorSettingsChangedEvent();
            }

            GlobalVariables.motorSettings.Save();

            ((Window)param).Close();
        }

        void RaiseMotorSettingsChangedEvent()
        {
            MotorSettingsChanged?.Invoke();
        }

        void RaiseRefPointChangedEvent()
        {
            RefPointChanged?.Invoke();
        }

        public void AddMotorSettingsChangedSubscriber(MotorSettingsChangedHandler handler)
        {
            MotorSettingsChanged += handler;
        }

        public void AddRefPointChangedSubscriber(RefPointChangedHandler handler)
        {
            RefPointChanged += handler;
        }

        #region dispose

        private bool _disposed = false;

        void Close()
        {

        }

        protected override void OnDispose()
        {
            ViewModelDispose(true);
            GC.SuppressFinalize(this);
        }

        void ViewModelDispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Close();
                }
                _disposed = true;
            }
        }

        #endregion
    }
}
