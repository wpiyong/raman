using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using RamanMapping.Model;
using ViewModelLib;
using _405Analyzer;
using System.Data;
using PeakFinder;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using System.Windows.Media;
using System.Windows.Threading;
//using System.Windows.Shapes;

namespace RamanMapping.ViewModel
{
    enum TEST_PARM_TYPE
    {
        SKIP = -1,
        NATURAL,
        SYNTHETIC,
        SIMULANT,
        INDIVIDUAL
    }

    class CamPhosResults
    {
        public ulong StartTimeStamp;
        public List<PtGreyCameraImage> Images;
        public List<System.Drawing.Bitmap> FilteredImages;

        public CamPhosResults(ulong startTime, List<PtGreyCameraImage> images)
        {
            StartTimeStamp = startTime;
            Images = images.ToList();
        }
        public CamPhosResults(ulong startTime, List<PtGreyCameraImage> images, List<System.Drawing.Bitmap> filteredImages)
        {
            StartTimeStamp = startTime;
            Images = images.ToList();
            FilteredImages = filteredImages;
        }
    }

    class SpectrometerPhosResults
    {
        public ulong StartTimeStamp;
        public List<AvantesSpectrum> Spectra;

        public SpectrometerPhosResults(ulong startTime, List<AvantesSpectrum> spectra)
        {
            StartTimeStamp = startTime;
            Spectra = spectra.ToList();
        }

        public SpectrometerPhosResults()
        {
            StartTimeStamp = (ulong)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            Spectra = new List<AvantesSpectrum>();
        }
    }

    class PhosResultsViewModel : ViewModelBase
    {
        bool _dataSaved = false;
        List<PtGreyCameraImage> _phosImages = new List<PtGreyCameraImage>();
        List<System.Drawing.Bitmap> _phosFilteredImages = new List<System.Drawing.Bitmap>();
        ulong _startTimeStamp;
        List<AvantesSpectrum> _phosSpectra = new List<AvantesSpectrum>();
        ulong _spectrumStartTimeStamp;
        bool _busy = false;
        bool mappingMeasure = false;
        System.Windows.Shapes.Rectangle RectMark = null;

        List<CamPhosResults> CamResList = null;
        List<SpectrometerPhosResults> SpecResList = null;
        List<List<PeakDetectInfo>> PeakInfoList = null;
        ANALYZER_RESULT DiamondResult;
        List<List<List<PeakDetectInfo>>> PeakInfoListList = null;
        List<ANALYZER_RESULT> DiamondResList = null;
        List<Tuple<System.Windows.Shapes.Ellipse, TextBlock, Point>> MarkerList = null;

        List<PtGreyCameraImage> CamResultsPre = new List<PtGreyCameraImage>();
        Dictionary<int, SolidColorBrush> ColorLegendList = new Dictionary<int, SolidColorBrush>();

        public PhosResultsViewModel(CamPhosResults camResults, SpectrometerPhosResults spectroResults)
        {
            base.DisplayName = "PhosResultsViewModel";
            ShowListItemControl = false;
            _phosImages = camResults.Images;
            _phosFilteredImages = camResults.FilteredImages;
            _phosSpectra = spectroResults.Spectra;
            CurrentPhosItem = 1;
            CurrentSpectraItem = 1;
            SaveFolderPath = Properties.Settings.Default.SaveFolderPath;
            _startTimeStamp = camResults.StartTimeStamp;
            _spectrumStartTimeStamp = spectroResults.StartTimeStamp;

            CommandSetFolder = new RelayCommand(param => SetFolder(), cc => _busy == false);
            CommandPreviousPhosItem = new RelayCommand(param => PreviousPhosItem(), cc => _busy == false);
            CommandNextPhosItem = new RelayCommand(param => NextPhosItem(), cc => _busy == false);
            CommandPreviousSpectraItem = new RelayCommand(param => PreviousSpectraItem(), cc => _busy == false);
            CommandNextSpectraItem = new RelayCommand(param => NextSpectraItem(), cc => _busy == false);
            CommandSave = new RelayCommand(param => Save(), cc => _busy == false);
            CommandSaveAll = new RelayCommand(param => Save(true), cc => _busy == false);

            CommandCalibrate = new RelayCommand(param => Calibrate());

            SpectrumData = new ObservableDataSource<Point>();
            SmoothSpectrumData = new ObservableDataSource<Point>();
        }


        public PhosResultsViewModel(List<CamPhosResults> camResList, List<SpectrometerPhosResults> specResList)
        {
            base.DisplayName = "PhosResultsViewModel";
            ShowListItemControl = true;
            CamResList = camResList;
            SpecResList = specResList;

            _phosImages = CamResList[0].Images;
            _phosFilteredImages = CamResList[0].FilteredImages;
            _phosSpectra = SpecResList[0].Spectra;
            CurrentListItem = 1;
            CurrentPhosItem = 1;
            CurrentSpectraItem = 1;
            SaveFolderPath = Properties.Settings.Default.SaveFolderPath;
            _startTimeStamp = CamResList[0].StartTimeStamp;
            _spectrumStartTimeStamp = SpecResList[0].StartTimeStamp;

            CommandSetFolder = new RelayCommand(param => SetFolder(), cc => _busy == false);
            CommandPreviousPhosItem = new RelayCommand(param => PreviousPhosItem(), cc => _busy == false);
            CommandNextPhosItem = new RelayCommand(param => NextPhosItem(), cc => _busy == false);
            CommandPreviousSpectraItem = new RelayCommand(param => PreviousSpectraItem(), cc => _busy == false);
            CommandNextSpectraItem = new RelayCommand(param => NextSpectraItem(), cc => _busy == false);
            CommandSave = new RelayCommand(param => Save(), cc => _busy == false);
            CommandSaveAll = new RelayCommand(param => Save(true), cc => _busy == false);

            CommandPreviousListItem = new RelayCommand(param => PreviousListItem(), cc => _busy == false);
            CommandNextListItem = new RelayCommand(param => NextListItem(), cc => _busy == false);

            CommandCalibrate = new RelayCommand(param => Calibrate());

            SpectrumData = new ObservableDataSource<Point>();
            SmoothSpectrumData = new ObservableDataSource<Point>();
        }

        public PhosResultsViewModel(CamPhosResults camResults, SpectrometerPhosResults spectroResults, List<List<PeakDetectInfo>> peakInfoList, ANALYZER_RESULT diamondResult, List<Tuple<System.Windows.Shapes.Ellipse, TextBlock, Point>> markerList, CamPhosResults camResultsPre)
        {
            base.DisplayName = "PhosResultsViewModel";
            ShowListItemControl = false;
            _phosImages = camResults.Images;
            _phosFilteredImages = camResults.FilteredImages;
            _phosSpectra = spectroResults.Spectra;
            NumSpectraItems = _phosSpectra.Count.ToString();
            CurrentListItem = 0;
            CurrentPhosItem = 1;
            CurrentSpectraItem = 1;
            SaveFolderPath = Properties.Settings.Default.SaveFolderPath;
            _startTimeStamp = camResults.StartTimeStamp;
            _spectrumStartTimeStamp = spectroResults.StartTimeStamp;
            PeakInfoList = peakInfoList;
            DiamondResult = diamondResult;
            SpectrumIntegrationTime = _phosSpectra[0].IntegrationTime.ToString();
            MarkerList = markerList;
            CamResultsPre = camResultsPre.Images;

            Width = CamResultsPre[0].Image.Width;
            Height = CamResultsPre[0].Image.Height;

            CommandSetFolder = new RelayCommand(param => SetFolder(), cc => _busy == false);
            CommandPreviousPhosItem = new RelayCommand(param => PreviousPhosItem(), cc => _busy == false);
            CommandNextPhosItem = new RelayCommand(param => NextPhosItem(), cc => _busy == false);
            CommandPreviousSpectraItem = new RelayCommand(param => PreviousSpectraItem(), cc => _busy == false);
            CommandNextSpectraItem = new RelayCommand(param => NextSpectraItem(), cc => _busy == false);
            CommandSave = new RelayCommand(param => Save(), cc => _busy == false);
            CommandSaveAll = new RelayCommand(param => Save(true), cc => _busy == false);

            CommandCalibrate = new RelayCommand(param => Calibrate());

            SpectrumData = new ObservableDataSource<Point>();
            SmoothSpectrumData = new ObservableDataSource<Point>();

            InitializeColorLegend();
        }

        public PhosResultsViewModel(List<CamPhosResults> camResList, List<SpectrometerPhosResults> specResList, List<List<List<PeakDetectInfo>>> peakInfoListList, List<ANALYZER_RESULT> diamondResList, List<Tuple<System.Windows.Shapes.Ellipse, TextBlock, Point>> markerList, CamPhosResults camResultsPre, bool mappingMeasure = false, System.Windows.Shapes.Rectangle rect = null)
        {
            base.DisplayName = "PhosResultsViewModelBatchMeasurement";
            ShowListItemControl = false;
            CamResList = camResList;
            SpecResList = specResList;
            PeakInfoListList = peakInfoListList;
            DiamondResList = diamondResList;
            MarkerList = markerList;
            CamResultsPre = camResultsPre.Images;
            this.mappingMeasure = mappingMeasure;
            RectMark = rect;

            Width = CamResultsPre[0].Image.Width;
            Height = CamResultsPre[0].Image.Height;

            _phosImages = CamResList[0].Images;
            _phosFilteredImages = CamResList[0].FilteredImages;
            _phosSpectra = SpecResList[0].Spectra;
            DiamondResult = DiamondResList[0];
            PeakInfoList = peakInfoListList[0];

            CurrentListItem = 1;
            CurrentPhosItem = 1;
            CurrentSpectraItem = 1;
            SaveFolderPath = Properties.Settings.Default.SaveFolderPath;
            _startTimeStamp = CamResList[0].StartTimeStamp;
            _spectrumStartTimeStamp = SpecResList[0].StartTimeStamp;
            SpectrumIntegrationTime = _phosSpectra[0].IntegrationTime.ToString();

            CommandSetFolder = new RelayCommand(param => SetFolder(), cc => _busy == false);
            CommandPreviousPhosItem = new RelayCommand(param => PreviousPhosItem(), cc => _busy == false);
            CommandNextPhosItem = new RelayCommand(param => NextPhosItem(), cc => _busy == false);
            CommandPreviousSpectraItem = new RelayCommand(param => PreviousSpectraItem(), cc => _busy == false);
            CommandNextSpectraItem = new RelayCommand(param => NextSpectraItem(), cc => _busy == false);
            CommandSave = new RelayCommand(param => Save(), cc => _busy == false);
            CommandSaveAll = new RelayCommand(param => Save(true), cc => _busy == false);

            CommandPreviousListItem = new RelayCommand(param => PreviousListItem(), cc => _busy == false);
            CommandNextListItem = new RelayCommand(param => NextListItem(), cc => _busy == false);

            CommandCalibrate = new RelayCommand(param => Calibrate(), cc => mappingMeasure == false);

            SpectrumData = new ObservableDataSource<Point>();
            SmoothSpectrumData = new ObservableDataSource<Point>();

            InitializeColorLegend();
        }

        public RelayCommand CommandPreviousPhosItem { get; set; }
        public RelayCommand CommandNextPhosItem { get; set; }
        public RelayCommand CommandPreviousSpectraItem { get; set; }
        public RelayCommand CommandNextSpectraItem { get; set; }
        public RelayCommand CommandSetFolder { get; set; }
        public RelayCommand CommandSave { get; set; }
        public RelayCommand CommandSaveAll { get; set; }

        public RelayCommand CommandPreviousListItem { get; set; }
        public RelayCommand CommandNextListItem { get; set; }

        public RelayCommand CommandCalibrate { get; set; }

        void InitializeColorLegend()
        {
            ColorLegendList.Add(-2, Brushes.Yellow);
            ColorLegendList.Add(-1, Brushes.Red);
            ColorLegendList.Add(0, Brushes.Cyan);
            ColorLegendList.Add(1, Brushes.Orange);
            ColorLegendList.Add(2, Brushes.Green);
            ColorLegendList.Add(3, Brushes.Magenta);
            ColorLegendList.Add(4, Brushes.Violet);
            ColorLegendList.Add(5, Brushes.Salmon);
            ColorLegendList.Add(6, Brushes.LawnGreen);
        }

        #region properties
        string _saveFolderPath;
        public string SaveFolderPath
        {
            get { return _saveFolderPath; }
            set
            {
                _saveFolderPath = value;
                OnPropertyChanged("SaveFolderPath");
            }
        }

        public string NumListItems
        {
            get
            {
                if (CamResList != null)
                {
                    return CamResList.Count.ToString();
                } else
                {
                    return "0";
                }
            }
        }

        public string NumPhosItems
        {
            get { return _phosImages.Count.ToString(); }
        }

        string _numSpectraItems;
        public string NumSpectraItems
        {
            get { return _numSpectraItems; }
            set
            {
                if(_numSpectraItems == value)
                {
                    return;
                }
                _numSpectraItems = value;
                OnPropertyChanged("NumSpectraItems");
            }
        }

        uint _currentPhosItem;
        public uint CurrentPhosItem
        {
            get
            {
                if (_phosImages.Count > 0)
                    return _currentPhosItem;
                else
                    return 0;
            }
            set
            {
                if (value > 0 && value <= _phosImages.Count)
                {
                    _currentPhosItem = value;
                }
                OnPropertyChanged("CurrentPhosItem");
                OnPropertyChanged("PhosImage");
                OnPropertyChanged("FrameId");
                OnPropertyChanged("TimeStamp");
                OnPropertyChanged("TimeStampDelta");
            }
        }

        uint _currentListItem;
        public uint CurrentListItem
        {
            get
            {
                if (CamResList != null && CamResList.Count > 0)
                    return _currentListItem;
                else
                    return 0;
            }
            set
            {
                if (value > 0 && value <= CamResList.Count)
                {
                    _currentListItem = value;
                }
                OnPropertyChanged("CurrentListItem");
                OnPropertyChanged("PhosImage");
                OnPropertyChanged("FrameId");
                OnPropertyChanged("TimeStamp");
                OnPropertyChanged("TimeStampDelta");
            }
        }

        uint _currentSpectraItem;
        public uint CurrentSpectraItem
        {
            get
            {
                if (_phosSpectra.Count > 0)
                    return _currentSpectraItem;
                else
                    return 0;
            }
            set
            {
                if (value > 0 && value <= _phosSpectra.Count)
                {
                    _currentSpectraItem = value;
                    SpectrumIntegrationTime = _phosSpectra[(int)_currentSpectraItem-1].IntegrationTime.ToString();
                }
                OnPropertyChanged("CurrentSpectraItem");
                OnPropertyChanged("PhosSpectrum");
                OnPropertyChanged("SpectrumId");
                OnPropertyChanged("SpectrumTimeStamp");
                OnPropertyChanged("SpectrumTimeStampDelta");
            }
        }

        bool _showListItemControl = true;
        public bool ShowListItemControl
        {
            get
            {
                return _showListItemControl;
            }
            set
            {
                if(_showListItemControl == value)
                {
                    return;
                }
                _showListItemControl = value;
                OnPropertyChanged("ShowListItemControl");
            }
        }

        public BitmapSource PhosImage
        {
            get
            {
                if (_phosImages.Count > 0)
                    return DisplayFilteredImage ? BitmapToBitmapSource(_phosFilteredImages[(int)(CurrentPhosItem - 1)]) : BitmapToBitmapSource(_phosImages[(int)(CurrentPhosItem - 1)].Image);
                else
                    return null;
            }
        }

        public BitmapSource ImagePre
        {
            get
            {
                if(CamResultsPre.Count > 0)
                {
                    return BitmapToBitmapSource(CamResultsPre[0].Image);
                } else
                {
                    return null;
                }
            }
        }

        public string FrameId
        {
            get
            {
                if (_phosImages.Count > 0)
                    return _phosImages[(int)(CurrentPhosItem - 1)].FrameId.ToString();
                else
                    return "";
            }
        }

        public string TimeStamp
        {
            get
            {
                if (_phosImages.Count > 0)
                    return _phosImages[(int)(CurrentPhosItem - 1)].TimeStamp.ToString();
                else
                    return "";
            }
        }

        public string TimeStampDelta
        {
            get
            {
                //if (CurrentItem > 1)
                //    return (Math.Round((double)(_phosImages[(int)(CurrentItem - 1)].TimeStamp - 
                //        _phosImages[(int)(CurrentItem - 2)].TimeStamp)/1000000d, 0)).ToString();
                if (_phosImages.Count > 0)
                    return (Math.Round((double)(_phosImages[(int)(CurrentPhosItem - 1)].TimeStamp -
                        _startTimeStamp) / 1000000d, 0)).ToString();
                else
                    return "";
            }
        }

        public ObservableDataSource<Point> PhosSpectrum
        {
            get
            {
                if (_phosSpectra.Count > 0)
                    return new ObservableDataSource<Point>(_phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Take(2048));
                else
                    return null;
            }
        }
        public string SpectrumId
        {
            get
            {
                if (_phosSpectra.Count > 0)
                    return _phosSpectra[(int)(CurrentSpectraItem - 1)].Id.ToString();
                else
                    return "";
            }
        }

        public string SpectrumTimeStamp
        {
            get
            {
                if (_phosSpectra.Count > 0)
                    return _phosSpectra[(int)(CurrentSpectraItem - 1)].TimeStamp.ToString();
                else
                    return "";
            }
        }

        string _spectrumIntegrationTime;
        public string SpectrumIntegrationTime
        {
            get
            {
                return _spectrumIntegrationTime;
            }
            set
            {
                if(_spectrumIntegrationTime == value)
                {
                    return;
                }
                _spectrumIntegrationTime = value;
                OnPropertyChanged("SpectrumIntegrationTime");
            }
        }

        public string SpectrumTimeStampDelta
        {
            get
            {
                if (_phosSpectra.Count > 0)
                    return (Math.Round((double)(_phosSpectra[(int)(CurrentSpectraItem - 1)].TimeStamp -
                        _spectrumStartTimeStamp) / 1000d, 0)).ToString();
                else
                    return "";
            }
        }

        public bool cbFilteredImageEnabled
        {
            get
            {
                if (_phosFilteredImages.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        bool _displayFilteredImage;
        public bool DisplayFilteredImage
        {
            get
            {
                return _displayFilteredImage;
            }
            set
            {
                _displayFilteredImage = value;
                OnPropertyChanged("PhosImage");
            }
        }

        string _analyzingResult;
        public string AnalyzingResult
        {
            get
            {
                return _analyzingResult;
            }
            set
            {
                if (mappingMeasure)
                {
                    _analyzingResult = "";
                }
                else
                {
                    _analyzingResult = value;
                }
                OnPropertyChanged("AnalyzingResult");
            }
        }

        ObservableDataSource<Point> _spectrumData;
        public ObservableDataSource<Point> SpectrumData
        {
            get
            {
                return _spectrumData;
            }
            set
            {
                _spectrumData = value;
                OnPropertyChanged("SpectrumData");
            }
        }

        public ObservableDataSource<Point> SmoothSpectrumData { get; set; }

        int _tabIndex = 0;
        public int TabIndex
        {
            get
            {
                return _tabIndex;
            }
            set
            {
                if(_tabIndex == value)
                {
                    return;
                }
                _tabIndex = value;
                if(_tabIndex == 1)
                {
                    AnalyzingResult = ResultString((int)DiamondResult);
                    UpdateMarker();
                }
                if(_tabIndex == 0)
                {
                    ShowListItemControl = false;
                } else
                {
                    ShowListItemControl = true;
                }
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
        #endregion

        static string ResultString(int dt)
        {
            var leg = "";
            switch (dt)
            {
                case -2:
                    leg = "ERROR SPIKE, please measure again";
                    break;
                case 0:
                    leg = "NATURAL DIAMOND";
                    break;
                case 1:
                    leg = "NON-DIAMOND";
                    break;
                case 2:
                    leg = "HPHT SYNTHETIC DIAMOND";
                    break;
                case 3:
                    leg = "DIAMOND, Referred for further testing";
                    break;
                case 4:
                    leg = "SATURATED";
                    break;
                case 5:
                    leg = "DIAMOND, Referred for further testing";
                    break;
                case 6:
                    leg = "CVD SYNTHETIC DIAMOND";
                    break;
                default:
                    leg = "ERROR";
                    break;
            }

            return leg;
        }

        void Calibrate()
        {
            List<double> wl = _phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Select(p => p.X).ToList();
            List<double> counts = _phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Select(p => p.Y).ToList();

            SpectrumAnalyzer analyzer = new SpectrumAnalyzer(wl, counts);
            string error;
            if (!analyzer.AutoCalibrate(out error))
            {
                MessageBox.Show(error, "Calibration failed, try another spectrum");
                return;
            }

            RamanAnalyzer.Analysis(wl, counts);

        }

        void Analyzing()
        {
            List<double> wl = _phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Select(p => p.X).ToList();
            List<double> counts = _phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Select(p => p.Y).ToList();
            Tuple<List<double>, List<double>> data = new Tuple<List<double>, List<double>>(wl, counts);
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += BwAnalyze;
            bw.RunWorkerCompleted += BwAnalyzeCompleted;
            bw.RunWorkerAsync(data);
        }

        private void BwAnalyze(object sender, DoWorkEventArgs e)
        {
            Tuple<List<double>, List<double>> data = (Tuple<List<double>, List<double>>)e.Argument;
            string res = ResultString((int)RamanAnalyzer.Analysis(data.Item1, data.Item2));
            e.Result = new Result(ResultStatus.SUCCESS, null, res);
        }

        private void BwAnalyzeCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (((Result)e.Result).Status == ResultStatus.SUCCESS)
            {
                AnalyzingResult = (string)((Result)e.Result).Value;
            }
            else
            {
                MessageBox.Show(((Result)e.Result).Message, "Error");
            }
        }

        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            //if(CamResList?.Count > 0)
            //{
            //    ShowListItemControl = true;
            //} else
            //{
            //    ShowListItemControl = false;
            //}
            //OnPropertyChanged("ShowListItemControl");

            {
                //if (PeakInfoList?.Count >= 2)
                //{
                //    DiamondResult = SpectrumAnalyzer.TestAggregate(PeakInfoList);
                //}
                //AnalyzingResult = ResultString((int)DiamondResult);
                //UpdateMarker();
                UpdateSummary();
            }
        }

        void SetFolder()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                    new System.Windows.Forms.FolderBrowserDialog();

            if (_saveFolderPath != null)
                folderBrowserDialog.SelectedPath = _saveFolderPath;

            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SaveFolderPath = folderBrowserDialog.SelectedPath;
                Properties.Settings.Default.SaveFolderPath = SaveFolderPath;
                Properties.Settings.Default.Save();
            }
        }

        void UpdateSummary()
        {
            View.PhosResultsWindow mw = (View.PhosResultsWindow)Application.Current.Windows.OfType<Window>().Last();
            for(int i = 0; i < MarkerList.Count; i++)
            {
                if (DisplayName.Equals("PhosResultsViewModelBatchMeasurement") && i == 0)
                {
                    continue;
                }
                Tuple<System.Windows.Shapes.Ellipse, TextBlock, Point> t = MarkerList[i];
                string savedEllipse = XamlWriter.Save(t.Item1);
                string savedText = XamlWriter.Save(t.Item2);
                System.Windows.Shapes.Ellipse ellipse = (System.Windows.Shapes.Ellipse)XamlReader.Load(XmlReader.Create(new StringReader(savedEllipse)));
                System.Windows.Controls.TextBlock txt = (TextBlock)XamlReader.Load(XmlReader.Create(new StringReader(savedText)));
                if (DisplayName.Equals("PhosResultsViewModelBatchMeasurement"))
                {
                    ellipse.Width *= 2;
                    ellipse.Height *= 2;
                    ellipse.Stroke = ColorLegendList[(int)DiamondResList[i - 1]];
                    txt.FontSize = 48;
                    txt.Foreground = ColorLegendList[(int)DiamondResList[i - 1]];
                } else
                {
                    ellipse.Stroke = ColorLegendList[(int)DiamondResult];
                }
                ellipse.SetValue(Canvas.LeftProperty, t.Item3.X - ellipse.Width / 2.0);
                ellipse.SetValue(Canvas.TopProperty, t.Item3.Y - ellipse.Height / 2.0);

                txt.SetValue(Canvas.LeftProperty, t.Item3.X - 15);
                txt.SetValue(Canvas.TopProperty, t.Item3.Y - 76);

                mw.CanvasSummary.Children.Add(ellipse);
                if (DisplayName.Equals("PhosResultsViewModelBatchMeasurement"))
                {
                    mw.CanvasSummary.Children.Add(txt);
                }
                if (!DisplayName.Equals("PhosResultsViewModelBatchMeasurement") && i == 0)
                {
                    break;
                }
            }

            if (!mappingMeasure)
            {
                double x = 50;
                double y = 50;
                for (int i = 0; i < ColorLegendList.Count; i++)
                {
                    DrawCanvas.Rect(x, y, 30, 30, ColorLegendList[i - 2], mw.CanvasSummary);
                    DrawCanvas.Text(x + 50, y, ResultString(i - 2), true, ColorLegendList[i - 2], mw.CanvasSummary, false);
                    y += 40;

                }
            } else
            {
                // rectangle mask
                double x = Canvas.GetLeft(RectMark);
                double y = Canvas.GetTop(RectMark);
                double w = RectMark.Width;
                double h = RectMark.Height;

                System.Windows.Shapes.Rectangle tmpRect = DrawCanvas.Rect(x, y, (int)w, (int)h, Brushes.Red, mw.CanvasSummary, 0.3);
            }
        }

        void UpdateMarker()
        {
            View.PhosResultsWindow mw = (View.PhosResultsWindow)Application.Current.Windows.OfType<Window>().Last();
            mw.CanvasResult.Children.Clear();

            Tuple<System.Windows.Shapes.Ellipse, TextBlock, Point> t = MarkerList[(int)CurrentListItem];
            string saved = XamlWriter.Save(t.Item1);
            System.Windows.Shapes.Ellipse ellipse = (System.Windows.Shapes.Ellipse)XamlReader.Load(XmlReader.Create(new StringReader(saved)));
            ellipse.Width *= CurrentListItem == 0 ? 1 : 2;
            ellipse.Height *= CurrentListItem == 0 ? 1 : 2;
            ellipse.SetValue(Canvas.LeftProperty, t.Item3.X - ellipse.Width / 2.0);
            ellipse.SetValue(Canvas.TopProperty, t.Item3.Y - ellipse.Height / 2.0);

            mw.CanvasResult.Children.Add(ellipse);
        }

        void PreviousListItem()
        {
            if (CurrentListItem > 1)
            {
                CurrentListItem--;

                _phosImages = CamResList[(int)CurrentListItem-1].Images;
                _phosFilteredImages = CamResList[(int)CurrentListItem-1].FilteredImages;
                _phosSpectra = SpecResList[(int)CurrentListItem - 1].Spectra;
                DiamondResult = DiamondResList[(int)CurrentListItem - 1];
                PeakInfoList = PeakInfoListList[(int)CurrentListItem - 1];

                NumSpectraItems = _phosSpectra.Count.ToString();
                SpectrumIntegrationTime = _phosSpectra[0].IntegrationTime.ToString();

                CurrentPhosItem = 1;
                CurrentSpectraItem = 1;
                _startTimeStamp = CamResList[(int)CurrentListItem - 1].StartTimeStamp;
                _spectrumStartTimeStamp = SpecResList[(int)CurrentListItem - 1].StartTimeStamp;

                //Analyzing();
                //if(PeakInfoList.Count >= 2)
                //{
                //    DiamondResult = SpectrumAnalyzer.TestAggregate(PeakInfoList);
                //}
                AnalyzingResult = ResultString((int)DiamondResult);
                UpdateMarker();
            }
        }

        void NextListItem()
        {
            if (CurrentListItem < CamResList.Count)
            {
                CurrentListItem++;

                _phosImages = CamResList[(int)CurrentListItem - 1].Images;
                _phosFilteredImages = CamResList[(int)CurrentListItem - 1].FilteredImages;
                _phosSpectra = SpecResList[(int)CurrentListItem - 1].Spectra;
                DiamondResult = DiamondResList[(int)CurrentListItem - 1];
                PeakInfoList = PeakInfoListList[(int)CurrentListItem - 1];

                NumSpectraItems = _phosSpectra.Count.ToString();
                SpectrumIntegrationTime = _phosSpectra[0].IntegrationTime.ToString();

                CurrentPhosItem = 1;
                CurrentSpectraItem = 1;
                _startTimeStamp = CamResList[(int)CurrentListItem - 1].StartTimeStamp;
                _spectrumStartTimeStamp = SpecResList[(int)CurrentListItem - 1].StartTimeStamp;

                //Analyzing();
                //if (PeakInfoList.Count >= 2)
                //{
                //    DiamondResult = SpectrumAnalyzer.TestAggregate(PeakInfoList);
                //}
                AnalyzingResult = ResultString((int)DiamondResult);
                UpdateMarker();
            }
        }

        void PreviousPhosItem()
        {
            if (CurrentPhosItem > 1)
                CurrentPhosItem--;
        }

        void NextPhosItem()
        {
            if (CurrentPhosItem < _phosImages.Count)
                CurrentPhosItem++;
        }

        void PreviousSpectraItem()
        {
            if (CurrentSpectraItem > 1)
            {
                CurrentSpectraItem--;
                //Analyzing();
            }
        }

        void NextSpectraItem()
        {
            if (CurrentSpectraItem < _phosSpectra.Count)
            {
                CurrentSpectraItem++;
                //Analyzing();
            }
        }

        class SaveArgs
        {
            public bool all;
            public FrameworkElement uiElement;
            public SaveArgs(bool _all, FrameworkElement _uiElement)
            {
                all = _all;
                uiElement = _uiElement;
            }
        }

        void Save(bool all = false)
        {
            _busy = true;
            View.PhosResultsWindow mw = (View.PhosResultsWindow)Application.Current.Windows.OfType<Window>().Last();
            Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));
            BackgroundWorker bw = new BackgroundWorker();
            bw.DoWork += SaveDoWork;
            bw.RunWorkerCompleted += SaveCompleted;
            bw.RunWorkerAsync(new SaveArgs(all, mw.TabSummary));
        }

        void SaveDoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = false;

            try
            {
                long timestamp = (long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string tempSaveFolderPath = SaveFolderPath + @"\" + timestamp.ToString();
                Directory.CreateDirectory(tempSaveFolderPath);

                string summaryFileName = tempSaveFolderPath + @"\" + "summary.bmp";

                Action a = () =>
                {
                    SaveUsingEncoder(summaryFileName, ((SaveArgs)(e.Argument)).uiElement, new PngBitmapEncoder());
                };

                App.Current.Dispatcher.Invoke(a);

                if (((SaveArgs)(e.Argument)).all == true)
                {
                    for (int m = 0; m < CamResList.Count; m++)
                    {
                        string dirRoot = tempSaveFolderPath + @"\" + m.ToString();
                        Directory.CreateDirectory(dirRoot);

                        var ts = DateTime.Now.ToString("MMddyyyy_HHmmss");
                        string imageFolderPath = dirRoot + @"\images_" + ts;
                        Directory.CreateDirectory(imageFolderPath);
                        string spectraFolderPath = dirRoot + @"\spectra_" + ts;
                        Directory.CreateDirectory(spectraFolderPath);
                        string markerFolderPath = dirRoot + @"\markers_" + ts;
                        Directory.CreateDirectory(markerFolderPath);

                        var _phosImages = CamResList[m].Images;

                        for (int i = 0; i < _phosImages.Count; i++)
                        {
                            var fileName = imageFolderPath + @"\phos_image" + "_" + i.ToString() +
                                "_" + _phosImages[i].TimeStamp + "ms.jpg";
                            var fileNameFiltered = imageFolderPath + @"\phos_image_filtered" + "_" + i.ToString() +
                                "_" + _phosImages[i].TimeStamp + "ms.jpg";

                            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(_phosImages[i].Image))
                            {
                                if (Path.GetExtension(fileName).ToUpper().Contains("JPG"))
                                {
                                    bmp.Save(fileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                                }
                                else
                                {
                                    bmp.Save(fileName);
                                }
                            }
                            if (_phosFilteredImages?.Count > 0)
                            {
                                using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(_phosFilteredImages[i]))
                                {
                                    if (Path.GetExtension(fileNameFiltered).ToUpper().Contains("JPG"))
                                    {
                                        bmp.Save(fileNameFiltered, System.Drawing.Imaging.ImageFormat.Jpeg);
                                    }
                                    else
                                    {
                                        bmp.Save(fileNameFiltered);
                                    }
                                }
                            }
                        }

                        var _phosSpectra = SpecResList[m].Spectra;
                        for (int i = 0; i < _phosSpectra.Count; i++)
                        {
                            var spectraFileName = spectraFolderPath + @"\phos_spectrum" + "_" + i.ToString() +
                            "_" + _phosSpectra[i].IntegrationTime.ToString() + "ms.spc";
                            double[] wl = _phosSpectra[i].Spectrum.Select(p => p.X).ToArray();
                            double[] counts = _phosSpectra[i].Spectrum.Select(p => p.Y).ToArray();

                            if (!SPCHelper.SaveToSPC(wl, counts, spectraFileName, "Wavelength (nm)", "Intensity"))
                                throw new Exception("spectra save fail");
                        }

                        // save markers and image
                        var markerFileName = markerFolderPath + @"\pre_image" + "_" + CamResultsPre[0].TimeStamp.ToString() + "ms.jpg";
                        using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(CamResultsPre[0].Image))
                        {
                            if (Path.GetExtension(markerFileName).ToUpper().Contains("JPG"))
                            {
                                bmp.Save(markerFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            else
                            {
                                bmp.Save(markerFileName);
                            }
                        }
                        var markerListName = markerFolderPath + @"\markers" + ".txt";
                        TextWriter tw = new StreamWriter(markerListName);
                        //for (int i = 0; i < MarkerList.Count; i++)
                        //{
                        tw.WriteLine(MarkerList[m+1].Item3.ToString());
                        //}
                        tw.Close();
                    }
                    _dataSaved = true;
                }
                else
                {
                    var ts = DateTime.Now.ToString("MMddyyyy_HHmmss");
                    string imageFolderPath = tempSaveFolderPath + @"\images_" + ts;
                    Directory.CreateDirectory(imageFolderPath);
                    string spectraFolderPath = tempSaveFolderPath + @"\spectra_" + ts;
                    Directory.CreateDirectory(spectraFolderPath);
                    string markerFolderPath = tempSaveFolderPath + @"\markers_" + ts;
                    Directory.CreateDirectory(markerFolderPath);

                    if (_phosImages.Count > 0)
                    {
                        var imgFileName = imageFolderPath + @"\phos_image" + _phosImages[(int)(CurrentPhosItem - 1)].FrameId +
                                "_" + (Math.Round((_phosImages[(int)(CurrentPhosItem - 1)].TimeStamp - _startTimeStamp) / 1000000d, 0)) + "ms.jpg";
                        using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(_phosImages[(int)(CurrentPhosItem - 1)].Image))
                        {
                            if (Path.GetExtension(imgFileName).ToUpper().Contains("JPG"))
                            {
                                bmp.Save(imgFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                            }
                            else
                            {
                                bmp.Save(imgFileName);
                            }
                        }
                    }

                    if (_phosSpectra.Count > 0)
                    {
                        var spectraFileName = spectraFolderPath + @"\phos_spectrum" +
                            "_" + _phosSpectra[(int)(CurrentSpectraItem - 1)].IntegrationTime.ToString() + "ms.spc";
                        double[] wl = _phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Select(p => p.X).ToArray();
                        double[] counts = _phosSpectra[(int)(CurrentSpectraItem - 1)].Spectrum.Select(p => p.Y).ToArray();

                        if (!SPCHelper.SaveToSPC(wl, counts, spectraFileName, "Wavelength (nm)", "Intensity"))
                            throw new Exception("spectra save fail");
                    }

                    // save markers and image
                    var markerFileName = markerFolderPath + @"\pre_image" + "_" + CamResultsPre[0].TimeStamp.ToString() + "ms.jpg";
                    using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(CamResultsPre[0].Image))
                    {
                        if (Path.GetExtension(markerFileName).ToUpper().Contains("JPG"))
                        {
                            bmp.Save(markerFileName, System.Drawing.Imaging.ImageFormat.Jpeg);
                        }
                        else
                        {
                            bmp.Save(markerFileName);
                        }
                    }
                    var markerListName = markerFolderPath + @"\markers" + ".txt";
                    TextWriter tw = new StreamWriter(markerListName);
                    tw.WriteLine(MarkerList[(int)CurrentListItem].Item3.ToString());
                    tw.Close();
                }

                e.Result = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("phosresultvm exception: " + ex.Message);
                e.Result = false;
            }
            finally
            {

            }

        }

        void SaveCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            string message = "Not saved";

            if ((bool)e.Result == true)
            {
                message = "Saved";
            }

            MessageBox.Show(message, "Complete");

            _busy = false;
            Application.Current.Dispatcher.BeginInvoke(new Action(System.Windows.Input.CommandManager.InvalidateRequerySuggested));
        }

        BitmapSource BitmapToBitmapSource(System.Drawing.Bitmap bitmap)
        {
            var bitmapData = bitmap.LockBits(
                new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                System.Drawing.Imaging.ImageLockMode.ReadOnly, bitmap.PixelFormat);

            var bitmapSource = System.Windows.Media.Imaging.BitmapSource.Create(
                bitmapData.Width, bitmapData.Height, bitmap.HorizontalResolution, bitmap.VerticalResolution,
                System.Windows.Media.PixelFormats.Bgr32, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);
            return bitmapSource;
        }


        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!_dataSaved)
            {
                var res = MessageBox.Show("All the data has not been saved.  Do you want to close anyway?",
                    "Data has not been saved", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (res != MessageBoxResult.Yes)
                {
                    e.Cancel = true;//prevent closing
                }
            }
        }

        public void SaveUsingEncoder(string fileName, FrameworkElement UIElement, BitmapEncoder encoder)
        {
            int height = (int)UIElement.ActualHeight;
            int width = (int)UIElement.ActualWidth;
            UIElement.Measure(new System.Windows.Size(width, height));
            UIElement.Arrange(new Rect(0, 0, width, height));
            RenderTargetBitmap bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(UIElement);
            SaveUsingBitmapTargetRenderer(fileName, bitmap, encoder);
        }

        private void SaveUsingBitmapTargetRenderer(string fileName, RenderTargetBitmap renderTargetBitmap, BitmapEncoder bitmapEncoder)
        {
            BitmapFrame frame = BitmapFrame.Create(renderTargetBitmap);
            bitmapEncoder.Frames.Add(frame);
            using (var stream = File.Create(fileName))
            {
                bitmapEncoder.Save(stream);
            }
        }
    }
}
