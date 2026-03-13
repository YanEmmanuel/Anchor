using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Application.Services;

public sealed class CommandExplainer
{
    private readonly ILocalizer _localizer;

    public CommandExplainer(ILocalizer localizer)
    {
        _localizer = localizer;
    }

    public CommandExplanation Explain(string commandText, string language)
    {
        var tokens = commandText
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
        {
            return EmptyExplanation(commandText, language);
        }

        var gitTokens = tokens[0].Equals("git", StringComparison.OrdinalIgnoreCase)
            ? tokens.Skip(1).ToArray()
            : tokens;

        if (gitTokens.Length == 0)
        {
            return EmptyExplanation(commandText, language);
        }

        var verb = gitTokens[0].ToLowerInvariant();
        return verb switch
        {
            "reset" => ExplainReset(commandText, gitTokens, language),
            "clean" => ExplainClean(commandText, language),
            "checkout" or "switch" => ExplainCheckout(commandText, gitTokens, language),
            "rebase" => ExplainRebase(commandText, language),
            "merge" => ExplainMerge(commandText, language),
            "commit" => ExplainCommit(commandText, language),
            "add" => ExplainAdd(commandText, language),
            "status" => ExplainStatus(commandText, language),
            "diff" => ExplainDiff(commandText, language),
            _ => ExplainGeneric(commandText, verb, language)
        };
    }

    private CommandExplanation ExplainReset(string commandText, IReadOnlyList<string> tokens, string language)
    {
        var hard = tokens.Any(token => token.Equals("--hard", StringComparison.OrdinalIgnoreCase));

        return new CommandExplanation
        {
            CommandText = commandText,
            Summary = T(hard ? "Explain.Reset.Summary.Hard" : "Explain.Reset.Summary.Default", language),
            HeadImpact = T("Explain.Reset.Head", language),
            IndexImpact = T(hard ? "Explain.Reset.Index.Hard" : "Explain.Reset.Index.Default", language),
            WorkingTreeImpact = T(hard ? "Explain.Reset.WorkingTree.Hard" : "Explain.Reset.WorkingTree.Default", language),
            BranchImpact = T("Explain.Reset.Branch", language),
            RiskLevel = hard ? RiskLevel.Critical : RiskLevel.High,
            UndoGuidance = T("Explain.Reset.Undo", language),
            Notes =
            [
                T("Explain.Reset.Note.Destructive", language),
                T("Explain.Reset.Note.Snapshot", language)
            ]
        };
    }

    private CommandExplanation ExplainClean(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Clean.Summary", language),
        HeadImpact = T("Explain.Clean.Head", language),
        IndexImpact = T("Explain.Clean.Index", language),
        WorkingTreeImpact = T("Explain.Clean.WorkingTree", language),
        BranchImpact = T("Explain.Clean.Branch", language),
        RiskLevel = RiskLevel.Critical,
        UndoGuidance = T("Explain.Clean.Undo", language),
        Notes =
        [
            T("Explain.Clean.Note.Preview", language),
            T("Explain.Clean.Note.Recovery", language)
        ]
    };

    private CommandExplanation ExplainCheckout(string commandText, IReadOnlyList<string> tokens, string language)
    {
        var createsBranch = tokens.Any(token => token.Equals("-b", StringComparison.OrdinalIgnoreCase));
        return new CommandExplanation
        {
            CommandText = commandText,
            Summary = T(createsBranch ? "Explain.Checkout.Summary.Create" : "Explain.Checkout.Summary.Switch", language),
            HeadImpact = T("Explain.Checkout.Head", language),
            IndexImpact = T("Explain.Checkout.Index", language),
            WorkingTreeImpact = T("Explain.Checkout.WorkingTree", language),
            BranchImpact = T(createsBranch ? "Explain.Checkout.Branch.Create" : "Explain.Checkout.Branch.Switch", language),
            RiskLevel = RiskLevel.High,
            UndoGuidance = T("Explain.Checkout.Undo", language),
            Notes =
            [
                T("Explain.Checkout.Note.DirtyTree", language),
                T("Explain.Checkout.Note.PreferSwitch", language)
            ]
        };
    }

    private CommandExplanation ExplainRebase(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Rebase.Summary", language),
        HeadImpact = T("Explain.Rebase.Head", language),
        IndexImpact = T("Explain.Rebase.Index", language),
        WorkingTreeImpact = T("Explain.Rebase.WorkingTree", language),
        BranchImpact = T("Explain.Rebase.Branch", language),
        RiskLevel = RiskLevel.High,
        UndoGuidance = T("Explain.Rebase.Undo", language),
        Notes =
        [
            T("Explain.Rebase.Note.SharedHistory", language),
            T("Explain.Rebase.Note.Snapshot", language)
        ]
    };

    private CommandExplanation ExplainMerge(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Merge.Summary", language),
        HeadImpact = T("Explain.Merge.Head", language),
        IndexImpact = T("Explain.Merge.Index", language),
        WorkingTreeImpact = T("Explain.Merge.WorkingTree", language),
        BranchImpact = T("Explain.Merge.Branch", language),
        RiskLevel = RiskLevel.Medium,
        UndoGuidance = T("Explain.Merge.Undo", language),
        Notes =
        [
            T("Explain.Merge.Note.FastForward", language),
            T("Explain.Merge.Note.DirtyTree", language)
        ]
    };

    private CommandExplanation ExplainCommit(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Commit.Summary", language),
        HeadImpact = T("Explain.Commit.Head", language),
        IndexImpact = T("Explain.Commit.Index", language),
        WorkingTreeImpact = T("Explain.Commit.WorkingTree", language),
        BranchImpact = T("Explain.Commit.Branch", language),
        RiskLevel = RiskLevel.Low,
        UndoGuidance = T("Explain.Commit.Undo", language),
        Notes =
        [
            T("Explain.Commit.Note.StagedOnly", language)
        ]
    };

    private CommandExplanation ExplainAdd(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Add.Summary", language),
        HeadImpact = T("Explain.Add.Head", language),
        IndexImpact = T("Explain.Add.Index", language),
        WorkingTreeImpact = T("Explain.Add.WorkingTree", language),
        BranchImpact = T("Explain.Add.Branch", language),
        RiskLevel = RiskLevel.Low,
        UndoGuidance = T("Explain.Add.Undo", language),
        Notes =
        [
            T("Explain.Add.Note.Staging", language)
        ]
    };

    private CommandExplanation ExplainStatus(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Status.Summary", language),
        HeadImpact = T("Explain.Status.Head", language),
        IndexImpact = T("Explain.Status.Index", language),
        WorkingTreeImpact = T("Explain.Status.WorkingTree", language),
        BranchImpact = T("Explain.Status.Branch", language),
        RiskLevel = RiskLevel.None,
        UndoGuidance = T("Explain.Status.Undo", language),
        Notes =
        [
            T("Explain.Status.Note.Safe", language)
        ]
    };

    private CommandExplanation ExplainDiff(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Diff.Summary", language),
        HeadImpact = T("Explain.Diff.Head", language),
        IndexImpact = T("Explain.Diff.Index", language),
        WorkingTreeImpact = T("Explain.Diff.WorkingTree", language),
        BranchImpact = T("Explain.Diff.Branch", language),
        RiskLevel = RiskLevel.None,
        UndoGuidance = T("Explain.Diff.Undo", language),
        Notes =
        [
            T("Explain.Diff.Note.Cached", language)
        ]
    };

    private CommandExplanation ExplainGeneric(string commandText, string verb, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Generic.Summary", language, verb),
        HeadImpact = T("Explain.Generic.Head", language),
        IndexImpact = T("Explain.Generic.Index", language),
        WorkingTreeImpact = T("Explain.Generic.WorkingTree", language),
        BranchImpact = T("Explain.Generic.Branch", language),
        RiskLevel = RiskLevel.Medium,
        UndoGuidance = T("Explain.Generic.Undo", language),
        Notes =
        [
            T("Explain.Generic.Note.Preview", language)
        ]
    };

    private CommandExplanation EmptyExplanation(string commandText, string language) => new()
    {
        CommandText = commandText,
        Summary = T("Explain.Empty.Summary", language),
        HeadImpact = T("Explain.Empty.Unknown", language),
        IndexImpact = T("Explain.Empty.Unknown", language),
        WorkingTreeImpact = T("Explain.Empty.Unknown", language),
        BranchImpact = T("Explain.Empty.Unknown", language),
        RiskLevel = RiskLevel.Medium,
        UndoGuidance = T("Explain.Empty.Undo", language)
    };

    private string T(string key, string language, params object[] arguments) =>
        _localizer.Get(key, language, arguments);
}
