// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Text;

namespace K9.Unity.TestRunner
{
    [Serializable]
    public class RunDefinition
    {
        public string Filters;
        public string Platform;
        public string Categories;
        public bool HaltOnCrash;
        public bool HaltOnFail;
        public bool CacheServerDownload = true;
        public bool CacheServerUpload;

        public string ToArgumentString()
        {
            StringBuilder arguments = new ();

            if (!string.IsNullOrEmpty(Filters) && Filters != "*")
            {
                arguments.AppendFormat("-testFilter {0} ", Filters);
            }
            if (!string.IsNullOrEmpty(Categories) && Categories != "*")
            {
                arguments.AppendFormat("-testCategory {0} ", Categories);
            }
            arguments.AppendFormat("-testPlatform {0} ", Platform);

            arguments.Append(CacheServerDownload
                ? "-cacheServerEnableDownload true "
                : "-cacheServerEnableDownload false ");

            arguments.Append(CacheServerUpload
                ? "-cacheServerEnableUpload true "
                : "-cacheServerEnableUpload false ");

            return arguments.ToString();
        }
    }
}