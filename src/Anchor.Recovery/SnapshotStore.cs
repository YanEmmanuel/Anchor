using System.Text.Json;
using Anchor.Application.Abstractions;
using Anchor.Domain;

namespace Anchor.Recovery;

public sealed class SnapshotStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IAppPathProvider _appPathProvider;

    public SnapshotStore(IAppPathProvider appPathProvider)
    {
        _appPathProvider = appPathProvider;
    }

    public async Task SaveAsync(SnapshotMetadata metadata, CancellationToken cancellationToken)
    {
        var directory = _appPathProvider.GetSnapshotDirectory(metadata.RepositoryRoot);
        Directory.CreateDirectory(directory);

        var filePath = GetSnapshotFilePath(metadata.RepositoryRoot, metadata.Id);
        var json = JsonSerializer.Serialize(metadata, SerializerOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    public async Task<SnapshotMetadata?> GetAsync(string repositoryRoot, string snapshotId, CancellationToken cancellationToken)
    {
        var filePath = GetSnapshotFilePath(repositoryRoot, snapshotId);
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = File.OpenRead(filePath);
        return await JsonSerializer.DeserializeAsync<SnapshotMetadata>(stream, SerializerOptions, cancellationToken);
    }

    public async Task<IReadOnlyList<SnapshotMetadata>> ListAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var directory = _appPathProvider.GetSnapshotDirectory(repositoryRoot);
        if (!Directory.Exists(directory))
        {
            return Array.Empty<SnapshotMetadata>();
        }

        var snapshots = new List<SnapshotMetadata>();
        foreach (var file in Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly))
        {
            await using var stream = File.OpenRead(file);
            var metadata = await JsonSerializer.DeserializeAsync<SnapshotMetadata>(stream, SerializerOptions, cancellationToken);
            if (metadata is not null)
            {
                snapshots.Add(metadata);
            }
        }

        return snapshots
            .OrderByDescending(static snapshot => snapshot.CreatedAt)
            .ToArray();
    }

    private string GetSnapshotFilePath(string repositoryRoot, string snapshotId) =>
        Path.Combine(_appPathProvider.GetSnapshotDirectory(repositoryRoot), $"{snapshotId}.json");
}
