using System.Diagnostics;
using Anchor.Application.Configuration;
using Anchor.Git;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Anchor.Git.Tests;

public sealed class GitStatusReaderTests
{
    [Fact]
    public async Task ReadAsync_DetectsStagedChanges()
    {
        var repositoryRoot = CreateRepository();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(repositoryRoot, "sample.txt"), "hello");
            RunGit(repositoryRoot, "add", "sample.txt");

            var options = Options.Create(new AnchorOptions());
            var runner = new GitProcessRunner(NullLogger<GitProcessRunner>.Instance, options);
            var conflictReader = new GitConflictReader(runner);
            var reader = new GitStatusReader(runner, conflictReader);

            var state = await reader.ReadAsync(repositoryRoot, CancellationToken.None);

            Assert.True(state.HasStagedChanges);
            Assert.Contains("sample.txt", state.ChangedFiles);
        }
        finally
        {
            Directory.Delete(repositoryRoot, true);
        }
    }

    private static string CreateRepository()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"anchor-git-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        RunGit(directory, "init");
        RunGit(directory, "config", "user.email", "tests@example.com");
        RunGit(directory, "config", "user.name", "Anchor Tests");
        return directory;
    }

    private static void RunGit(string workingDirectory, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "git",
            WorkingDirectory = workingDirectory,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo);

        if (process is null)
        {
            throw new InvalidOperationException("Could not start git process.");
        }
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(process.StandardError.ReadToEnd());
        }
    }
}
