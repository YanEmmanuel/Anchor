# Anchor

Anchor is an intelligent CLI that lives around Git instead of trying to replace it.

It keeps Git compatibility, adds guard rails for risky commands, generates strong semantic commits with AI, explains Git operations in context, and stays usable even when AI is unavailable.

## Why Anchor exists

Git is powerful, but many workflows still feel brittle:

- dangerous commands are easy to run without enough preview
- explanations are often disconnected from the current repository state
- commit messages are frequently too generic for serious projects
- AI integrations are often tightly coupled to one provider or depend on the cloud only

Anchor is designed as a professional, modular foundation for a real open source tool:

- clean layered architecture
- provider-agnostic AI integration
- local AI by default
- deterministic fallbacks when AI fails
- localization-first CLI behavior
- safety-oriented Git wrappers and recovery points

## Current status

This repository contains a functional V1 foundation built on `.NET 10`, which satisfies the original requirement of `.NET 8 or superior`.

Implemented focus areas:

- `commit-ai`
- `explain`
- `doctor`
- local AI via Ollama by default
- automatic user-language detection
- snapshots and recovery points
- PR/work summaries and branch/file reasoning helpers
- transparent Git passthrough with risk preview hooks

## CI/CD

This repository is ready for GitHub Actions on Linux:

- `CI` restores, builds and tests the solution on `ubuntu-latest`
- `Release` uses Conventional Commits with `release-please` to open the release PR, update `CHANGELOG.md` and `version.txt`, create the GitHub tag/release and publish a self-contained `linux-x64` binary
- if you want CI to run on release PRs as well, add a `RELEASE_PLEASE_TOKEN` secret with a PAT; otherwise the workflow falls back to `GITHUB_TOKEN`

## Solution layout

```text
Anchor.sln
src/
  Anchor.Cli/
  Anchor.Application/
  Anchor.Domain/
  Anchor.Infrastructure/
  Anchor.Git/
  Anchor.AI/
  Anchor.Localization/
  Anchor.Recovery/
  Anchor.Diagnostics/
  Anchor.Presentation/
tests/
  Anchor.Cli.Tests/
  Anchor.Application.Tests/
  Anchor.Domain.Tests/
  Anchor.Infrastructure.Tests/
  Anchor.Git.Tests/
  Anchor.AI.Tests/
examples/
  config.json
```

## Architecture

### `Anchor.Domain`

Pure domain records and enums for:

- repository state
- commit suggestions
- AI request/response contracts
- recovery points
- risk analysis
- doctor reports

### `Anchor.Application`

Use cases and orchestration:

- `CommitAiUseCase`
- `ExplainCommandUseCase`
- `DoctorUseCase`
- `ExecuteGitCommandUseCase`
- `SnapshotUseCase`
- `RecoverUseCase`
- `PrSummaryUseCase`
- `SummarizeUseCase`
- `WhyFileUseCase`
- `BranchAiUseCase`

This layer also contains deterministic heuristics for commit intent and summary generation.

### `Anchor.Git`

Binary Git integration through `Process` with swappable readers/executors:

- `GitProcessRunner`
- `GitRepositoryLocator`
- `GitStatusReader`
- `GitDiffReader`
- `GitLogReader`
- `GitBranchReader`
- `GitCommandExecutor`
- `GitConflictReader`
- `GitWorktreeAnalyzer`
- `GitReflogReader`

### `Anchor.AI`

Provider abstraction and prompt builders:

- `IAIProvider`
- `AIProviderFactory`
- `OllamaAIProvider`
- `OpenAICompatibleProvider`
- `AnthropicCompatibleProvider`
- `DisabledAIProvider`
- `CommitPromptBuilder`
- `ExplainPromptBuilder`
- `ConflictPromptBuilder`
- `SummaryPromptBuilder`

### `Anchor.Localization`

Localization-first behavior:

- `UserLanguageResolver`
- `ResourceLocalizer`
- embedded resources for English, Portuguese and Spanish UI strings

### `Anchor.Recovery`

Safety and restoration support:

- `SnapshotService`
- `RecoveryService`
- `SnapshotStore`

Snapshots use Git refs plus stash metadata when needed.

### `Anchor.Diagnostics`

Repository health and risk analysis:

- `RepositoryDoctor`
- `RiskAnalyzer`
- detached HEAD detection
- interrupted rebase detection
- merge-in-progress detection
- suspicious untracked artifact detection
- divergence detection
- dirty tree/conflict detection

### `Anchor.Presentation`

Terminal rendering via `Spectre.Console`:

- banner
- panels
- tables
- trees
- dangerous-command preview
- commit/doctor/summary renderers

## Commands

### `anchor commit-ai`

Analyzes staged changes by default and generates a Conventional Commit suggestion.

Features:

- automatic type detection: `feat`, `fix`, `refactor`, `perf`, `docs`, `test`, `chore`, `build`, `ci`
- automatic scope inference from paths
- AI-first generation with deterministic fallback
- interactive accept/edit/regenerate/copy/commit flow
- breaking-change support

Useful flags:

```bash
anchor commit-ai
anchor commit-ai --all
anchor commit-ai --provider ollama --model qwen2.5:14b-instruct
anchor commit-ai --copy
anchor commit-ai --commit
anchor commit-ai --lang pt-BR
```

### `anchor explain <git command>`

Explains impact on:

- HEAD
- index
- working tree
- branch
- risk
- undo guidance

Examples:

```bash
anchor explain reset --hard HEAD~1
anchor explain rebase main
```

### `anchor doctor`

Inspects repository health and reports practical guidance for:

- detached HEAD
- interrupted rebase
- merge in progress
- suspicious untracked artifacts
- divergence from upstream
- dirty working tree
- unresolved conflicts

### `anchor snapshot`

Creates a manual recovery point.

```bash
anchor snapshot
anchor snapshot "before rebasing feature/auth"
```

### `anchor recover`

Lists or restores Anchor recovery points.

```bash
anchor recover
anchor recover anchor-20260313153000-abc123
```

### `anchor pr-summary`

Builds a PR description from branch diff and recent history.

### `anchor summarize`

Summarizes recent repository work for dailies or session handoff.

### `anchor why <file>`

Explains why a file changed recently using file history and current diff context.

### `anchor branch-ai [goal]`

Suggests branch names such as:

- `feature/auth-refresh-token`
- `fix/login-validation`
- `refactor/user-service-cleanup`

### `anchor <git command>`

Transparent wrapper for common Git commands.

Examples:

```bash
anchor status
anchor log --oneline -5
anchor reset --hard HEAD~1
```

When the command looks dangerous, Anchor analyzes risk first and can create a safety snapshot automatically.

## Local AI setup

Anchor defaults to Ollama.

1. Install Ollama.
2. Pull a local instruct model.
3. Run Anchor.

Example:

```bash
ollama pull qwen2.5:14b-instruct
anchor commit-ai
```

If the configured model is missing, Anchor reports it clearly and falls back deterministically where possible.

## External providers

Anchor is prepared for optional external providers through config:

- OpenAI-compatible APIs
- Anthropic-compatible APIs

Edit `~/.anchor/config.json` or use [`examples/config.json`](./examples/config.json) as a reference.

## Configuration

The first run creates:

```text
~/.anchor/config.json
```

Example:

```json
{
  "language": "auto",
  "gitExecutablePath": "git",
  "commitLanguageMode": "user",
  "customCommitLanguage": "",
  "ai": {
    "defaultProvider": "ollama",
    "timeoutSeconds": 45,
    "maxPromptDiffLines": 500,
    "ollama": {
      "baseUrl": "http://localhost:11434",
      "model": "qwen2.5:14b-instruct"
    },
    "openaiCompatible": {
      "enabled": false,
      "baseUrl": "",
      "apiKey": "",
      "model": ""
    },
    "anthropicCompatible": {
      "enabled": false,
      "baseUrl": "",
      "apiKey": "",
      "model": ""
    }
  },
  "safety": {
    "autoSnapshotBeforeDangerousCommands": true,
    "confirmationLevel": "strict"
  }
}
```

### Commit language mode

`commitLanguageMode` supports:

- `user`
- `english`
- `custom`

When `custom` is selected, set `customCommitLanguage` to the desired language tag.

## Language detection

Anchor resolves the UI language in this order:

1. explicit Anchor config
2. CLI `--lang`
3. `ANCHOR_LANG`
4. `CultureInfo.CurrentUICulture`
5. `CultureInfo.CurrentCulture`
6. `LANG`
7. English fallback

The resolved language is reused for the current process, and AI prompts explicitly instruct the model to answer in that language.

## Safety model

For risky operations such as hard resets, cleans, rebases, merges and branch switches with a dirty tree, Anchor is designed to:

1. analyze risk
2. preview impact
3. create a snapshot when configured
4. ask for confirmation

Current recovery implementation uses:

- temporary refs under `refs/anchor/snapshots/*`
- stash snapshots when working tree state must be preserved
- metadata stored under `~/.anchor/snapshots`

Anchor does not promise a perfect undo when Git itself cannot guarantee one.

## Build and test

```bash
dotnet build Anchor.sln
dotnet test Anchor.sln
dotnet run --project src/Anchor.Cli -- explain reset --hard HEAD~1
```

## Tests included

Current automated coverage includes:

- commit message formatting
- commit intent heuristics
- prompt builder structure
- configuration bootstrap
- real Git status parsing in a temporary repository
- CLI argument routing for transparent Git passthrough

## Limitations

- the deterministic natural-language fallback is strongest for English, Portuguese and Spanish; AI providers can go broader if the model supports it
- the Git passthrough wrapper currently adds safety around repository-scoped commands; commands outside a repository fall back to raw Git execution
- recovery restores are intentionally conservative and require a clean working tree before applying a saved point
- `explain` is currently deterministic; AI prompt scaffolding exists for future enrichment
- command coverage is broad, but the highest polish today is still around `commit-ai`, `explain`, `doctor`, snapshots and passthrough safety

## Roadmap

### Phase 2

- deeper PR summary quality
- stronger `why` analysis with blame-aware context
- smarter branch naming from explicit user goals
- richer recovery browsing and restore previews

### Phase 3

- more advanced dangerous-command interception
- better diff compaction and AI context packing
- more localization coverage
- additional AI provider refinements
- richer terminal ergonomics and configuration options

## Development notes

- `net10.0` target for modern SDK compatibility while remaining within the original `.NET 8+` requirement
- `Spectre.Console` for terminal UX
- `Microsoft.Extensions.*` for DI, options and logging
- xUnit-based tests

Anchor is ready to evolve as a serious open source codebase rather than a throwaway prototype.
