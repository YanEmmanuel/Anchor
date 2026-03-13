using Anchor.Application.Configuration;
using Anchor.Infrastructure;

namespace Anchor.Infrastructure.Tests;

public sealed class AppPathProviderTests
{
    [Fact]
    public async Task EnsureConfigurationFileAsync_CreatesDefaultConfig()
    {
        var originalHome = Environment.GetEnvironmentVariable("HOME");
        var tempHome = Path.Combine(Path.GetTempPath(), $"anchor-home-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempHome);
        Environment.SetEnvironmentVariable("HOME", tempHome);

        try
        {
            var pathProvider = new AppPathProvider();
            var bootstrapper = new ConfigurationBootstrapper(pathProvider);

            await bootstrapper.EnsureConfigurationFileAsync(CancellationToken.None);

            var configPath = pathProvider.GetConfigFilePath();
            Assert.True(File.Exists(configPath));
            var json = await File.ReadAllTextAsync(configPath);
            Assert.Contains("\"language\": \"auto\"", json);
            Assert.Contains("\"gitExecutablePath\": \"git\"", json);
        }
        finally
        {
            Environment.SetEnvironmentVariable("HOME", originalHome);
            Directory.Delete(tempHome, true);
        }
    }
}
