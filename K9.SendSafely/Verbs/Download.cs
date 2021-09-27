// Copyright (c) 2018-2021 dotBunny Inc.

using CommandLine;
using K9.SendSafely;

namespace K9.Setup.Verbs
{
    public class Download : DefaultOptions
    {
        [Option('t', "target", Required = true, HelpText = "Target ID")]
        public string FileID { get; set; }

        public bool CanExecute()
        {
            return true;
        }

        public bool Execute()
        {
            return true;
        }
    }
}