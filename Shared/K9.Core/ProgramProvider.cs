// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System.Collections.Generic;
using K9.Core.Modules;

namespace K9.Core;

public class ProgramProvider
{
    public virtual void ParseArguments(ArgumentsModule args)
    {

    }

    public virtual bool IsValid(ArgumentsModule args)
    {
        return true;
    }

    public virtual string GetDescription()
    {
        return string.Empty;
    }

    public virtual KeyValuePair<string, string>[] GetArgumentHelp()
    {
        return [];
    }

    public virtual KeyValuePair<string, string>[] GetFlagHelp()
    {
        return [];
    }

    public virtual bool IsHelpOverride()
    {
        return false;
    }
}