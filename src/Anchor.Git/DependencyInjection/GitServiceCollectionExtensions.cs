using Anchor.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Anchor.Git.DependencyInjection;

public static class GitServiceCollectionExtensions
{
    public static IServiceCollection AddAnchorGitServices(this IServiceCollection services)
    {
        services.AddSingleton<GitProcessRunner>();
        services.AddSingleton<IGitRepositoryLocator, GitRepositoryLocator>();
        services.AddSingleton<IGitStatusReader, GitStatusReader>();
        services.AddSingleton<IGitDiffReader, GitDiffReader>();
        services.AddSingleton<IGitLogReader, GitLogReader>();
        services.AddSingleton<IGitBranchReader, GitBranchReader>();
        services.AddSingleton<IGitCommandExecutor, GitCommandExecutor>();
        services.AddSingleton<IGitConflictReader, GitConflictReader>();
        services.AddSingleton<IGitWorktreeAnalyzer, GitWorktreeAnalyzer>();
        services.AddSingleton<IGitReflogReader, GitReflogReader>();
        return services;
    }
}
