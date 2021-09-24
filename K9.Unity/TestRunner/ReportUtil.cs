using System.Collections.Generic;
using K9.Reports;
using K9.Unity.TestRunner.Report;

namespace K9.Unity.TestRunner
{
    public static class ReportUtil
    {
        public static List<TestCase> GetTestCases(this TestRun inTestRun)
        {
            return inTestRun.TestSuite.GetTestCases();
        }

        public static List<TestCase> GetTestCases(this TestSuite inTestSuite)
        {
            List<TestCase> returnList = new List<TestCase>();

            if (inTestSuite.TestCases != null && inTestSuite.TestCases.Count > 0)
            {
                foreach (TestCase t in inTestSuite.TestCases)
                {
                    if (t != null)
                    {
                        returnList.Add(t);
                    }
                }
            }

            if (inTestSuite.TestSuites == null || inTestSuite.TestSuites.Count <= 0)
            {
                return returnList;
            }

            foreach (TestSuite s in inTestSuite.TestSuites)
            {
                returnList.AddRange(GetTestCases(s));
            }

            return returnList;
        }

        public static List<IResult> GetResults(this List<TestCase> inTestCases)
        {
            int count = inTestCases.Count;
            List<IResult> returnResults = new List<IResult>(count);
            for (int i = 0; i < count; i++)
            {
                if (inTestCases[i] == null)
                {
                    continue;
                }

                returnResults.Add(inTestCases[i].CreateResult());
            }

            return returnResults;
        }
    }
}