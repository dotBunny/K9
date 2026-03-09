// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.Test.CompareImage
{
    public class CompareImageConfig : ProgramConfig
    {
        public string? LeftHandSidePath;
        public string? RightHandSidePath;
        public float Threshold = 50.0f;
        public bool ShouldFailCode = false;

        public override void Parse(ArgumentsModule args)
        {
            base.Parse(args);

            if (args.HasOverrideArgument("LHS"))
            {
                LeftHandSidePath = args.OverrideArguments["LHS"];
            }
            else
            {
                throw new Exception("No LHS image path provided.");
            }

            if (!File.Exists(LeftHandSidePath))
            {
                throw new Exception($"Unable to find LHS path {LeftHandSidePath}.");
            }

            if (args.HasOverrideArgument("RHS"))
            {
                RightHandSidePath = args.OverrideArguments["RHS"];
            }
            else
            {
                throw new Exception("No RHS image path provided.");
            }

            if (!File.Exists(RightHandSidePath))
            {
                throw new Exception($"Unable to find RHS path {RightHandSidePath}.");
            }

            if (args.HasOverrideArgument("THRESHOLD"))
            {
                Threshold = float.Parse(args.OverrideArguments["THRESHOLD"]);
            }

            if (Threshold > 100.0f | Threshold < 0.0f)
            {
                throw new Exception("Threshold must be between 0 and 100.");
            }

            // ReSharper disable StringLiteralTypo
            if (args.HasOverrideArgument("FAILCODE"))
            {
                ShouldFailCode = bool.Parse(args.OverrideArguments["FAILCODE"]);
            }
            // ReSharper restore StringLiteralTypo
        }

    }
}