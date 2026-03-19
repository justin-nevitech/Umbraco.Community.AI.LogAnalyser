using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Hosting;
using Umbraco.Cms.Core.Configuration;
using Umbraco.Cms.Core.Services;

namespace AI.LogAnalyser.Services;

public class SystemDiagnosticsProvider : ISystemDiagnosticsProvider
{
    private readonly Lazy<string> _context;

    public SystemDiagnosticsProvider(
        IUmbracoVersion umbracoVersion,
        IRuntimeState runtimeState,
        IHostEnvironment hostEnvironment)
    {
        _context = new Lazy<string>(() => BuildContext(umbracoVersion, runtimeState, hostEnvironment));
    }

    public string GetContext() => _context.Value;

    private static string BuildContext(
        IUmbracoVersion umbracoVersion,
        IRuntimeState runtimeState,
        IHostEnvironment hostEnvironment)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"Umbraco: {umbracoVersion.SemanticVersion}");
        sb.AppendLine($".NET: {RuntimeInformation.FrameworkDescription}");
        sb.AppendLine($"OS: {RuntimeInformation.OSDescription}");
        sb.AppendLine($"Environment: {hostEnvironment.EnvironmentName}");
        sb.AppendLine($"Runtime mode: {runtimeState.Level}");

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
}
