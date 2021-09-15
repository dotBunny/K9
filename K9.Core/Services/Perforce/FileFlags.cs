using System;

namespace K9.Services.Perforce
{
    [Flags]
    public enum FileFlags
    {
        ModTime = 1,
        AlwaysWritable = 2,
        Executable = 4,
        ExclusiveCheckout = 8
    }
}