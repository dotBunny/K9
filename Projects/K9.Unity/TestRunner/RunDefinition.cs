// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;

namespace K9.Unity.TestRunner
{
    [Serializable]
    public class RunDefinition
    {
        public string Filters;
        public string Platform;
        public string Categories;
        public bool HaltOnCrash;
        public bool HalfOnFail;
    }
}