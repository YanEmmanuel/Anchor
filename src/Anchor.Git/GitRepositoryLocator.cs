using Anchor.Application.Abstractions;

namespace Anchor.Git;

public sealed class GitRepositoryLocator : IGitRepositoryLocator
{
    private readonly GitProcessRunner _runner;

    public GitRepositoryLocator(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<string> LocateAsync(string? startPath, CancellationToken cancellationToken)
    {
        var workingDirectory = string.IsNullOrWhiteSpace(startPath)
            ? Environment.CurrentDirectory
            : Path.GetFullPath(startPath);

        var result = await _runner.RunAsync(workingDirectory, ["rev-parse", "--show-toplevel"], cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            throw new InvalidOperationException(result.StandardError.Length > 0 ? result.StandardError : "The current directory is not inside a Git repository.");
        }

        return result.StandardOutput.Trim();
    }
}
