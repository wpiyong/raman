using MathNet.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using PeakFinder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _405Analyzer
{
    public class Parabola
    {
        List<Point> curve = null;
        
        public double A { get; private set; }
        public double B { get; private set; }
        public double C { get; private set; }

        public Parabola(List<Point> points)
        {
            curve = points;
        }

        public bool Fit()
        {
            try
            {
                //Ax*x + Bx + C
                var Y = new DenseVector(new []
                {
                    curve[0].Y,
                    curve[1].Y,
                    curve[2].Y
                });

                var X = DenseMatrix.OfArray(new double[,]
                    {
                        { curve[0].X*curve[0].X, curve[0].X, 1 },
                        { curve[1].X*curve[1].X, curve[1].X, 1},
                        { curve[2].X*curve[2].X, curve[2].X, 1}
                    });

                var result = X.LU().Solve(Y);

                A = result.At(0);
                B = result.At(1);
                C = result.At(2);

                return true;
            }
            catch
            {

            }

            return false;
        }

    }

    public class Quadratic
    {
        Spectrum _spectrum;
        const string _shape = "0,0.01448523,0.044401084,0.113840321,0.226467652,0.39041535,0.626999595,0.937757976,1.311179113,1.743431967,2.213457616,2.697898363,3.203395495,3.739322807,4.316742224,4.91852973,5.551490086,6.202052449,6.859954891,7.496694416,8.131880752,8.736460371,9.334164842,9.889182418,10.44002349,10.97129644,11.48952308,12.00185376,12.51358964,13.00921698,13.4971797,13.99363303,14.48431773,14.97767241,15.48220075,15.99352637,16.53230819,17.07288987,17.64423934,18.22905856,18.83838082,19.47615927,20.13556557,20.82478899,21.54916709,22.31585808,23.11081302,23.95021057,24.82559133,25.73222826,26.6707835,27.63185485,28.61087633,29.60559597,30.60559597,31.59354179,32.5559274,33.48818415,34.38468369,35.26660974,36.13705826,36.99915124,37.86152729,38.73878591,39.61191228,40.48800996,41.36873868,42.26968485,43.17992416,44.08277673,44.97472323,45.87948485,46.77730638,47.6727514,48.56423551,49.4624337,50.36117216,51.27088681,52.19562695,53.12268654,54.06811946,55.01690303";
        List<double> Y1_sum;

        public Quadratic(Spectrum spec)
        {
            _spectrum = new Spectrum(spec.SpectrumData);
            Y1_sum = _shape.Split(',').Select(double.Parse).ToList();
        }

        public bool CheckDiamondCurve()
        {
            try
            {
                //normalize 
                var curve = _spectrum.SpectrumData.Where(p => p.X >= 415 && p.X <= 500).ToList();
                var x = curve.Select(p => p.X).ToArray();
                var y = curve.Select(p => p.Y).ToArray();
                var minY = y.Min();
                var maxY = y.Max();
                y = y.Select(p => (p - minY) / (maxY - minY)).ToArray();
                curve = x.Zip(y, (xp, yp) => new System.Windows.Point(xp, yp)).ToList();

                //fit quadratic function
                var xq = curve.Select(p => p.X).ToArray();
                var yq = curve.Select(p => p.Y).ToArray();
                var p2 = Fit.Polynomial(xq, yq, 2);
                //y = p2[2]x^2 + p2[1]x + p2[0]

                if ((p2[2] >= -0.00055) && (p2[2] <= -0.0003) &&
                      (p2[1] >= 0.25) && (p2[1] <= 0.5) &&
                      (p2[0] >= -110) && (p2[0] <= -60))
                    return true;

                //normalize 
                var curve1 = _spectrum.SpectrumData.Where(p => p.X >= 423 && p.X <= 447).ToList();
                var y2 = curve1.Select(p => p.Y).ToList();
                var minY2 = y2.Min();
                var maxY2 = y2.Max();
                y2 = y2.Select(p => (p - minY2) / (maxY2 - minY2)).ToList();
                List<double> Y2_sum = new List<double>();
                Y2_sum.Add(y2.First());
                for (int i = 1; i < y2.Count; i++)
                {
                    Y2_sum.Add(Y2_sum[i - 1] + y2[i]);
                }
                var Y2_diff = Y1_sum.Zip(Y2_sum, (y_1, y_2) => Math.Abs(y_1 - y_2)).ToList();
                var dissimilarity = Y2_diff.Max();
                if (dissimilarity < 10)
                    return true;


                return false;
            }
            catch
            {

            }

            return true;
        }
    }
}
