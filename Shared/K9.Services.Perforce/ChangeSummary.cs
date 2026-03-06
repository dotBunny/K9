// Copyright dotBunny Inc. All Rights Reserved.
// See the LICENSE file at the repository root for more information.

using System;

namespace K9.Services.Perforce
{
    public class ChangeSummary
    {
        public string? Client;
        public DateTime Date;
        public string? Description;
        public int Number;
        public string? User;
    }
}