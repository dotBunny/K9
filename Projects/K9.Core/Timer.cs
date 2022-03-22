// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

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
            float divisor = _stopwatch.ElapsedMilliseconds / 1000f;
            if (divisor == 0)
            {
                return string.Empty;
            }

            float speed = (byteCount / 125000f) / divisor;
            return $"{System.Math.Round(speed, 2)} Mbps";
        }
    }
}