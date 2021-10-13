// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using NUnit.Framework;

namespace K9.Tests
{
    public class CoreTests
    {
        public static Program Instance;

        [SetUp]
        public void Setup()
        {
            Instance = new Program();
            Core.Init(Instance);
        }
    }
}