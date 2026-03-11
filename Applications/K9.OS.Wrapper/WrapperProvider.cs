// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using K9.Core;

namespace K9.OS.Wrapper;

public class WrapperProvider : ProgramProvider
{
    public override string GetDescription()
    {
        return "Wrap execution of applications to control error handling and logging.";
    }
}