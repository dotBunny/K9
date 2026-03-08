// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using K9.Core;

namespace K9.Test.CompareImage;

internal static class Program
{
    static void Main()
    {
        using ConsoleApplication framework = new(
            new ConsoleApplicationSettings()
            {
                // ReSharper disable once StringLiteralTypo
                DefaultLogCategory = "TEST.COMPAREIMAGE",
                LogOutputs = [new Core.LogOutputs.ConsoleLogOutput()]
            });

        try
        {
            if (framework.Platform.OperatingSystem != Core.Modules.PlatformModule.PlatformType.Windows)
            {
                Log.WriteLine("Image comparison is not supported on non-Windows platforms.");
                framework.Shutdown(true);
            }

#pragma warning disable CA1416
            CompareImageConfig config = CompareImageConfig.Get(framework);
            if (config.LeftHandSidePath == null || config.RightHandSidePath == null)
            {
                Log.WriteLine("Either path are null, failing.");
                framework.Environment.UpdateExitCode(-1);
                framework.Shutdown(true);
                return;
            }

             // Build LHS Dataset
            Bitmap lhs = new(config.LeftHandSidePath);


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
            Bitmap rhs = new (config.RightHandSidePath);

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
                Log.WriteLine($"Image pixel format must match (LHS {lhs.PixelFormat} vs RHS {rhs.PixelFormat}).");
                framework.Environment.UpdateExitCode(-1);
                framework.Shutdown(true);
                return;

            }

            // Image sizes need to be the same
            if (lhsLength != rhsLength)
            {
                Log.WriteLine($"Image sizes must match (LHS {lhs.Width}x{lhs.Height} vs RHS {rhs.Width}x{rhs.Height}).");
                framework.Environment.UpdateExitCode(-1);
                framework.Shutdown(true);
                return;
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
            if (difference > config.Threshold)
            {
                Log.WriteLine($"FAIL: A difference of {difference}% was found.");
                framework.Environment.UpdateExitCode(-1);
                framework.Shutdown(true);
            }
#pragma warning restore CA1416

            Log.WriteLine($"PASS: A difference of {difference}% was found.");
            framework.Environment.UpdateExitCode(0);
            framework.Shutdown();

        }
        catch (Exception ex)
        {
            framework.ExceptionHandler(ex);
        }
    }
}