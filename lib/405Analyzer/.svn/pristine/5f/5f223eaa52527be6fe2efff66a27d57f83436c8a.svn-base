#region copyright
/*************************************************************************
 * 
 * GEMOLOGICAL INSTITUTE OF AMERICA
 * __________________
 * 
 *  � Gemological Insitute Of America (GIA) 2018
 *  All Rights Reserved.
 * 
 * NOTICE:  All information contained herein is, and remains the property of GIA and its 
 * suppliers, if any.  The intellectual and technical concepts contained herein are 
 * proprietary to GIA and its suppliers and may be covered by U.S. and Foreign Patents,
 * patents in process, and are protected by trade secret or copyright law.
 * Dissemination of this information or reproduction of this material is strictly forbidden 
 * unless prior written permission is obtained from GIA.
 *************************************************************************/
#endregion



using PeakFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _405Analyzer
{
    public enum ANALYZER_RESULT
    {
        ERROR_SPIKE = -2,
        ERROR = -1,
        NATURAL_DIAMOND = 0,
        NON_DIAMOND,
        HPHT_SYNTHETIC_DIAMOND,
        REFER,
        SATURATED,
        NONE,
        CVD_SYNTHETIC_DIAMOND
    }

    

    public class PeakDetectInfo
    {
        public PeakDetectInfo(double sp)
        {
            StartPosition = sp;
            IsDetected = IsSaturated = IsFlagged = false;
            SmoothPeaks = null;
            SmoothSpectrumData = null;
        }

        public double StartPosition { get; set; }
        public bool IsSaturated { get; set; }
        public bool IsDetected { get; set; }
        public bool IsFlagged { get; set; }
        public bool IsFlaggedSat { get; set; }
        public List<Point> SmoothSpectrumData { get; set; }
        public List<Peak> SmoothPeaks { get; set; }

    }

    public class DebugInfo
    {
        public bool IsDetectedStraightLine { get; set; }
        public bool IsDetectedPeak { get; set; }
        public bool IsDetectedN3Side2 { get; set; }

    }

    public class SpectrumAnalyzer
    {
        static AppSettings _analysisSettings = new AppSettings();


        class TwoListPeak
        {
            public List<Point> List1 { get; set; }
            public List<Peak> List2 { get; set; }
        }

        List<PeakDetectInfo> _peakDetectInfo = new List<PeakDetectInfo>();
        Spectrum _spectrum;
        
        public Spectrum SpectrumData { get { return _spectrum; } }

        void SetDebugPeakInfo(double sp, bool? det, bool? sat, bool? flag, List<Peak> smooth, List<Point> data, bool? flagSat)
        {
            try
            {
                var pdi = _peakDetectInfo.Where(p => p.StartPosition == sp).FirstOrDefault();
                if (det != null)
                    pdi.IsDetected = (bool)det;
                if (sat != null)
                    pdi.IsSaturated = (bool)sat;
                if (flag != null)
                    pdi.IsFlagged = (bool)flag;
                if (smooth != null)
                    pdi.SmoothPeaks = smooth.ToList();
                if (data != null)
                    pdi.SmoothSpectrumData = data.ToList();
                if (flagSat != null)
                    pdi.IsFlaggedSat = (bool)flagSat;
            }
            catch (Exception ex)
            { }

        }

        
        public SpectrumAnalyzer(List<double> wavlengths, List<double> counts)
        {
            _spectrum = new Spectrum(wavlengths.Zip(counts, (w, c) => new Point(w, c)).ToList());
            if (!_analysisSettings.Load())
                throw new Exception("Could not load analysis settings");

            //n3,diamond,466,490,525,737,788,882
            var positions = new List<double> {
                _analysisSettings.N3_START_WAVELEN,
                _analysisSettings.DIAMOND_START_WAVELEN,
                _analysisSettings._468_START_WAVELEN,
                _analysisSettings._490_START_WAVELEN,
                _analysisSettings._525_START_WAVELEN,
                _analysisSettings.SIV_START_WAVELEN,
                _analysisSettings._788_START_WAVELEN,
                _analysisSettings.NI_START_WAVELEN

            };

            foreach (var pos in positions)
            {
                _peakDetectInfo.Add(new PeakDetectInfo(pos));
            }
        }

        public double GetDiamondRamanPeakCount()
        {
            double count = 0;
            try
            {
                //var diamondYs = _spectrum.SpectrumData.Where(d => d.X >= _analysisSettings.DIAMOND_START_WAVELEN &&
                //    d.X <= _analysisSettings.DIAMOND_END_WAVELEN).Select(d => d.Y).ToList();
                //diamondYs.Sort();
                //count = diamondYs.Last();

                bool diamondSaturated = _spectrum.IsSaturated(_analysisSettings.DIAMOND_START_WAVELEN, 
                    _analysisSettings.DIAMOND_END_WAVELEN);
                if (diamondSaturated)
                    return -1;

                var allPeaksList = _spectrum.FindPeaksByGradient(4);
                var dpks = allPeaksList.Where(p => Math.Round(p.Width, 1) > 1 &&
                            Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.DIAMOND_MAX_FWHM
                            && Math.Round(p.Height) >= _analysisSettings.DIAMOND_MIN_HEIGHT && 
                            p.Top.X >= _analysisSettings.DIAMOND_START_WAVELEN
                            && p.Top.X <= _analysisSettings.DIAMOND_END_WAVELEN).OrderBy(p => p.Height).ToList();

                return dpks.Last().Top.Y;
            }
            catch
            {
                count = -1;
            }

            return count;
        }

        public ANALYZER_RESULT Test()
        {
            List<Peak> allPeaks;
            List<Peak> sigPeaks;
            List<PeakDetectInfo> debugPeaks;
            DebugInfo cz;

            return Test(out allPeaks, out sigPeaks, out debugPeaks, out cz);
        }

        public ANALYZER_RESULT Test(out List<PeakDetectInfo> debugPeaks)
        {
            List<Peak> allPeaks;
            List<Peak> sigPeaks;
            DebugInfo cz;

            return Test(out allPeaks, out sigPeaks, out debugPeaks, out cz);
        }

        public ANALYZER_RESULT Test(out List<PeakDetectInfo> debugPeaks, out DebugInfo cz)
        {
            List<Peak> allPeaks;
            List<Peak> sigPeaks;

            return Test(out allPeaks, out sigPeaks, out debugPeaks, out cz);
        }


        public ANALYZER_RESULT Test(out List<Peak> allPeaksList, out List<Peak> significantPeaksList,
            out List<PeakDetectInfo> debugPeaks, out DebugInfo debugInfo)
        {
            ANALYZER_RESULT result = ANALYZER_RESULT.REFER;
            allPeaksList = new List<Peak>();
            significantPeaksList = new List<Peak>();
            debugPeaks = _peakDetectInfo;
            debugInfo = new DebugInfo();
            

            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();

            try
            {
                bool n3Saturated = _spectrum.IsSaturated(_analysisSettings.N3_START_WAVELEN, _analysisSettings.N3_END_WAVELEN);
                bool n3Side2Saturated = _spectrum.IsSaturated(_analysisSettings.N3_SIDE2_START_WAVELEN, _analysisSettings.N3_SIDE2_END_WAVELEN);
                bool _468Saturated = _spectrum.IsSaturated(_analysisSettings._468_START_WAVELEN, _analysisSettings._468_END_WAVELEN);
                bool siVSaturated = _spectrum.IsSaturated(_analysisSettings.SIV_START_WAVELEN, _analysisSettings.SIV_END_WAVELEN);
                bool niSaturated = _spectrum.IsSaturated(_analysisSettings.NI_START_WAVELEN, _analysisSettings.NI_END_WAVELEN);
                bool diamondSaturated = _spectrum.IsSaturated(_analysisSettings.DIAMOND_START_WAVELEN, _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN);

                #region first_set_of_parallels

                //Task<List<Peak>> taskAllFilterPeaks = Task<List<Peak>>.Factory.StartNew(() =>
                //{
                //    var filteredSpectrum = new Spectrum();
                //    filteredSpectrum = _spectrum.FilteredSpectrum(3);
                //    return filteredSpectrum.FindPeaksByGradient(4).
                //            Where(p => p.Height > 0 && p.Width > 0).ToList();
                //});


                //smooth spectrum async (n3 + diamond)
                Task<TwoListPeak> taskN3SmoothOutlierPeaks = Task<TwoListPeak>.Factory.StartNew(() =>
                {
                    var smoothSpectrum = new Spectrum();
                    List<Peak> smoothN3Peaks = new List<Peak>();
                    List<Peak> smoothDiamondPeaks = new List<Peak>();
                    TwoListPeak returnVal = new TwoListPeak();
                    for (int i = 1; i < 5; i++)
                    {
                        smoothSpectrum = _spectrum.SmoothedSpectrum(3, i);
                        var smoothPeaksList = smoothSpectrum.FindPeaksByGradient
                                (_analysisSettings.N3_START_WAVELEN - 3,
                            _analysisSettings.N3_END_WAVELEN + 3).
                            Where(p => p.Height > 0 && p.Width > 0);
                        var smoothPeaksCount = smoothPeaksList.Count();

                        if ((smoothPeaksCount <= 1) || (i == 4))
                        {
                            if (smoothPeaksList.Count(p => p.Height > 50) == 1)
                                smoothN3Peaks = smoothSpectrum.FindPeaksByGradient(4).
                                    Where(p => p.Height > 0 && p.Width > 0).ToList();
                            else
                                smoothN3Peaks = new List<Peak>();
                            break;
                        }

                    }
                    returnVal.List1 = smoothSpectrum.SpectrumData;

                    for (int i = 1; i < 5; i++)
                    {
                        smoothSpectrum = _spectrum.SmoothedSpectrum(3, i);
                        var smoothPeaksList = smoothSpectrum.FindPeaksByGradient
                                (_analysisSettings.DIAMOND_START_WAVELEN - 7,
                            _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN).
                            Where(p => p.Height > 0 && p.Width > 0);

                        var diamondList = smoothPeaksList.Where(p => ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH,
                                                p.Top.X) > 1282 && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH,
                                                p.Top.X) < 1382 && p.Height >= 1).ToList();
                        var n3SideBandCnt = smoothPeaksList.Count(p => p.Top.X > _analysisSettings.DIAMOND_START_WAVELEN 
                                            && p.Top.X < _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN &&
                                                p.Width >= 1 && p.Height >= 1);
                        if ( (diamondList.Count(p => p.Width >= 1) == 1) 
                                || (n3SideBandCnt >= 1) )
                        {
                            smoothDiamondPeaks = smoothSpectrum.FindPeaksByGradient(4).
                                    Where(p => p.Height > 0 && p.Width > 0).ToList();                                
                            break;
                        }
                        if (i == 4)
                            smoothDiamondPeaks = new List<Peak>();

                    }
                    var smoothPeaks = smoothN3Peaks.ToList();
                    if (smoothDiamondPeaks.Count() > smoothPeaks.Count())
                    {
                        smoothPeaks = smoothDiamondPeaks.
                            Where(p => p.Top.X < _analysisSettings.N3_START_WAVELEN-3 ||
                                        p.Top.X > _analysisSettings.N3_END_WAVELEN+3).ToList();
                        smoothPeaks.AddRange(smoothN3Peaks.
                            Where(p => p.Top.X >= _analysisSettings.N3_START_WAVELEN - 3 &&
                                    p.Top.X <= _analysisSettings.N3_END_WAVELEN + 3));
                        smoothPeaks = smoothPeaks.OrderBy(p => p.Top.X).ToList();
                    }
                    else
                    {
                        smoothPeaks = smoothN3Peaks.
                            Where(p => p.Top.X < _analysisSettings.DIAMOND_START_WAVELEN - 7 ||
                                        p.Top.X > _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN).ToList();
                        smoothPeaks.AddRange(smoothDiamondPeaks.
                            Where(p => p.Top.X >= _analysisSettings.DIAMOND_START_WAVELEN - 7 &&
                                    p.Top.X <= _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN));
                        smoothPeaks = smoothPeaks.OrderBy(p => p.Top.X).ToList();
                    }
                    
                    SetDebugPeakInfo(_analysisSettings.N3_START_WAVELEN, null, null, null, smoothPeaks, smoothSpectrum.SpectrumData.ToList(), null);
                    SetDebugPeakInfo(_analysisSettings.DIAMOND_START_WAVELEN, null, null, null, smoothPeaks, smoothSpectrum.SpectrumData.ToList(), null);

                    var stats = new PeakStats(smoothPeaks.Select(p => p.Height).ToList());
                    var outliers = stats.OutliersUsingMAD(7.5);
                    outliers.Sort();
                    var smoothHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                    returnVal.List2 = smoothPeaks.Where(p => ((p.Top.X >= _analysisSettings.N3_START_WAVELEN &&
                            p.Top.X <= _analysisSettings.N3_END_WAVELEN) ||
                            (p.Top.X >= _analysisSettings.DIAMOND_START_WAVELEN &&
                            p.Top.X <= _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN)) &&
                        Math.Round(p.Height, 1) >= smoothHeightThreshold).ToList();

                    return returnVal;
                });

                //smooth spectrum async (788)
                Task<List<Peak>> task788SmoothOutlierPeaks = Task<List<Peak>>.Factory.StartNew(() =>
                {
                    var smooth788Spectrum = new Spectrum();

                    List<Peak> smoothPeaks = new List<Peak>();

                    for (int i = 1; i < 15; i++)
                    {
                        smooth788Spectrum = _spectrum.SmoothedSpectrum(3, i);
                        var smooth788PeaksCount = smooth788Spectrum.FindPeaksByGradient
                                (_analysisSettings._788_START_WAVELEN_SMOOTH,
                            _analysisSettings._788_END_WAVELEN_SMOOTH).
                            Where(p => p.Height > 0 && p.Width > 0).Count();

                        if ((smooth788PeaksCount <= 1) || i == 14)
                        {
                            smoothPeaks = smooth788Spectrum.FindPeaksByGradient(4).
                                Where(p => p.Height > 0 && p.Width > 0).ToList();
                            break;
                        }
                    }
                    SetDebugPeakInfo(_analysisSettings._788_START_WAVELEN, null, null, null, smoothPeaks, smooth788Spectrum.SpectrumData.ToList(), null);

                    var peakStats = new PeakStats(smoothPeaks.Select(p => p.Height).ToList());
                    var peakOutliers = peakStats.OutliersUsingMAD(5);
                    peakOutliers.Sort();
                    var threshold = peakOutliers.Count > 0 ? Math.Round(peakOutliers[0], 1) : double.MaxValue;
                    peakStats = new PeakStats(smoothPeaks.Select(p => p.Width).ToList());
                    peakOutliers = peakStats.OutliersUsingMAD(3);
                    peakOutliers.Sort();
                    var widthThresh = peakOutliers.Count > 0 ? Math.Round(peakOutliers[0], 1) : double.MaxValue;

                    return smoothPeaks.Where(p => p.Top.X >= _analysisSettings._788_START_WAVELEN_SMOOTH &&
                        p.Top.X <= _analysisSettings._788_END_WAVELEN_SMOOTH &&
                        //peakStats.Percentile(p.Width) >= 0.85 &&
                        Math.Round(p.Width,1) >= widthThresh &&
                        Math.Round(p.Height, 1) >= threshold).ToList();
                });

                //smooth spectrum async (strntium titanite )
                Task<bool> taskStrTitan = Task<bool>.Factory.StartNew(() =>
                {
                    var smoothSpectrum = new Spectrum();
                    List<Peak> smoothPeaks = new List<Peak>();
                    for (int i = 1; i < 15; i++)
                    {
                        smoothSpectrum = _spectrum.SmoothedSpectrum(3, i);
                        var smoothPeaksCount = smoothSpectrum.FindPeaksByGradient
                                (_analysisSettings.N3_START_WAVELEN - 6,
                            _analysisSettings.DIAMOND_END_WAVELEN + 10).
                            Where(p => p.Height > 0 && p.Width > 0).Count();

                        if ((smoothPeaksCount <= 4) || (i == 14))
                        {
                            if (smoothPeaksCount <= 4)
                                smoothPeaks = smoothSpectrum.FindPeaksByGradient(4).
                                    Where(p => p.Height > 0 && p.Width > 0).ToList();
                            else
                                smoothPeaks = new List<Peak>();
                            break;
                        }

                    }
                    
                    var stats = new PeakStats(smoothPeaks.Select(p => p.Height).ToList());
                    var outliers = stats.OutliersUsingMAD(3);
                    outliers.Sort();
                    var smoothHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                    var list1 = smoothPeaks.Where(p => Math.Round(p.Width) > 1 && Math.Round(p.Height) >= smoothHeightThreshold &&
                        p.Top.X >= _analysisSettings.N3_START_WAVELEN - 6 &&
                        p.Top.X <= _analysisSettings.N3_END_WAVELEN + 1).ToList();
                    var list2 = smoothPeaks.Where(p => Math.Round(p.Width) > 1 && Math.Round(p.Height) >= smoothHeightThreshold &&
                        p.Top.X >= _analysisSettings.DIAMOND_END_WAVELEN + 1 &&
                        p.Top.X <= _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN + 2).ToList();
                    return (list1.Count() == 2) &&
                        (list2.Count() == 1);
                });

                //smooth spectrum async (490)
                Task<List<Peak>> task490SmoothOutlierPeaks = Task<List<Peak>>.Factory.StartNew(() =>
                {
                    var smooth490Spectrum = new Spectrum();

                    List<Peak> smoothPeaks = new List<Peak>();

                    for (int i = 1; i < 15; i++)
                    {
                        smooth490Spectrum = _spectrum.SmoothedSpectrum(3, i);
                        var smooth490PeaksCount = smooth490Spectrum.FindPeaksByGradient
                                (_analysisSettings._490_START_WAVELEN - 5,
                            _analysisSettings._490_END_WAVELEN + 5).
                            Where(p => p.Height > 0 && p.Width > 0).Count();

                        if ((smooth490PeaksCount <= 1) || i == 14)
                        {
                            smoothPeaks = smooth490Spectrum.FindPeaksByGradient(4).
                                Where(p => p.Height > 0 && p.Width > 0).ToList();
                            break;
                        }
                    }
                    SetDebugPeakInfo(_analysisSettings._490_START_WAVELEN, null, null, null, smoothPeaks, smooth490Spectrum.SpectrumData.ToList(),null);

                    var peakStats = new PeakStats(smoothPeaks.Select(p => p.Height).ToList());
                    var peakOutliers = peakStats.OutliersUsingMAD(5);
                    peakOutliers.Sort();
                    var threshold = peakOutliers.Count > 0 ? Math.Round(peakOutliers[0], 1) : double.MaxValue;
                    if (threshold < 100)
                        threshold = 100;
                    peakStats = new PeakStats(smoothPeaks.Select(p => p.Width).ToList());
                    peakOutliers = peakStats.OutliersUsingMAD(3);
                    peakOutliers.Sort();
                    var widthThresh = peakOutliers.Count > 0 ? Math.Round(peakOutliers[0], 1) : double.MaxValue;

                    return smoothPeaks.Where(p => p.Top.X >= _analysisSettings._490_START_WAVELEN - 5 &&
                        p.Top.X <= _analysisSettings._490_END_WAVELEN + 5 &&
                        Math.Round(p.Width, 1) >= widthThresh &&
                        Math.Round(p.Height, 1) >= threshold).ToList();
                });


                //smooth spectrum async (n3 sideband 2 )
                Task<bool> taskN3Side2 = Task<bool>.Factory.StartNew(() =>
                {
                    var smoothSpectrum = new Spectrum();
                    List<Peak> smoothPeaks = new List<Peak>();
                    for (int i = 1; i < 5; i++)
                    {
                        smoothSpectrum = _spectrum.SmoothedSpectrum(3, i);
                        var smoothPeaksList = smoothSpectrum.FindPeaksByGradient
                                (_analysisSettings.N3_SIDE2_START_WAVELEN - 2,
                            _analysisSettings.N3_SIDE2_END_WAVELEN + 2).
                            Where(p => p.Height > 0 && p.Width > 0).ToList();
                        var smoothPeaksCount = smoothPeaksList.Count();

                        if ((smoothPeaksCount <= 1) || (i == 4))
                        {
                            if (smoothPeaksCount <= 1)
                                smoothPeaks = smoothSpectrum.FindPeaksByGradient(4).
                                    Where(p => p.Height > 0 && p.Width > 0).ToList();
                            else
                                smoothPeaks = new List<Peak>();
                            break;
                        }

                    }

                    var stats = new PeakStats(smoothPeaks.Select(p => p.Height).ToList());
                    var outliers = stats.OutliersUsingMAD(7.5);
                    outliers.Sort();
                    var smoothHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;
                    if (smoothHeightThreshold < _analysisSettings.N3_SIDE2_MIN_HEIGHT)
                        smoothHeightThreshold = _analysisSettings.N3_SIDE2_MIN_HEIGHT;

                    var list1 = smoothPeaks.Where(p => Math.Round(p.FullWidthHalfMax,1) >= 1 && 
                        Math.Round(p.Height, 1) >= smoothHeightThreshold &&
                        (Math.Round(p.FullWidthHalfMax, 1) <= 1.1 * _analysisSettings.DIAMOND_MAX_FWHM) &&
                        p.Top.X >= _analysisSettings.N3_SIDE2_START_WAVELEN - 0.5 &&
                        p.Top.X <= _analysisSettings.N3_SIDE2_END_WAVELEN + 0.5).ToList();

                    //if it is a real smooth peak, all points on the unsmoothed spectrum should be higher than the base

                    return (list1.Count() == 1);
                });

                #endregion

                #region raw_spectra_analysis
                allPeaksList = _spectrum.FindPeaksByGradient(4).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                System.Diagnostics.Debug.WriteLine("Find all peaks " + stopwatch.ElapsedMilliseconds + " ms" + ", " + allPeaksList.Count);

                //find outliers
                var rawStats = new PeakStats(allPeaksList.Select(p => p.Height).ToList());
                var rawOutliers = rawStats.OutliersUsingMAD(5.0);
                rawOutliers.Sort();
                var heightThreshold = rawOutliers.Count > 0 ? Math.Round(rawOutliers[0], 1) : double.MaxValue;
                rawOutliers = rawStats.OutliersUsingMAD(15);
                rawOutliers.Sort();
                var bigHeightThreshold = rawOutliers.Count > 0 ? Math.Round(rawOutliers[0], 1) : double.MaxValue;
                rawStats = new PeakStats(allPeaksList.Select(p => p.Width).ToList());
                rawOutliers = rawStats.OutliersUsingMAD(5.0);
                rawOutliers.Sort();
                var widthThreshold = rawOutliers.Count > 0 ? Math.Round(rawOutliers[0], 1) : double.MaxValue;
                rawOutliers = rawStats.OutliersUsingMAD(30);
                rawOutliers.Sort();
                var bigWidthThreshold = rawOutliers.Count > 0 ? Math.Round(rawOutliers[0], 1) : double.MaxValue;

                var temp = allPeaksList.Where(p => Math.Round(p.Width, 1) >= widthThreshold &&
                    Math.Round(p.Height, 1) >= heightThreshold).ToList();

                significantPeaksList.AddRange(temp);
                #endregion

                #region check_spikes
                /*
                //filter spectrum async
                var allFilterPeaks = taskAllFilterPeaks.Result;
                System.Diagnostics.Debug.WriteLine("Find all filtered peaks " + stopwatch.ElapsedMilliseconds + " ms" + ", " + allFilterPeaks.Count);
                var filterStats = new PeakStats(allFilterPeaks.Select(p => p.Height).ToList());
                var filterOutliers = filterStats.OutliersUsingMAD(3);
                filterOutliers.Sort();
                var filterHeightThreshold = filterOutliers.Count > 0 ? Math.Round(filterOutliers[0], 1) : double.MaxValue;

                var bigPeaks = allPeaksList.Where(p => Math.Round(p.Height, 1) >= bigHeightThreshold).ToList();
                var bigFilterPeaks = allFilterPeaks.Where(p => Math.Round(p.Height, 1) >= filterHeightThreshold).ToList();

                //check for spikes
                //every big peak must exist in filtered peak
                if (bigPeaks.Count > 0 && bigFilterPeaks.Count == 0)
                    return ANALYZER_RESULT.ERROR_SPIKE;

                foreach (var pk in bigPeaks)
                {
                    if ((pk.Top.X >= _analysisSettings.N3_START_WAVELEN && pk.Top.X <= _analysisSettings.N3_END_WAVELEN) ||
                        (pk.Top.X >= _analysisSettings.DIAMOND_START_WAVELEN && pk.Top.X <= _analysisSettings.DIAMOND_END_WAVELEN)
                        )
                    {
                        if (bigFilterPeaks.Where(p => Math.Abs(p.Top.X - pk.Top.X) <= 1).ToList().Count == 0)
                        {
                            return ANALYZER_RESULT.ERROR_SPIKE;
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine("Check spikes: " + stopwatch.ElapsedMilliseconds + " ms");
                */
                #endregion
                

                var significantN3_788SmoothPeaks = new List<Peak>();
                significantN3_788SmoothPeaks.AddRange(taskN3SmoothOutlierPeaks.Result.List2);
                significantN3_788SmoothPeaks.AddRange(task788SmoothOutlierPeaks.Result);

                System.Diagnostics.Debug.WriteLine("Find significant n3 smooth peaks: " + stopwatch.ElapsedMilliseconds + " ms");

                #region n3_diamond_788
                bool n3Detected = false;
                bool possibleCzDetected = false;
                bool strTitanDetected = taskStrTitan.Result;
                bool moissaniteDetected = false;
                if (n3Saturated == false)
                {
                    var n3Peaks = significantPeaksList.Where(p => Math.Round(p.Width, 1) >= 1.8 &&
                                    Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.N3_MAX_FWHM && 
                                    Math.Round(p.Height) >= _analysisSettings.N3_MIN_HEIGHT &&
                                    p.Top.X >= _analysisSettings.N3_START_WAVELEN
                                    && p.Top.X <= _analysisSettings.N3_END_WAVELEN).ToList();
                    var n3SmoothPeaks = significantN3_788SmoothPeaks.Where(p => Math.Round(p.Width, 1) >= 1 &&
                                    Math.Round(p.FullWidthHalfMax, 1) <= 1.2 * _analysisSettings.N3_MAX_FWHM &&
                                    Math.Round(p.Height) >= 0.8*_analysisSettings.N3_MIN_HEIGHT && 
                                    p.Top.X >= _analysisSettings.N3_START_WAVELEN
                                    && p.Top.X <= _analysisSettings.N3_END_WAVELEN).ToList();
                    bool n3Sideband2Detected = taskN3Side2.Result;
                    debugInfo.IsDetectedN3Side2 = n3Sideband2Detected;
                    n3Detected = n3SmoothPeaks.Count() > 0;


                    if (n3Detected)
                    {
                        //check for noisy n3 peak indicative of posible cz
                        var n3SmoothRegion = taskN3SmoothOutlierPeaks.Result.List1.Where(
                            p => p.X >= _analysisSettings.N3_START_WAVELEN - 5
                                    && p.X <= _analysisSettings.N3_END_WAVELEN + 5).ToList();
                        if (CheckN3Peak(n3SmoothPeaks.First(), n3SmoothRegion, debugInfo) == false)
                        {
                            //cz
                            var sigPeaksList = significantPeaksList.Where(p => p.Top.X >= 408).ToList();//ignore laser peaks
                            var ct = sigPeaksList.Where(p => Math.Round(p.Width, 1) >= 1.8 &&
                                        Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.CZ_MAX_FWHM &&
                                        Math.Round(p.Height) >= _analysisSettings.CZ_MIN_HEIGHT &&
                                        (ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) > 550
                                        && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) < 665)).ToList();
                            possibleCzDetected = (ct.Count() >= 1) &&
                                (ct.Max(p => p.Height) == sigPeaksList.Max(p => p.Height));

                            n3Detected = n3Sideband2Detected && !possibleCzDetected;
                        }
                    }
                    else
                    {
                        //cz
                        var sigPeaksList = significantPeaksList.Where(p => p.Top.X >= 408).ToList();//ignore laser peaks
                        var ct = sigPeaksList.Where(p => Math.Round(p.Width, 1) >= 1.8 &&
                                    Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.CZ_MAX_FWHM &&
                                    Math.Round(p.Height) >= _analysisSettings.CZ_MIN_HEIGHT &&
                                    (ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) > 550
                                    && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) < 665)).ToList();
                        possibleCzDetected = (ct.Count() >= 1) &&
                            (ct.Max(p => p.Height) == sigPeaksList.Max(p => p.Height));

                        n3Detected = n3Sideband2Detected && !possibleCzDetected;
                    }

                    if (!n3Detected)
                    {
                        //check for 3 significant peaks in an expanded region
                        var n3SigPeaks = significantPeaksList.Where(p => Math.Round(p.Width, 1) >= 1.8 &&
                                    Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.N3_MAX_FWHM &&
                                    Math.Round(p.Height) >= 100 &&
                                    p.Top.X >= _analysisSettings.N3_START_WAVELEN - 1
                                    && p.Top.X <= _analysisSettings.N3_END_WAVELEN + 1).ToList();
                        var n3Side2SigPeaks = significantPeaksList.Where(p => Math.Round(p.Width) > 1 &&
                            Math.Round(p.Height, 1) >= 100 &&
                            (Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.N3_MAX_FWHM) &&
                            p.Top.X >= _analysisSettings.N3_SIDE2_START_WAVELEN - 1 &&
                            p.Top.X <= _analysisSettings.N3_SIDE2_END_WAVELEN + 1).ToList();
                        var diamondSigPeaks = significantPeaksList.Where(p => Math.Round(p.Width, 1) > 1.7 &&
                            Math.Round(p.Height) >= 100
                            && p.Top.X >= _analysisSettings.DIAMOND_START_WAVELEN - 1
                            && p.Top.X <= _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN + 1).ToList();

                        if (n3SigPeaks.Count == 1 && n3Side2SigPeaks.Count == 1 && diamondSigPeaks.Count == 1 &&
                            (n3SigPeaks[0].Top.Y < diamondSigPeaks[0].Top.Y) && 
                            (diamondSigPeaks[0].Top.Y < n3Side2SigPeaks[0].Top.Y) )
                            n3Detected = true;
                    }


                    //moissanite
                    moissaniteDetected = significantPeaksList.Where(p => Math.Round(p.Width, 1) >= 1.8 &&
                                    Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.MOIS_MAX_FWHM &&
                                    Math.Round(p.Height) >= _analysisSettings.N3_MIN_HEIGHT &&
                                    ( ( ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) >= 750 && 
                                            ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) <= 830) 
                                        || (ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) >= 940 &&
                                            ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) <= 1000)
                                            )).Count() >= 2;

                    //if (moissaniteDetected)
                    //    Debug.WriteLine("Moissante");
                    //else
                    //    Debug.WriteLine("test");

                    if (n3Peaks.Count == 0 && n3SmoothPeaks.Count > 0)
                        significantPeaksList.AddRange(n3SmoothPeaks);
                }

                var _788Peaks = significantPeaksList.Where(p => Math.Round(p.Width, 1) >= 2 && Math.Round(p.Width, 1) <= 15 &&
                               Math.Round(p.Height) >= _analysisSettings._788_MIN_HEIGHT && p.Top.X >= _analysisSettings._788_START_WAVELEN
                               && p.Top.X <= _analysisSettings._788_END_WAVELEN).ToList();
                var _788SmoothPeaks = significantN3_788SmoothPeaks.Where(p => Math.Round(p.Width, 1) >= 2
                                && Math.Round(p.Width, 1) <= 15 &&
                                Math.Round(p.Height) >= _analysisSettings._788_MIN_HEIGHT && p.Top.X >= _analysisSettings._788_START_WAVELEN
                        && p.Top.X <= _analysisSettings._788_END_WAVELEN).ToList();
                bool _788Detected = (_788Peaks.Count() > 0) || (_788SmoothPeaks.Count() > 0);
                if (_788Peaks.Count == 0 && _788SmoothPeaks.Count > 0)
                    significantPeaksList.AddRange(_788SmoothPeaks);

                bool diamondDetected = false;
                bool _525Detected = false, _525Saturated = false;
                if (diamondSaturated == false)
                {
                    var dpks = significantPeaksList.Where(p => Math.Round(p.Width, 1) > 1 &&
                            Math.Round(p.FullWidthHalfMax, 1) <= _analysisSettings.DIAMOND_MAX_FWHM 
                            && Math.Round(p.Height) >= _analysisSettings.DIAMOND_MIN_HEIGHT
                            && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) > 1282
                            && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) < 1382).ToList();
                    var dSmoothpks = significantN3_788SmoothPeaks.Where(p => Math.Round(p.Width, 1) > 1 &&
                        Math.Round(p.Height) >= _analysisSettings.DIAMOND_MIN_HEIGHT 
                        && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) > 1282
                            && ConvertToRamanShift(_analysisSettings.LASER_WAVELENGTH, p.Top.X) < 1382).ToList();

                    if ( (n3Detected && dpks.Count == 0 && dSmoothpks.Count == 0) || n3Side2Saturated)
                    {
                        //check for n3 sideband
                        dSmoothpks = significantN3_788SmoothPeaks.Where(p => Math.Round(p.Width, 1) > 1.7 &&
                            Math.Round(p.Height) >= _analysisSettings.N3_SIDE2_MIN_HEIGHT
                            && p.Top.X >= _analysisSettings.DIAMOND_START_WAVELEN
                            && p.Top.X <= _analysisSettings.DIAMOND_N3_VIB_END_WAVELEN).ToList();
                    }

                    if (dpks.Count == 0 && dSmoothpks.Count > 0)
                        significantPeaksList.AddRange(dSmoothpks);

                    diamondDetected = ((dpks.Count() > 0)
                                      || (dSmoothpks.Count() > 0)) &&
                                      !strTitanDetected && !moissaniteDetected && !possibleCzDetected;

                    if (_788Detected == false && n3Detected == true)
                    {
                        if (_spectrum.IsSaturated(_analysisSettings._525_START_WAVELEN, _analysisSettings._525_END_WAVELEN) == true)
                        {
                            _525Saturated = true;
                        }
                        else
                        {
                            //check for 525
                            var _525pks = significantPeaksList.Where(p => Math.Round(p.Width, 1) >= 1.5
                                && Math.Round(p.FullWidthHalfMax, 1) >= 1.5
                                && Math.Round(p.Height) >= 50
                                && p.Top.X >= _analysisSettings._525_START_WAVELEN
                                && p.Top.X <= _analysisSettings._525_END_WAVELEN).ToList();
                            _525Detected = (_525pks.Count() > 0);
                        }

                    }
                    
                }
                else//diamond saturated
                {
                    diamondDetected = false;
                    if (n3Detected == true)
                    {
                        //check for 525
                        _525Saturated = _spectrum.IsSaturated(_analysisSettings._525_START_WAVELEN, _analysisSettings._525_END_WAVELEN);
                    }
                }
                #endregion

                #region red_fl
                bool redFlExists = false;
                if (_analysisSettings.CHECK_RED_FL)
                {
                    Task<bool> taskRedFl = Task<bool>.Factory.StartNew(() =>
                    {
                        RedFluorescence rfl = new RedFluorescence(_spectrum);
                        return rfl.Exists();
                    });
                    redFlExists = taskRedFl.Result;
                }
                #endregion

                #region synthetic_peaks
                Task< TaskResult> task468 = null;
                bool _468Flag = false;
                bool _468Detected = (significantPeaksList.Where(p => p.Width > 1.8 && p.Top.X >= _analysisSettings._468_START_WAVELEN 
                    && p.Top.X <= _analysisSettings._468_END_WAVELEN).Count() > 0);
                if (_468Detected == false &&
                        (allPeaksList.Where(p => p.Top.X >= _analysisSettings._468_START_WAVELEN_SMOOTH &&
                            p.Top.X <= _analysisSettings._468_END_WAVELEN_SMOOTH).Count() > 1))
                {
                    task468 = Task<TaskResult>.Factory.StartNew(() => Smooth468Check(_spectrum));
                }
                else if (_468Detected == true)
                {
                    //check shape
                    var yVals = _spectrum.SpectrumData
                        .Where(p => p.X > _analysisSettings._468_END_WAVELEN + 2
                                   && p.X <= _analysisSettings._468_END_WAVELEN + 8).ToList();
                    var lowestY = yVals.Min(p => p.Y);
                    var lowestPoint = yVals.Where(y => y.Y == lowestY).First();

                    if (((double)yVals.Where(p => p.X > lowestPoint.X).Count() / yVals.Count()) < 0.6)
                    {
                        _468Detected = false;
                    }
                }

                bool _490Saturated = _spectrum.IsSaturated(_analysisSettings._490_START_WAVELEN, _analysisSettings._490_END_WAVELEN);
                bool _490Detected = false;

                if (!_490Saturated)
                {
                    _490Detected = (significantPeaksList.Where(p => p.Width > 2 &&
                        p.Top.X >= _analysisSettings._490_START_WAVELEN
                        && p.Top.X <= _analysisSettings._490_END_WAVELEN).Count() > 0);
                    if (!_490Detected)
                    {
                        var significant490SmoothPeaks = task490SmoothOutlierPeaks.Result;
                        var _490SmoothPeaks = significant490SmoothPeaks.Where(p => Math.Round(p.Width, 1) >= 2 &&
                            p.Top.X >= _analysisSettings._490_START_WAVELEN
                            && p.Top.X <= _analysisSettings._490_END_WAVELEN).ToList();
                        _490Detected = _490SmoothPeaks.Count > 0;
                    }
                }

                Task<TaskResult> taskSiV = null;
                bool siVFlag = false;
                bool siVDetected = (significantPeaksList.Where(p => Math.Round(p.Width, 1) > bigWidthThreshold
                    && p.Top.X >= _analysisSettings.SIV_START_WAVELEN 
                    && p.Top.X <= _analysisSettings.SIV_END_WAVELEN).Count() > 0);
                if (siVDetected == false &&
                        (allPeaksList.Where(p => p.Top.X >= _analysisSettings.SIV_START_WAVELEN_SMOOTH &&
                            p.Top.X <= _analysisSettings.SIV_END_WAVELEN_SMOOTH).Count() > 1))
                {
                    taskSiV = Task<TaskResult>.Factory.StartNew(() => SmoothSiVCheck(_spectrum, _788Detected));
                }

                Task<TaskResult> taskNi = null;
                bool niFlag = false;
                bool niFlagSaturated = false;
                bool niDetected = (significantPeaksList.Where(p => Math.Round(p.Width, 1) > bigWidthThreshold
                    && p.Top.X >= _analysisSettings.NI_START_WAVELEN 
                    && p.Top.X <= _analysisSettings.NI_END_WAVELEN).Count() > 0);
                //if (niDetected == false &&
                //        (allPeaksList.Where(p => p.Top.X >= _analysisSettings.NI_START_WAVELEN_SMOOTH &&
                //            p.Top.X <= _analysisSettings.NI_END_WAVELEN_SMOOTH).Count() > 1))
                //{
                    taskNi = Task<TaskResult>.Factory.StartNew(() => SmoothNiCheck(_spectrum));
                //}

                System.Diagnostics.Debug.WriteLine("Check for 468,SiV,Ni peaks " + stopwatch.ElapsedMilliseconds + " ms");

                if (task468 != null)
                {
                    _468Detected = task468.Result.result;
                    _468Flag = task468.Result.flag;
                    if (_468Detected == true)
                    {
                        significantPeaksList.Add(task468.Result.peak);
                    }
                }
                if (taskSiV != null)
                {
                    siVDetected = taskSiV.Result.result;
                    siVFlag = taskSiV.Result.flag;
                    if (siVDetected == true)
                    {
                        significantPeaksList.Add(taskSiV.Result.peak);
                    }
                }
                if (taskNi != null)
                {
                    niDetected = niDetected || taskNi.Result.result;
                    niFlag = taskNi.Result.flag;
                    niFlagSaturated = taskNi.Result.saturated;
                    if (niDetected == true)
                    { 
                        significantPeaksList.Add(taskNi.Result.peak);                        
                    }
                }

                System.Diagnostics.Debug.WriteLine("Check for 468,SiV,Ni peaks (smooth) " + stopwatch.ElapsedMilliseconds + " ms");
                #endregion

                SetDebugPeakInfo(_analysisSettings.N3_START_WAVELEN, n3Detected, n3Saturated, null, null, null, null);
                SetDebugPeakInfo(_analysisSettings.DIAMOND_START_WAVELEN, diamondDetected, diamondSaturated, null, null, null, null);
                SetDebugPeakInfo(_analysisSettings._468_START_WAVELEN, _468Detected, _468Saturated, _468Flag, null, null, null);
                SetDebugPeakInfo(_analysisSettings._490_START_WAVELEN, _490Detected, _490Saturated, null, null, null, null);
                SetDebugPeakInfo(_analysisSettings._525_START_WAVELEN, _525Detected, _525Saturated, null, null, null, null);
                SetDebugPeakInfo(_analysisSettings.SIV_START_WAVELEN, siVDetected, siVSaturated, siVFlag, null, null, null);
                SetDebugPeakInfo(_analysisSettings._788_START_WAVELEN, _788Detected, null, null, null, null, null);
                SetDebugPeakInfo(_analysisSettings.NI_START_WAVELEN, niDetected, niSaturated, niFlag, null, null, niFlagSaturated);

                #region criteria
                if (_analysisSettings.USE_N3_SIDE2_SATURATION)
                {
                    n3Saturated = n3Saturated || n3Side2Saturated;
                }
                result = Criteria(n3Detected, n3Saturated,
                    diamondDetected, diamondSaturated,
                    _468Detected, _468Flag,
                    _490Detected,
                    _525Detected, _525Saturated,
                    siVDetected, siVSaturated, siVFlag,
                    _788Detected,
                    niDetected, niSaturated, niFlag, niFlagSaturated, 
                    possibleCzDetected);
                #endregion

                if (result != ANALYZER_RESULT.SATURATED && redFlExists)
                {
                    throw new Exception("Red Fluorescence detected");
                }

            }
            catch (Exception ex)
            {
                result = ANALYZER_RESULT.ERROR;
            }
            finally
            {
                stopwatch.Stop();
                System.Diagnostics.Debug.WriteLine("End of function " + stopwatch.ElapsedMilliseconds + " ms");
            }

            return result;

        }

        public bool DiamondCurveExists()
        {
            Quadratic quad = new Quadratic(_spectrum);
            return quad.CheckDiamondCurve();
        }

        double CalculateStdDev(IEnumerable<double> values)
        {
            double ret = 0;
            if (values.Count() > 0)
            {
                //Compute the Average      
                double avg = values.Average();
                //Perform the Sum of (value-avg)_2_2      
                double sum = values.Sum(d => Math.Pow(d - avg, 2));
                //Put it all together      
                ret = Math.Sqrt((sum) / (values.Count() - 1));
            }
            return ret;
        }

        bool CheckStraightLine(List<Point> points, double thresh, out double stddev, out double slope)
        {
            bool result = false;
            stddev = Double.MaxValue;
            slope = -1;

            try
            {
                double maxY = points.Select(p => p.Y).Max();
                var pts = points.Select(p => new Point(p.X, p.Y / maxY)).ToList();

                var wls = pts.Select(p => p.X).ToArray();
                var counts = pts.Select(p => p.Y).ToArray();
                var res = MathNet.Numerics.Fit.Line(wls, counts);

                double m = res.Item2;// (pts.Last().Y - pts.First().Y) / (pts.Last().X - pts.First().X);
                double C = res.Item1;// pts.First().Y;
                List<double> error = new List<double>();

                for(int i = 0; i < pts.Count; i++)
                {
                    double x = wls[i];// pts[i].X - pts.First().X;
                    double y = m * x + C;
                    //Debug.WriteLine(pts[i].Y + ", " +  y + "," + Math.Abs((int)(pts[i].Y - y)));
                    error.Add((pts[i].Y - y) * (pts[i].Y - y));
                }

                stddev = CalculateStdDev(error);
                slope = m;
                Debug.WriteLine("cz: stddev = " + stddev);
                result = (stddev < thresh) && slope > 0 ;
            } 
            catch
            {

            }
            return result;
        }

        bool CheckQuadraticLine(List<Point> points, double thresh, out double stddev)
        {
            bool result = false;
            stddev = Double.MaxValue;
            var coeffs = new double[3];

            try
            {
                double[] wls = points.Select(p => p.X).ToArray();
                double[] counts = points.Select(p => p.Y).ToArray();
                counts = counts.Select(c => c / counts.Max()).ToArray();
                coeffs = MathNet.Numerics.Fit.Polynomial(wls, counts, 2);

                List<double> error = new List<double>();

                for (int i = 0; i < points.Count; i++)
                {
                    double x = wls[i];
                    double y_actual = counts[i];
                    double y_calc = coeffs[2] * x * x + coeffs[1] * x + coeffs[0];
                    //Debug.WriteLine(pts[i].Y + ", " +  y + "," + Math.Abs((int)(pts[i].Y - y)));
                    error.Add((y_calc - y_actual) * (y_calc - y_actual));
                    //error.Add(y_calc - y_actual);
                }

                stddev = CalculateStdDev(error);
                //stddev = error.Sum();
                Debug.WriteLine("cz: stddev = " + stddev);
                result = (stddev < thresh);
            }
            catch
            {

            }
            return result;
        }

        bool CheckSmoothDiamondN3Curve(List<Point> points)
        {
            bool result = false;

            try
            {
                double[] wls = points.Select(p => p.X).ToArray();
                double[] counts = points.Select(p => p.Y).ToArray();
                counts = counts.Select(c => c / counts.Max()).ToArray();
                double slope = Math.Atan((counts[1] - counts[0]) / (wls[1] - wls[0])) * (180/Math.PI);
                int state = 0;

                for (int i = 1; i < points.Count-3; i++)//skip last two points
                {
                    double newSlope = Math.Atan((counts[i+1] - counts[i]) / (wls[i+1] - wls[i])) * (180/Math.PI);

                    switch (state)
                    {
                        case 0:
                            if (newSlope < 0.8*slope)
                                state = 1;
                            break;
                        case 1:
                            if (newSlope >= slope)
                                state = 2;
                            else
                                return false;
                            break;
                        case 2:
                            if (newSlope < 0.8*slope)
                                return false;
                            break;
                        default:
                            break;
                    }
                    
                    slope = newSlope;
                }

                result = true;
            }
            catch
            {
                result = false;
            }
            return result;
        }

        bool CzDiamondCurveComparision(List<double> sample, List<double> comp_data, 
            double thresh, out double stddev)
        {
            bool result = false;
            stddev = Double.MaxValue;

            try
            {
                List<double> error = new List<double>();
                sample = sample.Select(d => Math.Round(d / sample.Max(), 4)).ToList();

                for (int i = 0; i < sample.Count; i++)
                {
                   error.Add((sample[i] - comp_data[i]) * (sample[i] - comp_data[i]));
                }

                stddev = CalculateStdDev(error);
                //stddev = error.Sum();
                Debug.WriteLine("cz: stddev = " + stddev);
                result = (stddev < thresh);
            }
            catch
            {

            }
            return result;
        }

        bool CheckN3Peak(Peak n3SmoothPeak, List<Point> n3SmoothRegion, DebugInfo di)
        {
            try
            {
                //symetry
                var start = n3SmoothPeak.Start;
                var end = n3SmoothPeak.End;
                var top = n3SmoothPeak.Top;                

                var leftX = top.X - (end.X - top.X);
                var leftY = n3SmoothRegion.Where(p => p.X >= leftX).First().Y;

                if (leftY <= 1.2 * end.Y)
                {
                    //left side of peak seems symetrical to right side
                    //check if the curve is constant gradient increase
                    if (CheckSmoothDiamondN3Curve(n3SmoothRegion.Where(p =>
                        p.X <= top.X && p.X >= leftX).ToList()))
                        return true;

                    di.IsDetectedStraightLine = true;
                }
                else
                    di.IsDetectedPeak = true;
            }
            catch
            {

            }

            return false;
        }

        public bool AutoCalibrate(out string error)
        {
            bool result = false;
            error = "";

            try
            {
                var allPeaks = _spectrum.FindPeaksByGradient(4).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                var laserPeaks = allPeaks.Where(p => p.Height > 50 && p.Width >= 1
                    && p.Top.X > 402 && p.Top.X < 408).ToList();
                if (laserPeaks.Count == 0 || laserPeaks.Count > 2)
                    throw new Exception("bad laser peaks");
                
                var laserPeak = laserPeaks[0];
                if ( (laserPeaks.Count > 1) 
                        && (Math.Abs(laserPeaks[1].Top.X - 405) < Math.Abs(laserPeaks[0].Top.X - 405)) )
                    laserPeak = laserPeaks[1];

                var n3Peaks = allPeaks.Where(p => p.Height > _analysisSettings.N3_MIN_HEIGHT && p.Width > 0.5
                    && p.Top.X > _analysisSettings.N3_START_WAVELEN 
                    && p.Top.X < _analysisSettings.N3_END_WAVELEN).ToList();
                if (n3Peaks.Count != 1)
                    throw new Exception("bad n3 peaks");

                var smoothSpectrum = new Spectrum();
                List<Peak> smoothPeaks = new List<Peak>();
                for (int i = 1; i < 5; i++)
                {
                    smoothSpectrum = _spectrum.SmoothedSpectrum(3, i);
                    var smoothPeaksList = smoothSpectrum.FindPeaksByGradient
                            (_analysisSettings.N3_SIDE2_START_WAVELEN - 2,
                        _analysisSettings.N3_SIDE2_END_WAVELEN + 2).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    var smoothPeaksCount = smoothPeaksList.Count();

                    if ((smoothPeaksCount <= 1) || (i == 4))
                    {
                        if (smoothPeaksCount <= 1)
                            smoothPeaks = smoothSpectrum.FindPeaksByGradient(4).
                                Where(p => p.Height > 0 && p.Width > 0).ToList();
                        else
                            smoothPeaks = new List<Peak>();
                        break;
                    }

                }
                var n3Side2Peaks = smoothPeaks.Where(p => p.Height > _analysisSettings.N3_MIN_HEIGHT && p.Width > 0.5
                    && p.Top.X > _analysisSettings.N3_SIDE2_START_WAVELEN
                    && p.Top.X < _analysisSettings.N3_SIDE2_END_WAVELEN).ToList();
                if (n3Side2Peaks.Count != 1)
                    throw new Exception("bad n3 second sideband peaks");

                var diamondPeaks = allPeaks.Where(p => p.Height > _analysisSettings.DIAMOND_MIN_HEIGHT 
                    && p.Width > 1
                    && ConvertToRamanShift(laserPeak.Top.X.ToString(), p.Top.X) > 1282 
                    && ConvertToRamanShift(laserPeak.Top.X.ToString(), p.Top.X) < 1382).ToList();
                if (diamondPeaks.Count != 1)
                    throw new Exception("bad diamond peaks");

                var laserPeakFromDiamond = Math.Round(1 / ( 1/ diamondPeaks.First().Top.X + 1332/Math.Pow(10,7)), 2);
                if (Math.Abs(laserPeakFromDiamond - laserPeak.Top.X) >= 0.5)
                    throw new Exception("could not reconcile diamond raman peak shift to laser");

                _analysisSettings.LASER_WAVELENGTH = Math.Round(laserPeakFromDiamond, 2).ToString();

                _analysisSettings.DIAMOND_START_WAVELEN = Math.Round(diamondPeaks.First().Top.X, 1) - 1.5;
                _analysisSettings.DIAMOND_END_WAVELEN = _analysisSettings.DIAMOND_START_WAVELEN + 3;
                _analysisSettings.DIAMOND_MIN_HEIGHT = Math.Round(diamondPeaks.First().Height / 100, 0);
                if (_analysisSettings.DIAMOND_MIN_HEIGHT < 100)
                    _analysisSettings.DIAMOND_MIN_HEIGHT = 100;

                _analysisSettings.N3_START_WAVELEN = Math.Round(n3Peaks.First().Top.X, 1) - 0.8;
                _analysisSettings.N3_END_WAVELEN = _analysisSettings.N3_START_WAVELEN + 1.6;

                _analysisSettings.N3_SIDE2_START_WAVELEN = Math.Round(n3Side2Peaks.First().Top.X, 1) - 0.8;
                _analysisSettings.N3_SIDE2_END_WAVELEN = _analysisSettings.N3_SIDE2_START_WAVELEN + 1.6;

                _analysisSettings.N3_SIDE2_MIN_HEIGHT = Math.Round(n3Side2Peaks.First().Height / 10, 0);
                if (_analysisSettings.N3_SIDE2_MIN_HEIGHT < 100)
                    _analysisSettings.N3_SIDE2_MIN_HEIGHT = 100;

                
                _analysisSettings.Save();

                result = true;
            }
            catch(Exception ex)
            {
                error = ex.Message;
            }

            return result;
        }

        public static ANALYZER_RESULT TestAggregate(List<List<PeakDetectInfo>> detectedPeaks)
        {
            ANALYZER_RESULT result = ANALYZER_RESULT.REFER;
            bool n3Det = false; bool n3Sat = true;
            bool diamondDet = false; bool diamondSat = true;
            bool _468Det = false; bool _468flag = false;
            bool _490Det = false;
            bool _525Det = false; bool _525Sat = true;
            bool siVDet = false; bool siVSat = true; bool siVFlag = false;
            bool _788Det = false;
            bool niDet = false; bool niSat = true; bool niFlag = false, niFlagSat = false;
            try
            {
                if (detectedPeaks.Count < 2)
                    throw new Exception("Not enough information to aggregate");

                foreach(var pdil in detectedPeaks)
                {
                    if (!n3Det)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.N3_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                            n3Det = pdi.IsDetected;
                    }

                    if (n3Sat)
                        n3Sat = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.N3_START_WAVELEN).FirstOrDefault().IsSaturated;

                    if (!diamondDet)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.DIAMOND_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                            diamondDet = pdi.IsDetected;
                    }

                    if (diamondSat)
                        diamondSat = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.DIAMOND_START_WAVELEN).FirstOrDefault().IsSaturated;

                    if (!_468Det)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings._468_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                            _468Det = pdi.IsDetected;
                    }

                    if (!_468flag)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings._468_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                        {
                            _468flag = pdi.IsFlagged;
                        }
                    }

                    if (!_490Det)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings._490_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                            _490Det = pdi.IsDetected;
                    }

                    if (!_525Det)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings._525_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                            _525Det = pdi.IsDetected;
                    }

                    if (_525Sat)
                        _525Sat = pdil.Where(p =>
                            p.StartPosition == _analysisSettings._525_START_WAVELEN).FirstOrDefault().IsSaturated;

                    if (!siVDet)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.SIV_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                            siVDet = pdi.IsDetected;
                    }

                    if (siVSat)
                        siVSat = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.SIV_START_WAVELEN).FirstOrDefault().IsSaturated;

                    if (!siVFlag)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.SIV_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                        {
                            siVFlag = pdi.IsFlagged;
                        }
                    }

                    if (!_788Det)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings._788_START_WAVELEN).FirstOrDefault();
                        if(!pdi.IsSaturated)
                            _788Det = pdi.IsDetected;
                    }

                    if (!niDet)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.NI_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                        {
                            niDet = pdi.IsDetected;
                        }
                    }

                    if (!niFlag)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.NI_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                        {
                            niFlag = pdi.IsFlagged;
                        }
                    }

                    if (!niFlag)
                    {
                        var pdi = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.NI_START_WAVELEN).FirstOrDefault();
                        if (!pdi.IsSaturated)
                        {
                            niFlagSat = pdi.IsFlaggedSat;
                        }
                    }
                    else
                        niFlagSat = false;
                    
                    if (niSat)
                        niSat = pdil.Where(p =>
                            p.StartPosition == _analysisSettings.NI_START_WAVELEN).FirstOrDefault().IsSaturated;

                }


                result = Criteria(n3Det, n3Sat,
                    diamondDet, diamondSat,
                    _468Det, _468flag,
                    _490Det,
                    _525Det, _525Sat,
                    siVDet, siVSat, siVFlag,
                    _788Det,
                    niDet, niSat, niFlag, niFlagSat,
                    false);
            }
            catch
            {
                result = ANALYZER_RESULT.ERROR;
            }

            return result;
        }

        static ANALYZER_RESULT Criteria(bool n3Det, bool n3Sat,
            bool diamondDet, bool diamondSat,
            bool _468Det, bool _468Flag,
            bool _490Det,
            bool _525Det, bool _525Sat,
            bool siVDet, bool siVSat, bool siVflag,
            bool _788Det,
            bool niDet, bool niSat, bool niFlag, bool niFlagSat,
            bool possibleCzDet)
        {
            ANALYZER_RESULT result = ANALYZER_RESULT.REFER;

            if (n3Sat == true || siVSat == true || niSat == true)
            {
                return ANALYZER_RESULT.SATURATED;
            }


            if (diamondDet == false && diamondSat == false)
            {
                if (n3Det == false || possibleCzDet)
                    result = ANALYZER_RESULT.NON_DIAMOND;
                else
                    result = ANALYZER_RESULT.NONE;//increase int time
            }
            else if (n3Det == true || _788Det == true)
            {
                if (n3Det)
                    _468Det = false;//not possible to have N3 and _468 so must be false peak


                if (n3Det == true && _788Det == true && _468Det == false && siVDet == false)
                {
                    result = ANALYZER_RESULT.NATURAL_DIAMOND;
                }
                else if (_468Det == false && siVDet == false && niDet == false)
                {
                    if (_788Det == true)
                        result = ANALYZER_RESULT.NATURAL_DIAMOND;
                    else if (_525Sat == true)
                        result = ANALYZER_RESULT.SATURATED;
                    else if (_525Det == true || _490Det == true)
                        result = ANALYZER_RESULT.REFER;
                    else
                        result = ANALYZER_RESULT.NATURAL_DIAMOND;
                }
                else if (n3Det == true && niDet == true && _525Sat == false  && _525Det == false)
                {
                    result = ANALYZER_RESULT.NATURAL_DIAMOND;//Ulrika 01/06/2020 Could we also add N3 + Ni + no 525 = natural to your criteria? 
                }
                else
                    result = ANALYZER_RESULT.REFER;
            }
            else
            {
                if (_468Det == false && siVDet == false && niDet == false && _490Det == false)
                    result = ANALYZER_RESULT.NONE;
                else
                {

                    if (niDet)
                    {
                        if (niFlag)
                            result = ANALYZER_RESULT.REFER;
                        else if (niFlagSat)
                            result = ANALYZER_RESULT.SATURATED;
                        else
                            result = ANALYZER_RESULT.HPHT_SYNTHETIC_DIAMOND;
                    }
                    else if (_490Det)
                        result = ANALYZER_RESULT.REFER;
                    else if ((_468Det && !_468Flag) || (siVDet && !siVflag))
                        result = ANALYZER_RESULT.CVD_SYNTHETIC_DIAMOND;
                    else
                        result = ANALYZER_RESULT.REFER;
                }
            }

            return result;
        }

        
        public List<int> CheckHotPixels()
        {
            return _spectrum.HotPixels(_analysisSettings.N3_START_WAVELEN, _analysisSettings.N3_END_WAVELEN)
                .Concat(_spectrum.HotPixels(_analysisSettings.DIAMOND_START_WAVELEN, _analysisSettings.DIAMOND_END_WAVELEN))
                .Concat(_spectrum.HotPixels(_analysisSettings._468_START_WAVELEN_SMOOTH, _analysisSettings._468_END_WAVELEN_SMOOTH))
                .Concat(_spectrum.HotPixels(_analysisSettings._490_START_WAVELEN, _analysisSettings._490_END_WAVELEN))
                .Concat(_spectrum.HotPixels(_analysisSettings._525_START_WAVELEN, _analysisSettings._525_END_WAVELEN))
                .Concat(_spectrum.HotPixels(_analysisSettings.SIV_START_WAVELEN_SMOOTH, _analysisSettings.SIV_END_WAVELEN_SMOOTH))
                .Concat(_spectrum.HotPixels(_analysisSettings._788_START_WAVELEN_SMOOTH, _analysisSettings._788_END_WAVELEN_SMOOTH))
                .Concat(_spectrum.HotPixels(_analysisSettings.NI_START_WAVELEN_SMOOTH, _analysisSettings.NI_END_WAVELEN_SMOOTH))
                .ToList();
        }

        class TaskResult
        {
            public bool result;
            public List<Peak> smoothPeaks;
            public Peak peak;
            public bool flag;
            public bool saturated;
        }

        TaskResult Smooth468Check(Spectrum spectrum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            TaskResult res = new TaskResult();
            res.result = false;

            #region 468_smooth

            var smooth468Spectrum = new Spectrum();
            List<Peak> allSmoothPeaks = new List<Peak>();
            for (int i = 1; i < 15; i++)
            {
                smooth468Spectrum = spectrum.SmoothedSpectrum(2, i);
                allSmoothPeaks = smooth468Spectrum.FindPeaksByGradient(_analysisSettings._468_START_WAVELEN_SMOOTH, 
                    _analysisSettings._468_END_WAVELEN_SMOOTH).
                    Where(p => p.Height > 0 && p.Width > 0).ToList();
                if(allSmoothPeaks.Count() <= 1 || i == 14)
                {
                    allSmoothPeaks = smooth468Spectrum.FindPeaksByGradient(4).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    break;
                }
            }

            res.smoothPeaks = allSmoothPeaks;
            SetDebugPeakInfo(_analysisSettings._468_START_WAVELEN, null, null, null, allSmoothPeaks, smooth468Spectrum.SpectrumData.ToList(), null);

            var peaks = allSmoothPeaks.Where(p => p.Width > 2 && p.Width < 10 && p.FullWidthHalfMax <= 2 && 
                    p.Top.X >= _analysisSettings._468_START_WAVELEN -1
                    && p.Top.X <= _analysisSettings._468_END_WAVELEN +1).ToList();

            var stats = new PeakStats(allSmoothPeaks.Select(p => p.Height).ToList());
            //var stats1 = new PeakStats(allSmoothPeaks.Select(p => p.Width).ToList());

            var outliers = stats.OutliersUsingMAD(_analysisSettings._468_SMOOTH_THRESHOLD);
            outliers.Sort();
            var heightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

            var peak = peaks.Where(p =>
                    Math.Round(p.Height, 1) >= 100 && 
                    Math.Round(p.Height,1) >= heightThreshold &&
                    p.FullWidthHalfMax <= 3
                    //( stats.Percentile(p.Height) > 0.95 || (stats.Percentile(p.Height) > 0.9 && stats1.Percentile(p.Width) > 0.95) )
                ).ToList();

           
            if (peak.Count > 0)
            {
                var yVals = smooth468Spectrum.SpectrumData
                    .Where(p => p.X > _analysisSettings._468_END_WAVELEN + 2
                               && p.X <= _analysisSettings._468_END_WAVELEN + 8);
                var lowestY = yVals.Min(p => p.Y);
                var lowestPoint = yVals.Where(y => y.Y == lowestY).First();

                if (((double)yVals.Where(p => p.X > lowestPoint.X).Count() / yVals.Count()) >= 0.6)
                {
                    res.result = true;
                    res.peak = peak[0];
                    res.flag = peak[0].Height < 500;
                }
            }

            #endregion

            sw.Stop();
            System.Diagnostics.Debug.WriteLine("468 smooth took " + sw.ElapsedMilliseconds + " ms");

            return res;
        }

        TaskResult SmoothSiVCheck(Spectrum spectrum, bool Present788)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            TaskResult res = new TaskResult();
            #region 737_smooth

            Task<Tuple<Spectrum, List<Peak>>> smooth1 = Task<Tuple<Spectrum, List<Peak>>>.Factory.StartNew(() =>
            {
                var smoothSiVSpectrum = new Spectrum();
                List<Peak> allPeaks = new List<Peak>();
                for (int i = 1; i < 9; i++)
                {
                    smoothSiVSpectrum = spectrum.SmoothedSpectrum(5, i);
                    allPeaks = smoothSiVSpectrum.FindPeaksByGradient(_analysisSettings.SIV_START_WAVELEN_SMOOTH,
                        _analysisSettings.SIV_END_WAVELEN_SMOOTH).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    if (allPeaks.Count() <= 1)
                    {
                        allPeaks = smoothSiVSpectrum.FindPeaksByGradient(4).
                            Where(p => p.Height > 0 && p.Width > 0).ToList();
                        break;
                    }
                    else
                        allPeaks = null;
                }

                return Tuple.Create(smoothSiVSpectrum, allPeaks); 
            });
            Task<Tuple<Spectrum, List<Peak>>> smooth2 = Task<Tuple<Spectrum, List<Peak>>>.Factory.StartNew(() =>
            {
                var smoothSiVSpectrum = new Spectrum();
                List<Peak> allPeaks = new List<Peak>();
                for (int i = 9; i < 18; i++)
                {
                    smoothSiVSpectrum = spectrum.SmoothedSpectrum(5, i);
                    allPeaks = smoothSiVSpectrum.FindPeaksByGradient(_analysisSettings.SIV_START_WAVELEN_SMOOTH,
                        _analysisSettings.SIV_END_WAVELEN_SMOOTH).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    if (allPeaks.Count() <= 1)
                    {
                        allPeaks = smoothSiVSpectrum.FindPeaksByGradient(4).
                            Where(p => p.Height > 0 && p.Width > 0).ToList();
                        break;
                    }
                    else
                        allPeaks = null;
                }

                return Tuple.Create(smoothSiVSpectrum, allPeaks); 
            });
            Task<Tuple<Spectrum, List<Peak>>> smooth3 = Task<Tuple<Spectrum, List<Peak>>>.Factory.StartNew(() =>
            {
                var smoothSiVSpectrum = new Spectrum();
                List<Peak> allPeaks = new List<Peak>();
                for (int i = 18; i < 25; i++)
                {
                    smoothSiVSpectrum = spectrum.SmoothedSpectrum(5, i);
                    allPeaks = smoothSiVSpectrum.FindPeaksByGradient(_analysisSettings.SIV_START_WAVELEN_SMOOTH,
                        _analysisSettings.SIV_END_WAVELEN_SMOOTH).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    if (allPeaks.Count() <= 1 || i == 24)
                    {
                        allPeaks = smoothSiVSpectrum.FindPeaksByGradient(4).
                            Where(p => p.Height > 0 && p.Width > 0).ToList();
                        break;
                    }
                    
                }

                return Tuple.Create(smoothSiVSpectrum, allPeaks);
            });

            Spectrum smoothSiVSpect = new Spectrum();
            List<Peak> allSmoothPeaks = new List<Peak>();
            if (smooth1.Result.Item2 != null)
            {
                smoothSiVSpect = smooth1.Result.Item1;
                allSmoothPeaks = smooth1.Result.Item2;
            }
            else if (smooth2.Result.Item2 != null)
            {
                smoothSiVSpect = smooth2.Result.Item1;
                allSmoothPeaks = smooth2.Result.Item2;
            }
            else
            {
                smoothSiVSpect = smooth3.Result.Item1;
                allSmoothPeaks = smooth3.Result.Item2;
                if (allSmoothPeaks.Count > 40)
                    allSmoothPeaks = new List<Peak>();
            }
            res.smoothPeaks = allSmoothPeaks;
            SetDebugPeakInfo(_analysisSettings.SIV_START_WAVELEN, null, null, null, allSmoothPeaks, smoothSiVSpect.SpectrumData.ToList(),null);

            var peak = allSmoothPeaks.Where(p =>
                    p.Top.X >= _analysisSettings.SIV_START_WAVELEN - 2 && p.Top.X <= _analysisSettings.SIV_END_WAVELEN + 2).ToList();

                          
            if (peak.Count > 0 && allSmoothPeaks.Count < 20)
            {
                var noisePeaks = allSmoothPeaks.Where(p => p.Top.X < 400).ToList();
                double noiseHeightThreshold = 50;
                double noiseWidthThreshold = 1.8;
                if (noisePeaks.Count > 0)
                {
                    noisePeaks.RemoveAt(0);
                    if (noisePeaks.Count > 0)
                    {
                        noiseHeightThreshold = noisePeaks.Select(p => p.Height).Average() * 3;
                        if (noiseHeightThreshold < 20)
                            noiseHeightThreshold = 20;
                        noiseWidthThreshold = noisePeaks.Select(p => p.Width).Average();
                    }
                }

                var heights = allSmoothPeaks.Select(p => p.Height).ToList();
                heights.Sort();
                if (heights[heights.Count - 5] > noiseHeightThreshold)
                    noiseHeightThreshold = heights[heights.Count - 5];

                peak = peak.Where(p => Math.Round(p.Height, 1) >= noiseHeightThreshold &&
                                    Math.Round(p.Width, 1) >= noiseWidthThreshold
                                && Math.Round(p.FullWidthHalfMax) <= 8).ToList();
            }
            else if (peak.Count > 0)
            {
                var stats = new PeakStats(allSmoothPeaks.Select(p => p.Height).ToList());
                var outliers = stats.OutliersUsingMAD(_analysisSettings.SI_SMOOTH_THRESHOLD);
                outliers.Sort();
                var bigHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                var temp_peak = peak.Where(p => Math.Round(p.Height, 1) >= bigHeightThreshold).ToList();
                if (temp_peak.Count() == 0)
                {
                    var smoothPeaks = allSmoothPeaks.Where(p =>
                            p.Top.X >= _analysisSettings.SIV_START_WAVELEN_SMOOTH
                            && p.Top.X <= _analysisSettings.SIV_END_WAVELEN_SMOOTH).ToList();

                    if (smoothPeaks.Count <= 2)
                    {
                        stats = new PeakStats(allSmoothPeaks.Select(p => p.Width).ToList());
                        outliers = stats.OutliersUsingMAD(_analysisSettings.SI_SMOOTH_THRESHOLD);
                        outliers.Sort();
                        var extremeWidthThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                        stats = new PeakStats(allSmoothPeaks.Where(p => p.Width < extremeWidthThreshold)
                                                        .Select(p => p.Height).ToList());
                        outliers = stats.OutliersUsingMAD(_analysisSettings.SI_SMOOTH_THRESHOLD / 4);
                        outliers.Sort();
                        var smallHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                        peak = peak.Where(p =>
                            (Math.Round(p.Height, 1) >= smallHeightThreshold &&
                                            Math.Round(p.FullWidthHalfMax) <= 7)
                            ).ToList();
                    }
                    else
                    {
                        smoothPeaks = smoothPeaks.OrderBy(p => p.Height).ToList();
                        var median = allSmoothPeaks.Select(p => p.Height).ToList();
                        median.Sort();
                        if ((peak.First().Height < median[2*median.Count/3]) ||
                            (peak.First().Height < 4 * smoothPeaks[smoothPeaks.Count - 2].Height))
                            peak = new List<Peak>();
                    }
                }
                else
                    peak = temp_peak;
            }
            

            if (peak.Count > 0)
            {
                if (!Present788)
                {
                    res.result = true;
                    res.peak = peak[0];
                    res.flag = peak[0].Height < 200;
                }
                else if (peak[0].Height > 500)
                {
                    res.result = true;
                    res.peak = peak[0];
                    res.flag = true;
                }
                else
                    res.result = false;//if 788 and small siV peak then ignore

            }
            else
                res.result = false;

            #endregion

            sw.Stop();
            System.Diagnostics.Debug.WriteLine("SiV smooth took " + sw.ElapsedMilliseconds + " ms");

            return res;
        }

        TaskResult SmoothNiCheck(Spectrum spectrum)
        {
            var sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            #region 882_smooth
            TaskResult res = new TaskResult();


            Task<Tuple<Spectrum, List<Peak>>> smooth1 = Task<Tuple<Spectrum, List<Peak>>>.Factory.StartNew(() =>
            {
                var smoothNiSpect = new Spectrum();
                List<Peak> allPeaks = new List<Peak>();
                for (int i = 1; i < 8; i++)
                {
                    smoothNiSpect = spectrum.SmoothedSpectrum(5, i);
                    allPeaks = smoothNiSpect.FindPeaksByGradient(_analysisSettings.NI_START_WAVELEN_SMOOTH,
                        _analysisSettings.NI_END_WAVELEN_SMOOTH).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    if (allPeaks.Count() <= 1)
                    {
                        allPeaks = smoothNiSpect.FindPeaksByGradient(4).
                            Where(p => p.Height > 0 && p.Width > 0).ToList();
                        break;
                    }
                    else
                        allPeaks = null;
                }

                return Tuple.Create(smoothNiSpect, allPeaks);
            });
            Task<Tuple<Spectrum, List<Peak>>> smooth2 = Task<Tuple<Spectrum, List<Peak>>>.Factory.StartNew(() =>
            {
                var smoothNiSpect = new Spectrum();
                List<Peak> allPeaks = new List<Peak>();
                for (int i = 8; i < 15; i++)
                {
                    smoothNiSpect = spectrum.SmoothedSpectrum(5, i);
                    allPeaks = smoothNiSpect.FindPeaksByGradient(_analysisSettings.NI_START_WAVELEN_SMOOTH,
                        _analysisSettings.NI_END_WAVELEN_SMOOTH).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                    if (allPeaks.Count() <= 1 || i == 14)
                    {
                        allPeaks = smoothNiSpect.FindPeaksByGradient(4).
                            Where(p => p.Height > 0 && p.Width > 0).ToList();
                        break;
                    }
                }

                return Tuple.Create(smoothNiSpect, allPeaks);
            });

            var smoothNiSpectrum = new Spectrum();
            List<Peak> allSmoothPeaks = new List<Peak>();
            if (smooth1.Result.Item2 != null)
            {
                smoothNiSpectrum = smooth1.Result.Item1;
                allSmoothPeaks = smooth1.Result.Item2;
            }
            else if (smooth2.Result.Item2 != null)
            {
                smoothNiSpectrum = smooth2.Result.Item1;
                allSmoothPeaks = smooth2.Result.Item2;
            }

            res.smoothPeaks = allSmoothPeaks;
            SetDebugPeakInfo(_analysisSettings.NI_START_WAVELEN, null, null, null, allSmoothPeaks, smoothNiSpectrum.SpectrumData.ToList(),null);

            var peak = allSmoothPeaks.Where(p =>
                p.Top.X >= _analysisSettings.NI_START_WAVELEN - 2 && p.Top.X <= _analysisSettings.NI_END_WAVELEN + 2).ToList();

            //check broadband curve 545 - 595 and 665 to 705
            var minHeight = smoothNiSpectrum.SpectrumData.Where(p =>
                p.X >= _analysisSettings.NI_START_WAVELEN && p.X <= _analysisSettings.NI_END_WAVELEN).
                Select(p => p.Y).Average();
            if (peak.Count > 0)
                minHeight = peak[0].Top.Y;

            List<Point> pts = new List<Point>();
            pts.Add(smoothNiSpectrum.SpectrumData.Where(p => p.X >= 545 && p.X < 550).First());
            pts.Add(smoothNiSpectrum.SpectrumData.Where(p => p.X >= 565 && p.X < 570).First());
            pts.Add(smoothNiSpectrum.SpectrumData.Where(p => p.X >= 590 && p.X < 595).First());
            Parabola parb = new Parabola(pts);
            if ((pts[1].Y > 4 * minHeight) && parb.Fit() && Math.Round(parb.A) <= -1)
            {
                res.flag = true;
            }
            else
            {
                pts = new List<Point>();
                pts.Add(smoothNiSpectrum.SpectrumData.Where(p => p.X >= 665 && p.X < 670).First());
                pts.Add(smoothNiSpectrum.SpectrumData.Where(p => p.X >= 680 && p.X < 685).First());
                pts.Add(smoothNiSpectrum.SpectrumData.Where(p => p.X >= 700 && p.X < 705).First());
                parb = new Parabola(pts);
                if ((pts[1].Y > 4 * minHeight) && parb.Fit() && Math.Round(parb.A) <= -1)
                {
                    res.flag = true;
                }
            }

            if (res.flag == false)
            {
                //check for saturated broadband 
                if (smoothNiSpectrum.IsSaturated(545,595) ||
                    smoothNiSpectrum.IsSaturated(665, 705) )
                {
                    res.saturated = true;
                }
            }

            if (peak.Count > 0)
            {
                //check ni peak is significant
                if (allSmoothPeaks.Count < 20)
                {
                    var stats = new PeakStats(allSmoothPeaks.Select(p => p.Height).ToList());
                    peak = peak.Where(p => (stats.Percentile(p.Height) > 0.75) && Math.Round(p.Width) >= 6
                                && p.Height > 100).ToList();
                }
                else
                {
                    var stats = new PeakStats(allSmoothPeaks.Select(p => p.Height).ToList());
                    var outliers = stats.OutliersUsingMAD(_analysisSettings.NI_SMOOTH_THRESHOLD);
                    outliers.Sort();
                    var bigHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                    outliers = stats.OutliersUsingMAD(_analysisSettings.NI_SMOOTH_THRESHOLD / 2);
                    outliers.Sort();
                    var medHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                    outliers = stats.OutliersUsingMAD(_analysisSettings.NI_SMOOTH_THRESHOLD / 4);
                    outliers.Sort();
                    var smallHeightThreshold = outliers.Count > 0 ? Math.Round(outliers[0], 1) : double.MaxValue;

                    stats = new PeakStats(allSmoothPeaks.Select(p => p.Width).ToList());

                    peak = peak.Where(p =>
                        (p.Height > _analysisSettings.NI_MIN_HEIGHT && 
                          ( (Math.Round(p.Height, 1) >= bigHeightThreshold) ||
                              (stats.Percentile(p.Width) > 0.90 && Math.Round(p.Height, 1) >= medHeightThreshold) ||
                              (stats.Percentile(p.Width) > 0.95 && Math.Round(p.Height, 1) >= smallHeightThreshold)
                          ) 
                        )
                    ).ToList();
                }

            }

            if (peak.Count > 0)
            {
                res.result = true;
                res.peak = peak[0];               
            }
            else
                res.result = false;

            sw.Stop();
            System.Diagnostics.Debug.WriteLine("Ni smooth took " + sw.ElapsedMilliseconds + " ms");

            return res;
            #endregion
        }



        List<double> ConvertToRamanShift(double laserWl, List<double> wl)
        {
            List<double> rs = new List<double>();

            try
            {
                rs = wl.Select(d => Math.Round((1 / laserWl - 1 / d) * Math.Pow(10, 7))).ToList();
            }
            catch
            {
                
            }

            return rs;
        }
        double ConvertToRamanShift(string laserWl, double wl)
        {
            double rs = Math.Round((1 / Convert.ToDouble(laserWl) - 1 / wl) * Math.Pow(10, 7));

            return rs;
        }

    }
}
