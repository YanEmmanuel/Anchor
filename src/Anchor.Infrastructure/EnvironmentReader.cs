using Anchor.Application.Abstractions;

namespace Anchor.Infrastructure;

public sealed class EnvironmentReader : IEnvironmentReader
{
    public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);
}
