// (c) Copyright Jacob Johnston.
// This source is subject to Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using NAudio.Dsp;

namespace Sample_NAudio
{
    public class SampleAggregator
    {
        private float volumeLeftMaxValue;
        private float volumeLeftMinValue;
        private float volumeRightMaxValue;
        private float volumeRightMinValue;

        private int channelDataPosition;
        private long bufferSize;

        public SampleAggregator(int bufferSize)
        {
            this.bufferSize = bufferSize;
        }

        public void Clear()
        {
            volumeLeftMaxValue = float.MinValue;
            volumeRightMaxValue = float.MinValue;
            volumeLeftMinValue = float.MaxValue;
            volumeRightMinValue = float.MaxValue;
            channelDataPosition = 0;
        }
             
        /// <summary>
        /// Add a sample value to the aggregator.
        /// </summary>
        /// <param name="value">The value of the sample.</param>
        public void Add(float leftValue, float rightValue)
        {            
            if (channelDataPosition == 0)
            {
                volumeLeftMaxValue = float.MinValue;
                volumeRightMaxValue = float.MinValue;
                volumeLeftMinValue = float.MaxValue;
                volumeRightMinValue = float.MaxValue;
            }

            channelDataPosition++;            

            volumeLeftMaxValue = Math.Max(volumeLeftMaxValue, leftValue);
            volumeLeftMinValue = Math.Min(volumeLeftMinValue, leftValue);
            volumeRightMaxValue = Math.Max(volumeRightMaxValue, rightValue);
            volumeRightMinValue = Math.Min(volumeRightMinValue, rightValue);

            if (channelDataPosition >= bufferSize)
            {
                channelDataPosition = 0;
            }
        }

      

        public float LeftMaxVolume
        {
            get { return volumeLeftMaxValue; }
        }

        public float LeftMinVolume
        {
            get { return volumeLeftMinValue; }
        }

        public float RightMaxVolume
        {
            get { return volumeRightMaxValue; }
        }

        public float RightMinVolume
        {
            get { return volumeRightMinValue; }
        }        
    }
}
