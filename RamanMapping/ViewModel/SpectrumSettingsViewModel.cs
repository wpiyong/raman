using Microsoft.Research.DynamicDataDisplay.DataSources;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using ViewModelLib;

namespace RamanMapping.ViewModel
{
    class SpectrumSettingsViewModel : ViewModelBase
    {
        SpectrometerViewModel _spectroMeterVM;
        XYZAxesViewModel _xyzAxesVM;
        StatusViewModel _statusVM;

        public SpectrumSettingsViewModel(SpectrometerViewModel spectroMeterVM, XYZAxesViewModel xyzAxesVM, StatusViewModel statusVM)
        {
            base.DisplayName = "SpectrumSettingsViewModel";
            _spectroMeterVM = spectroMeterVM;
            _xyzAxesVM = xyzAxesVM;
            _statusVM = statusVM;
            IntegrationTime = GlobalVariables.spectrometerSettings.IntegrationTime;
            NumAverage = GlobalVariables.spectrometerSettings.NumAverages;
            Delay = GlobalVariables.spectrometerSettings.Delay;
            Counts = GlobalVariables.spectrometerSettings.Counts;

            _spectroMeterVM.AddLiveViewUpdateSubscriber(new LiveViewUpdateHandler(LiveViewUpdate));

            JogX = new RelayCommand(parm => Jog(0, GlobalVariables.motorSettings.JogVelX, int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            JogY = new RelayCommand(parm => Jog(1, GlobalVariables.motorSettings.JogVelY, int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            JogZ = new RelayCommand(parm => Jog(2, GlobalVariables.motorSettings.JogVelZ, int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            JogStop = new RelayCommand(parm => Stop(int.Parse(parm.ToString())));
            CommandUpdateSettings = new RelayCommand(param => UpdateSettings(param));
            CommandCancelSettings = new RelayCommand(param => CancelSettings(param));
        }
        #region property
        public bool XEnabled
        {
            get
            {
                return _xyzAxesVM.XEnabled;
            }
        }

        public bool YEnabled
        {
            get
            {
                return _xyzAxesVM.YEnabled;
            }
        }

        double _integrationTime;
        public double IntegrationTime
        {
            get
            {
                return _integrationTime;
            }
            set
            {
                if(_integrationTime == value)
                {
                    return;
                }
                _integrationTime = value;
                OnPropertyChanged("IntegrationTime");
                GlobalVariables.spectrometerSettings.IntegrationTime = IntegrationTime;
                _spectroMeterVM.UpdateSettings();
            }
        }

        uint _numAverage;
        public uint NumAverage
        {
            get
            {
                return _numAverage;
            }
            set
            {
                if(_numAverage == value)
                {
                    return;
                }
                _numAverage = value;
                OnPropertyChanged("NumAverage");
                GlobalVariables.spectrometerSettings.NumAverages = NumAverage;
                _spectroMeterVM.UpdateSettings();
            }
        }

        long _delay;
        public long Delay
        {
            get
            {
                return _delay;
            }
            set
            {
                if(_delay == value)
                {
                    return;
                }
                _delay = value;
                OnPropertyChanged("Delay");
                GlobalVariables.spectrometerSettings.Delay = Delay;
                //_spectroMeterVM.UpdateSettings();
            }
        }

        uint _counts;
        public uint Counts
        {
            get
            {
                return _counts;
            }
            set
            {
                if(_counts == value)
                {
                    return;
                }
                _counts = value;
                OnPropertyChanged("Counts");
                GlobalVariables.spectrometerSettings.Counts = Counts;
                //_spectroMeterVM.UpdateSettings();
            }
        }

        ObservableDataSource<Point> _phosSpectrum;
        public ObservableDataSource<Point> PhosSpectrum
        {
            get
            {
                return _phosSpectrum;
            }
            set
            {
                _phosSpectrum = value;
                OnPropertyChanged("PhosSpectrum");
            }
        }

        #endregion

        public void LiveViewUpdate()
        {
            PhosSpectrum = _spectroMeterVM.Spectrum;
        }

        public RelayCommand JogX { get; private set; }
        public RelayCommand JogY { get; private set; }
        public RelayCommand JogZ { get; private set; }
        public RelayCommand JogStop { get; private set; }
        public RelayCommand CommandUpdateSettings { get; set; }
        public RelayCommand CommandCancelSettings { get; set; }

        void Jog(int axisNo, double vel, int direction)
        {
            _xyzAxesVM.Jog(axisNo, vel, direction);
        }

        public void Stop(int axisNo)
        {
            _xyzAxesVM.Stop(axisNo);
        }

        public void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            _spectroMeterVM.StartLiveView();
        }

        void CancelSettings(object param)
        {
            _spectroMeterVM.RemoveLiveViewUpdateSubscriber(new LiveViewUpdateHandler(LiveViewUpdate));
            _spectroMeterVM.StopLiveView();
            while (_spectroMeterVM.SpectrometerBusy)
            {
                Thread.Sleep(200);
            }
            ((Window)param).Close();
        }

        public void OnViewClosing(object sender, CancelEventArgs e)
        {
            _spectroMeterVM.RemoveLiveViewUpdateSubscriber(new LiveViewUpdateHandler(LiveViewUpdate));
            _spectroMeterVM.StopLiveView();
            while (_spectroMeterVM.SpectrometerBusy)
            {
                Thread.Sleep(200);
            }
            e.Cancel = false;
            //((Window)sender).Close();
        }

        void UpdateSettings(object param)
        {
            bool settingsChanged = false;
            if (GlobalVariables.spectrometerSettings.IntegrationTime != IntegrationTime)
            {
                settingsChanged = true;
                GlobalVariables.spectrometerSettings.IntegrationTime = IntegrationTime;
            }

            if (GlobalVariables.spectrometerSettings.NumAverages != NumAverage)
            {
                settingsChanged = true;
                GlobalVariables.spectrometerSettings.NumAverages = NumAverage;
            }

            if (GlobalVariables.spectrometerSettings.Delay != Delay)
            {
                settingsChanged = true;
                GlobalVariables.spectrometerSettings.Delay = Delay;
            }

            if (GlobalVariables.spectrometerSettings.Counts != Counts)
            {
                settingsChanged = true;
                GlobalVariables.spectrometerSettings.Counts = Counts;
            }

            GlobalVariables.spectrometerSettings.Save();

            if (settingsChanged)
            {
                _spectroMeterVM.UpdateSettings();
            }
            _spectroMeterVM.RemoveLiveViewUpdateSubscriber(new LiveViewUpdateHandler(LiveViewUpdate));
            _spectroMeterVM.StopLiveView();
            while (_spectroMeterVM.SpectrometerBusy)
            {
                Thread.Sleep(200);
            }
            ((Window)param).Close();
        }

        #region dispose

        private bool _disposed = false;

        void Close()
        {
            _spectroMeterVM.RemoveLiveViewUpdateSubscriber(new LiveViewUpdateHandler(LiveViewUpdate));
            _spectroMeterVM.StopLiveView();
            while (_spectroMeterVM.SpectrometerBusy)
            {
                Thread.Sleep(200);
            }
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
