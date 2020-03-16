// 
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



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Threading.Tasks;


namespace PeakFinder
{
    public class Spectrum
    {

        #region spectrum_smoothing
        class Smoother
        {
            double[] window;
            int windowIndex;
            bool loaded;

            public Smoother(int width)
            {
                window = new double[width];
                windowIndex = 0;
                loaded = false;
            }

            public int Offset
            {
                get
                {
                    return (window.Length - 1) / 2;
                }
            }

            public bool Add(double val)
            {
                window[windowIndex] = val;
                if (++windowIndex >= window.Length)
                {
                    windowIndex = 0;
                    loaded = true;
                }

                return loaded;
            }

            public double Average()
            {
                double sum = 0;
                foreach (double d in window)
                    sum += d;

                return sum / window.Length;
            }

        }

        class MedianFilter
        {
            double[] window;
            int windowIndex;
            bool loaded;

            public MedianFilter(int width)
            {
                window = new double[width];
                windowIndex = 0;
                loaded = false;
            }

            public int Offset
            {
                get
                {
                    return (window.Length - 1) / 2;
                }
            }

            public bool Add(double val)
            {
                window[windowIndex] = val;
                if (++windowIndex >= window.Length)
                {
                    windowIndex = 0;
                    loaded = true;
                }

                return loaded;
            }

            public double Median()
            {
                List<double> windowList = window.ToList();
                windowList.Sort();
                return windowList[(window.Length - 1) / 2];
            }

        }
    
    #endregion


        List<Point> spectrumData;
        double saturationThreshold = 65535;
        public double SaturationThreshold { get { return saturationThreshold; } }

        public Spectrum()
        {
            spectrumData = new List<Point>();
        }

        public Spectrum(List<Point> spectrum)
        {
            double saturationRange = 200;
            spectrumData = spectrum.ToList();
            double maxY = spectrumData.Max(p => p.Y);
            int countMaxY = spectrumData.Where(p => Math.Abs(p.Y - maxY) <= saturationRange).Count();
            double percentSat = 100d * countMaxY / spectrumData.Count();
            if (maxY > 63500 && percentSat > 0.05)
                saturationThreshold = maxY - saturationRange;
        }

        public List<Point> SpectrumData
        {
            get
            {
                return spectrumData.ToList();
            }
        }

        public double StdDevIntensities
        {
            get
            {
                double stdDev = 0;
                if (spectrumData.Count > 1)
                {
                    double avg = spectrumData.Select(p => p.Y).Average();
                    double sum = spectrumData.Select(p => p.Y).Sum(s => (s - avg) * (s - avg));
                    stdDev = Math.Sqrt(sum / spectrumData.Count);
                }
                return stdDev;
            }
        }

        public Spectrum SmoothedSpectrum(int width, int passCount = 1)
        {
            var spectrum = SpectrumData;

            int numPasses = 0;
            while (numPasses++ < passCount)
            {
                Smoother sm = new Smoother(width);
                int offset = sm.Offset;
                for (int i = 0; i < spectrum.Count; i++)
                {
                    if (sm.Add(spectrum[i].Y))
                    {
                        spectrum[i - offset] = new Point(spectrum[i-offset].X, sm.Average());
                    }
                }
            }

            return new Spectrum(spectrum) ;
        }

        public Spectrum FilteredSpectrum(int width)
        {
            var spectrum = SpectrumData;

            MedianFilter mf = new MedianFilter(width);
            int offset = mf.Offset;
            for (int i = 0; i < spectrum.Count; i++)
            {
                if (mf.Add(spectrum[i].Y))
                {
                    spectrum[i - offset] = new Point(spectrum[i - offset].X, mf.Median());
                }
            }

            return new Spectrum(spectrum);
        }

        public bool IsSaturated(double startWavelength, double endWavelength, double count = 1)
        {
            return SpectrumData.Where(p => p.X >= startWavelength && p.X <= endWavelength 
                    && p.Y >= saturationThreshold).Count() >= count;
        }

        public List<int> HotPixels(double startWavelength, double endWavelength)
        {
            return Enumerable.Range(0, spectrumData.Count)
                    .Where(i => spectrumData[i].X >= startWavelength && spectrumData[i].X <= endWavelength
                    && spectrumData[i].Y >= saturationThreshold * 0.1)
                    .ToList();
        }

        #region find_peaks

        public List<Peak> FindPeaksByGradient(int numProcessors)
        {
            List <Peak> peakList = new List<Peak>();

            //int numLogicalProcs = Environment.ProcessorCount;
            int numPerGroup = spectrumData.Count / numProcessors;
            int startIndex = 0;                                  

            Task<List<Peak>>[] taskArray = new Task<List<Peak>>[numProcessors];
            for (int i = 0; i < taskArray.Length; i++)
            {
                int endIndex = (i+1)*numPerGroup;
                if (i == taskArray.Length - 1)
                    endIndex = spectrumData.Count;

                taskArray[i] = Task<List<Peak>>.Factory.StartNew((Object p) =>
                    {
                        var data = (dynamic)p;
                        return FindPeaksByGradient(data.si, data.ei);
                    }, new { si = startIndex, ei = endIndex });

                startIndex = endIndex - (numPerGroup / 10);
            }

            for (int i = 0; i < taskArray.Length; i++)
            {
                peakList.AddRange(taskArray[i].Result);
            }
            
            //remove duplicates
            HashSet<double> peakPositions = new HashSet<double>();
            peakList.RemoveAll(x => !peakPositions.Add(x.Top.X));
            

            return peakList;
        }

        enum PeakStage
        {
            Flat,
            Rising,
            Falling
        }

        public List<Peak> FindPeaksByGradient(double startWavelength, double endWavelength)
        {
            int startWavelenIndex = -1, endWavelenIndex = -1;

            for (int i = 0; i < spectrumData.Count; i++)
            {
                if (startWavelenIndex == -1 && spectrumData[i].X >= startWavelength)
                    startWavelenIndex = i;
                if (endWavelenIndex == -1 && spectrumData[i].X > endWavelength)
                {
                    endWavelenIndex = i - 1;
                    break;
                }
            }
            if (endWavelenIndex == -1)
                endWavelenIndex = spectrumData.Count - 1;

            return FindPeaksByGradient(startWavelenIndex, endWavelenIndex); ;

        }


        public List<Peak> FindPeaksByGradient(int startWavelengthIndex, int endWavelengthIndex)
        {
            List<Point> spectrumData = SpectrumData;

            List<Peak> peakList = new List<Peak>();

            if (spectrumData.Count > 0)
            {
                try
                {

                    double[] _intensity = spectrumData.Select(d => d.Y).ToArray();
                    double[] _wavelength = spectrumData.Select(d => d.X).ToArray();

                    Point firstPoint = new Point(_wavelength[startWavelengthIndex], _intensity[startWavelengthIndex]);
                    PeakStage currentPeakStage = PeakStage.Flat;
                    PeakStage nextPeakStage = PeakStage.Flat;
                    Peak p = new Peak();
                    p.Start = p.End = firstPoint;

                    #region findPeaks
                    double flatGradient = Math.Round(Math.Tan((15 * Math.PI) / 180.0), 2);

                    double newGradient = (_intensity[startWavelengthIndex+1] - _intensity[startWavelengthIndex]) /
                                                (_wavelength[startWavelengthIndex+1] - _wavelength[startWavelengthIndex]);

                    p.End = new Point(_wavelength[startWavelengthIndex + 1], _intensity[startWavelengthIndex + 1]);
                    if (Math.Abs(newGradient) <= flatGradient)
                    {
                        currentPeakStage = PeakStage.Flat;
                        p.Start = new Point(_wavelength[startWavelengthIndex + 1], _intensity[startWavelengthIndex + 1]);
                    }
                    else if (newGradient > flatGradient)
                    {
                        currentPeakStage = PeakStage.Rising;
                    }
                    else
                    {
                        currentPeakStage = PeakStage.Falling;
                    }


                    for (int i = startWavelengthIndex + 2; i < endWavelengthIndex; i++)
                    {
                        newGradient = (_intensity[i] - _intensity[i - 1]) /
                                                (_wavelength[i] - _wavelength[i - 1]);

                        if (Math.Abs(newGradient) <= flatGradient)
                        {
                            nextPeakStage = PeakStage.Flat;
                        }
                        else if (newGradient > flatGradient)
                        {
                            nextPeakStage = PeakStage.Rising;
                        }
                        else
                        {
                            nextPeakStage = PeakStage.Falling;
                        }

                        switch (currentPeakStage)
                        {
                            case PeakStage.Flat:
                                p.End = new Point(_wavelength[i], _intensity[i]);
                                switch (nextPeakStage)
                                {
                                    case PeakStage.Flat:
                                        p.Start = new Point(_wavelength[i], _intensity[i]);
                                        break;
                                    case PeakStage.Rising:
                                        currentPeakStage = nextPeakStage;
                                        break;
                                    case PeakStage.Falling:
                                        currentPeakStage = nextPeakStage;
                                        break;
                                }
                                break;
                            case PeakStage.Rising:
                                p.End = new Point(_wavelength[i], _intensity[i]);
                                switch (nextPeakStage)
                                {
                                    case PeakStage.Flat:
                                        break;
                                    case PeakStage.Rising:
                                        break;
                                    case PeakStage.Falling:
                                        currentPeakStage = nextPeakStage;
                                        break;
                                }
                                break;

                            case PeakStage.Falling:
                                switch (nextPeakStage)
                                {
                                    case PeakStage.Flat:
                                        peakList.Add(p);
                                        p = new Peak();
                                        p.Start = new Point(_wavelength[i], _intensity[i]);
                                        p.End = new Point(_wavelength[i], _intensity[i]);
                                        currentPeakStage = nextPeakStage;
                                        break;
                                    case PeakStage.Rising:
                                        peakList.Add(p);
                                        p = new Peak();
                                        p.Start = new Point(_wavelength[i - 1], _intensity[i - 1]);
                                        p.End = new Point(_wavelength[i], _intensity[i]);
                                        currentPeakStage = nextPeakStage;
                                        break;
                                    case PeakStage.Falling:
                                        p.End = new Point(_wavelength[i], _intensity[i]);
                                        break;
                                }
                                break;
                            default:
                                break;
                        }

                    }
                    if (p.Start != p.End)
                        peakList.Add(p);

                    for (int i = 0; i < peakList.Count; i++)
                    {
                        //get max intensity from spectrumData between startpoint and endpoint
                        var maxPeakKvps = from x in spectrumData
                                          where x.X >= peakList[i].Start.X && x.X <= peakList[i].End.X
                                          select x;

                        var maxPeak = maxPeakKvps.First();
                        foreach (Point kvp in maxPeakKvps)
                        {
                            if (kvp.Y > maxPeak.Y)
                                maxPeak = kvp;
                        }

                        peakList[i].Top = new Point(maxPeak.X, maxPeak.Y);
                        peakList[i].HalfMaxStart = peakList[i].HalfMaxEnd = new Point(0,0);

                        foreach (Point kvp in maxPeakKvps)
                        {
                            if (peakList[i].HalfMaxStart.X == 0)
                            {
                                if (kvp.Y >= peakList[i].HalfMaximum)
                                {
                                    peakList[i].HalfMaxStart = new Point(kvp.X, kvp.Y);
                                }
                            }
                            else
                            {
                                if (kvp.Y <= peakList[i].HalfMaximum)
                                {
                                    peakList[i].HalfMaxEnd = new Point(kvp.X, kvp.Y);
                                    break;
                                }
                            }
                        }
                    }
                    #endregion

                }
                catch
                {
                    peakList = new List<Peak>();
                }
            }

            return peakList;
        }
        #endregion
    }
}
