using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Diagnostics;

public sealed class RiskAnalyzer : IRiskAnalyzer
{
    public Task<CommandRiskAnalysis> AnalyzeAsync(GitCommandContext context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tokens = context.Tokens.Select(static token => token.ToLowerInvariant()).ToArray();
        if (tokens.Length == 0)
        {
            return Task.FromResult(new CommandRiskAnalysis
            {
                RiskLevel = RiskLevel.None,
                Summary = "No Git command was supplied."
            });
        }

        var verb = tokens[0] == "git" && tokens.Length > 1 ? tokens[1] : tokens[0];
        var state = context.RepoState;
        var affectedFiles = state.ChangedFiles.Take(10).ToArray();

        CommandRiskAnalysis result = verb switch
        {
            "reset" when tokens.Contains("--hard") => new CommandRiskAnalysis
            {
                RiskLevel = RiskLevel.Critical,
                RequiresConfirmation = true,
                ShouldCreateSnapshot = true,
                Summary = "Hard reset can discard tracked changes and move the current branch.",
                Reasons =
                [
                    "HEAD and the current branch will move.",
                    "Tracked working tree changes can be lost."
                ],
                PotentiallyAffectedFiles = affectedFiles,
                Alternatives = ["git reset --soft", "git restore --source=<commit> <path>"]
            },
            "clean" => new CommandRiskAnalysis
            {
                RiskLevel = RiskLevel.Critical,
                RequiresConfirmation = true,
                ShouldCreateSnapshot = state.HasUntrackedFiles,
                Summary = "git clean can permanently delete untracked files.",
                Reasons =
                [
                    "Untracked files are not stored in commit history.",
                    "There is no reliable undo after deletion."
                ],
                PotentiallyAffectedFiles = state.UntrackedFiles.Take(10).ToArray(),
                Alternatives = ["git clean -nd"]
            },
            "rebase" => new CommandRiskAnalysis
            {
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true,
                ShouldCreateSnapshot = true,
                Summary = "Rebase rewrites history and may stop on conflicts.",
                Reasons =
                [
                    "Commit identities change during rebase.",
                    "Dirty working trees increase the chance of conflicts."
                ],
                PotentiallyAffectedFiles = affectedFiles,
                Alternatives = ["git merge"]
            },
            "merge" => new CommandRiskAnalysis
            {
                RiskLevel = state.HasUnstagedChanges || state.HasUntrackedFiles ? RiskLevel.High : RiskLevel.Medium,
                RequiresConfirmation = state.HasUnstagedChanges || state.HasUntrackedFiles,
                ShouldCreateSnapshot = state.HasStagedChanges || state.HasUnstagedChanges,
                Summary = "Merge can create conflicts or an unexpected merge commit.",
                Reasons =
                [
                    "Conflicts can leave the repository in a partially merged state."
                ],
                PotentiallyAffectedFiles = affectedFiles,
                Alternatives = ["git merge --no-commit", "git rebase"]
            },
            "checkout" or "switch" when state.HasUnstagedChanges || state.HasUntrackedFiles => new CommandRiskAnalysis
            {
                RiskLevel = RiskLevel.High,
                RequiresConfirmation = true,
                ShouldCreateSnapshot = true,
                Summary = "Switching branches with a dirty working tree can overwrite or block local work.",
                Reasons =
                [
                    "Local changes may conflict with files in the target branch."
                ],
                PotentiallyAffectedFiles = affectedFiles,
                Alternatives = ["git stash push -u", "git worktree add"]
            },
            _ => new CommandRiskAnalysis
            {
                RiskLevel = RiskLevel.Low,
                RequiresConfirmation = false,
                ShouldCreateSnapshot = false,
                Summary = "This command does not look destructive.",
                PotentiallyAffectedFiles = affectedFiles
            }
        };

        return Task.FromResult(result);
    }
}
