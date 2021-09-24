using System.Collections.Generic;

namespace K9.Services.Perforce
{
    public class DescribeRecord
    {
        public int ChangeNumber;
        public string ChangeType;
        public string Client;
        public string Description;
        public List<DescribeFileRecord> Files = new();
        public string Path;
        public string Status;
        public long Time;
        public string User;

        public DescribeRecord(Dictionary<string, string> Tags)
        {
            string ChangeString;
            if (!Tags.TryGetValue("change", out ChangeString) || !int.TryParse(ChangeString, out ChangeNumber))
            {
                ChangeNumber = -1;
            }

            Tags.TryGetValue("user", out User);
            Tags.TryGetValue("client", out Client);

            string TimeString;
            if (!Tags.TryGetValue("time", out TimeString) || !long.TryParse(TimeString, out Time))
            {
                Time = -1;
            }

            Tags.TryGetValue("desc", out Description);
            Tags.TryGetValue("status", out Status);
            Tags.TryGetValue("changeType", out ChangeType);
            Tags.TryGetValue("path", out Path);

            for (int Idx = 0;; Idx++)
            {
                string Suffix = string.Format("{0}", Idx);

                DescribeFileRecord File = new DescribeFileRecord();
                if (!Tags.TryGetValue("depotFile" + Suffix, out File.DepotFile))
                {
                    break;
                }

                Tags.TryGetValue("action" + Suffix, out File.Action);
                Tags.TryGetValue("type" + Suffix, out File.Type);

                string RevisionString;
                if (!Tags.TryGetValue("rev" + Suffix, out RevisionString) ||
                    !int.TryParse(RevisionString, out File.Revision))
                {
                    File.Revision = -1;
                }

                string FileSizeString;
                if (!Tags.TryGetValue("fileSize" + Suffix, out FileSizeString) ||
                    !int.TryParse(FileSizeString, out File.FileSize))
                {
                    File.FileSize = -1;
                }

                Tags.TryGetValue("digest" + Suffix, out File.Digest);
                Files.Add(File);
            }
        }
    }
}