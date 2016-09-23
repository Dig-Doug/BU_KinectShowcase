using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.Filters
{
    public class HandStateCounter
    {
        public HandState State { get; private set; }
        public int Count { get; private set; }

        public HandStateCounter()
        {

        }

        public void Add(HandState aNext)
        {
            if (State == aNext)
            {
                Count++;
            }
            else
            {
                State = aNext;
                Count = 1;
            }
        }

        public void Reset()
        {
            Count = 0;
            State = HandState.Unknown;
        }

    }
}
