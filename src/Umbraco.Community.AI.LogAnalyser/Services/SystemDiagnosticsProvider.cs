using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Services;

namespace Umbraco.Community.AI.LogAnalyser.Services;

public class SystemDiagnosticsProvider : ISystemDiagnosticsProvider
{
    // Additional diagnostically-relevant assembly-name prefixes that do NOT contain the word
    // "umbraco" (anything containing "umbraco" is already included). These are infrastructure
    // packages whose failures commonly surface in Umbraco logs, where knowing the installed
    // version aids diagnosis. Extend this to surface more package families in the AI prompt.
    // (Serilog.* and Lucene.Net.* are intentionally omitted — they span many assemblies and add
    //  little beyond what the exception stack trace already shows; add them here if you want them.)
    private static readonly string[] RelevantAssemblyPrefixes =
    [
        "uSync",                // sync / deploy
        "Examine",              // search / indexing
        "NPoco",                // database / ORM (Umbraco's data layer)
        "Microsoft.Data.",      // database drivers (SqlClient / Sqlite)
        "MailKit", "MimeKit",   // email / SMTP
        "SixLabors",            // image processing (ImageSharp)
        "Smidge",               // asset bundling / minification
        "StackExchange.Redis",  // distributed cache / load balancing
        "Hangfire",             // background jobs
        "Newtonsoft.Json",      // JSON serialisation
    ];

    private readonly Lazy<string> _context;

    public SystemDiagnosticsProvider(
        IUmbracoVersion umbracoVersion,
        IRuntimeState runtimeState,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        _context = new Lazy<string>(() => BuildContext(umbracoVersion, runtimeState, hostEnvironment, configuration));
    }

    public string GetContext() => _context.Value;

    private static string BuildContext(
        IUmbracoVersion umbracoVersion,
        IRuntimeState runtimeState,
        IHostEnvironment hostEnvironment,
        IConfiguration configuration)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Umbraco: {umbracoVersion.SemanticVersion}");
        sb.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"Environment: {hostEnvironment.EnvironmentName}");
        sb.AppendLine($"Runtime mode: {runtimeState.Level}");

        // Database provider — SECURITY: only the inferred provider name (e.g. "SQLite", "SQL Server") is
        // included in the output. The raw connection string must NEVER be appended to the StringBuilder
        // as this context is sent to the AI provider.
        var connectionString = configuration.GetConnectionString("umbracoDbDSN") ?? "";
        var dbProvider = configuration["ConnectionStrings:umbracoDbDSN_ProviderName"] ?? "";
        sb.AppendLine($"Database provider: {(string.IsNullOrEmpty(dbProvider) ? InferDatabaseProvider(connectionString) : dbProvider)}");

        // ModelsBuilder mode
        var modelsMode = configuration["Umbraco:CMS:ModelsBuilder:ModelsMode"];
        if (!string.IsNullOrEmpty(modelsMode))
            sb.AppendLine($"ModelsBuilder mode: {modelsMode}");

        // Hosting model
        sb.AppendLine($"Hosting: {DetectHostingModel()}");

        // Application start time
        var process = Process.GetCurrentProcess();
        sb.AppendLine($"Application started: {process.StartTime:O}");

        var loaded = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => (Name: a.GetName().Name ?? "", Version: a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? a.GetName().Version?.ToString() ?? "?"));

        sb.Append(FormatInstalledPackages(loaded, Assembly.GetEntryAssembly()?.GetName().Name));

        return sb.ToString();
    }

    /// <summary>
    /// Filters the loaded assemblies down to the diagnostically-relevant ones and renders them as
    /// the "Installed packages:" block. Kept separate from <see cref="BuildContext"/> (and from
    /// the reflection that feeds it) so the filtering and collapsing rules can be unit tested
    /// against a fixed assembly list.
    /// </summary>
    internal static string FormatInstalledPackages(
        IEnumerable<(string Name, string Version)> loadedAssemblies,
        string? entryAssemblyName)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Installed packages:");

        var assemblies = loadedAssemblies
            .Where(a => !string.IsNullOrEmpty(a.Name) && IsRelevant(a.Name, entryAssemblyName))
            .Select(a => (a.Name, Version: CleanVersion(a.Version)))
            .ToList();

        // Collapse a package's many same-version sub-assemblies (e.g. Umbraco.AI.Agent.Core,
        // .Agent.Persistence.Sqlite, .Agent.Web.StaticAssets) into a single line, grouping by the
        // first three name segments. A group with one assembly prints its full name; otherwise it
        // prints "<family>.* <version> (N assemblies)". This keeps meaningful package/provider
        // names (e.g. Umbraco.AI.OpenAI) while trimming redundant internal sub-assemblies.
        var groups = assemblies
            .GroupBy(a => (Family: FamilyRoot(a.Name), a.Version))
            .OrderBy(g => g.Key.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(g => g.Key.Version, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            var count = group.Count();
            if (count == 1)
                sb.AppendLine($"  {group.First().Name} {group.Key.Version}");
            else
                sb.AppendLine($"  {group.Key.Family}.* {group.Key.Version} ({count} assemblies)");
        }

        return sb.ToString();
    }

    // Only surface diagnostically-relevant packages: anything Umbraco-related (any assembly whose
    // name contains "umbraco"), the application's own assembly, and the curated infrastructure
    // allowlist. The long tail of transitive dependencies (cloud SDKs, Lucene internals,
    // serialisation libraries, etc.) is version-locked to the CMS, is rarely consulted when
    // diagnosing a single log entry, and would otherwise dominate the prompt — so it's excluded
    // to keep the request small and fast.
    internal static bool IsRelevant(string name, string? entryAssemblyName) =>
        name.Contains("umbraco", StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, entryAssemblyName, StringComparison.Ordinal)
        || RelevantAssemblyPrefixes.Any(p => name.StartsWith(p, StringComparison.Ordinal));

    // The package-family key used to collapse sub-assemblies: the first three dot-separated
    // segments (or the whole name when it has three or fewer).
    internal static string FamilyRoot(string name)
    {
        var parts = name.Split('.');
        return parts.Length <= 3 ? name : string.Join('.', parts[..3]);
    }

    // Strips build metadata (the '+sha' suffix) from an informational version.
    internal static string CleanVersion(string version) =>
        version.Contains('+') ? version[..version.IndexOf('+')] : version;

    internal static string InferDatabaseProvider(string connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
            return "Unknown";

        // SQLite: check for common SQLite markers (file-based data source without Server= keyword)
        if (connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            return "SQLite";

        // SQL Server: explicit Server= or Initial Catalog= keywords
        if (connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase)
            || connectionString.Contains("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
            return "SQL Server";

        return "Unknown";
    }

    private static string DetectHostingModel()
    {
        if (Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") != null)
            return "Azure App Service";

        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
            return "Docker/Container";

        var processName = Process.GetCurrentProcess().ProcessName;
        if (processName.Equals("w3wp", StringComparison.OrdinalIgnoreCase))
            return "IIS (in-process)";

        if (processName.Equals("iisexpress", StringComparison.OrdinalIgnoreCase))
            return "IIS Express";

        return "Kestrel";
    }

}
