using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.ComponentModel;
using System.Threading.Tasks;
using ViewModelLib;
using RamanMapping.Model;
using System.Diagnostics;
using ImageProcessorLib;
using System.Windows;
using _405Analyzer;

namespace RamanMapping.ViewModel
{
    enum MeasurementType
    {
        CALIBRATION = 0,
        CALIBRATION_REF,
        MEASURE
    }

    public enum ResultStatus
    {
        SUCCESS = 0,
        ERROR
    }

    public class Result
    {
        public ResultStatus Status;
        public string Message;
        public object Value;
        public Result(ResultStatus status, string message, object value = null)
        {
            Status = status;
            Message = message;
            Value = value;
        }
    }

    public delegate void RefPointSelectedHandler(Point e);
    public delegate void CalibrateXYPixelInMMHandler(Point p);

    class MeasurementViewModel : ViewModelBase
    {
        CameraViewModel _cameraVM;
        XYZAxesViewModel _xyzAxesVM;
        MeasurementType _measurementType;
        SpectrometerViewModel _spectrometerVM;
        StatusViewModel _statusVM;

        EventWaitHandle _whMeasureControl = new AutoResetEvent(false);

        CamPhosResults _camPhosResults;
        CamPhosResults _camResultsPre;
        SpectrometerPhosResults _spectrometerPhosResults;
        SpectrometerPhosResults _spectroResults = new SpectrometerPhosResults();
        List<List<PeakDetectInfo>> _peakInfoList = new List<List<PeakDetectInfo>>();
        ANALYZER_RESULT _diamondResult;

        List<CamPhosResults> CamResList = new List<CamPhosResults>();
        List<SpectrometerPhosResults> SpeResList = new List<SpectrometerPhosResults>();
        List<ANALYZER_RESULT> DiamondResList = new List<ANALYZER_RESULT>();
        List<List<List<PeakDetectInfo>>> PeakInfoListList = new List<List<List<PeakDetectInfo>>>();

        List<Tuple<System.Windows.Shapes.Ellipse, System.Windows.Controls.TextBlock, Point>> MarkerList = new List<Tuple<System.Windows.Shapes.Ellipse, System.Windows.Controls.TextBlock, Point>>();
        List<System.Windows.Shapes.Ellipse> tempEllipseList = new List<System.Windows.Shapes.Ellipse>();
        List<System.Windows.Controls.TextBlock> tempTBList = new List<System.Windows.Controls.TextBlock>();
        bool mappingMeasure = false;

        public event RefPointSelectedHandler RefPointSelected;
        public event CalibrateXYPixelInMMHandler CalibrateXYPixelInMM;

        List<Tuple<double, uint>> SpectrumParameters = new List<Tuple<double, uint>>
        {
            Tuple.Create<double, uint>(0.5, 100),
            Tuple.Create<double, uint>(10, 10),
            Tuple.Create<double, uint>(100, 1),
            Tuple.Create<double, uint>(500, 1),
            Tuple.Create<double, uint>(1000, 1)
        };

        public MeasurementViewModel(CameraViewModel cameraVM, XYZAxesViewModel xyzAxesVM, SpectrometerViewModel spectrometerVM, StatusViewModel statusVM)
        {
            base.DisplayName = "MeasurementViewModel";
            _cameraVM = cameraVM;
            _xyzAxesVM = xyzAxesVM;
            _spectrometerVM = spectrometerVM;
            _statusVM = statusVM;

            CommandCalibrateRefPoint = new RelayCommand(param => CalibrateRefPoint(), cc => { return !_statusVM.IsBusy; });
            CommandCalibrate = new RelayCommand(param => Calibrate(), cc => { return !_statusVM.IsBusy; });
            CommandCameraSettings = new RelayCommand(param => EditCameraSettings());
            CommandMeasure = new RelayCommand(param => Measure(), cc=> { return !_statusVM.IsBusy; });
            CommandMotorSettings = new RelayCommand(param => EditMotorSettings());
            CommandDisplayMarkedPoints = new RelayCommand(param => DisplayMarkedPoints());
            CommandBatchMeasure = new RelayCommand(param => BatchMeasure(), cc => { return _xyzAxesVM.PositionCount > 1 && !_statusVM.IsBusy; });
            CommandToggleLaser = new RelayCommand(param => ToggleLaser());
            CommandSpectrumSettings = new RelayCommand(param => EditSpectrumSettings(), cc=> { return _spectrometerVM.SpectrometerConnected; });
            CommandToggleLED = new RelayCommand(param => ToggleLED());
            CommandMappingMeasure = new RelayCommand(param => MappingMeasure());
        }

        bool _ledOn;
        public bool LedOn
        {
            get
            {
                return _ledOn;
            }
            set
            {
                if (_ledOn == value)
                {
                    return;
                }
                _ledOn = value;
                OnPropertyChanged("LedOn");
            }
        }

        bool _laserOn;
        public bool LaserOn
        {
            get
            {
                return _laserOn;
            }
            set
            {
                if(_laserOn == value)
                {
                    return;
                }
                _laserOn = value;
                OnPropertyChanged("LaserOn");
            }
        }

        bool _showMarkedPoints = false;
        public bool ShowMarkedPoints
        {
            get
            {
                return _showMarkedPoints;
            }
            set
            {
                _showMarkedPoints = value;
                OnPropertyChanged("ShowMarkedPoints");
            }
        }

        bool _calibrated = true;
        public bool Calibrated
        {
            get
            {
                return _calibrated;
            }
            set
            {
                _calibrated = value;
                OnPropertyChanged("Calibrated");
            }
        }

        public void AddImageEnqueuedSubscriber()
        {
            _cameraVM?.AddImageEnqueuedSubscriber(new ImageEnqueueHandler(OnImageEnqueued));
        }

        public RelayCommand CommandCalibrateRefPoint { get; set; }
        public RelayCommand CommandCalibrate { get; set; }
        public RelayCommand CommandMeasure { get; set; }
        public RelayCommand CommandBatchMeasure { get; set; }
        public RelayCommand CommandCameraSettings { get; set; }
        public RelayCommand CommandMotorSettings { get; set; }
        public RelayCommand CommandDisplayMarkedPoints { get; set; }
        public RelayCommand CommandToggleLaser { get; set; }
        public RelayCommand CommandSpectrumSettings { get; set; }
        public RelayCommand CommandToggleLED { get; set; }
        public RelayCommand CommandMappingMeasure { get; set; }

        void EditCameraSettings()
        {
            _cameraVM?.CameraSettings();
        }

        void EditMotorSettings()
        {
            View.MotorSettings motorSettings = new View.MotorSettings();
            MotorSettingsViewModel motorSettingsVM = new MotorSettingsViewModel();
            motorSettings.DataContext = motorSettingsVM;
            motorSettingsVM.AddMotorSettingsChangedSubscriber(new MotorSettingsChangedHandler(_xyzAxesVM.MotorSettingsChanged));
            motorSettingsVM.AddRefPointChangedSubscriber(new RefPointChangedHandler(_xyzAxesVM.RefPointChanged));
            motorSettings.ShowDialog();
        }

        void EditSpectrumSettings()
        {
            View.SpectrumSettings spectrumSettings = new View.SpectrumSettings();
            SpectrumSettingsViewModel spectrumSettingsVM = new SpectrumSettingsViewModel(_spectrometerVM, _xyzAxesVM, _statusVM);
            spectrumSettings.DataContext = spectrumSettingsVM;
            spectrumSettings.Loaded += spectrumSettingsVM.OnViewLoaded;
            spectrumSettings.Closing += spectrumSettingsVM.OnViewClosing;
            spectrumSettings.ShowDialog();
        }

        void DisplayMarkedPoints()
        {
            ShowMarkedPoints = !ShowMarkedPoints;
            if (ShowMarkedPoints)
            {
                _cameraVM.ShowMarkedPoints();
            }else
            {
                _cameraVM.HideMarkedPoints();
            }
        }

        void ToggleLED()
        {
            bool isOn = !LedOn;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwToggleLed;
            bw.RunWorkerCompleted += BwToggleLedCompleted;
            bw.RunWorkerAsync(isOn);
        }

        void BwToggleLed(object sender, DoWorkEventArgs e)
        {
            var isOn = (bool)e.Argument;

            if (_xyzAxesVM.SetLedOnOff(isOn))
            {
                e.Result = new Result(ResultStatus.SUCCESS, null);
            }
            else
            {
                e.Result = new Result(ResultStatus.ERROR, isOn ? "Failed to turn on LED" : "Failed to turn off LED");
            }
        }

        void BwToggleLedCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (((Result)e.Result).Status == ResultStatus.ERROR)
            {
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
            else
            {
                LedOn = !LedOn;
            }
        }

        void ToggleLaser()
        {
            bool isOn = !LaserOn;
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwToggleLaser;
            bw.RunWorkerCompleted += BwToggleLaserCompleted;
            bw.RunWorkerAsync(isOn);
        }

        void BwToggleLaser(object sender, DoWorkEventArgs e)
        {
            var isOn = (bool)e.Argument;

            if (_xyzAxesVM.SetLaserOnOff(isOn))
            {
                e.Result = new Result(ResultStatus.SUCCESS, null);
            } else
            {
                e.Result = new Result(ResultStatus.ERROR, isOn ? "Failed to turn on laser" : "Failed to turn off laser");
            }
        }

        void BwToggleLaserCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(((Result)e.Result).Status == ResultStatus.ERROR)
            {
                MessageBox.Show(((Result)e.Result).Message, "Error");
            } else
            {
                LaserOn = !LaserOn;
            }
        }

        void BwProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            _statusVM.CurrentProgress = e.ProgressPercentage;
        }

        void Measure()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwMeasure;
            bw.RunWorkerCompleted += BwMeasureCompleted;
            bw.ProgressChanged += BwProgressChanged;
            bw.WorkerReportsProgress = true;
            bw.RunWorkerAsync();
        }

        void BwMeasure(object sender, DoWorkEventArgs e)
        {
            try
            {
                BackgroundWorker bw = sender as BackgroundWorker;
                int progress = 0;
                int step = 25;
                bw.ReportProgress(step / 2);
                _statusVM.IsBusy = true;

                _measurementType = MeasurementType.MEASURE;
                // 0. capture image before measurement
                _cameraVM.StartEnqueue();
                if (!_whMeasureControl.WaitOne(2000))
                {
                    // todo: error in capture image
                    e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                    return;
                }
                // camera reset to continue mode for display
                _cameraVM.StopEnqueue();
                ulong timeStamp = (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                _camResultsPre = new CamPhosResults(timeStamp, _cameraVM.GetImageQueue().ToList());
                Thread.Sleep(200);

                // 1. turn on laser
                if (!_xyzAxesVM.SetLaserOnOff(true))
                {
                    e.Result = new Result(ResultStatus.ERROR, "Failed to turn on the laser");
                    return;
                }
                bw.ReportProgress(progress+=step);
                Thread.Sleep(1000);

                // 1.5 capture image
                _cameraVM.StartEnqueue();
                if (!_whMeasureControl.WaitOne(2000))
                {
                    // todo: error in capture image
                    e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                    return;
                }
                // camera reset to continue mode for display
                _cameraVM.StopEnqueue();
                timeStamp = (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                _camPhosResults = new CamPhosResults(timeStamp, _cameraVM.GetImageQueue().ToList());
                Thread.Sleep(200);

                bw.ReportProgress(progress += step);
                // 2. spectrometer measure
                Stopwatch watch = Stopwatch.StartNew();
                int factor = 0;
                int index = 2;
                _peakInfoList.Clear();
                ResetSpectroResults();
                while (index >= 0 && index <= 4)
                {
                    Console.WriteLine("spectrum parameter index: {0}", index);
                    double integrationTime = SpectrumParameters[index].Item1;
                    uint numAverages = SpectrumParameters[index].Item2;

                    Callback MeasurementEnd = MeasurementCompleteCallback;
                    if (!_spectrometerVM.StartPhosphorescenceMeasurement(GlobalVariables.spectrometerSettings.Delay, GlobalVariables.spectrometerSettings.Counts, integrationTime, numAverages, MeasurementEnd, out string error))
                    {
                        e.Result = new Result(ResultStatus.ERROR, "Failed in start phosphorescence measurement");
                        return;
                    }
                    if (!_whMeasureControl.WaitOne(15000))
                    {
                        // todo: error in getting spectrometer data
                        e.Result = new Result(ResultStatus.ERROR, "Failed in phosphorescence measurement");
                        return;
                    }

                    // save spectrum data to _spectroResults
                    for(int i = 0; i < _spectrometerPhosResults.Spectra.Count; i++)
                    {
                        _spectroResults.Spectra.Add(_spectrometerPhosResults.Spectra[i]);
                    }

                    // analyze spectrum data 
                    List<double> wl = _spectrometerPhosResults.Spectra[0].Spectrum.Select(p => p.X).ToList();
                    List<double> counts = _spectrometerPhosResults.Spectra[0].Spectrum.Select(p => p.Y).ToList();
                    List<PeakDetectInfo> peakInfo = new List<PeakDetectInfo>();
                    _diamondResult = RamanAnalyzer.Analysis(wl, counts, out peakInfo);

                    if (_diamondResult == ANALYZER_RESULT.ERROR || _diamondResult == ANALYZER_RESULT.ERROR_SPIKE)
                    {
                        break;
                    }

                    if(_diamondResult == ANALYZER_RESULT.NONE)
                    {
                        _peakInfoList.Add(peakInfo);
                        if(factor == 0 || factor == 1)
                        {
                            index++;
                            factor = 1;
                            continue;
                        } else
                        {
                            break;
                        }
                    } else if(_diamondResult == ANALYZER_RESULT.SATURATED)
                    {
                        _peakInfoList.Add(peakInfo);
                        if (factor == 0 || factor == -1)
                        {
                            index--;
                            factor = -1;
                            continue;
                        } else
                        {
                            break;
                        }
                    }
                    else if (_diamondResult == ANALYZER_RESULT.NON_DIAMOND)
                    {
                        if(index >= 2)
                        {
                            _peakInfoList.Clear();
                        }
                        _peakInfoList.Add(peakInfo);
                        break;
                    }
                    else
                    {
                        _peakInfoList.Clear();
                        break;
                    }
                }

                if (_peakInfoList.Count >= 2)
                    _diamondResult = SpectrumAnalyzer.TestAggregate(_peakInfoList);
                _spectrometerPhosResults = _spectroResults;

                watch.Stop();
                long ms = watch.ElapsedMilliseconds;
                Console.WriteLine("spectrum measurement spend: {0}", ms);
                Thread.Sleep(200);

                bw.ReportProgress(progress += step);
                // 3. turn off laser
                if (!_xyzAxesVM.SetLaserOnOff(false))
                {
                    e.Result = new Result(ResultStatus.ERROR, "Failed to turn off the laser");
                    return;
                }

                bw.ReportProgress(progress += step);
                e.Result = new Result(ResultStatus.SUCCESS, null);
            } catch(Exception ex)
            {
                Console.WriteLine("measurementvm exception: " + ex.Message);
                e.Result = new Result(ResultStatus.ERROR, ex.Message);
            }
        }

        void BwMeasureCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _statusVM.IsBusy = false;
            if (((Result)e.Result).Status == ResultStatus.SUCCESS)
            {
                ShowPhosResults();
            } else
            {
                if (_xyzAxesVM.LaserStateON)
                {
                    _xyzAxesVM.SetLaserOnOff(false);
                }
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        void BatchMeasure()
        {
            CamResList.Clear();
            SpeResList.Clear();
            DiamondResList.Clear();
            PeakInfoListList.Clear();

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwBatchMeasure;
            bw.RunWorkerCompleted += BwBatchMeasureCompleted;
            bw.ProgressChanged += BwProgressChanged;
            bw.WorkerReportsProgress = true;
            bw.RunWorkerAsync(_xyzAxesVM.GetPositionList());
        }

        void BwBatchMeasure(object sender, DoWorkEventArgs e)
        {
            List<Point> posiList = (List<Point>)e.Argument;
            if(posiList.Count <= 1)
            {
                e.Result = new Result(ResultStatus.ERROR, "No stones selected for measurement");
                Debug.WriteLine("no selected positions"); 
            } else
            {
                try
                {
                    BackgroundWorker bw = sender as BackgroundWorker;
                    int progress = 0;
                    int step = (int) (100.0 / ( posiList.Count - 1 ) + 0.99);
                    bw.ReportProgress(step / 2);
                    _statusVM.IsBusy = true;

                    _measurementType = MeasurementType.MEASURE;
                    // 0. capture image before measurement
                    _cameraVM.StartEnqueue();
                    if (!_whMeasureControl.WaitOne(2000))
                    {
                        // todo: error in capture image
                        e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                        return;
                    }
                    // camera reset to continue mode for display
                    _cameraVM.StopEnqueue();
                    ulong timeStamp = (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                    _camResultsPre = new CamPhosResults(timeStamp, _cameraVM.GetImageQueue().ToList());
                    Thread.Sleep(200);

                    // loop through the positionlist to do each measurement
                    for (int i = 1; i < posiList.Count; i++)
                    {
                        // 1. move to ref position (EndPoint)
                        double deltaX = -(posiList[i].X - posiList[i - 1].X);
                        double deltaY = posiList[i].Y - posiList[i - 1].Y;

                        if(!_xyzAxesVM.MoveTo(deltaX, deltaY, false))
                        {
                            e.Result = new Result(ResultStatus.ERROR, "Failed to move stone to the reference position");
                            Debug.WriteLine("batchmeasure: MoveTo({0}, {1}) failed", deltaX, deltaY);
                            return;
                        }
                        Thread.Sleep(300);

                        // 2. turn on laser
                        if (!_xyzAxesVM.SetLaserOnOff(true))
                        {
                            e.Result = new Result(ResultStatus.ERROR, "Failed to turn on laser");
                            Debug.WriteLine("batchmeasure: SetLaserOnOff(true) failed");
                            return;
                        }
                        Thread.Sleep(1000);

                        // 3. capture image
                        _cameraVM.StartEnqueue();
                        if (!_whMeasureControl.WaitOne(2000))
                        {
                            // todo: error in capture image
                            e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                            Debug.WriteLine("batchmeasure: StartEnqueue failed");
                            return;
                        }
                        // camera reset to continue mode for display
                        _cameraVM.StopEnqueue();
                        timeStamp = (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
                        _camPhosResults = new CamPhosResults(timeStamp, _cameraVM.GetImageQueue().ToList());
                        Thread.Sleep(300);

                        // 4. spectrometer measure
                        if (mappingMeasure)
                        {
                            ResetSpectroResults();

                            double integrationTime = GlobalVariables.spectrometerSettings.IntegrationTime;
                            uint numAverages = GlobalVariables.spectrometerSettings.NumAverages;

                            Callback MeasurementEnd = MeasurementCompleteCallback;
                            if (!_spectrometerVM.StartPhosphorescenceMeasurement(GlobalVariables.spectrometerSettings.Delay, GlobalVariables.spectrometerSettings.Counts, integrationTime, numAverages, MeasurementEnd, out string error))
                            {
                                e.Result = new Result(ResultStatus.ERROR, "Failed in start phosphorescence measurement");
                                return;
                            }
                            if (!_whMeasureControl.WaitOne(15000))
                            {
                                // todo: error in getting spectrometer data
                                e.Result = new Result(ResultStatus.ERROR, "Failed in phosphorescence measurement");
                                return;
                            }

                            // save spectrum data to _spectroResults
                            for (int m = 0; m < _spectrometerPhosResults.Spectra.Count; m++)
                            {
                                _spectroResults.Spectra.Add(_spectrometerPhosResults.Spectra[m]);
                            }
                        }
                        else
                        {
                            Stopwatch watch = Stopwatch.StartNew();
                            int factor = 0;
                            int index = 2;
                            _peakInfoList.Clear();
                            ResetSpectroResults();
                            while (index >= 0 && index <= 4)
                            {
                                Console.WriteLine("spectrum parameter index: {0}", index);
                                double integrationTime = SpectrumParameters[index].Item1;
                                uint numAverages = SpectrumParameters[index].Item2;

                                Callback MeasurementEnd = MeasurementCompleteCallback;
                                if (!_spectrometerVM.StartPhosphorescenceMeasurement(GlobalVariables.spectrometerSettings.Delay, GlobalVariables.spectrometerSettings.Counts, integrationTime, numAverages, MeasurementEnd, out string error))
                                {
                                    e.Result = new Result(ResultStatus.ERROR, "Failed in start phosphorescence measurement");
                                    return;
                                }
                                if (!_whMeasureControl.WaitOne(15000))
                                {
                                    // todo: error in getting spectrometer data
                                    e.Result = new Result(ResultStatus.ERROR, "Failed in phosphorescence measurement");
                                    return;
                                }

                                // save spectrum data to _spectroResults
                                for (int m = 0; m < _spectrometerPhosResults.Spectra.Count; m++)
                                {
                                    _spectroResults.Spectra.Add(_spectrometerPhosResults.Spectra[m]);
                                }

                                // analyze spectrum data 
                                List<double> wl = _spectrometerPhosResults.Spectra[0].Spectrum.Select(p => p.X).ToList();
                                List<double> counts = _spectrometerPhosResults.Spectra[0].Spectrum.Select(p => p.Y).ToList();
                                List<PeakDetectInfo> peakInfo = new List<PeakDetectInfo>();
                                _diamondResult = RamanAnalyzer.Analysis(wl, counts, out peakInfo);

                                if (_diamondResult == ANALYZER_RESULT.ERROR || _diamondResult == ANALYZER_RESULT.ERROR_SPIKE)
                                {
                                    break;
                                }

                                if (_diamondResult == ANALYZER_RESULT.NONE)
                                {
                                    _peakInfoList.Add(peakInfo);
                                    if (factor == 0 || factor == 1)
                                    {
                                        index++;
                                        factor = 1;
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else if (_diamondResult == ANALYZER_RESULT.SATURATED)
                                {
                                    _peakInfoList.Add(peakInfo);
                                    if (factor == 0 || factor == -1)
                                    {
                                        index--;
                                        factor = -1;
                                        continue;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                else if (_diamondResult == ANALYZER_RESULT.NON_DIAMOND)
                                {
                                    if (index >= 2)
                                    {
                                        _peakInfoList.Clear();
                                    }
                                    _peakInfoList.Add(peakInfo);
                                    break;
                                }
                                else
                                {
                                    _peakInfoList.Clear();
                                    break;
                                }
                            }

                            watch.Stop();
                            long ms = watch.ElapsedMilliseconds;
                            Console.WriteLine("spectrum measurement spend: {0}", ms);

                            if (_peakInfoList.Count >= 2)
                                _diamondResult = SpectrumAnalyzer.TestAggregate(_peakInfoList);

                        }
                        Thread.Sleep(300);

                        // 5. turn off laser
                        if (!_xyzAxesVM.SetLaserOnOff(false))
                        {
                            e.Result = new Result(ResultStatus.ERROR, "Failed to turn off laser");
                            Debug.WriteLine("batchmeasure: SetLaserOnOff(false) failed");
                            return;
                        }
                        Thread.Sleep(300);

                        CamResList.Add(_camPhosResults);
                        SpeResList.Add(_spectroResults);
                        DiamondResList.Add(_diamondResult);
                        PeakInfoListList.Add(_peakInfoList);

                        bw.ReportProgress(progress += step);
                    }
                    e.Result = new Result(ResultStatus.SUCCESS, null);
                } catch(Exception ex)
                {
                    e.Result = new Result(ResultStatus.ERROR, ex.Message);
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        void BwBatchMeasureCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _statusVM.IsBusy = false;
            if (((Result)e.Result).Status == ResultStatus.SUCCESS)
            {
                if (mappingMeasure)
                {
                    ShowPhosResults(true, true);
                    mappingMeasure = false;
                }
                else
                {
                    ShowPhosResults(true);
                }
            }
            else
            {
                if (_xyzAxesVM.LaserStateON)
                {
                    _xyzAxesVM.SetLaserOnOff(false);
                }
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        void MappingMeasure()
        {
            // first adjust spectrum setting
            EditSpectrumSettings();

            // second mapping process
            CamResList.Clear();
            SpeResList.Clear();
            DiamondResList.Clear();
            PeakInfoListList.Clear();

            MarkerList.Clear();
            RemoveTempInRectMarker();
            List<Point> posiList = CreatePointListFromRectMask(_cameraVM.GetRectMarker());
            mappingMeasure = true;

            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwBatchMeasure;
            bw.RunWorkerCompleted += BwBatchMeasureCompleted;
            bw.ProgressChanged += BwProgressChanged;
            bw.WorkerReportsProgress = true;
            bw.RunWorkerAsync(posiList);
        }

        void RemoveTempInRectMarker()
        {
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            for(int i = 0; i < tempEllipseList.Count; i++)
            {
                mv.CanvasDraw.Children.Remove(tempEllipseList[i]);
                mv.CanvasDraw.Children.Remove(tempTBList[i]);
            }
            tempEllipseList.Clear();
            tempTBList.Clear();
        }

        List<Point> CreatePointListFromRectMask(System.Windows.Shapes.Rectangle rect)
        {
            List<Point> pList = new List<Point>();
            if(rect == null)
            {
                return pList;
            }
            List<Point> refList = _xyzAxesVM.GetPositionList();
            Point refP = refList[0];
            pList.Add(refP);
            Tuple<System.Windows.Shapes.Ellipse, System.Windows.Controls.TextBlock, Point> refTuple = new Tuple<System.Windows.Shapes.Ellipse, System.Windows.Controls.TextBlock, Point>(null, null, refP);
            MarkerList.Add(refTuple);

            double xStepInPixel = GlobalVariables.motorSettings.XStep * GlobalVariables.motorSettings.XPixelInMM;
            double yStepInPixel = GlobalVariables.motorSettings.YStep * GlobalVariables.motorSettings.YPixelInMM;

            double x = System.Windows.Controls.Canvas.GetLeft(rect);
            double y = System.Windows.Controls.Canvas.GetTop(rect);
            int w = (int)(rect.Width / xStepInPixel) + 1;
            int h = (int)(rect.Height / yStepInPixel) + 1;

            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            for (int i = 0; i < h; i++)
            {
                double Y = y + i * xStepInPixel;
                for (int j = 0; j < w; j++)
                {
                    double X = x + j * yStepInPixel;
                    Point p = new Point(X, Y);
                    pList.Add(p);

                    System.Windows.Shapes.Ellipse e = DrawCanvas.Circle(X, Y, 15, 15, false, mv.CanvasDraw);
                    tempEllipseList.Add(e);

                    string s = MarkerList.Count.ToString();
                    System.Windows.Controls.TextBlock t = DrawCanvas.Text(X, Y, s, false, null, mv.CanvasDraw);
                    tempTBList.Add(t);

                    Tuple<System.Windows.Shapes.Ellipse, System.Windows.Controls.TextBlock, Point> tuple = new Tuple<System.Windows.Shapes.Ellipse, System.Windows.Controls.TextBlock, Point>(e, t, p);
                    MarkerList.Add(tuple);
                }
            }
            return pList;
        }

        public void OnRemovePointListOnRectMarker()
        {
            RemoveTempInRectMarker();            
        }

        void ResetSpectroResults()
        {
            _spectroResults = new SpectrometerPhosResults();
        }

        void CalibrateRefPoint()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwCalibrateRef;
            bw.RunWorkerCompleted += BwCalibrateRefCompleted;
            bw.WorkerReportsProgress = true;
            bw.ProgressChanged += BwProgressChanged;
            bw.RunWorkerAsync();
        }

        void BwCalibrateRef(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bw = sender as BackgroundWorker;
            int progress = 0;
            int step = 16;
            bw.ReportProgress(step / 2);
            _statusVM.IsBusy = true;

            _measurementType = MeasurementType.CALIBRATION_REF;
            try
            {
                // 1. camera setting: gain 1, shutter time 1ms, auto exposure false, auto gain false
                _cameraVM.InitialCalibrateSettings();
                Thread.Sleep(200);
                bw.ReportProgress(progress += step);

                // 2. turn on the laser
                if (!_xyzAxesVM.SetLaserOnOff(true))
                {
                    e.Result = new Result(ResultStatus.ERROR, "Failed to turn on laser");
                    return;
                }
                Thread.Sleep(5000);
                bw.ReportProgress(progress += step);

                // 3. capture one image
                // software trigger to capture one image
                _cameraVM.StartEnqueue(true);
                if (!_whMeasureControl.WaitOne(2000))
                {
                    // todo: error in capture image
                    e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                    return;
                }
                _cameraVM.StopEnqueue();
                bw.ReportProgress(progress += step);

                // 4. turn off the laser
                if (!_xyzAxesVM.SetLaserOnOff(false))
                {
                    e.Result = new Result(ResultStatus.ERROR, "Failed to turn off laser");
                    return;
                }
                bw.ReportProgress(progress += step);

                // 5. reset camera setting
                _cameraVM.PostCalibrateSettings();
                Thread.Sleep(200);
                bw.ReportProgress(progress += step);

                // 6. calculate laser spot center position
                Queue<PtGreyCameraImage> imgQ = _cameraVM.GetImageQueue();
                System.Drawing.Bitmap src = imgQ.Dequeue().Image;
                double xRef, yRef;
                ImageProcessing.CalBlobCenter(ref src, out xRef, out yRef, false);
                Debug.WriteLine("xRef = {0}, yRef = {1}", xRef, yRef);

                List<Point> pList = new List<Point>();
                pList.Add(new Point(xRef, yRef));

                e.Result = new Result(ResultStatus.SUCCESS, null, pList);
                bw.ReportProgress(progress += step);

            }
            catch (Exception ex)
            {
                e.Result = new Result(ResultStatus.ERROR, ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        void BwCalibrateRefCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _statusVM.IsBusy = false;
            if (((Result)e.Result).Status == ResultStatus.SUCCESS)
            {
                Calibrated = true;
                RaiseRefPointSelectedEvent(((List<Point>)((Result)e.Result).Value)[0]);
            }
            else
            {
                if (_xyzAxesVM.LaserStateON)
                {
                    _xyzAxesVM.SetLaserOnOff(false);
                }
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        void Calibrate()
        {
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwCalibrate;
            bw.RunWorkerCompleted += BwCalibrateCompleted;
            bw.WorkerReportsProgress = true;
            bw.ProgressChanged += BwProgressChanged;
            bw.RunWorkerAsync();
        }

        void BwCalibrate(object sender, DoWorkEventArgs e)
        {
            try
            {
                BackgroundWorker bw = sender as BackgroundWorker;
                int progress = 0;
                int step = 25;
                bw.ReportProgress(step/2);
                _statusVM.IsBusy = true;

                _measurementType = MeasurementType.CALIBRATION;

                // 1. software trigger to capture one image
                _cameraVM.StartEnqueue();
                if (!_whMeasureControl.WaitOne(2000))
                {
                    // todo: error in capture image
                    e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                    return;
                }
                // camera reset to continue mode for display
                _cameraVM.StopEnqueue();
                bw.ReportProgress(progress += step);

                // 2. move stage (5mm, 5mm)
                double xShift = 5;
                double yShift = 5;
                if (!_xyzAxesVM.MoveTo(xShift, yShift))
                {
                    e.Result = new Result(ResultStatus.ERROR, "Failed to move to calibration position");
                    return;
                }
                bw.ReportProgress(progress += step);

                // 3. capture one image
                _cameraVM.StartEnqueue(false);
                if (!_whMeasureControl.WaitOne(2000))
                {
                    // todo: error in capture image
                    e.Result = new Result(ResultStatus.ERROR, "Failed to enqueue image");
                    return;
                }
                _cameraVM.StopEnqueue();
                bw.ReportProgress(progress += step);

                // 4. calculate the pixel / mm
                Queue<PtGreyCameraImage> imgQ = _cameraVM.GetImageQueue();
                System.Drawing.Bitmap src = imgQ.Dequeue().Image;
                double x, y;
                ImageProcessing.CalBlobCenter(ref src, out x, out y, true);
                Debug.WriteLine("x = {0}, y = {1}", x, y);

                src = imgQ.Dequeue().Image;
                double xRef, yRef;
                ImageProcessing.CalBlobCenter(ref src, out xRef, out yRef, true);
                Debug.WriteLine("xRef = {0}, yRef = {1}", xRef, yRef);
                bw.ReportProgress(progress += step);

                double xPixelPerMM = (x - xRef) / xShift;
                double yPixelPerMM = (y - yRef) / yShift;
                Debug.WriteLine("x pixel in mm = {0}, y pixel in mm = {1}", xPixelPerMM, yPixelPerMM);

                List<Point> pList = new List<Point>();
                pList.Add(new Point(xPixelPerMM, yPixelPerMM));

                e.Result = new Result(ResultStatus.SUCCESS, null, pList);
                bw.ReportProgress(progress += step);

            } catch(Exception ex)
            {
                e.Result = new Result(ResultStatus.ERROR, ex.Message);
                Debug.WriteLine(ex.Message);
            }
        }

        void BwCalibrateCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _statusVM.IsBusy = false;
            _xyzAxesVM.Homing();
            if (((Result)e.Result).Status == ResultStatus.SUCCESS)
            {
                Calibrated = true;
                RaiseCalibrateXYPixelInMMEvent(((List<Point>)((Result)e.Result).Value)[0]);
            }
            else
            {
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        void MeasurementCompleteCallback(object sender, object result)
        {
            if (Object.ReferenceEquals(sender.GetType(), _spectrometerVM.GetType()))
            {
                _spectrometerPhosResults = (SpectrometerPhosResults)result;
                _whMeasureControl.Set();
            }
        }

        void RaiseRefPointSelectedEvent(Point p)
        {
            RefPointSelected?.Invoke(p);
        }

        void RaiseCalibrateXYPixelInMMEvent(Point p)
        {
            CalibrateXYPixelInMM?.Invoke(p);
        }

        void ShowPhosResults(bool batch = false, bool mappingMeasure =  false)
        {
            var phosResWindow = new View.PhosResultsWindow();
            PhosResultsViewModel phosResultsVM = null;
            if (batch)
            {
                //phosResultsVM = new PhosResultsViewModel(CamResList, SpeResList);
                if (mappingMeasure)
                {
                    phosResultsVM = new PhosResultsViewModel(CamResList, SpeResList, PeakInfoListList, DiamondResList, MarkerList, _camResultsPre, true, _cameraVM.GetRectMarker());
                }
                else
                {
                    phosResultsVM = new PhosResultsViewModel(CamResList, SpeResList, PeakInfoListList, DiamondResList, _cameraVM.GetMarkedPointsList(), _camResultsPre);
                }
            } else
            {
                //phosResultsVM = new PhosResultsViewModel(_camPhosResults, _spectrometerPhosResults);
                phosResultsVM = new PhosResultsViewModel(_camPhosResults, _spectrometerPhosResults, _peakInfoList, _diamondResult, _cameraVM.GetMarkedPointsList(), _camResultsPre);
            }
            phosResWindow.DataContext = phosResultsVM;
            phosResWindow.Closing += phosResultsVM.OnWindowClosing;
            phosResWindow.Closed += OnResultWindowClosed;
            phosResWindow.Loaded += phosResultsVM.OnWindowLoaded;
            phosResWindow.Owner = Window.GetWindow(Application.Current.MainWindow);
            phosResWindow.ShowDialog();
        }

        void OnResultWindowClosed(object sender, EventArgs e)
        {
            PhosResultsViewModel phosResVM = (PhosResultsViewModel)((View.PhosResultsWindow)sender).DataContext;
            if(phosResVM != null && phosResVM.DisplayName.Equals("PhosResultsViewModelBatchMeasurement"))
            {
                _xyzAxesVM.ClearAllPoints(1);
            }
        }

        void OnImageEnqueued(ImageEnqueuedEventArgs e)
        {
            if(_measurementType == MeasurementType.CALIBRATION)
            { // take two images
                if(e.index == 0)
                {
                    _whMeasureControl.Set();
                } else if(e.index == 1)
                {
                    _whMeasureControl.Set();
                    // camera capture finished, stop enqueuing image
                } else if(e.index == 2)
                {
                    _whMeasureControl.Set();
                }
                else
                {
                    _whMeasureControl.Set();
                    Debug.WriteLine("OnImageEnqueued: MeasurementType.CALIBRATION received more images {0}", e.index);
                }
            }

            if (_measurementType == MeasurementType.CALIBRATION_REF)
            { // take only one image
                if (e.index == 0)
                {
                    _whMeasureControl.Set();
                }
                else if (e.index == 1)
                {
                    _whMeasureControl.Set();
                    // camera capture finished, stop enqueuing image
                }
                else
                {
                    _whMeasureControl.Set();
                    Debug.WriteLine("OnImageEnqueued: MeasurementType.CALIBRATION received more images {0}", e.index);
                }
            }

            if (_measurementType == MeasurementType.MEASURE)
            {
                if (e.index == 1)
                {
                    _whMeasureControl.Set();
                    // camera capture finished, stop enqueuing image
                }
                else
                {
                    Debug.WriteLine("OnImageEnqueued: MeasurementType.CALIBRATION received more images {0}", e.index);
                }
            }
        }

        public void AddRefPointSelectedSubscriber(RefPointSelectedHandler h)
        {
            RefPointSelected += h;
        }

        public void AddCalibrateXYPixelInMMSubscriber(CalibrateXYPixelInMMHandler h)
        {
            CalibrateXYPixelInMM += h;
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
