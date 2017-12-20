﻿using GitHub.Api;
using GitHub.Models;
using GitHub.Services;
using GitHub.Settings;
using Microsoft.TeamFoundation.Controls;
using System.ComponentModel.Composition;

namespace GitHub.VisualStudio.TeamExplorer.Connect
{
    [TeamExplorerSection(GitHubConnectSection1Id, TeamExplorerPageIds.Connect, 10)]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class GitHubConnectSection1 : GitHubConnectSection
    {
        public const string GitHubConnectSection1Id = "519B47D3-F2A9-4E19-8491-8C9FA25ABE91";

        [ImportingConstructor]
        public GitHubConnectSection1(IGitHubServiceProvider serviceProvider,
            ISimpleApiClientFactory apiFactory,
            ITeamExplorerServiceHolder holder,
            IConnectionManager manager,
            IPackageSettings settings,
            IVSServices vsServices,
            ILocalRepositories localRepositories)
            : base(serviceProvider, apiFactory, holder, manager, settings, vsServices, localRepositories, 1)
        {
        }
    }
}
