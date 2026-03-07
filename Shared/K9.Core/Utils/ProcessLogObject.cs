// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;

namespace K9.Core.Utils
{
    public class ProcessLogObject_StringStreamWriter
    {
        private readonly Action<string, StreamWriter> _outputAction;

        public ProcessLogObject_StringStreamWriter(Action<string, StreamWriter> action, StreamWriter streamWriter)
        {
            _outputAction = action;
            _streamWriter = streamWriter;
        }

        private readonly object _lockObject = new object();
        private readonly StreamWriter _streamWriter;

        public void OutputHandler(object x, DataReceivedEventArgs y)
        {
            if (y.Data == null)
            {
                return;
            }

            lock (_lockObject)
            {
                _outputAction(y.Data.TrimEnd(), _streamWriter);
            }
        }
    }

    public class ProcessLogObject_IntegerString
    {
        private readonly Action<int, string> _outputAction;

        public ProcessLogObject_IntegerString(Action<int, string> action)
        {
            _outputAction = action;
        }

        public void SetProcessIdentifier(int processIdentifier)
        {
            lock (_lockObject)
            {
                _processIdentifier = processIdentifier;
            }
        }

        private int _processIdentifier;

        private readonly object _lockObject = new object();

        public void OutputHandler(object x, DataReceivedEventArgs y)
        {
            if (y.Data == null)
            {
                return;
            }

            lock (_lockObject)
            {
                _outputAction(_processIdentifier, y.Data.TrimEnd());
            }
        }
    }
}