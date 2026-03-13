using Anchor.Application.Abstractions;

namespace Anchor.Git;

public sealed class GitConflictReader : IGitConflictReader
{
    private readonly GitProcessRunner _runner;

    public GitConflictReader(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<IReadOnlyList<string>> ReadConflictedFilesAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(repositoryRoot, ["diff", "--name-only", "--diff-filter=U"], cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return Array.Empty<string>();
        }

        return result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
