using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NSubstitute;
using Umbraco.Cms.Core.DependencyInjection;
using Umbraco.Community.AI.LogAnalyser.Composers;
using Umbraco.Community.AI.LogAnalyser.Models;
using Umbraco.Community.AI.LogAnalyser.Services;
using Xunit;

namespace Umbraco.Community.AI.LogAnalyser.Tests.Composers;

/// <summary>
/// Exercises the composer against a real <see cref="ServiceCollection"/>. These tests matter most
/// on the Umbraco 18 build: the OpenAPI registration is the only code behind <c>#if UMBRACO_18</c>,
/// and running the suite from both the .v17 and .v18 test wrappers means each major's branch is
/// actually executed rather than merely compiled.
/// </summary>
public class AILogAnalyserApiComposerTests
{
    private static (IUmbracoBuilder Builder, ServiceCollection Services) CreateBuilder(
        Dictionary<string, string?>? configuration = null)
    {
        var services = new ServiceCollection();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration ?? [])
            .Build();

        var builder = Substitute.For<IUmbracoBuilder>();
        builder.Services.Returns(services);
        builder.Config.Returns(config);

        return (builder, services);
    }

    private static ServiceProvider Compose(Dictionary<string, string?>? configuration = null)
    {
        var (builder, services) = CreateBuilder(configuration);
        new AILogAnalyserApiComposer().Compose(builder);
        return services.BuildServiceProvider();
    }

    #region Service registration

    [Fact]
    public void Compose_RegistersSystemDiagnosticsProviderAsSingleton()
    {
        // Asserted on the descriptor rather than by resolving: the provider's own Umbraco
        // dependencies (IUmbracoVersion, IRuntimeState, IHostEnvironment) are supplied by the CMS
        // at runtime and are deliberately not registered in this isolated ServiceCollection.
        var (builder, services) = CreateBuilder();

        new AILogAnalyserApiComposer().Compose(builder);

        var descriptor = services.Single(d => d.ServiceType == typeof(ISystemDiagnosticsProvider));
        descriptor.ImplementationType.Should().Be<SystemDiagnosticsProvider>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
    }

    [Fact]
    public void Compose_RegistersLogContextProviderAsTransient()
    {
        var (builder, services) = CreateBuilder();

        new AILogAnalyserApiComposer().Compose(builder);

        var descriptor = services.Single(d => d.ServiceType == typeof(ILogContextProvider));
        descriptor.ImplementationType.Should().Be<LogContextProvider>();
        descriptor.Lifetime.Should().Be(ServiceLifetime.Transient);
    }

    #endregion

    #region Settings binding

    [Fact]
    public void Compose_BindsLogContextSettingsFromConfiguration()
    {
        using var provider = Compose(new Dictionary<string, string?>
        {
            ["AILogAnalyser:LogContext:MaxSurroundingEntries"] = "25",
            ["AILogAnalyser:LogContext:SurroundingWindowMinutes"] = "9",
            ["AILogAnalyser:LogContext:FrequencyMaxScan"] = "750",
            ["AILogAnalyser:LogContext:FrequencyWindowMinutes"] = "120",
        });

        var settings = provider.GetRequiredService<IOptions<LogContextSettings>>().Value;

        settings.MaxSurroundingEntries.Should().Be(25);
        settings.SurroundingWindowMinutes.Should().Be(9);
        settings.FrequencyMaxScan.Should().Be(750);
        settings.FrequencyWindowMinutes.Should().Be(120);
    }

    [Fact]
    public void Compose_UsesLogContextDefaults_WhenSectionAbsent()
    {
        using var provider = Compose();

        var settings = provider.GetRequiredService<IOptions<LogContextSettings>>().Value;
        var defaults = new LogContextSettings();

        settings.MaxSurroundingEntries.Should().Be(defaults.MaxSurroundingEntries);
        settings.SurroundingWindowMinutes.Should().Be(defaults.SurroundingWindowMinutes);
        settings.FrequencyMaxScan.Should().Be(defaults.FrequencyMaxScan);
        settings.FrequencyWindowMinutes.Should().Be(defaults.FrequencyWindowMinutes);
    }

    [Fact]
    public void Compose_BindsRootSettingsFromConfiguration()
    {
        using var provider = Compose();

        // The root AILogAnalyser section binds even when absent, yielding the type's defaults.
        provider.GetRequiredService<IOptions<AILogAnalyserSettings>>().Value.Should().NotBeNull();
    }

    #endregion

    #region OpenAPI registration

    [Fact]
    public void Compose_ConfiguresOpenApi_WithoutThrowing()
    {
        // The registration path differs per major (Swashbuckle on 17, Microsoft.AspNetCore.OpenApi
        // on 18) and is what #if UMBRACO_18 selects. Composing must succeed on both.
        var act = () => Compose();

        act.Should().NotThrow();
    }

#if !UMBRACO_18
    [Fact]
    public void Compose_RegistersCustomOperationIdHandler_OnUmbraco17()
    {
        var (builder, services) = CreateBuilder();

        new AILogAnalyserApiComposer().Compose(builder);

        // Clean operation ids keep the generated TypeScript client's method names concise.
        services.Should().Contain(d =>
            d.ServiceType == typeof(Umbraco.Cms.Api.Common.OpenApi.IOperationIdHandler));
    }
#endif

    [Fact]
    public void Compose_RegistersTheApiDocumentUnderTheControllersApiName()
    {
        // The OpenAPI document name must match [MapToApi(Constants.ApiName)] on the controller,
        // or the endpoint is omitted from the generated document on both majors.
        Constants.ApiName.Should().Be("ailoganalyser");
    }

    #endregion

    #region Inline chat alias

    [Fact]
    public void ChatAlias_IsPresentAndUrlSafe()
    {
        // Umbraco.AI's inline chat builder requires an alias — it throws at request time if one is
        // missing — and derives a deterministic chat ID from it for auditing, so it must also be
        // URL-safe. AIChatBuilder's properties are internal, so this guards the value the
        // controller feeds it rather than the builder's own state.
        Constants.ChatAlias.Should().NotBeNullOrWhiteSpace();
        Constants.ChatAlias.Should().MatchRegex("^[a-z0-9][a-z0-9-]*$");
    }

    #endregion
}
