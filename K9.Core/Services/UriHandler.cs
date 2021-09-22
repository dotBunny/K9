using System;

namespace K9.Services
{
    public static class UriHandler
    {
        public static IFileAccessor GetFileAccessor(string connectionString)
        {
            var uri = new Uri(connectionString);
            switch (uri.Scheme.ToUpper())
            {
                case "SMB":

                    // Determine address
                    var address = uri.Host;
                    
                    // Need to figure out the share/file path
                    var share = string.Empty;
                    var filePath = string.Empty;
                    if (!string.IsNullOrEmpty(uri.AbsolutePath))
                    {
                        var info = uri.AbsolutePath.Split('\\', 2);
                        share = info[0];
                        filePath = info[1];
                    }
                    
                    // Handle Authentication
                    var username = string.Empty;
                    var password = string.Empty;
                    if (!string.IsNullOrEmpty(uri.UserInfo))
                    {
                        var info = uri.UserInfo.Split(':', 2);
                        username = info[0];
                        password = info[1];
                    }
                    
                    // Send back new object
                    return new SMBFileAccessor(address, username, password, share, filePath);

            }
            return null;
        }
    }
}