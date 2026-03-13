using System.Security.Cryptography;
using System.Text;
using Anchor.Application.Abstractions;

namespace Anchor.Infrastructure;

public sealed class AppPathProvider : IAppPathProvider
{
    public string GetConfigDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".anchor");

    public string GetConfigFilePath() => Path.Combine(GetConfigDirectory(), "config.json");

    public string GetSnapshotDirectory(string repositoryRoot) =>
        Path.Combine(GetConfigDirectory(), "snapshots", GetRepositoryId(repositoryRoot));

    public string GetRepositoryId(string repositoryRoot)
    {
        var normalized = Path.GetFullPath(repositoryRoot).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant()[..12];
    }
}
