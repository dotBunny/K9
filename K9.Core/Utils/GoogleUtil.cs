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

            using (var stream = new FileStream(TokenPath, FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream).CreateScoped(Scopes);
            }

            if (credential != null) return credential;

            Log.WriteLine("Google credentials FAILED.", "GOOGLE");
            return null;
        }

        public static SheetsService GetSheetsService(this GoogleCredential Credential, string ApplicationName)
        {
            var service = new SheetsService(new BaseClientService.Initializer
            {
                HttpClientInitializer = Credential,
                ApplicationName = ApplicationName
            });

            return service;
        }
    }
}