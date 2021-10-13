// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;

namespace K9.Services.Google
{
    public static class GoogleUtil
    {
        public static GoogleCredential GetCredentials(string TokenPath, string[] Scopes)
        {
            GoogleCredential credential;
            if (!File.Exists(TokenPath))
            {
                Log.WriteLine("Google credentials NOT FOUND.", "GOOGLE");
                return null;
            }

            using (FileStream stream = new(TokenPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            if (credential != null)
            {
                return credential;
            }

            Log.WriteLine("Google credentials FAILED.", "GOOGLE");
            return null;
        }

        public static SheetsService GetSheetsService(this GoogleCredential Credential, string ApplicationName)
        {
            SheetsService service = new(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential, ApplicationName = ApplicationName
            });

            return service;
        }
    }
}