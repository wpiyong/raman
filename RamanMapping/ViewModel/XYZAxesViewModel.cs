using System;
using System.Timers;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModelLib;
using RamanMapping.Model;
using System.Windows.Threading;
using System.Threading;
using ACS.SPiiPlusNET;
using System.ComponentModel;

namespace RamanMapping.ViewModel
{
    public delegate void MarkedPointsListHandler(Point p, bool refPoint);

    class XYZAxesViewModel : ViewModelBase
    {
        StatusViewModel _statusVM;
        MotorManager motorManager = null;
        MainWindowVM mainVM = null;

        bool xyPixelInMMCalculated = true;

        static bool timeOut = false;
        System.Timers.Timer aTimer = null;

        //double refPositionX = 0, refPositionY = 0, refPositionZ = 0;

        List<Point> PositionList = new List<Point>();

        BackgroundWorker bwXYZMotoConnector = new BackgroundWorker();

        public event MarkedPointsListHandler MarkedPointsList;

        public XYZAxesViewModel(StatusViewModel svm)
        {
            base.DisplayName = "XYZAxesViewModel";
            _statusVM = svm;

            JogX = new RelayCommand(parm => Jog(0, GlobalVariables.motorSettings.JogVelX, int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            JogY = new RelayCommand(parm => Jog(1, GlobalVariables.motorSettings.JogVelY, int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            JogZ = new RelayCommand(parm => Jog(2, GlobalVariables.motorSettings.JogVelZ, int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            JogStop = new RelayCommand(parm => Stop(int.Parse(parm.ToString())));
            CommandEnableAxis = new RelayCommand(parm => EnableAxis(int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            CommandDisableAxis = new RelayCommand(parm => DisableAxis(int.Parse(parm.ToString())), cc => { return !_statusVM.IsBusy; });
            CommandMoveTo = new RelayCommand(parm => MoveTo(), cc => { return !_statusVM.IsBusy; });
            CommandStop = new RelayCommand(parm => Stop());
            CommandAddNewPoint = new RelayCommand(parm => AddNewPosition(Point.Parse(parm.ToString())));
            CommandRemoveLastPoint = new RelayCommand(parm => RemoveLastPosition(), cc => { return !_statusVM.IsBusy && PositionList.Count > 1; });
            CommandClearAllPoints = new RelayCommand(parm => ClearAllPoints(), cc => { return !_statusVM.IsBusy; });
            CommandHoming = new RelayCommand(parm => Homing(), cc => { return !_statusVM.IsBusy; });
            CommandSaveRefPosition = new RelayCommand(param => SaveRefPosition(), cc => { return !_statusVM.IsBusy; });

            InitTimer(30000, false);
        }

        public void connectMotor()
        {
            bwXYZMotoConnector.DoWork += ConnectXYZMotoDoWork;
            bwXYZMotoConnector.RunWorkerCompleted += ConnectXYZMotoCompleted;
            bwXYZMotoConnector.RunWorkerAsync();
        }

        #region Properties
        bool _laserStateON;
        public bool LaserStateON
        {
            get
            {
                return _laserStateON;
            }
            set
            {
                _laserStateON = value;
                OnPropertyChanged("LaserStateON");
            }
        }

        bool _ledStateON;
        public bool LedStateON
        {
            get
            {
                return _ledStateON;
            }
            set
            {
                _ledStateON = value;
                OnPropertyChanged("LedStateON");
            }
        }

        bool _connected;
        public bool Connected
        {
            get
            {
                return _connected;
            }
            set
            {
                _connected = value;
                OnPropertyChanged("Connected");
            }
        }

        bool _xEnabled;
        public bool XEnabled
        {
            get
            {
                return _xEnabled;
            }
            set
            {
                if (value != _xEnabled)
                {
                    _xEnabled = value;
                    OnPropertyChanged("XEnabled");
                }
            }
        }

        bool _zEnabled;
        public bool ZEnabled
        {
            get
            {
                return _zEnabled;
            }
            set
            {
                if (value != _zEnabled)
                {
                    _zEnabled = value;
                    OnPropertyChanged("ZEnabled");
                }
            }
        }

        bool _yEnabled;
        public bool YEnabled
        {
            get
            {
                return _yEnabled;
            }
            set
            {
                if (value != _yEnabled)
                {
                    _yEnabled = value;
                    OnPropertyChanged("YEnabled");
                }
            }
        }

        bool _startPointChecked = true;
        public bool StartPointChecked
        {
            get
            {
                return _startPointChecked;
            }
            set
            {
                if(value != _startPointChecked)
                {
                    _startPointChecked = value;
                    OnPropertyChanged("StartPointChecked");
                }
            }
        }

        bool _endPointChecked = false;
        public bool EndPointChecked
        {
            get
            {
                return _endPointChecked;
            }
            set
            {
                if (value != _endPointChecked)
                {
                    _endPointChecked = value;
                    OnPropertyChanged("EndPointChecked");
                }
            }
        }

        bool _axesMoving;
        public bool AxesMoving
        {
            get { return _axesMoving; }
            set
            {
                if (value != _axesMoving)
                {
                    _axesMoving = value;
                    OnPropertyChanged("AxesMoving");
                }
            }
        }
        bool _axesInPosition;
        public bool AxesInPosition
        {
            get { return _axesInPosition; }
            set
            {
                if (value != _axesInPosition)
                {
                    _axesInPosition = value;
                    OnPropertyChanged("AxesInPosition");
                }
            }
        }

        string _rpos;
        public string RPOS
        {
            get { return _rpos; }
            set
            {
                if (_rpos != value)
                {
                    _rpos = value;
                    OnPropertyChanged("RPOS");
                }
            }
        }
        string _fpos;
        public string FPOS
        {
            get { return _fpos; }
            set
            {
                if (_fpos != value)
                {
                    _fpos = value;
                    OnPropertyChanged("FPOS");
                }
            }
        }

        Point _startPoint;
        public Point StartPoint
        {
            get
            {
                return _startPoint;
            }
            set
            {
                if(_startPoint != value)
                {
                    _startPoint = value;
                    OnPropertyChanged("StartPoint");
                }
            }
        }

        Point _endPoint;
        public Point EndPoint
        {
            get
            {
                return _endPoint;
            }
            set
            {
                if (_endPoint != value)
                {
                    _endPoint = value;
                    OnPropertyChanged("EndPoint");
                }
            }
        }

        bool _xMoving;
        public bool XMoving
        {
            get
            {
                return _xMoving;
            }
            set
            {
                if(_xMoving != value)
                {
                    _xMoving = value;
                    OnPropertyChanged("XMoving");
                }
            }
        }

        bool _yMoving;
        public bool YMoving
        {
            get
            {
                return _yMoving;
            }
            set
            {
                if (_yMoving != value)
                {
                    _yMoving = value;
                    OnPropertyChanged("YMoving");
                }
            }
        }

        bool _zMoving;
        public bool ZMoving
        {
            get
            {
                return _zMoving;
            }
            set
            {
                if (_zMoving != value)
                {
                    _zMoving = value;
                    OnPropertyChanged("ZMoving");
                }
            }
        }

        bool _xInPosition;
        public bool XInPosition
        {
            get
            {
                return _xInPosition;
            }
            set
            {
                if(_xInPosition != value)
                {
                    _xInPosition = value;
                    OnPropertyChanged("XInPosition");
                }
            }
        }

        bool _yInPosition;
        public bool YInPosition
        {
            get
            {
                return _yInPosition;
            }
            set
            {
                if (_yInPosition != value)
                {
                    _yInPosition = value;
                    OnPropertyChanged("YInPosition");
                }
            }
        }

        bool _zInPosition;
        public bool ZInPosition
        {
            get
            {
                return _zInPosition;
            }
            set
            {
                if (_zInPosition != value)
                {
                    _zInPosition = value;
                    OnPropertyChanged("ZInPosition");
                }
            }
        }

        double _xRPosition;
        public double XRPosition {
            get
            {
                return _xRPosition;
            }
            set
            {
                _xRPosition = value;
                OnPropertyChanged("XRPosition");
            }
        }
        double _xFPosition;
        public double XFPosition
        {
            get
            {
                return _xFPosition;
            }
            set
            {
                _xFPosition = value;
                OnPropertyChanged("XFPosition");
            }
        }

        double _yRPosition;
        public double YRPosition
        {
            get
            {
                return _yRPosition;
            }
            set
            {
                _yRPosition = value;
                OnPropertyChanged("YRPosition");
            }
        }
        double _yFPosition;
        public double YFPosition
        {
            get
            {
                return _yFPosition;
            }
            set
            {
                _yFPosition = value;
                OnPropertyChanged("YFPosition");
            }
        }

        double _zRPosition;
        public double ZRPosition
        {
            get
            {
                return _zRPosition;
            }
            set
            {
                _zRPosition = value;
                OnPropertyChanged("ZRPosition");
            }
        }
        double _zFPosition;
        public double ZFPosition
        {
            get
            {
                return _zFPosition;
            }
            set
            {
                _zFPosition = value;
                OnPropertyChanged("ZFPosition");
            }
        }

        int _positionCount;
        public int PositionCount
        {
            get
            {
                return PositionList.Count;
            }
        }

        bool _xLimitL;
        public bool XLimitL
        {
            get
            {
                return _xLimitL;
            }
            set
            {
                if(_xLimitL == value)
                {
                    return;
                }
                _xLimitL = value;
                OnPropertyChanged("XLimitL");
            }
        }
        bool _xLimitR;
        public bool XLimitR
        {
            get
            {
                return _xLimitR;
            }
            set
            {
                if (_xLimitR == value)
                {
                    return;
                }
                _xLimitR = value;
                OnPropertyChanged("XLimitR");
            }
        }

        bool _yLimitL;
        public bool YLimitL
        {
            get
            {
                return _yLimitL;
            }
            set
            {
                if (_yLimitL == value)
                {
                    return;
                }
                _yLimitL = value;
                OnPropertyChanged("YLimitL");
            }
        }
        bool _yLimitR;
        public bool YLimitR
        {
            get
            {
                return _yLimitR;
            }
            set
            {
                if (_yLimitR == value)
                {
                    return;
                }
                _yLimitR = value;
                OnPropertyChanged("YLimitR");
            }
        }

        bool _zLimitL;
        public bool ZLimitL
        {
            get
            {
                return _zLimitL;
            }
            set
            {
                if (_zLimitL == value)
                {
                    return;
                }
                _zLimitL = value;
                OnPropertyChanged("ZLimitL");
            }
        }
        bool _zLimitR;
        public bool ZLimitR
        {
            get
            {
                return _zLimitR;
            }
            set
            {
                if (_zLimitR == value)
                {
                    return;
                }
                _zLimitR = value;
                OnPropertyChanged("ZLimitR");
            }
        }

        #endregion

        public void AddMarkedPointListSubscriber(MarkedPointsListHandler h)
        {
            MarkedPointsList += h;
        }

        void RaiseMarkedPointsListEvent(Point p, bool refPoint)
        {
            MarkedPointsList?.Invoke(p, refPoint);
        }

        void AddNewPosition(Point p)
        {
            PositionList.Add(p);
            OnPropertyChanged("PositionCount");
            RaiseMarkedPointsListEvent(p, false);
        }

        void ReplaceRefPosition(Point p)
        {
            if(PositionList.Count == 0)
            {
                PositionList.Add(p);
                OnPropertyChanged("PositionCount");
                RaiseMarkedPointsListEvent(p, true);
            } else
            {
                PositionList[0] = p;
                OnPropertyChanged("PositionCount");
                RaiseMarkedPointsListEvent(p, true);
            }
        }

        void RemoveLastPosition()
        {
            if (PositionList.Count > 0)
            {
                PositionList.RemoveAt(PositionList.Count - 1);
                OnPropertyChanged("PositionCount");
                RaiseMarkedPointsListEvent(new Point(-1, -1), false);
                if(PositionList.Count > 1)
                {
                    StartPoint = PositionList[PositionList.Count - 1];
                } else if(PositionList.Count == 1)
                {
                    StartPoint = new Point(0, 0);
                } else
                {
                    EndPoint = new Point(0, 0);
                }
            }
        }

        public void ClearAllPoints( int refP = 0)
        {
            while(PositionList.Count > refP)
            {
                RemoveLastPosition();
            }
        }

        public void Homing()
        {
            BackgroundWorker bwHoming = new BackgroundWorker();
            bwHoming.DoWork += DoHoming;
            bwHoming.RunWorkerCompleted += DoHomingCompleted;
            bwHoming.RunWorkerAsync();
        }

        void DoHoming(object sender, DoWorkEventArgs e)
        {
            _statusVM.IsBusy = true;

            double deltaX = -(XRPosition - GlobalVariables.motorSettings.XRefPositionInMM);
            double deltaY = -(YRPosition - GlobalVariables.motorSettings.YRefPositionInMM);
            double deltaZ = -(ZRPosition - GlobalVariables.motorSettings.ZRefPositionInMM);

            if (!MoveToXYZ(deltaX, deltaY, deltaZ))
            {
                e.Result = new Result(ResultStatus.ERROR, "Failed to move stone to the reference position");
                Console.WriteLine("batchmeasure: MoveTo({0}, {1}) failed", deltaX, deltaY);
                return;
            }

            e.Result = new Result(ResultStatus.SUCCESS, null);
            //if (motorManager.Homing())
            //{
            //    StartTimer();
            //    Thread.Sleep(100);
            //    while (!XInPosition || !YInPosition || !ZInPosition)
            //    {
            //        if (timeOut)
            //        {
            //            e.Result = new Result(ResultStatus.ERROR, "Timeout in GoToHome");
            //            return;
            //        }
            //        Thread.Sleep(500);
            //    }
            //    StopTimer();
            //    e.Result = new Result(ResultStatus.SUCCESS, null);
            //} else
            //{
            //    e.Result = new Result(ResultStatus.ERROR, "Failed in GoToHome");
            //}
        }

        void DoHomingCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _statusVM.IsBusy = false;
            if (((Result)e.Result).Status == ResultStatus.ERROR)
            {
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        void SaveRefPosition()
        {
            GlobalVariables.motorSettings.XRefPositionInMM = XRPosition;
            GlobalVariables.motorSettings.YRefPositionInMM = YRPosition;
            GlobalVariables.motorSettings.ZRefPositionInMM = ZRPosition;

            GlobalVariables.motorSettings.Save();
        }

        #region connect_xyzmoto
        void ConnectXYZMotoDoWork(object sender, DoWorkEventArgs e)
        {
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Trying to connect to motor..." };
            _statusVM.MotorMessages.Add(sts);

            if (Connect())
            {
                e.Result = motorManager.SerialNumber;
            }
            else
            {
                e.Result = "Error";
            }
        }

        void ConnectXYZMotoCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Result.ToString().Substring(0, 5) != "Error")
            {
                Connected = true;
                _statusVM.MotorMessages.Add(new StatusMessage { Timestamp = DateTime.Now, Message = "Connected to  " + e.Result.ToString() });
                // add ref point
                RefPointChanged();
            }
            else
            {
                Connected = false;
                var res = MessageBox.Show(e.Result.ToString(), "XYZMoto connection error, retry?", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    bwXYZMotoConnector.RunWorkerAsync();
                }
                else
                {
                    var sts = new StatusMessage { Timestamp = DateTime.Now, Message = "Camera connect error!" };
                    _statusVM.MotorMessages.Add(sts);
                }
            }
        }
        #endregion

        public RelayCommand JogX { get; private set; }
        public RelayCommand JogY { get; private set; }
        public RelayCommand JogZ { get; private set; }
        public RelayCommand JogStop { get; private set; }
        public RelayCommand CommandEnableAxis { get; private set; }
        public RelayCommand CommandDisableAxis { get; private set; }
        public RelayCommand CommandMoveTo { get; private set; }
        public RelayCommand CommandStop { get; private set; }
        public RelayCommand CommandAddNewPoint { get; private set; }
        public RelayCommand CommandRemoveLastPoint { get; private set; }
        public RelayCommand CommandClearAllPoints { get; private set; }
        public RelayCommand CommandHoming { get; private set; }
        public RelayCommand CommandSaveRefPosition { get; private set; }

        public bool Connect()
        {
            try
            {
                motorManager = MotorManager.getInstance();
                if (motorManager.Open() && motorManager.GoToHome(GlobalVariables.motorSettings.HomingBuffer))
                {
                    if (GlobalVariables.motorSettings.UseSimulator)
                    {
                        XInPosition = true;
                        YInPosition = true;
                        ZInPosition = true;
                    }
                    motorManager.MotorStateChanged += OnMotorStateChanged;
                    motorManager.MotorPositionChanged += OnMotorPositionChanged;
                    motorManager.LaserStateChanged += OnLaserStateChanged;
                    motorManager.LedStateChanged += OnLedStateChanged;
                    motorManager.AxisLimitChanged += OnAxisLimitChanged;

                    // initial motor profile after buffer homing finished
                    {
                        Thread.Sleep(200);
                        while (!XInPosition || !YInPosition || !ZInPosition)
                        {
                            Thread.Sleep(500);
                        }
                        InitialMotorProfile();

                        // move to saved ref position
                        Homing();
                    }
                    
                    return true;
                }
                else
                    throw new Exception("Could not connect to motor manager");
            }
            catch (Exception ex)
            {
                motorManager.Close();
                MessageBox.Show(ex.Message, "Error");
            }

            return false;
        }

        public void Jog(int axisNo, double vel, int direction)
        {
            if (motorManager == null)
                return;

            motorManager.Jog(axisNo, vel, direction);
        }

        public void Stop(int axisNo)
        {
            if (motorManager != null)
            {
                motorManager.Halt(axisNo);
            }
        }

        void EnableAxis(int axisNo)
        {
            if(motorManager != null && axisNo >= 0 && axisNo <= 2)
            {
                motorManager.EnableAxis(axisNo);
            }
        }

        void DisableAxis(int axisNo)
        {
            if (motorManager != null && axisNo >= 0 && axisNo <= 2)
            {
                motorManager.DisableAxis(axisNo);
            }
        }

        public bool FocusMove(double pos)
        {
            return motorManager.MoveToPoint_Relative(2, pos);
        }

        void Halt()
        {
            if (motorManager != null)
                motorManager.Halt();
        }

        private void OnMotorStateChanged(object sender, MotorStateEventArg e)
        {
            if(e.axisNo == 0)
            {
                if(e.state == State.Enable)
                {
                    XEnabled = e.value;
                } else if(e.state == State.Moving)
                {
                    XMoving = e.value;
                } else if(e.state == State.InPosition)
                {
                    XInPosition = e.value;
                }
            } else if(e.axisNo == 1)
            {
                if (e.state == State.Enable)
                {
                    YEnabled = e.value;
                }
                else if (e.state == State.Moving)
                {
                    YMoving = e.value;
                }
                else if (e.state == State.InPosition)
                {
                    YInPosition = e.value;
                }
            } else if(e.axisNo == 2)
            {
                if (e.state == State.Enable)
                {
                    ZEnabled = e.value;
                }
                else if (e.state == State.Moving)
                {
                    ZMoving = e.value;
                }
                else if (e.state == State.InPosition)
                {
                    ZInPosition = e.value;
                }
            }
        }

        private void OnMotorPositionChanged(object sender, MotorPositionEventArg e)
        {
            if (e.axisNo == 0)
            {
                XRPosition = e.rValue;
                XFPosition = e.fValue;
            }
            else if (e.axisNo == 1)
            {
                YRPosition = e.rValue;
                YFPosition = e.fValue;
            }
            else if (e.axisNo == 2)
            {
                ZRPosition = e.rValue;
                ZFPosition = e.fValue;
            }
        }

        private void OnLaserStateChanged(object sender, bool state)
        {
            LaserStateON = state;
        }

        private void OnLedStateChanged(object sender, bool state)
        {
            LedStateON = state;
        }

        private void OnAxisLimitChanged(object sender, AxisLimitEventArg e)
        {
            if (e.axisNo == 0)
            {
                if (e.limit == Limit.Left)
                {
                    XLimitL = e.value;
                }
                else if (e.limit == Limit.Right)
                {
                    XLimitR = e.value;
                }
            }
            else if (e.axisNo == 1)
            {
                if (e.limit == Limit.Left)
                {
                    YLimitL = e.value;
                }
                else if (e.limit == Limit.Right)
                {
                    YLimitR = e.value;
                }
            }
            else if (e.axisNo == 2)
            {
                if (e.limit == Limit.Left)
                {
                    ZLimitL = e.value;
                }
                else if (e.limit == Limit.Right)
                {
                    ZLimitR = e.value;
                }
            }

            if(e.value == true)
            {
                // axis reached limit cancel the work and popup the message
            }
        }

        public bool SetLedOnOff(bool state)
        {
            if (state == _ledStateON)
            {
                return true;
            }

            if (motorManager != null)
            {
                motorManager.SetLedOnOff(state);
                StartTimer();
            }
            else
            {
                return false;
            }
            Thread.Sleep(100);
            while (state != _ledStateON)
            {
                if (timeOut)
                {
                    return false;
                }
                Thread.Sleep(500);
            }

            StopTimer();
            return true;
        }

        public bool SetLaserOnOff(bool state)
        {
            if(state == _laserStateON)
            {
                return true;
            }

            if(motorManager != null)
            {
                motorManager.SetLaserOnOff(state);
                StartTimer();
            } else
            {
                return false;
            }
            Thread.Sleep(100);
            while (state != _laserStateON)
            {
                if (timeOut)
                {
                    return false;
                }
                Thread.Sleep(500);
            }

            StopTimer();
            return true;
        }

        void MoveTo()
        {
            BackgroundWorker bwMoveTo = new BackgroundWorker();
            bwMoveTo.DoWork += DoMoveTo;
            bwMoveTo.RunWorkerCompleted += DoMoveToCompleted;
            bwMoveTo.RunWorkerAsync();
        }

        void DoMoveTo(object sender, DoWorkEventArgs e)
        {
            _statusVM.IsBusy = true;
            if (motorManager != null)
            {
                double x = (EndPoint.X - StartPoint.X) / GlobalVariables.motorSettings.XPixelInMM;
                double y = -(EndPoint.Y - StartPoint.Y) / GlobalVariables.motorSettings.YPixelInMM;
                double[] pos = { x, y };
                
                if (!motorManager.MoveToPointXY_Relative(pos))
                {
                    e.Result = new Result(ResultStatus.ERROR, "Failed in MoveToPointXY_Relative");
                } else
                {
                    StartTimer();
                    Thread.Sleep(100);
                    while (!XInPosition || !YInPosition)
                    {
                        if (timeOut)
                        {
                            e.Result = new Result(ResultStatus.ERROR, "Timeout in MoveToPointXY_Relative");
                            return;
                        }
                        Thread.Sleep(500);
                    }
                    StopTimer();
                    e.Result = new Result(ResultStatus.SUCCESS, null);
                }
            } else
            {
                e.Result = new Result(ResultStatus.ERROR, "motorManager is null");
            }
        }

        void DoMoveToCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _statusVM.IsBusy = false;
            if (((Result)e.Result).Status == ResultStatus.ERROR)
            {
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        public bool MoveTo( double x, double y, bool isMM = true)
        {
            if (motorManager != null)
            {
                double[] pos = { isMM ? x : x / GlobalVariables.motorSettings.XPixelInMM, isMM ? y : y / GlobalVariables.motorSettings.YPixelInMM };
                motorManager.MoveToPointXY_Relative(pos);
                StartTimer();
            } else
            {
                return false;
            }

            Thread.Sleep(100);
            for (int i = 0; i < 3; i++)
            {
                while (!XInPosition || !YInPosition)
                {
                    if (timeOut)
                    {
                        return false;
                    }
                    if (XLimitL || XLimitR || YLimitL || YLimitR)
                    {
                        Console.WriteLine("MoveTo reached the limit in X or Y axis");
                        // reached the axis limit
                        // stop motor
                        Stop();
                        // stop timer
                        StopTimer();
                        return false;
                    }
                    Thread.Sleep(500);
                }
                Thread.Sleep(200);
            }
            
            StopTimer();
            return true;
        }

        public bool MoveToXYZ(double x, double y, double z)
        {
            if (motorManager != null)
            {
                double[] pos = { x, y, z };
                motorManager.MoveToPointXYZ_Relative(pos);
                StartTimer();
            }
            else
            {
                return false;
            }

            Thread.Sleep(100);
            for (int i = 0; i < 3; i++)
            {
                while (!XInPosition || !YInPosition || !ZInPosition)
                {
                    if (timeOut)
                    {
                        return false;
                    }
                    if (XLimitL || XLimitR || YLimitL || YLimitR || ZLimitL || ZLimitR)
                    {
                        Console.WriteLine("MoveTo reached the limit in X or Y axis");
                        // reached the axis limit
                        // stop motor
                        Stop();
                        // stop timer
                        StopTimer();
                        return false;
                    }
                    Thread.Sleep(500);
                }
                Thread.Sleep(200);
            }

            StopTimer();
            return true;
        }

        void Stop()
        {
            if (motorManager != null)
            {
                motorManager.Halt();
            }
        }

        void InitialMotorProfile()
        {
            motorManager.InitialMotionProfile();
        }

        public void MotorSettingsChanged()
        {
            InitialMotorProfile();
        }

        public void RefPointChanged()
        {
            EndPoint = new Point(GlobalVariables.motorSettings.XRef, GlobalVariables.motorSettings.YRef);
            ReplaceRefPosition(EndPoint);
            RaiseMarkedPointsListEvent(EndPoint, true);
        }
        #region dispose

        private bool _disposed = false;

        void CloseAxes()
        {
            if (motorManager != null)
            {
                motorManager.Close();
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
                    SetLaserOnOff(false);
                    SetLedOnOff(false);
                    if (aTimer != null)
                    {
                        aTimer.Dispose();
                    }
                    CloseAxes();
                }
                _disposed = true;
            }
        }

        #endregion

        private void InitTimer(double interval, bool autoReset)
        {
            if(aTimer != null)
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

        private void OnPointSelected(object sender, PointSelectedEventArgs e)
        {
            if(mainVM == null)
            {
                mainVM = (MainWindowVM)Application.Current.Windows.OfType<MainWindow>().FirstOrDefault().DataContext;
            }
            double width = mainVM.CameraVM.Width;
            double height = mainVM.CameraVM.Height;
            if (StartPointChecked)
            {
                if (PositionList.Count == 0)
                {
                    MessageBox.Show("Please select the End point or Calibrate first, then select Start point", "Warning");
                    return;
                }
                StartPoint = new Point(e.Value.X / e.Width * width, e.Value.Y / e.Height * height);
                AddNewPosition(StartPoint);
            } else if (EndPointChecked)
            {
                if (xyPixelInMMCalculated)
                {
                    EndPoint = new Point(e.Value.X / e.Width * width, e.Value.Y / e.Height * height);
                    ReplaceRefPosition(EndPoint);
                } else
                {
                    MessageBox.Show("Please calibrate the device first", "Warning");
                }
            }
        }

        public List<Point> GetPositionList()
        {
            return PositionList;
        }

        public void OnRefPointSelected(Point e)
        {
            //if(PositionList.Count > 0)
            //{
            //    ClearAllPoints();
            //}
            //EndPoint = e;
            //PositionList.Add(EndPoint);
            //OnPropertyChanged("PositionCount");
            //RaiseMarkedPointsListEvent(EndPoint, true);
            GlobalVariables.motorSettings.XRef = e.X;
            GlobalVariables.motorSettings.YRef = e.Y;
            RefPointChanged();
        }

        public void OnCalibrateXYPixelInMM(Point p)
        {
            xyPixelInMMCalculated = true;

            GlobalVariables.motorSettings.XPixelInMM = p.X;
            GlobalVariables.motorSettings.YPixelInMM = p.Y;
        }

        public void SetPointSelectedHandler(ref ZoomBorder zoomBorder) 
        {
            if (zoomBorder != null)
            {
                zoomBorder.PointSelected += OnPointSelected;
            }
        }

    }
}
