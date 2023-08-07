// Copyright (c) 2018-2021 dotBunny Inc.
// dotBunny licenses this file to you under the BSL-1.0 license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using CommandLine;
using DocumentFormat.OpenXml.Math;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using TeamCitySharp.DomainEntities;

namespace K9.TeamCity.Verbs
{
    [Verb("SteamCMD")]
    public class SteamCMD : IVerb
    {        
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
            Log.WriteLine("Check Authentication ...", "STEAM");
            string code = null;
            m_LoginState = LoginState.Idle;

            Utils.ProcessUtil.ExecuteProcess(SteamCommand, SteamWorkingDirectory, $"\"+login {SteamUsername} {SteamPassword} +info +quit\"", "quit", HandleLogin);

            if (IsFatal())
            {
                return false;
            }

            if (m_LoginState == LoginState.SteamGuardCode)
            {
                Log.WriteLine("Waiting 30 seconds ...", "STEAM");
                System.Threading.Thread.Sleep(30000);

                int tryCount = 0;
                if (string.IsNullOrEmpty(code))
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
                            Log.WriteLine("Waiting 30 seconds ...", "STEAM");
                            System.Threading.Thread.Sleep(30000);
                        }
                        tryCount++;
                    }
                }

                if (code != null)
                {
                    Log.WriteLine($"Attempt SteamGuard Authentication ...", "STEAM");
                    Utils.ProcessUtil.ExecuteProcess(SteamCommand, SteamWorkingDirectory, $"\"+login {SteamUsername} {SteamPassword} {code} +set_steam_guard_code {code} +quit\"", "quit", HandleLogin);
                    if (m_LoginState != LoginState.OK)
                    {
                        Log.WriteLine($"SteamGuard Authentication FAILED", "STEAM");
                    }
                }
            }

            if(m_LoginState == LoginState.OK)
            {
                Log.WriteLine("Run App Build ...", "STEAM");
                // Execute the actual command
                return Utils.ProcessUtil.ExecuteProcess(SteamCommand, SteamWorkingDirectory, $"\"+login {SteamUsername} {SteamPassword} +run_app_build {SteamAppBuild} +quit\"", "quit", (ProcessID, Line) =>
                {
                    Log.WriteLine(Line, "STEAM", Log.LogType.ExternalProcess);
                }) == 0;
            }
            return false;
        }

        enum LoginState
        {
            Idle,
            InvalidStoredAuth,
            SteamGuardCode,
            Failed,
            OK
        }

        LoginState m_LoginState;
        void HandleLogin(int processID, string logline)
        {
            Log.WriteLine(logline, $"STEAM ({processID})", Log.LogType.ExternalProcess);
            string cleanLine = logline.Trim();
            if (
                cleanLine.StartsWith("This computer has not been authenticated for your account using Steam Guard.") ||
                cleanLine.StartsWith("Please check your email for the message from Steam") ||
                cleanLine.StartsWith("Steam Guard code:FAILED (Account Logon Denied)"))
            {
                m_LoginState = LoginState.SteamGuardCode;
            }
            else if (cleanLine.StartsWith($"Logging in user '{SteamUsername}' to Steam Public...FAILED (Invalid Login Auth Code)"))
            {
                m_LoginState = LoginState.InvalidStoredAuth;
            }
            else if (cleanLine.StartsWith($"Logging in user '{SteamUsername}' to Steam Public...FAILED(Rate Limit Exceeded)"))
            {
                m_LoginState = LoginState.Failed;
            }
            else if (cleanLine.StartsWith("Waiting for user info...OK"))
            {
                m_LoginState = LoginState.OK;
            }

            if(IsFatal())
            {
                Log.WriteLine("An error occured. Killing process.", "STEAM");
                System.Diagnostics.Process.GetProcessById(processID).Kill();
            }
        }

        bool IsFatal()
        {
            if(m_LoginState == LoginState.InvalidStoredAuth || m_LoginState == LoginState.Failed)
            {
                return true;
            }
            return false;
        }
    }
}
