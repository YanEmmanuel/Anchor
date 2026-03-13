using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitCommandExecutor : IGitCommandExecutor
{
    private readonly GitProcessRunner _runner;

    public GitCommandExecutor(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public Task<GitCommandResult> ExecuteAsync(string repositoryRoot, IReadOnlyList<string> arguments, CancellationToken cancellationToken) =>
        _runner.RunAsync(repositoryRoot, arguments, cancellationToken);
}
