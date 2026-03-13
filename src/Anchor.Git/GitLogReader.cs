using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitLogReader : IGitLogReader
{
    private readonly GitProcessRunner _runner;

    public GitLogReader(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public Task<IReadOnlyList<GitCommitSummary>> ReadRecentCommitsAsync(string repositoryRoot, int count, CancellationToken cancellationToken) =>
        ReadAsync(repositoryRoot, ["log", $"-n{count}", "--date=iso-strict", "--pretty=format:%H%x1f%an%x1f%aI%x1f%s%x1e", "--name-only"], cancellationToken);

    public Task<IReadOnlyList<GitCommitSummary>> ReadFileHistoryAsync(string repositoryRoot, string filePath, int count, CancellationToken cancellationToken) =>
        ReadAsync(repositoryRoot, ["log", $"-n{count}", "--date=iso-strict", "--pretty=format:%H%x1f%an%x1f%aI%x1f%s%x1e", "--name-only", "--", filePath], cancellationToken);

    private async Task<IReadOnlyList<GitCommitSummary>> ReadAsync(string repositoryRoot, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(repositoryRoot, arguments, cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return Array.Empty<GitCommitSummary>();
        }

        var records = result.StandardOutput.Split('\x1e', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var commits = new List<GitCommitSummary>(records.Length);

        foreach (var record in records)
        {
            var lines = record.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0)
            {
                continue;
            }

            var header = lines[0].Split('\x1f');
            if (header.Length < 4)
            {
                continue;
            }

            DateTimeOffset.TryParse(header[2], out var date);
            commits.Add(new GitCommitSummary
            {
                Sha = header[0],
                Author = header[1],
                Date = date,
                Subject = header[3],
                Files = lines.Skip(1).Select(static line => line.Trim()).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray()
            });
        }

        return commits;
    }
}
