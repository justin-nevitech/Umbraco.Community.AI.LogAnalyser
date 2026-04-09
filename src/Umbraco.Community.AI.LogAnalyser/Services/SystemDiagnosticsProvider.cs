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

        sb.AppendLine("Assemblies:");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic)
            .Select(a => (Name: a.GetName().Name ?? "", Version: a.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? a.GetName().Version?.ToString() ?? "?"))
            .Where(a => !string.IsNullOrEmpty(a.Name)
                && !a.Name.StartsWith("System.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.Extensions.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.CodeAnalysis.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.CSharp", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.Win32.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.VisualStudio.", StringComparison.Ordinal)
                && !a.Name.StartsWith("Microsoft.WebTools.", StringComparison.Ordinal)
                && !a.Name.Equals("netstandard", StringComparison.Ordinal))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var (name, version) in assemblies)
        {
            var ver = version.Contains('+') ? version[..version.IndexOf('+')] : version;
            sb.AppendLine($"  {name} {ver}");
        }

        return sb.ToString();
    }

    private static string InferDatabaseProvider(string connectionString)
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
