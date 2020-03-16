using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using _405Analyzer;
using System.Data;
using PeakFinder;
using System.Windows;

namespace RamanMapping.Model
{
    public enum TEST_PARM_TYPE
    {
        SKIP = -1,
        NATURAL,
        SYNTHETIC,
        SIMULANT,
        INDIVIDUAL
    }

    public static class RamanAnalyzer
    {
        static DataTable dtSigPeaks;
        static DataTable dtPeakInfo;
        static DataTable dtAllPeaks;
        static DataTable dtSmoothPeaks;

        static RamanAnalyzer()
        {
            dtSigPeaks = new DataTable();
            dtSigPeaks.Columns.Add("Location", typeof(double));
            dtSigPeaks.Columns.Add("Height", typeof(double));
            dtSigPeaks.Columns.Add("Width", typeof(double));
            dtSigPeaks.Columns.Add("FWHM", typeof(double));
            dtAllPeaks = dtSigPeaks.Clone();
            dtSmoothPeaks = dtSigPeaks.Clone();
            dtPeakInfo = new DataTable();
            dtPeakInfo.Columns.Add("Location", typeof(double));
            dtPeakInfo.Columns.Add("Saturated", typeof(bool));
            dtPeakInfo.Columns.Add("Found", typeof(bool));
        }

        public static ANALYZER_RESULT Analysis(List<double> wl, List<double> counts, out List<PeakDetectInfo> peaks)
        {
            peaks = new List<PeakDetectInfo>();
            try
            {
                SpectrumAnalyzer analyzer = new SpectrumAnalyzer(wl, counts);
                var result = analyzer.Test(out peaks);
                return result;
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return ANALYZER_RESULT.ERROR;
        }

        public static ANALYZER_RESULT Analysis(List<double> wl, List<double> counts, bool skipSaturated = false, bool skipNone = false,
            bool skipRefer = false, TEST_PARM_TYPE test_type = TEST_PARM_TYPE.SKIP, List<double> checks = null)
        {
            List<List<double>> wlList = new List<List<double>>();
            List<List<double>> countsList = new List<List<double>>();
            wlList.Add(wl);
            countsList.Add(counts);

            return Analysis(wlList, countsList, skipSaturated, skipNone, skipRefer, test_type, checks);
        }

        static ANALYZER_RESULT Analysis(List<List<double>> wlList, List<List<double>> countsList, bool skipSaturated = false,
            bool skipNone = false, bool skipRefer = false, TEST_PARM_TYPE test_type = TEST_PARM_TYPE.SKIP, List<double> checks = null)
        {
            bool result = false;
            try
            {
                dtAllPeaks.Rows.Clear();
                dtSigPeaks.Rows.Clear();
                dtSmoothPeaks.Rows.Clear();
                dtPeakInfo.Rows.Clear();

                List<Peak> allPeaks;
                List<Peak> sigPeaks;
                List<List<PeakDetectInfo>> debugPeakInfoList = new List<List<PeakDetectInfo>>();
                ANALYZER_RESULT testResult = ANALYZER_RESULT.ERROR;
                List<Point> spectrum = new List<Point>();
                List<Point> smoothSpectrum = new List<Point>();

                if (wlList.Count == 1)
                {
                    var wl = wlList[0];
                    var counts = countsList[0];
                    SpectrumAnalyzer analyzer = new SpectrumAnalyzer(wl, counts);

                    spectrum = analyzer.SpectrumData.SpectrumData;
                    var debugPeakInfo = new List<PeakDetectInfo>();
                    var debugInfo = new DebugInfo();
                    testResult = analyzer.Test(out allPeaks, out sigPeaks, out debugPeakInfo, out debugInfo);
                    debugPeakInfoList.Add(debugPeakInfo);

                    for (int i = 0; i < allPeaks.Count; i++)
                    {
                        DataRow row = dtAllPeaks.NewRow();
                        var top = Math.Round(allPeaks[i].Top.X, 1);
                        var height = Math.Round(allPeaks[i].Height, 1);
                        var width = Math.Round(allPeaks[i].Width, 1);
                        var fwhm = Math.Round(allPeaks[i].FullWidthHalfMax, 1);
                        row[0] = Math.Round(allPeaks[i].Top.X, 1);
                        row[1] = Math.Round(allPeaks[i].Height, 1);
                        row[2] = Math.Round(allPeaks[i].Width, 1);
                        row[3] = Math.Round(allPeaks[i].FullWidthHalfMax, 1);
                        //if ( (top>=414&&top<=416))
                        //    File.AppendAllText(@"P:\Projects\405\temp\sudhin.csv", 
                        //        top+","+height+","+width+","+fwhm+"\r\n");
                        dtAllPeaks.Rows.Add(row);

                    }
                    for (int i = 0; i < sigPeaks.Count; i++)
                    {
                        DataRow row = dtSigPeaks.NewRow();
                        row[0] = Math.Round(sigPeaks[i].Top.X, 1);
                        row[1] = Math.Round(sigPeaks[i].Height, 1);
                        row[2] = Math.Round(sigPeaks[i].Width, 1);
                        row[3] = Math.Round(sigPeaks[i].FullWidthHalfMax, 1);
                        dtSigPeaks.Rows.Add(row);

                    }
                }
                //else
                //{
                //    for (int i = 0; i < wlList.Count; i++)
                //    {
                //        var wl = wlList[i];
                //        var counts = countsList[i];
                //        List<PeakDetectInfo> peakInfo = new List<PeakDetectInfo>();
                //        var debugInfo = new DebugInfo();
                //        SpectrumAnalyzer analyzer = new SpectrumAnalyzer(wl, counts);
                //        var s = analyzer.SpectrumData.SpectrumData;
                //        if (i % 2 != 0)
                //            s.Reverse();
                //        spectrum.AddMany(s);
                //        analyzer.Test(out peakInfo, out debugInfo);
                //        if (chkPossibleCz.IsChecked == false && chkPossibleCzPeak.IsChecked == false)
                //        {
                //            chkPossibleCz.IsChecked = debugInfo.IsDetectedStraightLine;
                //            chkPossibleCzPeak.IsChecked = debugInfo.IsDetectedPeak;
                //        }
                //        if (chkN3Side2.IsChecked == false)
                //            chkN3Side2.IsChecked = debugInfo.IsDetectedN3Side2;

                //        debugPeakInfoList.Add(peakInfo);
                //    }

                //    testResult = SpectrumAnalyzer.TestAggregate(debugPeakInfoList);
                //}

                for (int i = 0; i < debugPeakInfoList.Count; i++)
                {
                    var debugPeakInfo = debugPeakInfoList[i];
                    for (int j = 0; j < debugPeakInfo.Count; j++)
                    {
                        var key = Math.Round(debugPeakInfo[j].StartPosition);
                        DataRow row = null;
                        string search = "Location = " + key;
                        var foundRows = dtPeakInfo.Select(search);
                        if (foundRows.Length == 0)
                        {
                            row = dtPeakInfo.NewRow();
                            row[0] = key;
                            row[1] = debugPeakInfo[j].IsSaturated;
                            row[2] = debugPeakInfo[j].IsDetected;
                            dtPeakInfo.Rows.Add(row);
                        }
                        else
                        {
                            row = foundRows[0];
                            if (!debugPeakInfo[j].IsSaturated)
                                row[1] = false;

                            if ((bool)row[2] == false)
                                row[2] = debugPeakInfo[j].IsDetected;
                        }
                    }

                }

                if (test_type != TEST_PARM_TYPE.SKIP)
                {
                    if (skipSaturated && testResult == ANALYZER_RESULT.SATURATED)
                        result = true;
                    else if (skipNone && testResult == ANALYZER_RESULT.NONE)
                        result = true;
                    else if (skipRefer && testResult == ANALYZER_RESULT.REFER)
                        result = true;
                    else
                    {
                        if (test_type == TEST_PARM_TYPE.NATURAL)//natural
                        {
                            result = testResult == ANALYZER_RESULT.NATURAL_DIAMOND;
                        }
                        else if (test_type == TEST_PARM_TYPE.SYNTHETIC)//synthetic
                        {
                            result = (testResult == ANALYZER_RESULT.HPHT_SYNTHETIC_DIAMOND ||
                                testResult == ANALYZER_RESULT.CVD_SYNTHETIC_DIAMOND);
                        }
                        else if (test_type == TEST_PARM_TYPE.SIMULANT)//simulant
                        {
                            result = testResult == ANALYZER_RESULT.NON_DIAMOND;
                        }
                        else if (test_type == TEST_PARM_TYPE.INDIVIDUAL)//individual
                        {
                            result = checks != null && checks.Count > 0;
                            foreach (var pos in checks)
                            {
                                var debugPeakInfo = debugPeakInfoList[0];
                                if (debugPeakInfo.Where(p => Math.Abs(p.StartPosition - pos) <= 3 && p.IsDetected).ToList().Count == 0)
                                {
                                    result = false;
                                    break;
                                }

                            }
                        }
                    }
                }

                return testResult;

                //if (result == false)
                //{
                //update gui
                //return ResultString((int)testResult);

                //if (comboShowSmooth.SelectedIndex < 1)
                //{
                //    SpectrumData = new ObservableDataSource<Point>(spectrum);
                //    lgSmoothSpectrum.Visibility = Visibility.Collapsed;
                //    lgMainSpectrum.Visibility = Visibility.Visible;
                //}
                //else
                //{
                //    double pos = Convert.ToDouble(comboShowSmooth.Text);
                //    var debugPeakInfo = debugPeakInfoList[0];
                //    var dpi = debugPeakInfo.Where(p => Math.Abs(p.StartPosition - pos) <= 3).FirstOrDefault();
                //    if (dpi != null)
                //    {
                //        SmoothSpectrumData.Collection.AddMany(dpi.SmoothSpectrumData);
                //        lgSmoothSpectrum.Visibility = Visibility.Visible;
                //        lgMainSpectrum.Visibility = Visibility.Collapsed;

                //        for (int i = 0; i < dpi.SmoothPeaks.Count; i++)
                //        {
                //            DataRow row = dtSmoothPeaks.NewRow();
                //            row[0] = Math.Round(dpi.SmoothPeaks[i].Top.X, 1);
                //            row[1] = Math.Round(dpi.SmoothPeaks[i].Height, 1);
                //            row[2] = Math.Round(dpi.SmoothPeaks[i].Width, 1);
                //            row[3] = Math.Round(dpi.SmoothPeaks[i].FullWidthHalfMax, 1);
                //            dtSmoothPeaks.Rows.Add(row);

                //        }
                //    }
                //    else
                //    {
                //        SpectrumData = new ObservableDataSource<Point>(spectrum);
                //        lgSmoothSpectrum.Visibility = Visibility.Collapsed;
                //        lgMainSpectrum.Visibility = Visibility.Visible;
                //    }

                //}

                //}


            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error processing data");

                //SpectrumData.Collection.Clear();
                //SmoothSpectrumData.Collection.Clear();

                //PeakHeightThreshold = 0;
                //PeakWidthThreshold = 0;
                //mainWindow.Title = "Bad data";
                return ANALYZER_RESULT.ERROR;
            }
        }
    }
}
