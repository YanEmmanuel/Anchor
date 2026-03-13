using Anchor.AI.DependencyInjection;
using Anchor.Application.Configuration;
using Anchor.Application.DependencyInjection;
using Anchor.Cli.Commands;
using Anchor.Cli.DependencyInjection;
using Anchor.Diagnostics.DependencyInjection;
using Anchor.Git.DependencyInjection;
using Anchor.Infrastructure;
using Anchor.Infrastructure.DependencyInjection;
using Anchor.Localization.DependencyInjection;
using Anchor.Presentation.DependencyInjection;
using Anchor.Recovery.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;

var pathProvider = new AppPathProvider();
var bootstrapper = new ConfigurationBootstrapper(pathProvider);
await bootstrapper.EnsureConfigurationFileAsync(CancellationToken.None);

var builder = Host.CreateApplicationBuilder(args);
builder.Configuration.AddJsonFile(pathProvider.GetConfigFilePath(), optional: true, reloadOnChange: false);
builder.Services.AddSingleton<Anchor.Application.Abstractions.IAppPathProvider>(pathProvider);
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddSimpleConsole(options =>
    {
        options.SingleLine = true;
        options.TimestampFormat = "HH:mm:ss ";
    });
    logging.SetMinimumLevel(LogLevel.Warning);
});
builder.Services.AddOptions<AnchorOptions>().Bind(builder.Configuration).ValidateOnStart();
builder.Services.AddAnchorApplicationServices();
builder.Services.AddAnchorInfrastructureServices();
builder.Services.AddAnchorLocalizationServices();
builder.Services.AddAnchorGitServices();
builder.Services.AddAnchorAiServices();
builder.Services.AddAnchorDiagnosticsServices();
builder.Services.AddAnchorRecoveryServices();
builder.Services.AddAnchorPresentationServices();

var registrar = new TypeRegistrar(builder.Services);
var app = new CommandApp(registrar);
app.Configure(config =>
{
    config.SetApplicationName("anchor");
    config.SetApplicationVersion(Anchor.Cli.ApplicationVersionResolver.Resolve());
    config.Settings.ConvertFlagsToRemainingArguments = true;
    config.AddCommand<CommitAiCommand>("commit-ai");
    config.AddCommand<ExplainCommand>("explain");
    config.AddCommand<DoctorCommand>("doctor");
    config.AddCommand<SnapshotCommand>("snapshot");
    config.AddCommand<RecoverCommand>("recover");
    config.AddCommand<PrSummaryCommand>("pr-summary");
    config.AddCommand<SummarizeCommand>("summarize");
    config.AddCommand<WhyCommand>("why");
    config.AddCommand<BranchAiCommand>("branch-ai");
    config.AddCommand<GitPassthroughCommand>("git");
});

var normalizedArgs = Anchor.Cli.ArgumentRouter.NormalizeArguments(args);
return await app.RunAsync(normalizedArgs);
