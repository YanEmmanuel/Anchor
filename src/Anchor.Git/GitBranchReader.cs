using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Git;

public sealed class GitBranchReader : IGitBranchReader
{
    private readonly GitProcessRunner _runner;

    public GitBranchReader(GitProcessRunner runner)
    {
        _runner = runner;
    }

    public async Task<BranchInfo> ReadAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var branchTask = _runner.RunAsync(repositoryRoot, ["branch", "--show-current"], cancellationToken);
        var upstreamTask = _runner.RunAsync(repositoryRoot, ["rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}"], cancellationToken);
        var statusTask = _runner.RunAsync(repositoryRoot, ["status", "--porcelain=2", "--branch"], cancellationToken);

        await Task.WhenAll(branchTask, upstreamTask, statusTask);

        var branchResult = await branchTask;
        var upstreamResult = await upstreamTask;
        var statusResult = await statusTask;

        var isDetachedHead = statusResult.StandardOutput.Split('\n').Any(static line => line.StartsWith("# branch.head (detached)", StringComparison.Ordinal));
        var aheadBy = 0;
        var behindBy = 0;

        foreach (var line in statusResult.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!line.StartsWith("# branch.ab ", StringComparison.Ordinal))
            {
                continue;
            }

            var parts = line["# branch.ab ".Length..].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            aheadBy = ParseBranchDistance(parts, '+');
            behindBy = ParseBranchDistance(parts, '-');
        }

        return new BranchInfo(
            string.IsNullOrWhiteSpace(branchResult.StandardOutput) ? null : branchResult.StandardOutput,
            upstreamResult.IsSuccess ? upstreamResult.StandardOutput : null,
            aheadBy,
            behindBy,
            isDetachedHead);
    }

    public async Task<string> ResolveBaseBranchAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var upstreamResult = await _runner.RunAsync(repositoryRoot, ["rev-parse", "--abbrev-ref", "--symbolic-full-name", "@{upstream}"], cancellationToken);
        if (upstreamResult.IsSuccess && !string.IsNullOrWhiteSpace(upstreamResult.StandardOutput))
        {
            var branch = upstreamResult.StandardOutput.Trim();
            var slashIndex = branch.IndexOf('/');
            return slashIndex >= 0 ? branch[(slashIndex + 1)..] : branch;
        }

        foreach (var candidate in new[] { "main", "master", "develop" })
        {
            var result = await _runner.RunAsync(repositoryRoot, ["rev-parse", "--verify", candidate], cancellationToken);
            if (result.IsSuccess)
            {
                return candidate;
            }
        }

        return "main";
    }

    private static int ParseBranchDistance(IEnumerable<string> parts, char prefix)
    {
        var token = parts.FirstOrDefault(part => part.Length > 1 && part[0] == prefix);
        return token is null || !int.TryParse(token[1..], out var value) ? 0 : value;
    }
}
