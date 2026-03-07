// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace K9.Core.Utils
{
    public class ProcessLogOutput
    {
        private readonly Action<int, string> _action;

        public ProcessLogOutput()
        {
            _action = (processIdentifier, line) =>
            {
                Log.WriteLine($"[{processIdentifier}] {line}");
            };
        }

        public Action<int, string> GetAction()
        {
            return _action;
        }
    }
}