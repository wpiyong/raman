using PeakFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _405Analyzer
{
    public enum SAPPHIRE_ANALYZER_RESULT
    {
        ERROR_SPIKE = -2,
        ERROR = -1,
        NATURAL_SAPPHIRE = 0,
        REFER = 3,
        SATURATED = 4,
        NONE = 5
    }

    public class SapphireSpectrumAnalyzer
    {
        static SapphireSettings _sapphireAnalysisSettings = new SapphireSettings();
        List<Spectrum> _spectra = new List<Spectrum>();
        List<double> _integrationTimes = new List<double>();

        public List<Spectrum> SpectraData { get { return _spectra; } }

        public SapphireSpectrumAnalyzer(List<double> intTimes, List<double> wavlengths, List<List<double>> countsList)
        {
            _integrationTimes = intTimes.ToList();

            foreach(var counts in countsList )
                _spectra.Add(new Spectrum(wavlengths.Zip(counts, (w, c) => new Point(w, c)).ToList()));

            if (!_sapphireAnalysisSettings.Load())
                throw new Exception("Could not load saphire analysis settings");
            
        }

        bool CheckForCrPeaks(Spectrum spectrum)
        {
            try
            {
                var allPeaksList = spectrum.FindPeaksByGradient(4).
                        Where(p => p.Height > 0 && p.Width > 0).ToList();
                
                //find outliers
                var rawStats = new PeakStats(allPeaksList.Select(p => p.Height).ToList());
                var rawOutliers = rawStats.OutliersUsingMAD(7.5);
                rawOutliers.Sort();
                var heightThreshold = rawOutliers.Count > 0 ? Math.Round(rawOutliers[0], 1) : double.MaxValue;
                rawStats = new PeakStats(allPeaksList.Select(p => p.Width).ToList());
                rawOutliers = rawStats.OutliersUsingMAD(5.0);
                rawOutliers.Sort();
                var widthThreshold = rawOutliers.Count > 0 ? Math.Round(rawOutliers[0], 1) : double.MaxValue;

                var significantPeaksList = allPeaksList.Where(p => Math.Round(p.Width, 1) >= widthThreshold &&
                    Math.Round(p.Height, 1) >= heightThreshold).ToList();

                var crPeaks = significantPeaksList.Where(p => p.Top.X >= _sapphireAnalysisSettings.CR_SATURATION_START_WAVELEN - 1 &&
                   p.Top.X <= _sapphireAnalysisSettings.CR_SATURATION_END_WAVELEN + 1).ToList();

                return crPeaks.Count == 2;

            }
            catch
            {

            }

            return false;
        }

        public bool CheckSpectraSaturatedOrNone(out SAPPHIRE_ANALYZER_RESULT result)
        {
            bool crPeaksPresent = false;
            bool saturated = true;
            result = SAPPHIRE_ANALYZER_RESULT.ERROR;

            try
            {

                for (int i = 0; i < _spectra.Count; i++)
                {
                    if (!crPeaksPresent)
                        crPeaksPresent = CheckForCrPeaks(_spectra[i]);

                    if (saturated)
                        saturated = _spectra[i].IsSaturated(_sapphireAnalysisSettings.CR_SATURATION_START_WAVELEN,
                            _sapphireAnalysisSettings.CR_SATURATION_END_WAVELEN);
                }

                if (!crPeaksPresent)
                {
                    result = SAPPHIRE_ANALYZER_RESULT.NONE;
                    return true;
                }

                if (saturated)
                {
                    result = SAPPHIRE_ANALYZER_RESULT.SATURATED;
                    return true;
                }

                
            }
            catch(Exception ex)
            {

            }

            return false;

        }

        double GetYValue(List<Point> pts, double xVal)
        {
            var y = pts.Where(p => Math.Round(p.X) == Math.Round(xVal)).ToList();
            if (y.Count == 1)
                return y[0].Y;

            var ptLow = pts.Where(p => Math.Round(p.X) < Math.Round(xVal)).Last();
            var ptHigh = pts.Where(p => Math.Round(p.X) > Math.Round(xVal)).First();
            var slope = (ptHigh.Y-ptLow.Y) / (ptHigh.X-ptLow.X);
            var yDiff = slope * (xVal - ptLow.X);

            return ptLow.Y + yDiff;
        }

        public SAPPHIRE_ANALYZER_RESULT TestNatural(out bool crPeaksPresentInAtLeaseOneSpectra,
            out bool noisyInAtLeastOneSpectra, out bool tiBandPresentInAllSpectra, out bool? crSaturated, 
            out bool? _620_750_pass, out List<double> _620_750_ratios,
            out List<double> tiSlopeList )
        {
            SAPPHIRE_ANALYZER_RESULT result = SAPPHIRE_ANALYZER_RESULT.REFER;
            crSaturated = null;
            tiBandPresentInAllSpectra = true;
            tiSlopeList = new List<double>();
            noisyInAtLeastOneSpectra = false;
            crPeaksPresentInAtLeaseOneSpectra = false;
            _620_750_pass = null;
            _620_750_ratios = new List<double>();

            try
            {
                for (int i = 0; i < _spectra.Count; i++)
                {
                    bool crPeaksPresentInThisSpectra = CheckForCrPeaks(_spectra[i]);
                    if (!crPeaksPresentInAtLeaseOneSpectra)
                        crPeaksPresentInAtLeaseOneSpectra = crPeaksPresentInThisSpectra;

                    var line = new StraightLine(_spectra[i]);
                    double tiSlope; bool badfit;
                    var straightLine = line.Test(_sapphireAnalysisSettings.TI_BAND_START_WAVELEN,
                        _sapphireAnalysisSettings.TI_BAND_END_WAVELEN,
                        Math.Tan(_sapphireAnalysisSettings.TI_BAND_MAX_SLOPE_DEGREES * Math.PI / 180),
                        Math.Tan(_sapphireAnalysisSettings.TI_BAND_MIN_SLOPE_DEGREES * Math.PI / 180),
                        _sapphireAnalysisSettings.TI_BAND_MAX_STD_ERROR,
                        out badfit, out tiSlope);
                    if (!badfit && straightLine)
                    {
                        tiBandPresentInAllSpectra = false;
                        if (crPeaksPresentInThisSpectra)//No Ti Band + Cr Peaks
                            return SAPPHIRE_ANALYZER_RESULT.NATURAL_SAPPHIRE;
                        
                    }
                    else if (badfit)
                        noisyInAtLeastOneSpectra = true;

                    tiSlopeList.Add(Math.Atan(tiSlope)*180/Math.PI);

                    

                    _620_750_ratios.Add(GetYValue(_spectra[i].SpectrumData, _sapphireAnalysisSettings._620_WAVELEN) /
                                        GetYValue(_spectra[i].SpectrumData, _sapphireAnalysisSettings._750_WAVELEN));

                }

                if (!crPeaksPresentInAtLeaseOneSpectra)
                    return SAPPHIRE_ANALYZER_RESULT.REFER;

                if (tiBandPresentInAllSpectra)
                {
                    int _500msIndex = _integrationTimes.FindIndex(d => d == 500);
                    if (_500msIndex == -1)
                        _500msIndex = 0;

                    crSaturated = _spectra[_500msIndex].IsSaturated(_sapphireAnalysisSettings.CR_SATURATION_START_WAVELEN,
                        _sapphireAnalysisSettings.CR_SATURATION_END_WAVELEN);

                    if (crSaturated == false)
                        result = SAPPHIRE_ANALYZER_RESULT.NATURAL_SAPPHIRE;
                    else
                    {
                        _620_750_pass = _620_750_ratios.Count(r => r > _sapphireAnalysisSettings._620_750_RATIO) 
                            == _620_750_ratios.Count;

                        if (_620_750_pass == true)
                            result = SAPPHIRE_ANALYZER_RESULT.NATURAL_SAPPHIRE;
                    }
                }
            }
            catch(Exception ex)
            {
                result = SAPPHIRE_ANALYZER_RESULT.ERROR;
            }

            return result;
        }
    }
}
