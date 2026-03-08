// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;

namespace K9.Test.CompareImage
{
    public class CompareImageConfig
    {
        public string? LeftHandSidePath;
        public string? RightHandSidePath;
        public float Threshold = 50.0f;

        public static CompareImageConfig Get(ConsoleApplication framework)
        {
            CompareImageConfig config = new();

            if (framework.Arguments.HasOverrideArgument("LHS"))
            {
                config.LeftHandSidePath = framework.Arguments.OverrideArguments["LHS"];
            }
            else
            {
                throw new Exception("No LHS image path provided.");
            }

            if (!File.Exists(config.LeftHandSidePath))
            {
                throw new Exception($"Unable to find LHS path {config.LeftHandSidePath}.");
            }

            if (framework.Arguments.HasOverrideArgument("RHS"))
            {
                config.RightHandSidePath = framework.Arguments.OverrideArguments["RHS"];
            }
            else
            {
                throw new Exception("No RHS image path provided.");
            }

            if (!File.Exists(config.RightHandSidePath))
            {
                throw new Exception($"Unable to find RHS path {config.RightHandSidePath}.");
            }

            if (framework.Arguments.HasOverrideArgument("THRESHOLD"))
            {
                config.Threshold = float.Parse(framework.Arguments.OverrideArguments["THRESHOLD"]);
            }

            if (config.Threshold > 100.0f | config.Threshold < 0.0f)
            {
                throw new Exception("Threshold must be between 0 and 100.");
            }

            return config;
        }
    }
}