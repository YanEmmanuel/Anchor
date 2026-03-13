using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitStatusReader : IGitStatusReader
{
    private readonly GitProcessRunner _runner;
    private readonly IGitConflictReader _conflictReader;

    public GitStatusReader(GitProcessRunner runner, IGitConflictReader conflictReader)
    {
        _runner = runner;
        _conflictReader = conflictReader;
    }

    public async Task<RepoState> ReadAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var statusResult = await _runner.RunAsync(repositoryRoot, ["status", "--porcelain=2", "--branch"], cancellationToken);
        if (!statusResult.IsSuccess)
        {
            throw new InvalidOperationException(statusResult.StandardError);
        }

        string? branchName = null;
        string headSha = string.Empty;
        var isDetachedHead = false;
        var aheadBy = 0;
        var behindBy = 0;
        var hasStagedChanges = false;
        var hasUnstagedChanges = false;
        var changedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var untrackedFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in statusResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("# branch.head ", StringComparison.Ordinal))
            {
                branchName = line["# branch.head ".Length..];
                isDetachedHead = branchName == "(detached)";
            }
            else if (line.StartsWith("# branch.oid ", StringComparison.Ordinal))
            {
                headSha = line["# branch.oid ".Length..];
            }
            else if (line.StartsWith("# branch.ab ", StringComparison.Ordinal))
            {
                var parts = line["# branch.ab ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                aheadBy = ParseBranchDistance(parts, '+');
                behindBy = ParseBranchDistance(parts, '-');
            }
            else if (line.StartsWith("? ", StringComparison.Ordinal))
            {
                var file = line[2..];
                untrackedFiles.Add(file);
                changedFiles.Add(file);
            }
            else if (line.StartsWith('1') || line.StartsWith('2') || line.StartsWith('u'))
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 9)
                {
                    continue;
                }

                var xy = parts[1];
                if (xy.Length >= 2)
                {
                    hasStagedChanges |= xy[0] != '.';
                    hasUnstagedChanges |= xy[1] != '.';
                }

                var filePath = parts[^1];
                changedFiles.Add(filePath);
            }
        }

        var conflictFiles = await _conflictReader.ReadConflictedFilesAsync(repositoryRoot, cancellationToken);

        return new RepoState
        {
            RepositoryRoot = repositoryRoot,
            BranchName = isDetachedHead ? null : branchName,
            HeadSha = headSha,
            IsDetachedHead = isDetachedHead,
            HasStagedChanges = hasStagedChanges,
            HasUnstagedChanges = hasUnstagedChanges,
            HasUntrackedFiles = untrackedFiles.Count > 0,
            HasConflicts = conflictFiles.Count > 0,
            AheadBy = aheadBy,
            BehindBy = behindBy,
            ChangedFiles = changedFiles.ToArray(),
            UntrackedFiles = untrackedFiles.ToArray(),
            ConflictFiles = conflictFiles
        };
    }

    private static int ParseBranchDistance(IEnumerable<string> parts, char prefix)
    {
        var token = parts.FirstOrDefault(part => part.Length > 1 && part[0] == prefix);
        return token is null || !int.TryParse(token[1..], out var value) ? 0 : value;
    }
}
