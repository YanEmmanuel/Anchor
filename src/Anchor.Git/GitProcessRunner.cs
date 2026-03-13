using System.Diagnostics;
using Anchor.Application.Configuration;
using Anchor.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Anchor.Git;

public sealed class GitProcessRunner
{
    private readonly ILogger<GitProcessRunner> _logger;
    private readonly IOptions<AnchorOptions> _options;

    public GitProcessRunner(ILogger<GitProcessRunner> logger, IOptions<AnchorOptions> options)
    {
        _logger = logger;
        _options = options;
    }

    public async Task<GitCommandResult> RunAsync(string workingDirectory, IReadOnlyList<string> arguments, CancellationToken cancellationToken)
    {
        var executable = string.IsNullOrWhiteSpace(_options.Value.GitExecutablePath)
            ? "git"
            : _options.Value.GitExecutablePath;

        var startInfo = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory)
                ? Environment.CurrentDirectory
                : workingDirectory
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        _logger.LogDebug("Running git command in {WorkingDirectory}: {Executable} {Arguments}", startInfo.WorkingDirectory, executable, string.Join(' ', arguments));

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        var outputTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);
        var standardOutput = await outputTask;
        var standardError = await errorTask;

        return new GitCommandResult(process.ExitCode, standardOutput.TrimEnd(), standardError.TrimEnd());
    }
}
