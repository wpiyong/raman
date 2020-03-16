using System;
using System.Windows;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ViewModelLib;
using RamanMapping.Model;

namespace RamanMapping.ViewModel
{
    class MainWindowVM : ViewModelBase
    {
        public StatusViewModel StatusVM { get; set; }
        public CameraViewModel CameraVM { get; set; }
        public XYZAxesViewModel XYZMotorVM { get; set; }
        public SpectrometerViewModel SpectrometerVM { get; set; }
        public MeasurementViewModel MeasurementVM { get; set; }

        public MainWindowVM()
        {
            base.DisplayName = "MainWindowViewModel";

            StatusVM = new StatusViewModel();
            StatusVM.Busy = 0;

            CameraVM = new CameraViewModel(StatusVM);
            XYZMotorVM = new XYZAxesViewModel(StatusVM);
            SpectrometerVM = new SpectrometerViewModel(StatusVM);
            MeasurementVM = new MeasurementViewModel(CameraVM, XYZMotorVM, SpectrometerVM, StatusVM);

            CameraVM.AddCameraConnectedSubscriber(new CameraConnectionHandler(OnCameraConnected));
            CameraVM.AddRemoveRectMarkerEventSubscriber(new RemoveRectMarkerHandler(MeasurementVM.OnRemovePointListOnRectMarker));
        }

        public void OnCameraConnected(bool connected)
        {
            if (connected)
            {
                MeasurementVM.AddImageEnqueuedSubscriber();
                MeasurementVM.AddRefPointSelectedSubscriber(new RefPointSelectedHandler(XYZMotorVM.OnRefPointSelected));
                MeasurementVM.AddCalibrateXYPixelInMMSubscriber(new CalibrateXYPixelInMMHandler(XYZMotorVM.OnCalibrateXYPixelInMM));
                //XYZMotorVM.EndPoint = new Point(CameraVM.Width / 2, CameraVM.Height / 2);

                XYZMotorVM.AddMarkedPointListSubscriber(new MarkedPointsListHandler(CameraVM.OnMarkedPointsList));
                if (XYZMotorVM.Connected)
                {
                    XYZMotorVM.RefPointChanged();
                }
            }
        }

        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            CameraVM.Dispose();
            XYZMotorVM.Dispose();
            MeasurementVM.Dispose();
            SpectrometerVM.Dispose();
            StatusVM.Dispose();
            App.Current.Shutdown();
        }

        public void OnViewLoaded(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault(); // how to access usercontrol instance declared in xaml
            XYZMotorVM.SetPointSelectedHandler(ref mainWindow.borderInstance);
            SpectrometerVM.MainWindow_SourceInitialized(mainWindow.MainWin);
            ConnectAll();
        }

        public void OnActivated(object sender, EventArgs e)
        {
            Console.WriteLine("OnActivated");
        }

        private void ConnectAll()
        {
            CameraVM.connectCamera();
            XYZMotorVM.connectMotor();
            SpectrometerVM.connectSpectrum();
        }
    }
}
