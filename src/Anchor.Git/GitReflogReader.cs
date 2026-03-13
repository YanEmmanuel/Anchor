using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitReflogReader : IGitReflogReader
{
    private readonly GitProcessRunner _runner;

    public GitReflogReader(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<IReadOnlyList<RecoveryPoint>> ReadAsync(string repositoryRoot, int count, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(repositoryRoot, ["reflog", $"-n{count}", "--date=iso-strict", "--pretty=format:%H%x1f%aI%x1f%gs"], cancellationToken);
        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
        {
            return Array.Empty<RecoveryPoint>();
        }

        var entries = new List<RecoveryPoint>();
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('\x1f');
            if (parts.Length < 3)
            {
                continue;
            }

            DateTimeOffset.TryParse(parts[1], out var createdAt);
            entries.Add(new RecoveryPoint
            {
                Id = parts[0][..Math.Min(8, parts[0].Length)],
                Description = parts[2],
                CreatedAt = createdAt,
                RepositoryRoot = repositoryRoot,
                HeadSha = parts[0]
            });
        }

        return entries;
    }
}
