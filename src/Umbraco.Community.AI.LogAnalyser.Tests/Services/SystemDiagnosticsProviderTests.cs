using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Semver;
using Umbraco.Cms.Core.Services;
using Umbraco.Community.AI.LogAnalyser.Services;
using Xunit;

namespace Umbraco.Community.AI.LogAnalyser.Tests.Services;

public class SystemDiagnosticsProviderTests
{
    private static SystemDiagnosticsProvider CreateSut(
        string? connectionString = "Data Source=|DataDirectory|/Umbraco.sqlite.db",
        string? providerName = null,
        string? modelsMode = "InMemoryAuto",
        string environmentName = "Development")
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:umbracoDbDSN"] = connectionString,
            ["ConnectionStrings:umbracoDbDSN_ProviderName"] = providerName,
            ["Umbraco:CMS:ModelsBuilder:ModelsMode"] = modelsMode,
        };

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var umbracoVersion = Substitute.For<IUmbracoVersion>();
        umbracoVersion.SemanticVersion.Returns(new SemVersion(17, 4, 0));

        var runtimeState = Substitute.For<IRuntimeState>();
        runtimeState.Level.Returns(RuntimeLevel.Run);

        var hostEnvironment = Substitute.For<IHostEnvironment>();
        hostEnvironment.EnvironmentName.Returns(environmentName);

        return new SystemDiagnosticsProvider(umbracoVersion, runtimeState, hostEnvironment, configuration);
    }

    #region Context content

    [Fact]
    public void GetContext_IncludesCoreEnvironmentFacts()
    {
        var context = CreateSut(environmentName: "Staging").GetContext();

        context.Should().Contain("Umbraco: 17.4.0");
        context.Should().Contain(".NET: ");
        context.Should().Contain("OS: ");
        context.Should().Contain("Environment: Staging");
        context.Should().Contain("Runtime mode: Run");
        context.Should().Contain("Hosting: ");
        context.Should().Contain("Application started: ");
        context.Should().Contain("Installed packages:");
    }

    [Fact]
    public void GetContext_IncludesModelsBuilderMode_WhenConfigured() =>
        CreateSut(modelsMode: "SourceCodeManual").GetContext()
            .Should().Contain("ModelsBuilder mode: SourceCodeManual");

    [Fact]
    public void GetContext_OmitsModelsBuilderMode_WhenNotConfigured() =>
        CreateSut(modelsMode: null).GetContext()
            .Should().NotContain("ModelsBuilder mode:");

    [Fact]
    public void GetContext_ListsUmbracoAssembliesButNotFrameworkOnes()
    {
        // Umbraco.Cms.Core is loaded in the test process (IUmbracoVersion lives there), and
        // System.* assemblies always are — so this exercises the real reflection path.
        var context = CreateSut().GetContext();

        context.Should().Contain("Umbraco");
        context.Should().NotContain("System.Private.CoreLib");
        context.Should().NotContain("System.Text.Json");
    }

    [Fact]
    public void GetContext_IsCached_AcrossCalls()
    {
        var sut = CreateSut();

        // Lazy<string> caches the built string, so both calls must return the very same instance —
        // not merely equal values, which would also hold if it were rebuilt.
        ReferenceEquals(sut.GetContext(), sut.GetContext()).Should().BeTrue();
    }

    #endregion

    #region Connection string safety

    [Fact]
    public void GetContext_NeverLeaksTheConnectionString()
    {
        const string secret = "Server=db.internal;Initial Catalog=Umbraco;User Id=sa;Password=SuperSecret123";

        var context = CreateSut(secret).GetContext();

        context.Should().NotContain("SuperSecret123");
        context.Should().NotContain("User Id=");
        context.Should().NotContain("db.internal");
        context.Should().Contain("Database provider: SQL Server");
    }

    [Fact]
    public void GetContext_PrefersExplicitProviderName_OverInference() =>
        CreateSut("Data Source=whatever.db", providerName: "Microsoft.Data.SqlClient").GetContext()
            .Should().Contain("Database provider: Microsoft.Data.SqlClient");

    [Theory]
    [InlineData("Data Source=|DataDirectory|/Umbraco.sqlite.db;Cache=Shared;Foreign Keys=True", "SQLite")]
    [InlineData("Server=.;Initial Catalog=Umbraco;Integrated Security=true", "SQL Server")]
    [InlineData("Data Source=tcp:x.database.windows.net;Initial Catalog=Umbraco", "SQL Server")]
    [InlineData("", "Unknown")]
    [InlineData("something-entirely-different", "Unknown")]
    public void InferDatabaseProvider_ClassifiesConnectionStrings(string connectionString, string expected) =>
        SystemDiagnosticsProvider.InferDatabaseProvider(connectionString).Should().Be(expected);

    #endregion

    #region Package filtering

    [Theory]
    [InlineData("Umbraco.Core")]
    [InlineData("Umbraco.Cms.Api.Management")]
    [InlineData("umbraco.lowercase.match")]  // the "umbraco" match is case-insensitive
    [InlineData("Our.Umbraco.SomeAddOn")]    // ...and matches anywhere, not just as a prefix
    [InlineData("uSync.Core")]
    [InlineData("Examine.Lucene")]
    [InlineData("NPoco.SqlServer")]
    [InlineData("Microsoft.Data.SqlClient")]
    [InlineData("MailKit")]
    [InlineData("MimeKit")]
    [InlineData("SixLabors.ImageSharp.Web")]
    [InlineData("Smidge.Nuglify")]
    [InlineData("StackExchange.Redis")]
    [InlineData("Hangfire.Core")]
    [InlineData("Newtonsoft.Json")]
    public void IsRelevant_KeepsDiagnosticallyUsefulAssemblies(string name) =>
        SystemDiagnosticsProvider.IsRelevant(name, entryAssemblyName: null).Should().BeTrue();

    [Theory]
    [InlineData("System.Text.Json")]
    [InlineData("Microsoft.Extensions.Logging")]
    [InlineData("Microsoft.AspNetCore.Mvc.Core")]
    [InlineData("AWSSDK.BedrockRuntime")]
    [InlineData("Azure.Core")]
    [InlineData("Google.Apis.Auth")]
    [InlineData("MessagePack")]
    [InlineData("Lucene.Net")]          // documented as deliberately excluded
    [InlineData("Serilog.Sinks.File")]  // documented as deliberately excluded
    [InlineData("netstandard")]
    public void IsRelevant_DropsTheTransitiveLongTail(string name) =>
        SystemDiagnosticsProvider.IsRelevant(name, entryAssemblyName: null).Should().BeFalse();

    [Fact]
    public void IsRelevant_KeepsTheApplicationsOwnAssembly()
    {
        SystemDiagnosticsProvider.IsRelevant("Acme.PublicWeb", "Acme.PublicWeb").Should().BeTrue();
        SystemDiagnosticsProvider.IsRelevant("Acme.PublicWeb", "SomethingElse").Should().BeFalse();
    }

    [Fact]
    public void IsRelevant_MatchesTheEntryAssemblyCaseSensitively() =>
        SystemDiagnosticsProvider.IsRelevant("Acme.PublicWeb", "acme.publicweb").Should().BeFalse();

    #endregion

    #region Family collapsing

    [Theory]
    [InlineData("Umbraco", "Umbraco")]
    [InlineData("Umbraco.Core", "Umbraco.Core")]
    [InlineData("Umbraco.Cms.Core", "Umbraco.Cms.Core")]
    [InlineData("Umbraco.AI.Agent.Persistence.Sqlite", "Umbraco.AI.Agent")]
    [InlineData("Umbraco.Cms.Api.Management", "Umbraco.Cms.Api")]
    public void FamilyRoot_TakesTheFirstThreeSegments(string name, string expected) =>
        SystemDiagnosticsProvider.FamilyRoot(name).Should().Be(expected);

    [Theory]
    [InlineData("17.4.0", "17.4.0")]
    [InlineData("17.4.0+abc1234", "17.4.0")]
    [InlineData("18.0.0-rc1+deadbeef", "18.0.0-rc1")]
    public void CleanVersion_StripsBuildMetadata(string version, string expected) =>
        SystemDiagnosticsProvider.CleanVersion(version).Should().Be(expected);

    [Fact]
    public void FormatInstalledPackages_CollapsesSameVersionSubAssemblies()
    {
        var output = SystemDiagnosticsProvider.FormatInstalledPackages(
        [
            ("Umbraco.AI.Agent.Core", "1.10.4"),
            ("Umbraco.AI.Agent.Persistence.Sqlite", "1.10.4"),
            ("Umbraco.AI.Agent.Web.StaticAssets", "1.10.4"),
        ], entryAssemblyName: null);

        output.Should().Contain("Umbraco.AI.Agent.* 1.10.4 (3 assemblies)");
        output.Should().NotContain("Umbraco.AI.Agent.Core");
    }

    [Fact]
    public void FormatInstalledPackages_PrintsSingletonsInFull()
    {
        var output = SystemDiagnosticsProvider.FormatInstalledPackages(
            [("Umbraco.AI.OpenAI", "18.2.0")], entryAssemblyName: null);

        output.Should().Contain("  Umbraco.AI.OpenAI 18.2.0");
        output.Should().NotContain(".*");
    }

    [Fact]
    public void FormatInstalledPackages_DoesNotCollapseAcrossDifferentVersions()
    {
        var output = SystemDiagnosticsProvider.FormatInstalledPackages(
        [
            ("Umbraco.Cms.Api.Common", "17.4.0"),
            ("Umbraco.Cms.Api.Management", "17.4.0"),
            ("Umbraco.Cms.Api.Delivery", "17.5.1"),
        ], entryAssemblyName: null);

        output.Should().Contain("Umbraco.Cms.Api.* 17.4.0 (2 assemblies)");
        output.Should().Contain("  Umbraco.Cms.Api.Delivery 17.5.1");
    }

    [Fact]
    public void FormatInstalledPackages_StripsBuildMetadataBeforeGrouping()
    {
        // Same release, different commit hashes — these must still collapse onto one line.
        var output = SystemDiagnosticsProvider.FormatInstalledPackages(
        [
            ("Umbraco.AI.Prompt.Core", "18.2.3+aaaaaaa"),
            ("Umbraco.AI.Prompt.Web", "18.2.3+bbbbbbb"),
        ], entryAssemblyName: null);

        output.Should().Contain("Umbraco.AI.Prompt.* 18.2.3 (2 assemblies)");
        output.Should().NotContain("+aaaaaaa");
    }

    [Fact]
    public void FormatInstalledPackages_FiltersTheLongTailAndOrdersByFamily()
    {
        var output = SystemDiagnosticsProvider.FormatInstalledPackages(
        [
            ("System.Text.Json", "10.0.0"),
            ("uSync.Core", "18.1.1"),
            ("AWSSDK.Core", "3.7.0"),
            ("Examine", "5.0.0"),
            ("", "1.0.0"),
        ], entryAssemblyName: null);

        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(l => l.Trim())
            .ToList();

        lines[0].Should().Be("Installed packages:");
        lines.Skip(1).Should().Equal("Examine 5.0.0", "uSync.Core 18.1.1");
    }

    [Fact]
    public void FormatInstalledPackages_HandlesAnEmptyAssemblyList() =>
        SystemDiagnosticsProvider.FormatInstalledPackages([], entryAssemblyName: null)
            .Trim().Should().Be("Installed packages:");

    #endregion
}
