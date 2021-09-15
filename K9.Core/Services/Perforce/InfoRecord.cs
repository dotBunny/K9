using System;
using System.Collections.Generic;
using System.Linq;

namespace K9.Services.Perforce
{
    public class InfoRecord
    {
        public string ClientAddress;
        public string HostName;
        public TimeSpan ServerTimeZone;
        public string UserName;

        public InfoRecord(Dictionary<string, string> Tags)
        {
            Tags.TryGetValue("userName", out UserName);
            Tags.TryGetValue("clientHost", out HostName);
            Tags.TryGetValue("clientAddress", out ClientAddress);

            string ServerDateTime;
            if (Tags.TryGetValue("serverDate", out ServerDateTime))
            {
                var Fields = ServerDateTime.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                if (Fields.Length >= 3)
                {
                    DateTimeOffset Offset;
                    if (DateTimeOffset.TryParse(string.Join(" ", Fields.Take(3)), out Offset))
                        ServerTimeZone = Offset.Offset;
                }
            }
        }
    }
}