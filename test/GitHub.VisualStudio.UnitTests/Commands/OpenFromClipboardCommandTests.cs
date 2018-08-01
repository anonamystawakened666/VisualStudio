﻿using System;
using System.Threading.Tasks;
using GitHub.Exports;
using GitHub.Primitives;
using GitHub.Services;
using GitHub.VisualStudio.Commands;
using GitHub.VisualStudio.UI;
using Microsoft.VisualStudio;
using NSubstitute;
using NUnit.Framework;

public class OpenFromClipboardCommandTests
{
    public class TheExecuteMethod
    {
        [Test]
        public async Task NothingInClipboard()
        {
            var vsServices = Substitute.For<IVSServices>();
            vsServices.ShowMessageBoxInfo(null).Returns(VSConstants.MessageBoxResult.IDOK);
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices, contextFromClipboard: null);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(Resources.NoGitHubUrlMessage);
        }

        [Test]
        public async Task NoLocalRepository()
        {
            var context = CreateGitHubContext();
            var repositoryDir = null as string;
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices, contextFromClipboard: context, repositoryDir: repositoryDir);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(Resources.NoActiveRepositoryMessage);
        }

        [Test]
        public async Task UnknownLinkType()
        {
            var context = new GitHubContext { LinkType = LinkType.Unknown };
            var expectMessage = string.Format(Resources.UnknownLinkTypeMessage, context.Url);
            var activeRepositoryDir = "activeRepositoryDir";
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices, contextFromClipboard: context, repositoryDir: activeRepositoryDir);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(expectMessage);
        }
        [Test]
        public  async void  DifferentLocalRepository_DifferentRepositoryMessage()
        {
            await DifferentLocalRepository("targetRepositoryName", "activeRepositoryName", Resources.DifferentRepositoryMessage);
        }

        [TestCase("SameRepositoryName", "SameRepositoryName", null)]
        [TestCase("same_repository_name", "SAME_REPOSITORY_NAME", null)]
        public async Task DifferentLocalRepository(string targetRepositoryName, string activeRepositoryName, string expectMessage)
        {
            var activeRepositoryDir = "activeRepositoryDir";
            var context = CreateGitHubContext(repositoryName: targetRepositoryName);
            var resolveBlobResult = ("commitish", "path", "SHA");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: activeRepositoryDir, repositoryName: activeRepositoryName, resolveBlobResult: resolveBlobResult);

            await target.Execute(null);

            if (expectMessage != null)
            {
                vsServices.Received(1).ShowMessageBoxInfo(string.Format(expectMessage, context.RepositoryName));
            }
            else
            {
                vsServices.DidNotReceiveWithAnyArgs().ShowMessageBoxInfo(null);
            }
        }


        [Test]
        public async void CouldNotResolve_NoResolveDifferentOwnerMessage()
        {
            await CouldNotResolve("TargetOwner", "CurrentOwner", Resources.NoResolveDifferentOwnerMessage);
        }
        [Test]
        public async void CouldNotResolve_NoResolveSameOwnerMessage_SameOwner_SameOwner()
        {
            await CouldNotResolve("SameOwner", "SameOwner", Resources.NoResolveSameOwnerMessage);
        }
        [Test]
        public async void CouldNotResolve_NoResolveSameOwnerMessage_sameowner_SAMEOWNER()
        {
            await CouldNotResolve("sameowner", "SAMEOWNER", Resources.NoResolveSameOwnerMessage);
        }
        public async Task CouldNotResolve(string targetOwner, string currentOwner, string expectMessage)
        {
            var repositoryDir = "repositoryDir";
            var repositoryName = "repositoryName";
            var context = CreateGitHubContext(repositoryName: repositoryName, owner: targetOwner);
            (string, string, string)? resolveBlobResult = null;
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, repositoryOwner: currentOwner, repositoryName: repositoryName, resolveBlobResult: resolveBlobResult);

            await target.Execute(null);

            vsServices.Received(1).ShowMessageBoxInfo(expectMessage);
        }

        [Test]
        public async Task CouldResolve()
        {
            var repositoryName = "repositoryName";
            var context = CreateGitHubContext(repositoryName: repositoryName);
            var repositoryDir = "repositoryDir";
            var resolveBlobResult = ("master", "foo.cs", "");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, repositoryName: repositoryName, resolveBlobResult: resolveBlobResult);

            await target.Execute(null);

            vsServices.DidNotReceiveWithAnyArgs().ShowMessageBoxInfo(null);
        }

        [Test]
        public async Task NoChangesInWorkingDirectory()
        {
            var repositoryDir = "repositoryDir";
            var repositoryName = "repositoryName";
            var context = CreateGitHubContext(repositoryName: repositoryName);
            var gitHubContextService = Substitute.For<IGitHubContextService>();
            var resolveBlobResult = ("master", "foo.cs", "");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(gitHubContextService: gitHubContextService, vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, repositoryName: repositoryName, resolveBlobResult: resolveBlobResult, hasChanges: false);

            await target.Execute(null);

            vsServices.DidNotReceiveWithAnyArgs().ShowMessageBoxInfo(null);
            gitHubContextService.Received(1).TryOpenFile(repositoryDir, context);
        }
        [Test]
        public async void  HasChangesInWorkingDirectorye_ChangesInWorkingDirectoryMessage()
        {
          await  HasChangesInWorkingDirectory(false, Resources.ChangesInWorkingDirectoryMessage, 1, 1);
        }
        [TestCase(true, null, 1, 0)]
        public async Task HasChangesInWorkingDirectory(bool annotateFileSupported, string message,
            int receivedTryAnnotateFile, int receivedTryOpenFile)
        {
            var repositoryDir = "repositoryDir";
            var repositoryName = "repositoryName";
            var targetBranch = "targetBranch";
            var context = CreateGitHubContext(repositoryName: repositoryName, branch: targetBranch);
            var gitHubContextService = Substitute.For<IGitHubContextService>();
            gitHubContextService.TryAnnotateFile(null, null, null).ReturnsForAnyArgs(annotateFileSupported);
            var currentBranch = "currentBranch";
            var resolveBlobResult = (targetBranch, "foo.cs", "");
            var vsServices = Substitute.For<IVSServices>();
            var target = CreateOpenFromClipboardCommand(gitHubContextService: gitHubContextService, vsServices: vsServices,
                contextFromClipboard: context, repositoryDir: repositoryDir, repositoryName: repositoryName, currentBranch: currentBranch, resolveBlobResult: resolveBlobResult, hasChanges: true);

            await target.Execute(null);

            if (message != null)
            {
                vsServices.Received(1).ShowMessageBoxInfo(message);
            }
            else
            {
                vsServices.DidNotReceiveWithAnyArgs().ShowMessageBoxInfo(null);
            }

            await gitHubContextService.Received(receivedTryAnnotateFile).TryAnnotateFile(repositoryDir, currentBranch, context);
            gitHubContextService.Received(receivedTryOpenFile).TryOpenFile(repositoryDir, context);
        }

        static GitHubContext CreateGitHubContext(UriString uri = null, string owner = "github",
            string repositoryName = "VisualStudio", string branch = "master")
        {
            uri = uri ?? new UriString($"https://github.com/{owner}/{repositoryName}/blob/{branch}/README.md");

            return new GitHubContextService(null, null).FindContextFromUrl(uri);
        }

        static OpenFromClipboardCommand CreateOpenFromClipboardCommand(
            IGitHubContextService gitHubContextService = null,
            ITeamExplorerContext teamExplorerContext = null,
            IVSServices vsServices = null,
            GitHubContext contextFromClipboard = null,
            string repositoryDir = null,
            string repositoryName = null,
            string repositoryOwner = null,
            string currentBranch = null,
            (string, string, string)? resolveBlobResult = null,
            bool? hasChanges = null)
        {
            var sp = Substitute.For<IServiceProvider>();
            gitHubContextService = gitHubContextService ?? Substitute.For<IGitHubContextService>();
            teamExplorerContext = teamExplorerContext ?? Substitute.For<ITeamExplorerContext>();
            vsServices = vsServices ?? Substitute.For<IVSServices>();

            gitHubContextService.FindContextFromClipboard().Returns(contextFromClipboard);
            teamExplorerContext.ActiveRepository.LocalPath.Returns(repositoryDir);
            teamExplorerContext.ActiveRepository.Name.Returns(repositoryName);
            teamExplorerContext.ActiveRepository.Owner.Returns(repositoryOwner);
            teamExplorerContext.ActiveRepository.CurrentBranch.Name.Returns(currentBranch);
            if (resolveBlobResult != null)
            {
                gitHubContextService.ResolveBlob(repositoryDir, contextFromClipboard).Returns(resolveBlobResult.Value);
            }

            if (hasChanges != null)
            {
                gitHubContextService.HasChangesInWorkingDirectory(repositoryDir, resolveBlobResult.Value.Item1, resolveBlobResult.Value.Item2).Returns(hasChanges.Value);
            }

            return new OpenFromClipboardCommand(
                new Lazy<IGitHubContextService>(() => gitHubContextService),
                new Lazy<ITeamExplorerContext>(() => teamExplorerContext),
                new Lazy<IVSServices>(() => vsServices));
        }
    }
}
