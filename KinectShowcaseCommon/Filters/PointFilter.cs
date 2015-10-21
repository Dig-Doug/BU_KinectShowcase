using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.Filters
{
    public class PointFilter
    {
        public Point Last { get; protected set; }

        public PointFilter()
        {

        }

        public virtual Point Next(Point aCurrent)
        {
            this.Last = aCurrent;
            return aCurrent;
        }
    }
}
