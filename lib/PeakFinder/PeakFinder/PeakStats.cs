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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakFinder
{
    public class PeakStats
    {
        List<double> _data;

        public PeakStats(List<double> data)
        {
            _data = data.ToList();
        }

        public double Median(int count)
        {
            if (count == -1)
                count = _data.Count;

            var orderedData = _data.ToList();
            orderedData.Sort();
            if (count % 2 == 0)
                return (orderedData[count / 2] + orderedData[(count / 2) - 1]) / 2.0d;
            else
                return orderedData[count / 2];
        }


        public List<double> OutliersUsingIQR()
        {
            List<double> outliers = null;
            try
            {
                double q1, q2, q3, iqr;
                int count = _data.Count;

                var orderedData = _data.ToList();
                orderedData.Sort();
                if (count % 2 == 0)
                    q2 = (orderedData[count / 2] + orderedData[(count / 2) - 1]) / 2.0d;
                else
                    q2 = orderedData[count / 2];

                q1 = orderedData[count / 4];
                q3 = orderedData[3 * count / 4];
                iqr = q3 - q1;

                outliers = orderedData.Where(d => d > (q3 + 1.5 * iqr)).ToList();

            }
            catch
            {
            }

            return outliers;
        }

        public double Percentile(double value)
        {
            var total = _data.Count();
            var count = _data.Where(v => v < value).Count();
            return (double)count / total; ;
        }

        public List<double> OutliersUsingMAD(double madThreshold = 3.0)
        {
            List<double> outliers = new List<double>();
            try
            {
                double median = Median(_data.Count);
                List<double> listMAD = new List<double>();
                foreach (double h in _data)
                {
                    listMAD.Add(Math.Abs(h - median));
                }
                listMAD.Sort();

                double MAD = listMAD.Count % 2 > 0 ? listMAD[(listMAD.Count / 2)] :
                    (listMAD[(listMAD.Count / 2) - 1] + listMAD[(listMAD.Count / 2)]) / 2.0;

                foreach (var d in _data)
                {
                    double M = MAD > 0 ? Math.Abs((0.6745 * (d - median)) / MAD) : 0;
                    if (M > madThreshold && d > median)
                        outliers.Add(d);
                }
            }
            catch
            {
            }

            return outliers;
        }
    }
}
