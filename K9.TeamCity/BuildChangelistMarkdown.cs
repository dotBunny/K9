using System;
using System.Collections.Generic;
using System.Text;
using TeamCitySharp;
using TeamCitySharp.DomainEntities;
using TeamCitySharp.Locators;

namespace K9.TeamCity
{
    public class BuildChangelistMarkdown
    {
        private const int MaxResultsPulled = 25;

        private const string HeavyDivider =
            "================================================================================";

        private const string LightDivider =
            "--------------------------------------------------------------------------------";

        private readonly int _backCount;
        private readonly TeamCityClient _client;

        private readonly StringBuilder _fullBuilder = new();

        private readonly List<string> _includedChangelists = new();
        private readonly StringBuilder _miniBuilder = new();
        private readonly Build _targetBuild;
        private readonly int _targetBuildNumber;

        public BuildChangelistMarkdown(TeamCityClient client, Build build, int backCount = 10, bool detailed = false)
        {
            _client = client;
            _targetBuild = build;
            _backCount = backCount;

            if (int.TryParse(_targetBuild.Number, out _targetBuildNumber))
            {
                string header = MarkdownUtil.H1($"{build.BuildTypeId}");
                _fullBuilder.AppendLine(header);
                _miniBuilder.AppendLine(header);

                // Add our base builds information
                AddBuild(_targetBuild);

                if (backCount > 0)
                {
                    List<Build> builds =
                        _client.Builds.ByBuildLocator(
                            BuildLocator.WithDimensions(
                                BuildTypeLocator.WithName(_targetBuild.BuildType.ToString()),
                                maxResults: MaxResultsPulled,
                                running: false));

                    int count = 0;

                    for (int i = 0; i < builds.Count; i++)
                    {
                        if (count == _backCount)
                        {
                            break;
                        }

                        if (int.TryParse(builds[i].Number, out int testBuildNumber))
                        {
                            if (testBuildNumber < _targetBuildNumber)
                            {
                                // Increment our counter to make sure we dont go over our count
                                count++;
                                if (count == 1)
                                {
                                    string pastHeader = MarkdownUtil.H1("Past Builds");
                                    _fullBuilder.AppendLine(pastHeader);
                                    _miniBuilder.AppendLine(pastHeader);
                                }

                                Build moreInfoBuild = client.Builds.ById(builds[i].Id);
                                AddBuild(moreInfoBuild, true);
                            }
                        }
                    }
                }
            }
        }

        public void AddBuild(Build build, bool isHistoric = false)
        {
            List<Change> buildChanges = _client.Changes.ByBuildConfigId($"&locator=build:(id:{build.Id})");

            // If there are no changes we are not going to even bother listing the build
            if (buildChanges == null || buildChanges.Count == 0)
            {
                return;
            }

            // Add Header
            string buildHeader =
                MarkdownUtil.H2(
                    $"{MarkdownUtil.Link($"Build #{build.Number}", build.WebUrl)} on {build.StartDate.ToString(Core.TimeFormat)}");
            _fullBuilder.AppendLine(buildHeader);
            _miniBuilder.AppendLine(buildHeader);

            if (isHistoric)
            {
                TimeSpan duration = build.FinishDate - build.StartDate;
                string completionLine =
                    MarkdownUtil.Italic(
                        $"Completed on {build.FinishDate.ToString(Core.TimeFormat)} in {duration.TotalMinutes} minutes.");
                _fullBuilder.AppendLine(completionLine);
            }

            if (buildChanges != null)
            {
                AddChangeset(buildChanges);
            }
        }

        public void AddChangeset(List<Change> changes)
        {
            foreach (Change c in changes)
            {
                Change change = _client.Changes.ByChangeId(c.Id);
                if (!_includedChangelists.Contains(change.Version))
                {
                    string changeHeader =
                        $"{MarkdownUtil.Link($"Changelist {change.Version}", change.WebUrl)} ({change.Username}) - {MarkdownUtil.Italic(change.Comment)}";
                    _fullBuilder.AppendLine(MarkdownUtil.H3(changeHeader));
                    _miniBuilder.AppendLine(MarkdownUtil.UnorderedList(changeHeader));
                    AddChangelist(change);
                    _includedChangelists.Add(change.Version);
                }
            }
        }

        public void AddChangelist(Change change)
        {
            foreach (File f in change.Files.File)
            {
                _fullBuilder.AppendLine(MarkdownUtil.UnorderedList($"//{f.Relativefile}"));
            }
        }


        public string GetMiniReport()
        {
            return _miniBuilder.ToString();
        }

        public string GetFullReport()
        {
            return _fullBuilder.ToString();
        }

        public override string ToString()
        {
            return GetFullReport();
        }
    }
}