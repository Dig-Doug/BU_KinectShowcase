using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KinectShowcaseCommon.Filters
{
    public class AverageValueFilter : ValueFilter
    {
        private double[] _buffer;
        private int _bufferIndex = 0;

        public AverageValueFilter(int aBufferSize)
        {
            _buffer = new double[aBufferSize];
        }

        override public double Next(double aCurrent)
        {
            AddToBuffer(aCurrent);
            double result = GetAverageOfBuffer();
            this.Last = result;
            return result;
        }

        private void AddToBuffer(double aValue)
        {
            _buffer[_bufferIndex] = aValue;
            _bufferIndex++;
            if (_bufferIndex >= _buffer.Length)
            {
                _bufferIndex = 0;
            }
        }

        private double GetAverageOfBuffer()
        {
            double average = 0;
            for (int i = 0; i < _buffer.Length; i++)
            {
                average += _buffer[i];
            }
            average /= _buffer.Length;
            return average;
        }

        public override void Set(double aVal)
        {
            base.Set(aVal);
            for (int i = 0; i < _buffer.Length; i++)
                _buffer[i] = aVal;
            _bufferIndex = 0;
        }

    }
}