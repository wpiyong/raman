using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ViewModelLib;
using RamanMapping.Model;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Shapes;

namespace RamanMapping.ViewModel
{
    public delegate void CameraConnectionHandler(bool connected);
    public delegate void RemoveRectMarkerHandler();

    class CameraViewModel : ViewModelBase
    {
        StatusViewModel _statusVM;
        PtGreyCamera _ptGreyCamera;
        BackgroundWorker bwCameraConnector = new BackgroundWorker();
        public event CameraConnectionHandler CameraConnected;
        public event RemoveRectMarkerHandler RemoveRectMarker; 

        List<Tuple<Ellipse, TextBlock, Point>> PointMarkerList = new List<Tuple<Ellipse, TextBlock, Point>>();

        System.Windows.Shapes.Rectangle tmpRect = new System.Windows.Shapes.Rectangle();
        System.Windows.Point currentPoint;
        double rectW, rectH;

        public CameraViewModel(StatusViewModel svm)
        {
            base.DisplayName = "CameraViewModel";
            //_cameraConnected = false;
            _statusVM = svm;
        }

        void RaiseRemoveRectMarkerEvent()
        {
            RemoveRectMarker?.Invoke();
        }

        public void AddRemoveRectMarkerEventSubscriber(RemoveRectMarkerHandler h)
        {
            RemoveRectMarker += h;
        }

        public void connectCamera()
        {
            bwCameraConnector.DoWork += ConnectCameraDoWork;
            bwCameraConnector.RunWorkerCompleted += ConnectCameraCompleted;
            bwCameraConnector.RunWorkerAsync();
        }

        BitmapSource _cameraImage;
        public BitmapSource CameraImage
        {
            get
            {
                return _cameraImage;
            }
            set
            {
                _cameraImage = value;
                OnPropertyChanged("CameraImage");
            }
        }

        double _width;
        public double Width
        {
            get
            {
                return _width;
            }
            set
            {
                if (_width != value)
                {
                    _width = value;
                    OnPropertyChanged("Width");
                }
            }
        }

        double _height;
        public double Height
        {
            get
            {
                return _height;
            }
            set
            {
                if (_height != value)
                {
                    _height = value;
                    OnPropertyChanged("Height");
                }
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
                Width = _ptGreyCamera.ImageWidth;
                Height = _ptGreyCamera.ImageHeight;
                OnPropertyChanged("Connected");
            }
        }

        private void OnImageChanged(object sender, ImageEventArgs e)
        {
            CameraImage = e.image;
        }

        public List<Tuple<Ellipse, TextBlock, Point>> GetMarkedPointsList()
        {
            return PointMarkerList;
        }

        public void OnMarkedPointsList(Point p, bool refPoint)
        {
            if(p.X >= 0)
            {
                MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                Ellipse e = DrawCanvas.Circle(p.X, p.Y, refPoint ? 30 : 15, refPoint ? 30 : 15, refPoint, mv.CanvasDraw);
                if (PointMarkerList.Count > 0)
                {
                    if (refPoint)
                    {
                        TextBlock t = DrawCanvas.Text(p.X, p.Y, "0", refPoint, null, mv.CanvasDraw);
                        Tuple<Ellipse, TextBlock, Point> tuple = new Tuple<Ellipse, TextBlock, Point>(e, t, p);
                        mv.CanvasDraw.Children.Remove(PointMarkerList[0].Item1);
                        mv.CanvasDraw.Children.Remove(PointMarkerList[0].Item2);

                        PointMarkerList[0] = tuple;
                    } else
                    {
                        string s = PointMarkerList.Count.ToString();
                        TextBlock t = DrawCanvas.Text(p.X, p.Y, s, refPoint, null, mv.CanvasDraw);
                        Tuple<Ellipse, TextBlock, Point> tuple = new Tuple<Ellipse, TextBlock, Point>(e, t, p);
                        PointMarkerList.Add(tuple);
                    }
                }
                else
                {
                    string s = PointMarkerList.Count.ToString();
                    TextBlock t = DrawCanvas.Text(p.X, p.Y, s, refPoint, null, mv.CanvasDraw);
                    Tuple<Ellipse, TextBlock, Point> tuple = new Tuple<Ellipse, TextBlock, Point>(e, t, p);

                    PointMarkerList.Add(tuple);
                }
            } else
            {
                // remove last marker
                if(PointMarkerList.Count > 0)
                {
                    MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    mv.CanvasDraw.Children.Remove(PointMarkerList[PointMarkerList.Count - 1].Item1);
                    mv.CanvasDraw.Children.Remove(PointMarkerList[PointMarkerList.Count - 1].Item2);
                    PointMarkerList.RemoveAt(PointMarkerList.Count - 1);
                }
            }
        }

        public void DrawMarkedPoints()
        {
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            DrawCanvas.Circle(10, 50, 5, 5, false, mv.CanvasDraw);
        }

        public void ShowMarkedPoints()
        {
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mv.CanvasDraw.Visibility = Visibility.Visible;
        }

        public void HideMarkedPoints()
        {
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            mv.CanvasDraw.Visibility = Visibility.Hidden;
        }

        public void MouseDownHandler(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                currentPoint = e.GetPosition((Canvas)sender);
                Console.WriteLine("selected point: {0}, {1}", currentPoint.X, currentPoint.Y);
            }
        }

        public void MouseMoveHandler(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && currentPoint.X != 0 && currentPoint.Y != 0)
            {
                System.Windows.Point pos = e.GetPosition((Canvas)sender);
                rectW = pos.X - currentPoint.X;
                rectH = pos.Y - currentPoint.Y;
            }
        }

        public void MouseUpHandler(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released)
            {
                if (rectW != 0 && rectH != 0 && currentPoint.X != 0 && currentPoint.Y != 0)
                {
                    if (rectW < 0)
                    {
                        currentPoint.X = currentPoint.X + rectW;
                        rectW = -rectW;
                    }

                    if (rectH < 0)
                    {
                        currentPoint.Y = currentPoint.Y + rectH;
                        rectH = -rectH;
                    }
                    //RemoveTempMarker();
                    MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    tmpRect = DrawCanvas.Rect(currentPoint.X, currentPoint.Y, (int)rectW, (int)rectH, Brushes.Red, mv.CanvasDraw, 0.3);

                    currentPoint = default(Point);
                    rectW = 0;
                    rectH = 0;
                }
            }
        }

        private void OnRectMaskSelected(object sender, RectMaskEventArgs e)
        {
            if (e.Width == 0 && e.Height == 0 && e.ActuralW == 0 && e.ActuralH == 0)
            {
                RemoveTempMarker();
            }
            else
            {
                RemoveTempMarker();
                MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                double x = e.P.X / e.ActuralW * Width;
                double y = e.P.Y / e.ActuralH * Height;
                double w = e.Width * Width / e.ActuralW;
                double h = e.Height * Height / e.ActuralH;

                tmpRect = DrawCanvas.Rect(x, y, (int)w, (int)h, Brushes.Red, mv.CanvasDraw, 0.3);
            }
        }

        void RemoveTempMarker()
        {
            MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            // remove single object
            mv.CanvasDraw.Children.Remove(tmpRect);
            tmpRect = null;
            // remove all object on canvas
            //mv.CanvasDraw.Children.Clear();
            RaiseRemoveRectMarkerEvent();
        }

        public Rectangle GetRectMarker()
        {
            return tmpRect;
        }

        #region connect_camera
        void ConnectCameraDoWork(object sender, DoWorkEventArgs e)
        {
            DateTime timestamp = DateTime.Now;
            var sts = new StatusMessage { Timestamp = timestamp, Message = "Trying to connect to camera..." };
            _statusVM.CamMessages.Add(sts);

            _ptGreyCamera = new PtGreyCamera();
            if (_ptGreyCamera.Connect())
            {
                e.Result = _ptGreyCamera.SerialNumber.ToString();
                Connected = true;
            }
            else
            {
                e.Result = "Error";
                Connected = false;
            }

        }

        void RaiseCameraConnectedEvent(bool connected)
        {
            CameraConnected?.Invoke(connected);
        }

        void ConnectCameraCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            if (e.Result.ToString().Substring(0, 5) != "Error")
            {
                //changing this property to a new reference now
                //_cameraImage = new System.Windows.Media.Imaging.WriteableBitmap(64, 64, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);
                //since this is happening after the original reference was bound to the UI
                //we have to notify UI of new reference
                //CameraImage = _cameraImage;

                _statusVM.CamMessages.Add(new StatusMessage { Timestamp = DateTime.Now, Message = "Connected to " + e.Result.ToString() });

                _ptGreyCamera.StartCapture();
                _ptGreyCamera.imageEventListener.ImageChanged += OnImageChanged;

                //MainWindowVM mainVM = (MainWindowVM)Application.Current.Windows.OfType<MainWindow>().FirstOrDefault().DataContext; // how to access usercontrol instance declared in xaml
                //mainVM.XYZMotorVM.EndPoint = new Point(_ptGreyCamera.ImageWidth / 2, _ptGreyCamera.ImageHeight / 2);

                // Hook up zoom enent
                MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                mv.borderInstance.AddRectMaskSelectedEventHandler(OnRectMaskSelected);

                RaiseCameraConnectedEvent(true);
            }
            else
            {
                RaiseCameraConnectedEvent(false);
                var res = MessageBox.Show(e.Result.ToString(), "Camera connection error, retry?", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                {
                    bwCameraConnector.RunWorkerAsync();
                }
                else
                {
                    var sts = new StatusMessage { Timestamp = DateTime.Now, Message = "Camera connect error!" };
                    _statusVM.CamMessages.Add(sts);
                }
            }


        }
        #endregion

        public Queue<PtGreyCameraImage> GetImageQueue()
        {
            return _ptGreyCamera.GetImageQueue();
        }

        public void AddImageEnqueuedSubscriber(ImageEnqueueHandler h)
        {
            _ptGreyCamera.imageEventListener.ImageEnqueued += h;
        }

        public void AddCameraConnectedSubscriber(CameraConnectionHandler h)
        {
            CameraConnected += h;
        }

        public void CameraSettings()
        {
            _ptGreyCamera.EditCameraSettings();
        }

        public void InitialCalibrateSettings()
        {
            _ptGreyCamera.SetProprtyAutomaticSetting("Gain", false);
            _ptGreyCamera.SetAbsolutePropertyValue("Gain", "25");

            _ptGreyCamera.SetProprtyAutomaticSetting("Shutter", false);
            _ptGreyCamera.SetAbsolutePropertyValue("Shutter", "20");
        }

        public void PostCalibrateSettings()
        {
            _ptGreyCamera.SetProprtyAutomaticSetting("Gain", true);
            _ptGreyCamera.SetProprtyAutomaticSetting("Shutter", true);
        }

        public void StartEnqueue(bool refresh = true)
        {
            try
            {
                // software trigger
                //_ptGreyCamera.StopCapture();
                _ptGreyCamera.StartMeasuring(refresh);
                //_ptGreyCamera.StartCapture();
                //if (_ptGreyCamera.configTrigger(TriggerMode.On, TriggerType.Software))
                //{
                //    _ptGreyCamera.StartMeasuring(true);
                //    _ptGreyCamera.StartCapture();
                //    _ptGreyCamera.Trigger();
                //}
            }
            catch (Exception e)
            {
                Console.WriteLine("cameravm exception: " + e.Message);
                _ptGreyCamera.StopMeasuring();
            }
        }

        public void StopEnqueue()
        {
            try
            {
                _ptGreyCamera.StopMeasuring();
            } catch(Exception e)
            {
                Console.WriteLine("cameravm exception: " + e.Message);
            }
        }

        #region dispose

        private bool _disposed = false;

        void CloseCamera()
        {
            if (_ptGreyCamera != null)
                _ptGreyCamera.Close();

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
                    CloseCamera();
                }
                _disposed = true;
            }
        }

        #endregion
    }

    public class PointSelectedEventArgs : EventArgs
    {
        public double Width { get; }
        public double Height { get; }
        public PointSelectedEventArgs(Point p, double width, double height)
        {
            Value = p;
            Width = width;
            Height = height;
        }
        public Point Value { get; }
    }

    public class RectMaskEventArgs : EventArgs
    {
        public double Width { get; }
        public double Height { get; }
        public Point P { get; }
        public double ActuralW { get; }
        public double ActuralH { get; }
        public RectMaskEventArgs(Point p, double w, double h, double aw, double ah)
        {
            P = p;
            Width = w;
            Height = h;
            ActuralW = aw;
            ActuralH = ah;
        }
    }

    public class ZoomBorder : Border
    {
        private UIElement child = null;
        private Point origin;
        private Point start;
        private Point currentPoint = new Point(-1,-1);
        private double rectW, rectH;
        private double imageW, imageH;

        public event EventHandler<PointSelectedEventArgs> PointSelected;
        public event EventHandler<RectMaskEventArgs> RectMaskSelected;

        protected virtual void RaisePointSelectedEvent(PointSelectedEventArgs eventArgs)
        {
            PointSelected?.Invoke(this, eventArgs);
        }

        protected virtual void RaiseRectMaskSelectedEvent(RectMaskEventArgs eventArgs)
        {
            RectMaskSelected?.Invoke(this, eventArgs);
        }

        public void AddRectMaskSelectedEventHandler(EventHandler<RectMaskEventArgs> h)
        {
            RectMaskSelected += h;
        }

        private TranslateTransform GetTranslateTransform(UIElement element)
        {
            return (TranslateTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is TranslateTransform);
        }

        private ScaleTransform GetScaleTransform(UIElement element)
        {
            return (ScaleTransform)((TransformGroup)element.RenderTransform)
              .Children.First(tr => tr is ScaleTransform);
        }

        public override UIElement Child
        {
            get { return base.Child; }
            set
            {
                if (value != null && value != this.Child)
                    this.Initialize(value);
                base.Child = value;
            }
        }

        public void Initialize(UIElement element)
        {
            this.child = element;
            if (child != null)
            {
                TransformGroup group = new TransformGroup();
                ScaleTransform st = new ScaleTransform();
                group.Children.Add(st);
                TranslateTransform tt = new TranslateTransform();
                group.Children.Add(tt);
                child.RenderTransform = group;
                child.RenderTransformOrigin = new Point(0.0, 0.0);
                this.MouseWheel += child_MouseWheel;
                this.MouseLeftButtonDown += child_MouseLeftButtonDown;
                this.MouseLeftButtonUp += child_MouseLeftButtonUp;
                this.MouseMove += child_MouseMove;
                this.PreviewMouseRightButtonDown += new MouseButtonEventHandler(
                  child_PreviewMouseRightButtonDown);
            }
        }

        public void Reset()
        {
            if (child != null)
            {
                // reset zoom
                var st = GetScaleTransform(child);
                st.ScaleX = 1.0;
                st.ScaleY = 1.0;

                // reset pan
                var tt = GetTranslateTransform(child);
                tt.X = 0.0;
                tt.Y = 0.0;
            }
        }

        #region Child Events

        private void child_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            return;
            if (child != null)
            {
                var st = GetScaleTransform(child);
                var tt = GetTranslateTransform(child);

                double zoom = e.Delta > 0 ? .2 : -.2;
                if (!(e.Delta > 0) && (st.ScaleX < .4 || st.ScaleY < .4))
                    return;

                Point relative = e.GetPosition(child);
                double absoluteX;
                double absoluteY;

                absoluteX = relative.X * st.ScaleX + tt.X;
                absoluteY = relative.Y * st.ScaleY + tt.Y;

                st.ScaleX += zoom;
                st.ScaleY += zoom;

                tt.X = absoluteX - relative.X * st.ScaleX;
                tt.Y = absoluteY - relative.Y * st.ScaleY;
            }
        }

        private void child_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                //var tt = GetTranslateTransform(child);
                //start = e.GetPosition(this);
                //origin = new Point(tt.X, tt.Y);
                //this.Cursor = Cursors.Hand;
                //child.CaptureMouse();
                double width, height;
                currentPoint = GetImageCoordsAt(e, out width, out height);
                imageW = width;
                imageH = height;
            }
        }

        private void child_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (child != null)
            {
                //child.ReleaseMouseCapture();
                //this.Cursor = Cursors.Arrow;
                if (rectW != 0 && rectH != 0 && currentPoint.X != -1 && currentPoint.Y != -1)
                {
                    if (rectW < 0)
                    {
                        currentPoint.X = currentPoint.X + rectW;
                        rectW = -rectW;
                    }

                    if (rectH < 0)
                    {
                        currentPoint.Y = currentPoint.Y + rectH;
                        rectH = -rectH;
                    }
                    //RemoveTempMarker();
                    //MainWindow mv = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
                    //tmpRect = DrawCanvas.Rect(currentPoint.X, currentPoint.Y, (int)rectW, (int)rectH, Brushes.Red, mv.CanvasResult, 0.3);

                    RaiseRectMaskSelectedEvent(new RectMaskEventArgs(currentPoint, rectW, rectH, imageW, imageH));

                    currentPoint = new Point(-1, -1);
                    rectW = 0;
                    rectH = 0;
                }
            }
        }

        //void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    this.Reset();
        //}

        public Point GetImageCoordsAt(MouseButtonEventArgs e, out double width, out double height)
        {
            width = 0;
            height = 0;
            if (child != null && child.IsMouseOver)
            {
                Image image = child as Image;
                width = image.ActualWidth;
                height = image.ActualHeight;
                return e.GetPosition(child);
            }
            return new Point(-1, -1);
        }

        public Point GetImageCoordsAt(MouseEventArgs e, out double width, out double height)
        {
            width = 0;
            height = 0;
            if (child != null && child.IsMouseOver)
            {
                Image image = child as Image;
                width = image.ActualWidth;
                height = image.ActualHeight;
                return e.GetPosition(child);
            }
            return new Point(-1, -1);
        }

        void child_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                //MessageBox.Show(GetImageCoordsAt(e).ToString());
                double width, height;
                Point p = GetImageCoordsAt(e, out width, out height);
                if (p.X >= 0 && p.Y >= 0)
                {
                    RaisePointSelectedEvent(new PointSelectedEventArgs(p, width, height));
                }
            } else
            {
                RaiseRectMaskSelectedEvent(new RectMaskEventArgs(currentPoint, 0, 0, 0, 0));
                this.Reset();
            }
        }

        private void child_MouseMove(object sender, MouseEventArgs e)
        {
            if (child != null)
            {
                if (e.LeftButton == MouseButtonState.Pressed && currentPoint.X != -1 && currentPoint.Y != -1)
                {
                    double width, height;
                    Point pos = GetImageCoordsAt(e, out width, out height);
                    rectW = pos.X - currentPoint.X;
                    rectH = pos.Y - currentPoint.Y;
                }
            }
            return;
            if (child != null)
            {
                if (child.IsMouseCaptured)
                {
                    var tt = GetTranslateTransform(child);
                    Vector v = start - e.GetPosition(this);
                    tt.X = origin.X - v.X;
                    tt.Y = origin.Y - v.Y;
                }
            }
        }

        #endregion
    }
}
