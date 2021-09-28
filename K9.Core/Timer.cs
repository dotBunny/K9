// Copyright (c) 2018-2021 dotBunny Inc.

using System.Diagnostics;

namespace K9
{
    public class Timer
    {
        private readonly Stopwatch _stopwatch;
        public Timer()
        {
            _stopwatch = new();
            _stopwatch.Start();
        }

        public void Reset(bool restart = true)
        {
            _stopwatch.Reset();
            if (restart)
            {
                _stopwatch.Start();
            }
        }

        public long GetElapsedMilliseconds()
        {
            _stopwatch.Stop();
            return _stopwatch.ElapsedMilliseconds;
        }

        public long GetElapsedSeconds()
        {
            return GetElapsedMilliseconds() / 1000;
        }

        public string TransferRate(long byteCount)
        {
            float speed = (byteCount / 125000) / (_stopwatch.ElapsedMilliseconds / 1000);
            return $"{System.Math.Round(speed, 2)} Mbps";
        }
    }
}