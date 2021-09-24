using System;
using K9.Reports;
using K9.Reports.Results;
using K9.Unity.TestRunner.Report;

namespace K9.Unity.TestRunner
{
    public static class ResultFactory
    {
        private static Agent _lastGoodAgent;

        public static IResult CreateResult(this TestCase inTestCase)
        {
            if (inTestCase.IsPerformanceTestCase())
            {
                return CreatePerformanceResult(inTestCase);
            }

            if (inTestCase.IsUnitTestCase())
            {
                return CreateUnitTestResult(inTestCase);
            }

            return new GenericResult();
        }

        public static bool IsPerformanceTestCase(this TestCase inTestCase)
        {
            if (string.IsNullOrEmpty(inTestCase.Output))
            {
                return false;
            }

            return inTestCase.Output.Contains("##performancetestresult");
        }

        public static bool IsUnitTestCase(this TestCase inTestCase)
        {
            return string.IsNullOrEmpty(inTestCase.Output) && !string.IsNullOrEmpty(inTestCase.Result);
        }

        private static PerformanceResult CreatePerformanceResult(TestCase inTestCase)
        {
            PerformanceResult newResult = new();

            newResult.Category = inTestCase.GetCategory();
            newResult.Timestamp = DateTime.Parse(inTestCase.EndTime);
            newResult.FullName = inTestCase.FullName;

            newResult.Runner = new Agent(inTestCase.Output, _lastGoodAgent);

            // Cache Agent (Unity doesnt report values with every test)
            if (newResult.Runner.IsValid())
            {
                _lastGoodAgent = newResult.Runner;
            }


            // Get Result Summary Lines
            int dataStartIndex = inTestCase.Output.IndexOf("##", StringComparison.Ordinal);
            string[] samples = inTestCase.Output.Substring(0, dataStartIndex).Trim().Split("\n");

            foreach (string sample in samples)
            {
                string[] parts = sample.Split("Nanosecond");
                PerformanceResult.PerformanceTestResultSample newSample =
                    new();
                newSample.Name = parts[0].Trim();

                // 0 median | 1 minimum | 2 maximum | 3 average | 4 standard deviation | 5 sample count | 6 sum
                string[] measurements = parts[1].Trim().Replace(": ", ":").Split(' ');

                float.TryParse(measurements[0].Split(':')[1], out float median);
                newSample.Median = median;

                float.TryParse(measurements[1].Split(':')[1], out float minimum);
                newSample.Minimum = minimum;

                float.TryParse(measurements[2].Split(':')[1], out float maximum);
                newSample.Maximum = maximum;

                float.TryParse(measurements[3].Split(':')[1], out float average);
                newSample.Average = average;

                float.TryParse(measurements[4].Split(':')[1], out float standardDeviation);
                newSample.StandardDeviation = standardDeviation;

                int.TryParse(measurements[5].Split(':')[1], out int sampleCount);
                newSample.SampleCount = sampleCount;

                float.TryParse(measurements[6].Split(':')[1], out float sum);
                newSample.Sum = sum;

                newResult.Samples.Add(newSample);
            }

            return newResult;
        }

        private static UnitTestResult CreateUnitTestResult(TestCase inTestCase)
        {
            float.TryParse(inTestCase.Duration, out float duration);

            UnitTestResult newResult = new()
            {
                Timestamp = DateTime.Parse(inTestCase.EndTime),
                Category = inTestCase.GetCategory(),
                FullName = inTestCase.FullName,
                Result = inTestCase.Result,
                Duration = duration
            };

            return newResult;
        }
    }
}