// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CommandLine;
using DocumentFormat.OpenXml.ExtendedProperties;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

namespace K9.TeamCity.Verbs
{
    [Verb("SteamCMD")]
    public class SteamCMD : IVerb
    {
        const int RetryCount = 10;
        const float SleepTime = 5;

        [Option(longName: "mail-host", Required = false, HelpText = "Hostname of mail server", Default = "imap.gmail.com")]
        public string MailHost {  get ; set; }

        [Option(longName: "mail-port", Required = false, HelpText = "Port of mail server", Default = 993)]
        public int MailPort { get; set; }

        [Option(longName: "mail-username", Required = true, HelpText = "Mail username")]
        public string MailUsername { get; set; }
        [Option(longName: "mail-password", Required = true, HelpText = "Mail password")]
        public string MailPassword { get; set; }

        [Option(longName: "steam-username", Required = true, HelpText = "Steam username")]
        public string SteamUsername { get; set; }
        [Option(longName: "steam-password", Required = true, HelpText = "Steam password")]
        public string SteamPassword { get; set; }
        [Option(longName: "steam-build", Required = true, HelpText = "Steam app build definition")]
        public string SteamAppBuild{ get; set; }

        
        public bool CanExecute()
        {
            return true;
        }

        public string GetSteamGuardCode()
        {
            string code = null;
            using (ImapClient imap = new ImapClient())
            {
                imap.Connect(MailHost, MailPort, true);
                imap.Authenticate(MailUsername, MailPassword);

                IMailFolder inbox = imap.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                SearchQuery emailQuery = new SearchQuery();
                emailQuery.And(SearchQuery.FromContains("noreply@steampowered.com"));
                emailQuery.And(SearchQuery.SubjectContains("Your Steam account"));
                emailQuery.And(SearchQuery.NotSeen);

                IList<UniqueId> messages = inbox.Search(emailQuery);
                foreach(UniqueId id in messages)
                {
                    MimeMessage message = inbox.GetMessage(id);
                    string plainMessage = message.GetTextBody(MimeKit.Text.TextFormat.Plain);
                    int loginCodeIndex = plainMessage.IndexOf("Login Code");
                    if (loginCodeIndex != -1)
                    {
                        loginCodeIndex += 10;
                        code = plainMessage.Substring(loginCodeIndex).Trim().Substring(0, 5);

                        // We've found the code, mark it seen
                        inbox.AddFlags(id, MessageFlags.Seen, true);
                        break;
                    }
                }
                imap.Disconnect(true);
                return code;
            }

        }


        public bool Execute()
        {

            K9.Utils.ProcessUtil.InteractiveProcess("cmd", "working", $"+login {SteamUsername} {SteamPassword} +run_app_build {SteamAppBuild} +quit", HandleLog);
            // steamcmd.exe +login dotbunny ******* +run_app_build D:\BuildAgent\work\bf298d0bcffe054d\BuildInfo\Steamworks\AppBuild\NO-Deploy-Steam-Development.vdf +quit
           

            return true;
            //Steam Guard code:
            //Steam Guard code:FAILED (Account Logon Denied)

        }


        void HandleLog(string logline, StreamWriter streamWriter)
        {
            string cleanLine = logline.Trim();
            if(cleanLine.StartsWith("Steam Guard code:FAILED (Account Logon Denied)"))
            {
                throw new System.Exception("Failed to login SteamGuard");
            }
            else if(cleanLine.StartsWith("Steam Guard code:"))
            {
                int tryCount = RetryCount;
                string code = null;
                while(tryCount >= 0)
                {
                    code = GetSteamGuardCode();
                    if(string.IsNullOrEmpty(code))
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                    tryCount--;
                }
                if(!string.IsNullOrEmpty(code))
                {
                    streamWriter.WriteLine(code);
                    return;
                }
            }
        }
    }
}
