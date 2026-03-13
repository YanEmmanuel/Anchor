using Anchor.Application.Services;

namespace Anchor.Application.Tests;

public sealed class CommitIntentAnalyzerTests
{
    private readonly CommitIntentAnalyzer _analyzer = new();

    [Fact]
    public void Analyze_ReturnsDocs_WhenOnlyDocumentationFilesChanged()
    {
        var result = _analyzer.Analyze(["README.md", "docs/setup.md"], "+ update docs");

        Assert.Equal("docs", result.InferredType);
    }

    [Fact]
    public void Analyze_ReturnsTest_WhenOnlyTestFilesChanged()
    {
        var result = _analyzer.Analyze(["tests/AuthTests.cs", "src/Anchor.Auth.Tests/LoginTests.cs"], "+ assert");

        Assert.Equal("test", result.InferredType);
    }

    [Fact]
    public void Analyze_InfersScope_FromSrcFolder()
    {
        var result = _analyzer.Analyze(["src/Anchor.Auth/LoginService.cs"], "+ public class LoginService");

        Assert.Equal("auth", result.InferredScope);
    }
}
