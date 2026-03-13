using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitDiffReader : IGitDiffReader
{
    private readonly GitProcessRunner _runner;

    public GitDiffReader(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public Task<DiffContent> ReadStagedDiffAsync(string repositoryRoot, int maxLines, CancellationToken cancellationToken) =>
        ReadDiffAsync(repositoryRoot, ["diff", "--cached", "--no-color", "--find-renames"], ["diff", "--cached", "--name-only"], "staged changes", maxLines, cancellationToken);

    public Task<DiffContent> ReadWorkingTreeDiffAsync(string repositoryRoot, int maxLines, CancellationToken cancellationToken) =>
        ReadDiffAsync(repositoryRoot, ["diff", "--no-color", "--find-renames"], ["diff", "--name-only"], "working tree changes", maxLines, cancellationToken);

    public async Task<DiffContent> ReadAllChangesDiffAsync(string repositoryRoot, int maxLines, CancellationToken cancellationToken)
    {
        var hasHead = await HasHeadAsync(repositoryRoot, cancellationToken);
        if (hasHead)
        {
            return await ReadDiffAsync(repositoryRoot, ["diff", "HEAD", "--no-color", "--find-renames"], ["diff", "HEAD", "--name-only"], "all changes against HEAD", maxLines, cancellationToken);
        }

        var staged = await ReadStagedDiffAsync(repositoryRoot, maxLines, cancellationToken);
        var workingTree = await ReadWorkingTreeDiffAsync(repositoryRoot, maxLines, cancellationToken);

        var patchLines = staged.PatchText.Split('\n').Concat(workingTree.PatchText.Split('\n')).Where(static line => !string.IsNullOrWhiteSpace(line)).ToArray();
        var truncated = patchLines.Length > maxLines;
        var finalLines = truncated ? patchLines.Take(maxLines).ToArray() : patchLines;

        return new DiffContent
        {
            RangeDescription = "all changes",
            Files = staged.Files.Concat(workingTree.Files).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            PatchText = string.Join(Environment.NewLine, finalLines),
            AddedLines = staged.AddedLines + workingTree.AddedLines,
            RemovedLines = staged.RemovedLines + workingTree.RemovedLines,
            IsTruncated = staged.IsTruncated || workingTree.IsTruncated || truncated
        };
    }

    public Task<DiffContent> ReadComparisonDiffAsync(string repositoryRoot, string baseRef, string headRef, int maxLines, CancellationToken cancellationToken) =>
        ReadDiffAsync(repositoryRoot, ["diff", $"{baseRef}...{headRef}", "--no-color", "--find-renames"], ["diff", $"{baseRef}...{headRef}", "--name-only"], $"{baseRef}...{headRef}", maxLines, cancellationToken);

    private async Task<DiffContent> ReadDiffAsync(
        string repositoryRoot,
        IReadOnlyList<string> patchArguments,
        IReadOnlyList<string> fileArguments,
        string description,
        int maxLines,
        CancellationToken cancellationToken)
    {
        var patchTask = _runner.RunAsync(repositoryRoot, patchArguments, cancellationToken);
        var filesTask = _runner.RunAsync(repositoryRoot, fileArguments, cancellationToken);
        var numStatTask = _runner.RunAsync(repositoryRoot, patchArguments.Take(2).Concat(["--numstat"]).ToArray(), cancellationToken);

        await Task.WhenAll(patchTask, filesTask, numStatTask);

        var patchResult = await patchTask;
        var filesResult = await filesTask;
        var numStatResult = await numStatTask;

        if (!patchResult.IsSuccess)
        {
            throw new InvalidOperationException(patchResult.StandardError);
        }

        var patchLines = patchResult.StandardOutput.Split('\n', StringSplitOptions.None);
        var truncated = maxLines > 0 && patchLines.Length > maxLines;
        var finalPatch = truncated
            ? string.Join(Environment.NewLine, patchLines.Take(maxLines))
            : patchResult.StandardOutput;

        var files = filesResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var (added, removed) = ParseNumStat(numStatResult.StandardOutput);

        return new DiffContent
        {
            RangeDescription = description,
            Files = files,
            PatchText = finalPatch.TrimEnd(),
            AddedLines = added,
            RemovedLines = removed,
            IsTruncated = truncated
        };
    }

    private async Task<bool> HasHeadAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var result = await _runner.RunAsync(repositoryRoot, ["rev-parse", "--verify", "HEAD"], cancellationToken);
        return result.IsSuccess;
    }

    private static (int Added, int Removed) ParseNumStat(string output)
    {
        var added = 0;
        var removed = 0;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var parts = line.Split('\t', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 3)
            {
                continue;
            }

            if (int.TryParse(parts[0], out var add))
            {
                added += add;
            }

            if (int.TryParse(parts[1], out var remove))
            {
                removed += remove;
            }
        }

        return (added, removed);
    }
}
