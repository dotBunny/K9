// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System;
using K9.IO;
using K9.IO.FileAccessors;

namespace K9
{
    public static class UriHandler
    {
        public static IFileAccessor GetFileAccessor(string connectionString)
        {
            Uri uri = new(connectionString);
            switch (uri.Scheme.ToUpper())
            {
                case "SMB":
                    // Determine address
                    string address = uri.Host;

                    // Need to figure out the share/file path
                    string share = string.Empty;
                    string filePath = string.Empty;
                    if (!string.IsNullOrEmpty(uri.AbsolutePath))
                    {
                        string fullPath = uri.AbsolutePath;
                        if (fullPath.StartsWith('/'))
                        {
                            fullPath = fullPath.Substring(1);
                        }

                        string[] info = fullPath.Split('/', 2);
                        share = info[0];
                        filePath = info[1];
                    }

                    // Handle Authentication
                    string username = string.Empty;
                    string password = string.Empty;
                    if (!string.IsNullOrEmpty(uri.UserInfo))
                    {
                        string[] info = uri.UserInfo.Split(':', 2);
                        username = info[0];
                        password = info[1];
                    }

                    // Send back new object
                    return new SMBFileAccessor(address, username, password, share, filePath);
                default:
                    // Default to a system level file stream
                    return new SystemFileAccessor(connectionString);
            }
        }
    }
}