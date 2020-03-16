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
using System.Windows;

namespace PeakFinder
{
    public class Peak
    {
        Point _start;
        Point _end;
        Point _highestPoint;
        Point _halfMaxStart;
        Point _halfMaxEnd;

        public Peak() { }

        public Point Start { get { return _start; } set { _start = value; } }
        public Point Top { get { return _highestPoint; } set { _highestPoint = value; } }
        public Point End { get { return _end; } set { _end = value; } }
        public Point HalfMaxStart { get { return _halfMaxStart; } set { _halfMaxStart = value; } }
        public Point HalfMaxEnd { get { return _halfMaxEnd; } set { _halfMaxEnd = value; } }
        public double Height { get { return _highestPoint.Y - (_start.Y >= _end.Y ? _start.Y : _end.Y); } }
        public double HalfMaximum { get { return Top.Y - (Height/2); } }
        public double Width
        {
            get
            {
                return _end.X - _highestPoint.X > _highestPoint.X - _start.X ?
                    2 * (_highestPoint.X - _start.X) : 2 * (_end.X - _highestPoint.X);
            }
        }
               

        public double FullWidthHalfMax
        {
            get
            {
                return _halfMaxEnd.X - _halfMaxStart.X;
            }
        }

    }

}
