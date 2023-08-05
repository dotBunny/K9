// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using CommandLine;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

namespace K9.TeamCity.Verbs
{
    [Verb("SteamCMD")]
    public class SteamCMD : IVerb
    {
        const int LoginAttemptLimit = 3;
        const int EmailRetryCount = 10;

        [Option(longName: "mail-host", Required = false, HelpText = "Hostname of mail server", Default = "imap.gmail.com")]
        public string MailHost {  get ; set; }
        [Option(longName: "mail-port", Required = false, HelpText = "Port of mail server", Default = 993)]
        public int MailPort { get; set; }
        [Option(longName: "mail-username", Required = true, HelpText = "Mail username")]
        public string MailUsername { get; set; }
        [Option(longName: "mail-password", Required = true, HelpText = "Mail password")]
        public string MailPassword { get; set; }

        [Option(longName: "steam-cmd", Required = true, HelpText = "Steam cmd lpath")]
        public string SteamCommand { get; set; }
        [Option(longName: "steam-username", Required = true, HelpText = "Steam username")]
        public string SteamUsername { get; set; }
        [Option(longName: "steam-password", Required = true, HelpText = "Steam password")]
        public string SteamPassword { get; set; }
        [Option(longName: "steam-build", Required = true, HelpText = "Steam app build definition")]
        public string SteamAppBuild{ get; set; }
        [Option(longName: "steam-dir", Required = true, HelpText = "Steam app working directory")]
        public string SteamWorkingDirectory { get; set; }


        public bool CanExecute()
        {
            return true;
        }

        public string GetSteamGuardCode()
        {
            string code = null;
            using (ImapClient imap = new ImapClient())
            {

                Log.WriteLine($"Login to IMAP ...", "STEAM");

                imap.Connect(MailHost, MailPort, true);
                imap.Authenticate(MailUsername, MailPassword);

                IMailFolder inbox = imap.Inbox;
                inbox.Open(FolderAccess.ReadWrite);

                SearchQuery emailQuery = new SearchQuery();
                emailQuery.And(SearchQuery.FromContains("noreply@steampowered.com"));
                emailQuery.And(SearchQuery.SubjectContains("Your Steam account"));
                emailQuery.And(SearchQuery.NotDeleted);

                IList<UniqueId> messages = inbox.Search(emailQuery);
                Log.WriteLine($"Found {messages.Count} viable messages.", "STEAM");
                foreach (UniqueId id in messages)
                {
                    MimeMessage message = inbox.GetMessage(id);
                    string plainMessage = message.GetTextBody(MimeKit.Text.TextFormat.Plain);
                    int loginCodeIndex = plainMessage.IndexOf("Login Code");
                    if (loginCodeIndex != -1)
                    {
                        loginCodeIndex += 10;
                        code = plainMessage.Substring(loginCodeIndex).Trim().Substring(0, 5);

                        // We've found the code, mark it seen
                        inbox.AddFlags(id, MessageFlags.Deleted, true);
                        break;
                    }
                }
                imap.Disconnect(true);
                return code;
            }

        }

        public bool Execute()
        {
            // Login run
            Log.WriteLine("Check Auth", "STEAM");
            string code = null;
            m_LoginState = LoginState.Idle;

            Utils.ProcessUtil.ExecuteProcess(SteamCommand, SteamWorkingDirectory, $"\"+login {SteamUsername} {SteamPassword} +info +quit\"", "quit", HandleLogin);

            // We aren't logged in
            if (m_LoginState != LoginState.OK)
            {
                Log.WriteLine("Waiting 30 seconds ...", "STEAM");
                System.Threading.Thread.Sleep(30000);

                int loginAttempt = 1;
                while (loginAttempt < LoginAttemptLimit)
                {
                    int tryCount = 0;
                    if(string.IsNullOrEmpty(code))
                    {
                        while (tryCount < EmailRetryCount)
                        {
                            Log.WriteLine($"Check for SteamGuard Email #{tryCount} ...", "STEAM");
                            code = GetSteamGuardCode();
                            if (!string.IsNullOrEmpty(code))
                            {
                                Log.WriteLine($"Found Code {code}", "STEAM");
                                break;
                            }
                            else
                            {
                                Log.WriteLine("Waiting 5 seconds ...", "STEAM");
                                System.Threading.Thread.Sleep(5000);
                            }
                            tryCount++;
                        }
                    }

                    if (code != null)
                    {
                        Log.WriteLine($"Login Attempt #{loginAttempt} ...", "STEAM");
                        Utils.ProcessUtil.ExecuteProcess(SteamCommand, SteamWorkingDirectory, $"\"+login {SteamUsername} {SteamPassword} {code} +set_steam_guard_code {code} +quit\"", "quit", HandleLogin);
                        loginAttempt++;

                        if (m_LoginState == LoginState.OK)
                        {
                            break;
                        }
                    }
                }
            }

            if(m_LoginState == LoginState.OK)
            {
                Log.WriteLine("Upload", "STEAM");
                // Execute the actual command
                return K9.Utils.ProcessUtil.ExecuteProcess(SteamCommand, SteamWorkingDirectory, $"\"+login {SteamUsername} {SteamPassword} +run_app_build {SteamAppBuild} +quit\"", "quit", Line =>
                {
                    Log.WriteLine(Line, "STEAM", Log.LogType.ExternalProcess);
                }) == 0;
            }
            return false;
        }

        enum LoginState
        {
            Idle,
            Attempt,
            SteamGuardEmail,
            NeedsAuthentication,
            SteamGuardCode,
            Failed,
            OK
        }

        LoginState m_LoginState;
        void HandleLogin(string logline)
        {
            Log.WriteLine(logline, "STEAM", Log.LogType.ExternalProcess);
            string cleanLine = logline.Trim();

            if (cleanLine.StartsWith($"Logging in user '{SteamUsername}' to Steam Public..."))
            {
                m_LoginState = LoginState.Attempt;
            }
            else if (cleanLine.StartsWith("This computer has not been authenticated for your account using Steam Guard."))
            {
                m_LoginState = LoginState.NeedsAuthentication;
            }
            else if (cleanLine.StartsWith("Please check your email for the message from Steam"))
            {
                m_LoginState = LoginState.SteamGuardEmail;
            }
            else if (cleanLine.StartsWith("Steam Guard code:FAILED"))
            {
                m_LoginState = LoginState.Failed;
            }
            else if (cleanLine.StartsWith("Steam Guard code:"))
            {
                m_LoginState = LoginState.SteamGuardCode;
            }
            else if (cleanLine.StartsWith("Waiting for user info...OK"))
            {
                m_LoginState = LoginState.OK;
            }
        }
    }
}
