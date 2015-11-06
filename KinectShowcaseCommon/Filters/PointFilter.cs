using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KinectShowcaseCommon.Filters
{
    public class PointFilter
    {
        public Point Last { get; protected set; }

        private ValueFilter _xFilter, _yFilter;

        public PointFilter(ValueFilter aXFilter, ValueFilter aYFilter)
        {
            _xFilter = aXFilter;
            _yFilter = aYFilter;
        }

        public virtual Point Next(Point aCurrent)
        {
            double x = _xFilter.Next(aCurrent.X);
            double y = _yFilter.Next(aCurrent.Y);
            this.Last = new Point(x, y);
            return this.Last;
        }
    }
}
