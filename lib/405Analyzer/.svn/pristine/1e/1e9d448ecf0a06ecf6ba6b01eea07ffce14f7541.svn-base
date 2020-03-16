using MathNet.Numerics;
using MathNet.Numerics.Distributions;
using PeakFinder;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _405Analyzer
{
    class RedFluorescence
    {
        Spectrum _spectrum;

        public RedFluorescence(Spectrum spec)
        {
            _spectrum = new Spectrum(spec.SpectrumData);
        }

        public bool Exists1()
        {
            try
            {
                var y450 = _spectrum.SpectrumData.First(p => p.X >= 450).Y;
                var y920 = _spectrum.SpectrumData.First(p => p.X >= 920).Y;

                return y920 > y450;
            }
            catch
            {

            }

            return true;
        }

        public bool Exists2()
        {
            try
            {
                var curve = _spectrum.SpectrumData.Where(p => p.X >= 700 && p.X <= 900).ToList();
                var x = curve.Select(p => p.X).ToArray();
                var y = curve.Select(p => p.Y).ToArray();
                var minY = y.Min();
                var maxY = y.Max();
                y = y.Select(p => (p - minY) / (maxY - minY)).ToArray();

                //fit cubic function
                var p3 = Fit.Polynomial(x, y, 3);
                //y = p3[3]x^3 + p3[2]x^2 + p3[1]x + p3[0]
                var pointOfInflectionX = (-1 * p3[2]) / (3 * p3[3]);
                var pointOfInflectionY = Polynomial.Evaluate(pointOfInflectionX, p3);
                var slopeAtInflectionPoint = 3*p3[3]*pointOfInflectionX*pointOfInflectionX 
                                                   + 2+p3[2]*pointOfInflectionX + p3[1];

                /*
                Debug.WriteLine("p3");
                foreach (var d in p3)
                    Debug.WriteLine(d);
                Debug.WriteLine(pointOfInflection + "," + Polynomial.Evaluate(pointOfInflection, p3));
                Debug.WriteLine(slopeAtInflectionPoint);
                for (int i = 0; i < x.Length; i++)
                {
                    String s = String.Format("{0},{1},{2}", x[i], y[i],
                        Polynomial.Evaluate(x[i], p3));
                    Debug.WriteLine(s);
                }
                */

                bool rule1 = p3[3] > 0 && p3[2] < 0 && p3[1] > 0 && p3[0] < 0;
                if (!rule1)
                    return false;//wrong shape

                bool rule2 = pointOfInflectionX > 800 && pointOfInflectionX < 850 && pointOfInflectionY > 0.5;
                if (!rule2)
                    return false;//wrong position

                bool rule3 = slopeAtInflectionPoint > 2 && slopeAtInflectionPoint < 4;
                if (!rule3)
                    return false;//wrong curve

                return true;
            }
            catch
            {

            }

            return true;
        }

        public bool Exists3()
        {
            try
            {
                var curve = _spectrum.SpectrumData.Where(p => p.X >= 560 && p.X <= 740).ToList();
                var x = curve.Select(p => p.X).ToArray();
                var y = curve.Select(p => p.Y).ToArray();
                var minY = y.Min();
                var maxY = y.Max();
                y = y.Select(p => (p - minY) / (maxY - minY)).ToArray();
                curve = x.Zip(y, (xp,yp) => new System.Windows.Point(xp,yp)).ToList();

                //fit quadratic function betweek 560 and 612
                var curve1 = curve.Where(p => p.X >= 560 && p.X <= 612).ToList();
                var xq = curve1.Select(p => p.X).ToArray();
                var yq = curve1.Select(p => p.Y).ToArray();
                var p2 = Fit.Polynomial(xq,yq, 2);
                //y = p2[2]x^2 + p2[1]x + p2[0]
                var highestPoint = curve1.Where(p => p.Y == yq.Max()).First();

                //fit linear function between 560 and 590
                var curve1a = curve.Where(p => p.X >= 560 && p.X <= 590).ToList();
                var xData = curve1a.Select(p => p.X).ToArray();
                var yData = curve1a.Select(p => p.Y).ToArray();
                var linear = Fit.Line(xData, yData);
                double intercept = linear.Item1;
                double slope = linear.Item2;
                var goodnessOfFit = GoodnessOfFit.RSquared(xData.Select(d => slope * d + intercept), yData);

                bool rule1 = (p2[2] < -0.00001) && (highestPoint.X > 580 && highestPoint.X < 610) && 
                    (slope > 0) && (goodnessOfFit > 0.85);
                if (!rule1)
                {
                    //fit quadratic function betweek 605 and 620
                    var curve1b = curve.Where(p => p.X >= 605 && p.X <= 620).ToList();
                    xq = curve1b.Select(p => p.X).ToArray();
                    yq = curve1b.Select(p => p.Y).ToArray();
                    p2 = Fit.Polynomial(xq, yq, 2);
                    //y = p2[2]x^2 + p2[1]x + p2[0]
                    var lowestPointX = Math.Round((-1 * p2[1]) / (2 * p2[2]));

                    bool rule1a = (p2[2] > 0.0003) && (lowestPointX > 610) && (lowestPointX < 615);
                    if (!rule1a)
                        return false;
                }
                             

                //fit quadratic function between highestPoints
                var curve2 = curve.Where(p => p.X >= 628 && p.X <= 638).ToList();
                var xs = curve2.Select(p => p.X).ToArray();
                var ys = curve2.Select(p => p.Y).ToArray();
                var highestPoint1 = curve2.Where(p => p.Y == ys.Max()).First();
                curve2 = curve.Where(p => p.X >= 605 && p.X <= highestPoint1.X).ToList();
                xs = curve2.Select(p => p.X).ToArray();
                ys = curve2.Select(p => p.Y).ToArray();
                p2 = Fit.Polynomial(xs, ys, 2);
                //y = p2[2]x^2 + p2[1]x + p2[0]
                var criticalPointX = Math.Round((-1*p2[1])/(2*p2[2]));
                var lowestPoint = curve2.Where(p => p.Y == ys.Min()).First();

                //for (int i = 0; i < xs.Length; i++)
                //{
                //    String s = String.Format("{0},{1},{2}", xs[i], ys[i],
                //        Polynomial.Evaluate(xs[i], p2));
                //    Debug.WriteLine(s);
                //}


                bool rule2 = (p2[2] > 0) && ( (criticalPointX > 609 && criticalPointX < 619) ||
                        (lowestPoint.X >= 611 && lowestPoint.X <= 615) );
                if (!rule2)
                    return false;

                
                //fit linear function between 640 and 740
                var curve3 = curve.Where(p => p.X >= 640 && p.X <= 740).ToList();
                xData = curve3.Select(p => p.X).ToArray();
                yData = curve3.Select(p => p.Y).ToArray();
                linear = Fit.Line(xData,yData);
                intercept = linear.Item1;
                slope = linear.Item2;
                goodnessOfFit = GoodnessOfFit.RSquared(xData.Select(d => slope * d + intercept), yData);

                bool rule3 = (slope > 0) && (goodnessOfFit > 0.9);
                if (!rule3)
                    return false;

                return true;
            }
            catch
            {

            }

            return true;
        }
        
        public bool Exists()
        {
            try
            {
                //normalize 
                var curve = _spectrum.SpectrumData.Where(p => p.X >= 560 && p.X <= 740).ToList();
                var x = curve.Select(p => p.X).ToArray();
                var y = curve.Select(p => p.Y).ToArray();
                var minY = y.Min();
                var maxY = y.Max();
                y = y.Select(p => (p - minY) / (maxY - minY)).ToArray();
                curve = x.Zip(y, (xp, yp) => new System.Windows.Point(xp, yp)).ToList();

                //fit linear function
                var curve1 = curve.Where(p => p.X >= 650 && p.X <= 740).ToList();
                var xData = curve1.Select(p => p.X).ToArray();
                var yData = curve1.Select(p => p.Y).ToArray();
                var linear = Fit.Line(xData, yData);
                var intercept = linear.Item1;
                var slope = linear.Item2;
                var goodnessOfFit = GoodnessOfFit.RSquared(xData.Select(d => slope * d + intercept), yData);

                bool rule1 = (slope > 0.004) && (goodnessOfFit > 0.95);
                if (!rule1)
                    return false;

                

                return true;
            }
            catch
            {

            }

            return true;
        }
        
    }
}
