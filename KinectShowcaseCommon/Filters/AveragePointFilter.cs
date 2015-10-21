using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcaseCommon.Filters
{
    public class AveragePointFilter : PointFilter
    {
        private AverageValueFilter _xFilter, _yFilter;

        public AveragePointFilter(int aBufferSize)
        {
            _xFilter = new AverageValueFilter(aBufferSize);
            _yFilter = new AverageValueFilter(aBufferSize);
        }

        override public Point Next(Point aCurrent)
        {
            double x = _xFilter.Next(aCurrent.X);
            double y = _xFilter.Next(aCurrent.Y);
            Point result = new Point(x, y);
            this.Last = result;
            return result;
        }
    }
}
