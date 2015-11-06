using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.Filters
{
    public class ValueFilter
    {
        public double Last { get; protected set; }

        public ValueFilter()
        {

        }

        public virtual double Next(double aCurrent)
        {
            this.Last = aCurrent;
            return aCurrent;
        }
    }
}