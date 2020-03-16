using MathNet.Numerics;
using PeakFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _405Analyzer
{
    class StraightLine
    {
        Spectrum _spectrum;

        public StraightLine(Spectrum spec)
        {
            _spectrum = new Spectrum(spec.SpectrumData);
        }

        public bool Test(double start_wl, double end_wl, double max_slope, double min_slope, double max_error,
            out bool noisyFit, out double slope)
        {
            noisyFit = false;

            //normalize
            var curve = _spectrum.SpectrumData;
            var x = curve.Select(p => p.X).ToArray();
            var y = curve.Select(p => p.Y).ToArray();
            var minY = y.Min();
            var maxY = y.Max();
            y = y.Select(p => (p - minY) / (maxY - minY)).ToArray();
            curve = x.Zip(y, (xp, yp) => new System.Windows.Point(xp, yp)).ToList();

            var roi = curve.Where(p => p.X >= start_wl && p.X <= end_wl).ToList();
            x = roi.Select(p => p.X).ToArray();
            y = roi.Select(p => p.Y).ToArray();

            var linear = Fit.Line(x, y);
            double intercept = linear.Item1;
            double s = linear.Item2;
            var standardError = GoodnessOfFit.PopulationStandardError(x.Select(d => s * d + intercept), y);

            if (Math.Round(standardError,3) > max_error)
                noisyFit = true;

            slope = s;
            bool res = (!noisyFit) && (slope >= min_slope) && (slope <= max_slope);

            return res;
        }
    }
}
