using System;
using System.Collections.Generic;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using ViewModelLib;
using RamanMapping.Model;
using Microsoft.Win32;
using System.IO;
using System.Timers;

namespace RamanMapping.ViewModel
{
    public delegate void Callback(object sender, object result);
    public delegate void LiveViewUpdateHandler();

    class AvantesSpectrum
    {
        public ulong Id { get; set; }
        public ulong TimeStamp { get; set; }
        public Point[] Spectrum { get; set; }
        public double IntegrationTime { get; set; }
        public uint NumAverages { get; set; }
    }

    class SpectrometerViewModel : ViewModelBase
    {
        StatusViewModel _statusVM;
        Stack<DateTime> _timestamps = new Stack<DateTime>();

        AvantesSpectrometer _avantes;
        AutoResetEvent _spectrumUpdated = new AutoResetEvent(false);
        AutoResetEvent _continuousMode = new AutoResetEvent(false);
        
        double[] _wavelengths;
        double[] _counts;

        uint _lastSpectrumTimestamp;
        ulong _phosStartTimestamp;
        List<AvantesSpectrum> _phosSpectra = new List<AvantesSpectrum>();
        ManualResetEvent _phosMeasureCompletionEvent = new ManualResetEvent(false);

        public event LiveViewUpdateHandler LiveViewUpdated;

        static bool timeOut = false;
        System.Timers.Timer aTimer = null;

        public bool LiveView = false;

        public SpectrometerViewModel(StatusViewModel svm)
        {
            base.DisplayName = "SpectrometerViewModel";

            _statusVM = svm;
            _avantes = new AvantesSpectrometer();

            CommandStop = new RelayCommand(param => Stop());
            CommandStart = new RelayCommand(param => Start(-1));
            CommandSnapshot = new RelayCommand(param => Start(1));
            CommandResetSettings = new RelayCommand(param => ResetSettings());
            CommandSave = new RelayCommand(param => Save());

            IntegrationTime = GlobalVariables.spectrometerSettings.IntegrationTime;
            NumAverages = GlobalVariables.spectrometerSettings.NumAverages;

            InitTimer(15000, false);
        }

        public void connectSpectrum()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += ConnectToSpectrometer;
            bw.RunWorkerCompleted += ConnectToSpectrometerCompleted;
            bw.RunWorkerAsync();
        }

        public RelayCommand CommandStop { get; set; }
        public RelayCommand CommandStart { get; set; }
        public RelayCommand CommandSnapshot { get; set; }
        public RelayCommand CommandResetSettings { get; set; }
        public RelayCommand CommandSave { get; set; }

        void RaiseLiveViewUpdateEvent()
        {
            LiveViewUpdated?.Invoke();
        }

        public void AddLiveViewUpdateSubscriber(LiveViewUpdateHandler h)
        {
            LiveViewUpdated += h;
        }

        public void RemoveLiveViewUpdateSubscriber(LiveViewUpdateHandler h)
        {
            LiveViewUpdated -= h;
        }

        #region properties
        public IntPtr MainWindowHandleForAvantesCallback { get; set; }

        bool _spectrometerConnected = false;
        public bool SpectrometerConnected
        {
            get
            {
                return _spectrometerConnected;
            }
            set
            {
                if(_spectrometerConnected == value)
                {
                    return;
                }
                _spectrometerConnected = value;
                OnPropertyChanged("SpectrometerConnected");
            }
        }
        ObservableDataSource<Point> _spectrum;
        object _spectrumLock = new object();
        public ObservableDataSource<Point> Spectrum
        {
            get { return _spectrum; }
            set
            {
                _spectrum = value;
                OnPropertyChanged("Spectrum");
                RaiseLiveViewUpdateEvent();
            }
        }

        double _integrationTime;
        public double IntegrationTime
        {
            get { return _integrationTime; }
            set
            {
                if (_integrationTime != value && value > 0)
                {
                    _integrationTime = value;
                    GlobalVariables.spectrometerSettings.IntegrationTime = value;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged("IntegrationTime");
                }
            }
        }

        uint _numAverages;
        public uint NumAverages
        {
            get { return _numAverages; }
            set
            {
                if (_numAverages != value && value >= 1 && value <= 10)
                {
                    _numAverages = value;
                    GlobalVariables.spectrometerSettings.NumAverages = value;
                    Properties.Settings.Default.Save();
                    OnPropertyChanged("NumAverages");
                }
            }
        }

        bool _spectrometerBusy;
        public bool SpectrometerBusy
        {
            get { return _spectrometerBusy; }
            set
            {
                _spectrometerBusy = value;
                OnPropertyChanged("SpectrometerBusy");
            }
        }

        bool _ready;
        public bool Ready
        {
            get { return _ready; }
            set
            {
                _ready = value;
                OnPropertyChanged("Ready");
            }
        }

        #endregion

        #region default_settings

        readonly int DEFAULT_INTEGRATION_TIME = 200;
        readonly uint DEFAULT_NUM_AVGS = 1;

        void ResetSettings()
        {
            IntegrationTime = DEFAULT_INTEGRATION_TIME;
            NumAverages = DEFAULT_NUM_AVGS;
        }
        #endregion

        private void InitTimer(double interval, bool autoReset)
        {
            if (aTimer != null)
            {
                aTimer.Stop();
                aTimer.Dispose();
            }
            aTimer = new System.Timers.Timer(interval);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = autoReset;
        }

        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            timeOut = true;
        }

        private void StartTimer()
        {
            timeOut = false;
            aTimer?.Start();
        }

        private void StopTimer()
        {
            aTimer?.Stop();
        }

        public void UpdateSettings()
        {
            IntegrationTime = GlobalVariables.spectrometerSettings.IntegrationTime;
            NumAverages = GlobalVariables.spectrometerSettings.NumAverages;

            if (LiveView)
            {
                StopLiveView();
                while (SpectrometerBusy)
                {
                    Thread.Sleep(200);
                }
                StartLiveView();
            }
        }

        #region connect_spectrometer

        void ConnectToSpectrometer(object sender, DoWorkEventArgs e)
        {
            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Trying to connect to spectrometer..." };
            _statusVM.SpecMessages.Add(sts);
            _timestamps.Push(timestamp);

            e.Result = null;
            _wavelengths = new double[0];

            try
            {
                if (_avantes.Connect())
                {
                    if (_avantes.GetWavelengths(out _wavelengths))
                        e.Result = _avantes.Name;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("connection to spectrometer exception: " + ex.Message);
                e.Result = null;
            }
        }

        void ConnectToSpectrometerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var ts = _timestamps.Pop();
            var sm = _statusVM.SpecMessages.Where(s => s.Timestamp == ts).First();
            _statusVM.SpecMessages.Remove(sm);
            _statusVM.Busy--;

            if (e.Result != null)
            {
                _statusVM.SpecMessages.Add(new StatusMessage { Timestamp = DateTime.Now, Message = "Connected to " + e.Result.ToString() });
                SpectrometerConnected = true;
                Ready = true;
                if (LiveView)
                {
                    Start(-1);
                }
            }
            else
            {
                var res = MessageBox.Show("Failed to connect to Spectrometer", "Spectrometer connection error, retry?", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += ConnectToSpectrometer;
                    bw.RunWorkerCompleted += ConnectToSpectrometerCompleted;
                    bw.RunWorkerAsync();
                }
                else
                {
                    var sts = new StatusMessage
                    {
                        Timestamp = DateTime.Now,
                        Message = "Spectrometer connect error!"
                    };
                    _statusVM.SpecMessages.Add(sts);
                }
            }
        }
        #endregion

        #region continuous_scan
        struct ScanParms
        {

            public short numScans;
            public uint delay;
            public double integrationTime;
            public uint numAverages;
        }
        bool Start(short numScans, uint delay = 0, double integrationTime = 10, uint numAverages = 1, bool externalTrigger = false)
        {
            try
            {
                {
                    ScanParms sp;
                    sp.delay = delay;
                    sp.numScans = numScans;
                    sp.integrationTime = integrationTime;
                    sp.numAverages = numAverages;

                    BackgroundWorker bw = new BackgroundWorker();
                    bw.DoWork += ScanDoWork;
                    bw.RunWorkerCompleted += ScanCompleted;
                    bw.RunWorkerAsync(sp);
                }

                Thread.Sleep(100);

                if (_avantes.StartMeasurement(MainWindowHandleForAvantesCallback, integrationTime,
                            numAverages, numScans, delay, externalTrigger))
                {
                    return true;
                }
                else
                    throw new Exception("error");
            }
            catch (Exception ex)
            {
                Console.WriteLine("spectrometer Start exception: " + ex.Message);
            }

            return false;
        }

        void ScanDoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                SpectrometerBusy = true;
                e.Result = false;
                _statusVM.Busy++;

                var sp = (ScanParms)(e.Argument);

                Point[] _xyData = new Point[0];
                lock (_spectrumLock)
                {
                    Spectrum = new ObservableDataSource<Point>(_xyData);
                }

                _spectrumUpdated.Reset();
                WaitHandle[] events = new WaitHandle[] { _continuousMode, _spectrumUpdated };

                ulong id = 0;
                short numScans = sp.numScans;
                while (true)
                {
                    int eventIndex = WaitHandle.WaitAny(events, (int)(sp.integrationTime * sp.numAverages) + (int)sp.delay + 10000);
                    if (eventIndex == 1)//got spectrum
                    {
                        bool isSaturated = false;
                        double[] spectrumData;

                        if (_avantes.GetSpectrum(out isSaturated, out spectrumData, out _lastSpectrumTimestamp))
                        {
                            _counts = spectrumData.ToArray();

                            _xyData = _wavelengths.Zip(_counts, (x, y) => (new Point(x, y))).ToArray();

                            lock (_spectrumLock)
                            {
                                Spectrum = new ObservableDataSource<Point>(_xyData.Take(2048));
                            }

                            if (numScans != -1)
                            {
                                _phosSpectra.Add(new AvantesSpectrum()
                                {
                                    Id = id++,
                                    TimeStamp = _lastSpectrumTimestamp * 10,
                                    Spectrum = _xyData.Take(2048).ToArray(),
                                    IntegrationTime = sp.integrationTime,
                                    NumAverages = sp.numAverages
                                });
                                if (--numScans == 0)
                                {
                                    _phosMeasureCompletionEvent.Set();
                                    return;//all scans completed
                                }
                            }else
                            {
                                Spectrum = new ObservableDataSource<Point>(_xyData.Take(2048).ToArray());
                            }
                        }
                    }
                    else //external stop or timeout
                    {
                        _avantes.StopMeasurement();
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("spectrometer exception: " + ex.Message);
            }
            finally
            {
                _statusVM.Busy--;
                SpectrometerBusy = false;
            }
        }

        void ScanCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!LiveView)
            {
                _avantes.StopMeasurement();
            }
        }
        #endregion

        public bool StartLiveView()
        {
            LiveView = true;
            bool res = Start(-1, (uint)GlobalVariables.spectrometerSettings.Delay, IntegrationTime, NumAverages);
            return res;
        }

        public void StopLiveView()
        {
            LiveView = false;
            if (SpectrometerBusy)
            {
                Stop();
            }
        }

        #region phos_capture
        struct BWArgument { public Callback measurementEnd; public int waitTime; }

        public bool StartPhosphorescenceMeasurement(long delay, uint count, double integrationTime, uint numAverages,
            Callback whenMeasurementEnds, out string error)
        {
            bool result = false;
            error = "";

            try
            {
                if (!SpectrometerConnected)
                    throw new Exception("Spectrometer not connected");

                Ready = false;

                //stop continuous scan
                if (SpectrometerBusy)
                {
                    Stop();
                    while (SpectrometerBusy)
                        Thread.Sleep(100);
                }

                _phosStartTimestamp = _avantes.GetTimestamp() * 10;

                //enable external trigger with count
                //enable delay
                //set phos count
                //start scan
                if (Start((short)count, (uint)delay, integrationTime, numAverages, false))
                {
                    StartTimer();
                    while (!SpectrometerBusy)
                    {
                        if (timeOut)
                        {
                            throw new Exception("Start Spectrometer timeout");
                        }
                        Thread.Sleep(100);
                    }
                    StopTimer();
                }
                else
                    throw new Exception("Could not start");

                //start backgroundworker to wait for completion
                BWArgument arg;
                arg.measurementEnd = whenMeasurementEnds;
                arg.waitTime = (int)(5000 + count * (IntegrationTime * NumAverages + delay));


                //empty phos spectra buffer
                _phosSpectra.Clear();
                _phosMeasureCompletionEvent.Reset();

                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += WaitForPhosCompletion;
                bw.RunWorkerCompleted += PhosMeasurementComplete;
                bw.RunWorkerAsync(arg);

                result = true;

            }
            catch (Exception ex)
            {
                Console.WriteLine("spectrometer exception: " + ex.Message);
                error = ex.Message;
                Ready = true;
                if (SpectrometerBusy)
                {
                    Stop();
                    while (SpectrometerBusy)
                        Thread.Sleep(100);
                }
                if (LiveView)
                {
                    Start(-1);
                }
            }

            return result;
        }

        public bool AbortPhosMeasurement()
        {
            try
            {
                Ready = true;
                if (SpectrometerBusy)
                {
                    Stop();
                    while (SpectrometerBusy)
                        Thread.Sleep(100);
                }
                if (LiveView)
                {
                    Start(-1);
                }
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("spectrometer exception: " + ex.Message);
                return false;
            }
        }

        void WaitForPhosCompletion(object sender, DoWorkEventArgs e)
        {
            try
            {
                var arg = (BWArgument)e.Argument;
                e.Result = arg.measurementEnd;
                bool got_signal = _phosMeasureCompletionEvent.WaitOne(arg.waitTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine("spectrometer exception: " + ex.Message);
            }
        }

        void PhosMeasurementComplete(object sender, RunWorkerCompletedEventArgs e)
        {
            Callback measurementEnds = (Callback)e.Result;

            if (!Ready)
            {
                Ready = true;
                if (SpectrometerBusy)
                {
                    Stop();
                    while (SpectrometerBusy)
                        Thread.Sleep(100);
                }

                //restore normal trigger mode
                //start video
                if (LiveView)
                {
                    Start(-1);
                }

                // todo: uncomment the line below to display the result
                measurementEnds(this, new SpectrometerPhosResults(_phosStartTimestamp, _phosSpectra));
            }
        }
        #endregion

        #region save
        void Save()
        {
            List<Point> saveData = null;
            lock (_spectrumLock)
            {
                if (Spectrum != null && Spectrum.Collection.Count > 0)
                    saveData = Spectrum.Collection.ToList();
            }

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += SaveDoWork;
            bw.RunWorkerCompleted += SaveCompleted;
            bw.RunWorkerAsync(saveData);
        }

        void SaveDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = false;
            _statusVM.Busy++;
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Saving spectrometer..." };
            _statusVM.SpecMessages.Add(sts);
            _timestamps.Push(timestamp);

            try
            {
                List<Point> spect = (List<Point>)(e.Argument);

                if (spect == null || spect.Count == 0)
                    throw new Exception("Bad data");

                double[] spectrumData = spect.Select(p => p.Y).ToArray(); ;
                double[] wavelengthData = spect.Select(p => p.X).ToArray();

                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.Filter = "SPC file (*.spc)|*.spc|CSV file (*.csv)|*.csv";
                if (saveDlg.ShowDialog() == true)
                {
                    if (Path.GetExtension(saveDlg.FileName).ToUpper().Contains("CSV"))
                    {
                        e.Result = SPCHelper.SaveToTXT(wavelengthData,
                            spectrumData,
                            saveDlg.FileName, "Wavelength (nm)", "Counts");
                    }
                    else
                    {
                        e.Result = SPCHelper.SaveToSPC(wavelengthData,
                           spectrumData,
                           saveDlg.FileName, "Wavelength (nm)", "Counts");
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("spectrometer exception: " + ex.Message);
                e.Result = false;
            }
            finally
            {

            }
        }

        void SaveCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var ts = _timestamps.Pop();
            var sm = _statusVM.SpecMessages.Where(s => s.Timestamp == ts).First();
            _statusVM.SpecMessages.Remove(sm);
            _statusVM.Busy--;

            string message = "Spectrum not saved";

            if ((bool)e.Result == true)
                message = "Spectrum saved";

            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = message };
            _statusVM.SpecMessages.Add(sts);
            Task.Delay(2000).ContinueWith(t => RemoveSaveStatusMessage(timestamp));
        }

        void RemoveSaveStatusMessage(DateTime ts)
        {
            var sm = _statusVM.SpecMessages.Where(s => s.Timestamp == ts).First();
            _statusVM.SpecMessages.Remove(sm);
        }
        #endregion

        void Stop()
        {
            _continuousMode.Set();
        }

        public void UpdateSpectrum()
        {
            _spectrumUpdated.Set();
        }

        #region dispose

        private bool _disposed = false;

        void CloseSpectrometer()
        {
            if (SpectrometerBusy)
            {
                Stop();
                while (SpectrometerBusy)
                    Thread.Sleep(100);
            }
            _avantes.Disconnect();
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
                    MainWindow_Closing();
                    CloseSpectrometer();
                }
                _disposed = true;
            }
        }
        #endregion

        const int WM_USER = 0x400;
        const int WM_APP = 0x8000;
        private const int WM_MEAS_READY = WM_APP + 1;
        private const int WM_DBG_INFOAs = WM_APP + 2;
        private const int WM_DEVICE_RESET = WM_APP + 3;

        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_MEAS_READY)
            {
                if ((int)wParam >= Avaspec.ERR_SUCCESS)
                {
                    if (Avaspec.ERR_SUCCESS == (int)wParam) // normal measurements
                    {
                        UpdateSpectrum();
                    }

                }
                handled = true;
            }

            return IntPtr.Zero;
        }

        public void MainWindow_SourceInitialized(IntPtr windowHandle)
        {
            System.Windows.Interop.HwndSource src = System.Windows.Interop.HwndSource.FromHwnd(windowHandle);
            src.AddHook(new System.Windows.Interop.HwndSourceHook(WndProc));

            MainWindowHandleForAvantesCallback = windowHandle;
        }

        private void MainWindow_Closing()
        {
            System.Windows.Interop.HwndSource src = System.Windows.Interop.HwndSource.FromHwnd(((MainWindow)Application.Current.MainWindow).MainWin);
            src.RemoveHook(new System.Windows.Interop.HwndSourceHook(this.WndProc));
        }
    }
}
