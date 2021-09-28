// Copyright (c) 2018-2021 dotBunny Inc.

using System;
using System.Globalization;
using K9.Setup;
using SendSafely;

namespace K9.SendSafely
{
    class ProgressCallback : ISendSafelyProgress
    {
        private double _interval;
        private double _nextUpdate;
        public ProgressCallback(double interval = 5d)
        {
            _interval = interval;
            _nextUpdate = -1;
        }
        public void UpdateProgress(string prefix, double progress)
        {

            if (progress > _nextUpdate || progress >= 100d)
            {
                _nextUpdate = progress + _interval;
                Log.WriteLine(
                    $"{prefix} {Math.Round(progress, 2).ToString(CultureInfo.InvariantCulture)}%", Program.Instance.DefaultLogCategory);
            }

        }
    }
}