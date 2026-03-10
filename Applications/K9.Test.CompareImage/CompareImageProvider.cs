// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using System.IO;
using K9.Core;
using K9.Core.Modules;

namespace K9.Test.CompareImage
{
    public class CompareImageProvider : ProgramProvider
    {
        public string? LeftHandSidePath;
        public string? RightHandSidePath;
        public float Threshold = 50.0f;
        public bool ShouldFailCode;

        public override string GetDescription()
        {
            return "A tool to compare two images and fail if they are different.";
        }
        public override KeyValuePair<string, string>[] GetArgumentHelp()
        {
            KeyValuePair<string, string>[] lines = new KeyValuePair<string, string>[4];

            lines[0] = new KeyValuePair<string, string>("LHS", "The absolute path of the left hand side image for comparison.");
            lines[1] = new KeyValuePair<string, string>("RHS", "The absolute path of the right hand side image for comparison.");
            lines[2] = new KeyValuePair<string, string>("THRESHOLD", "The percentage difference when exceeded triggers a failure.\n\t\t(Optional: 50.0f)");
            // ReSharper disable once StringLiteralTypo
            lines[3] = new KeyValuePair<string, string>("FAILCODE", "Should the failure code be passed back to the programs exit code.\n\t\t(Optional: false)");

            return lines;
        }

        public override bool IsValid(ArgumentsModule args)
        {
            // LHS
            if (args.HasOverrideArgument("LHS"))
            {
                if (!File.Exists(args.GetOverrideArgument("LHS")))
                {
                    Log.WriteLine($"Unable to find LHS path {args.GetOverrideArgument("LHS")}.");
                    return false;
                }
            }
            else
            {
                Log.WriteLine("No LHS image path provided.");
                return false;
            }

            // RHS
            if (args.HasOverrideArgument("RHS"))
            {
                if (!File.Exists(args.GetOverrideArgument("RHS")))
                {
                    Log.WriteLine($"Unable to find RHS path {args.GetOverrideArgument("RHS")}.");
                    return false;
                }
            }
            else
            {
                Log.WriteLine("No RHS image path provided.");
                return false;
            }

            //  THRESHOLD
            if (args.HasOverrideArgument("THRESHOLD"))
            {
                if(float.TryParse(args.GetOverrideArgument("THRESHOLD"), out float threshold))
                {
                    if (Threshold > 100.0f | Threshold < 0.0f)
                    {
                        Log.WriteLine($"THRESHOLD({threshold}) must be between 0 and 100");
                        return false;
                    }
                }
                else
                {
                    Log.WriteLine("THRESHOLD must be a float.");
                    return false;
                }
            }

            // ReSharper disable StringLiteralTypo
            // ReSharper disable once CommentTypo
            // FAILCODE
            if (args.HasOverrideArgument("FAILCODE") && !bool.TryParse(args.GetOverrideArgument("FAILCODE"), out bool _))
            {
                Log.WriteLine("FAILCODE must be able to be parsed to a boolean value.");
                return false;
            }
            // ReSharper restore StringLiteralTypo

            return base.IsValid(args);
        }

        public override void ParseArguments(ArgumentsModule args)
        {
            base.ParseArguments(args);

            LeftHandSidePath = args.GetOverrideArgument("LHS");
            RightHandSidePath = args.GetOverrideArgument("RHS");


            if (args.HasOverrideArgument("THRESHOLD"))
            {
                Threshold = float.Parse(args.GetOverrideArgument("THRESHOLD"));
            }

            // ReSharper disable StringLiteralTypo
            if (args.HasOverrideArgument("FAILCODE"))
            {
                ShouldFailCode = bool.Parse(args.GetOverrideArgument("FAILCODE"));
            }
            // ReSharper restore StringLiteralTypo
        }
    }
}