﻿// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using CommandLine;
using K9.Utils;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;
using File = System.IO.File;

namespace K9.TeamCity.Verbs
{
    [Verb("BuildChangelist")]
    public class BuildChangelist : IVerb
    {
        private BuildChangelistMarkdown _markdown;

        [Option('h', "host", Required = false, HelpText = "TeamCity Host:Port", Default = "dotbunny.dyndns.org:2018")]
        public string Host { get; set; }

        [Option('u', "username", Required = false, HelpText = "TeamCity Username")]
        public string Username { get; set; }

        [Option('p', "password", Required = false, HelpText = "TeamCity Password")]
        public string Password { get; set; }

        [Option('t', "token", Required = false, HelpText = "TeamCity Token")]
        public string Token { get; set; }

        [Option('b', "build", Required = false, HelpText = "Target Build ID")]
        public string BuildID { get; set; }

        [Option('c', "count", Required = false, Default = 0, HelpText = "How many builds back should the history go?")]
        public int History { get; set; }

        [Option('f', "full", Required = false, HelpText = "Where to output full processed report.")]
        public string FullPath { get; set; }

        [Option('m', "mini", Required = false, HelpText = "Where to output mini processed report.")]
        public string MiniPath { get; set; }

        public bool CanExecute()
        {
            if (!string.IsNullOrEmpty(Token))
            {
                return true;
            }

            if (string.IsNullOrEmpty(Username))
            {
                return false;
            }

            if (string.IsNullOrEmpty(Password))
            {
                return false;
            }

            return true;
        }

        public bool Execute()
        {
            TeamCityClient client = new(Host);
            if (!string.IsNullOrEmpty(Token))
            {
                client.ConnectWithAccessToken(Token);
            }
            else
            {
                client.Connect(Username, Password);
            }


            if (!string.IsNullOrEmpty(BuildID))
            {
                Build targetBuild = client.Builds.ById(BuildID);
                _markdown = new BuildChangelistMarkdown(client, targetBuild, History);
            }

            Log.Write(_markdown.ToString());

            if (!string.IsNullOrEmpty(FullPath))
            {
                string outputPath = Path.GetFullPath(FullPath.FixDirectorySeparator());
                outputPath.MakeWritable();
                File.WriteAllText(outputPath, _markdown.GetFullReport());
            }

            if (!string.IsNullOrEmpty(MiniPath))
            {
                string outputPath = Path.GetFullPath(MiniPath.FixDirectorySeparator());
                outputPath.MakeWritable();
                File.WriteAllText(outputPath, _markdown.GetMiniReport());
            }

            return true;
        }
    }
}