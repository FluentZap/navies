using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace Net_Navis
{
    class PerformanceTimer
    {
        [DllImport("KERNEL32")]
        private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

        [DllImport("Kernel32.dll")]
        private static extern bool QueryPerformanceFrequency(out long lpFrequency);

        private long frequency;
        private long start;
        private long stop;
        private double interval;
        private double perMillisecond;

        /// <summary>
        /// Returns difference between stop time and start time in milliseconds.
        /// </summary>
        public double ElapsedTime
        {
            get { return (stop - start) / perMillisecond; }
        }

        public bool HasElapsed
        {
            get { return (stop - start) >= interval; }
        }

        public PerformanceTimer(double fps = 60.0)
        {
            if (QueryPerformanceFrequency(out frequency) == false)
                throw new SystemException("win32Exception. QueryPerformanceFrequency unavailable");
            start = 0;
            stop = 0;
            interval = frequency / fps;
            perMillisecond = frequency / 1000.0;
        }

        public void Start()
        {
            QueryPerformanceCounter(out start);
            stop = start;
        }

        public void Stop()
        {
            QueryPerformanceCounter(out stop);
        }
    }
}
