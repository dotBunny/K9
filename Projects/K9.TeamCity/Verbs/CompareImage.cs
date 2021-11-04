// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace K9.TeamCity.Verbs
{
    [Verb("CompareImage")]
    public class CompareImage : IVerb
    {
        [Option('l', "lhs", Required = true, HelpText = "LHS Image Path")]
        public string LeftHandSidePath { get; set; }

        [Option('r', "rhs", Required = true, HelpText = "RHS Image Path")]
        public string RightHandSidePath { get; set; }

        [Option('t', "threshold", Required = false, Default = 50f, HelpText = "At which point is this considered too far?")]
        public float Threshold { get; set; }

        /// <inheritdoc />
        public bool CanExecute()
        {
            if (!System.IO.File.Exists(LeftHandSidePath))
            {
                Log.WriteLine("A base image is required to compare against (LHS).");
                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public bool Execute()
        {
            if (!System.IO.File.Exists(RightHandSidePath))
            {
                Core.UpdateExitCode(-1);
                Log.WriteLine("RHS Not Found.");
                return false;
            }

            // Build LHS Dataset
            Bitmap lhs = new(LeftHandSidePath);
            Rectangle lhsRect = new (0, 0, lhs.Width, lhs.Height);
            System.Drawing.Imaging.BitmapData lhsData =
                lhs.LockBits(lhsRect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    lhs.PixelFormat);
            IntPtr lhsPtr = lhsData.Scan0;
            int lhsLength  = Math.Abs(lhsData.Stride) * lhs.Height;
            byte[] lhsBytes = new byte[lhsLength];
            Marshal.Copy(lhsPtr, lhsBytes, 0, lhsLength);
            lhs.UnlockBits(lhsData);

            // Build RHS Dataset
            Bitmap rhs = new (RightHandSidePath);
            Rectangle rhsRect = new (0, 0, rhs.Width, rhs.Height);
            System.Drawing.Imaging.BitmapData rhsData =
                rhs.LockBits(rhsRect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    rhs.PixelFormat);
            IntPtr rhsPtr = rhsData.Scan0;
            int rhsLength  = Math.Abs(rhsData.Stride) * rhs.Height;
            byte[] rhsBytes = new byte[rhsLength];
            Marshal.Copy(rhsPtr, rhsBytes, 0, rhsLength);
            rhs.UnlockBits(rhsData);

            // Image format needs to the same
            if (lhs.PixelFormat != rhs.PixelFormat)
            {
                Core.UpdateExitCode(-1);
                Log.WriteLine($"Image pixel format must match (LHS {lhs.PixelFormat} vs RHS {rhs.PixelFormat}).");
                return false;
            }

            // Image sizes need to be the same
            if (lhsLength != rhsLength)
            {
                Core.UpdateExitCode(-1);
                Log.WriteLine($"Image sizes must match (LHS {lhs.Width}x{lhs.Height} vs RHS {rhs.Width}x{rhs.Height}).");
                return false;
            }

            // Compare!
            int differenceCount = 0;
            Parallel.For(0, lhsLength, index =>
            {
                if (lhsBytes[index] != rhsBytes[index])
                {
                    Interlocked.Increment(ref differenceCount);
                }
            });

            float difference = ((float)differenceCount / lhsLength) * 100f;
            if (difference > Threshold)
            {
                Core.UpdateExitCode(-1);
                Log.WriteLine($"FAIL: A difference of {difference}% was found.");
                return false;
            }
            Log.WriteLine($"PASS: A difference of {difference}% was found.");
            return true;
        }
    }
}