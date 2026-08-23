# AI Log Analyser

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.AI.LogAnalyser?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.AI.LogAnalyser?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser)
[![GitHub license](https://img.shields.io/github/license/justin-nevitech/Umbraco.Community.AI.LogAnalyser?color=8AB803)](https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser/blob/main/LICENSE)

An Umbraco package that adds AI-powered log analysis to the backoffice log viewer. Each log entry gets an "Analyse with AI" button that returns a concise, actionable summary using your configured AI provider.

Supports any AI provider available through [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI), including OpenAI, Anthropic, Google, Amazon Bedrock and Microsoft AI Foundry.

![AI Log Analysis modal showing a summary, cause and recommended action for an error log entry](https://raw.githubusercontent.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser/main/docs/screenshot.png)

## Compatibility

Packaging is **version-aligned** — the package major matches your Umbraco major:

| Umbraco | Package version | Status |
|---------|-----------------|--------|
| 17.x    | `17.x`          | ✅ Supported (requires Umbraco 17.4.0+) |
| 18.x    | `18.x`          | ✅ Supported |

Umbraco 18 dropped Swashbuckle, so the OpenAPI integration uses the `Microsoft.AspNetCore.OpenApi` stack on that major; everything else is shared code.

> **Pin the major when installing.** Both majors publish under the same package ID, and NuGet resolves the *latest* version rather than the one matching your Umbraco major — so a bare `dotnet add package` on an Umbraco 17 site will pull the `18.x` package and fail with a `NU1107` version conflict.

## Quick Start

Install the package, pinning the major that matches your Umbraco version:

```
# Umbraco 17
dotnet add package Umbraco.Community.AI.LogAnalyser --version "17.*"

# Umbraco 18
dotnet add package Umbraco.Community.AI.LogAnalyser --version "18.*"
```

You will also need at least one Umbraco.AI provider package installed and configured (e.g. `Umbraco.AI.OpenAI`).

Then navigate to **Settings > Log Viewer > Search** in the backoffice and click the AI icon on any log entry.

## Features

- One-click AI analysis of any log entry from the backoffice log viewer
- Structured response with summary, likely cause and recommended action
- Includes surrounding log entries for sequence-of-events context
- Detects error frequency to distinguish one-off vs systemic issues
- Includes system diagnostics (Umbraco version, .NET, OS, database provider, hosting model, and a focused list of relevant installed packages) for environment-aware analysis
- Logs performance diagnostics (context gathering time, AI response time) to the Umbraco log
- Renders responses as formatted markdown
- Works with any AI provider configured via Umbraco.AI
- Fully configurable via `appsettings.json`

## Configuration

All settings are optional with sensible defaults:

```json
{
  "AILogAnalyser": {
    "LogContext": {
      "MaxSurroundingEntries": 10,
      "SurroundingWindowMinutes": 5,
      "FrequencyMaxScan": 500,
      "FrequencyWindowMinutes": 60
    }
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `MaxSurroundingEntries` | `10` | Log entries to fetch before/after the selected entry |
| `SurroundingWindowMinutes` | `5` | Time window for surrounding entries |
| `FrequencyMaxScan` | `500` | Max entries to scan for frequency counting |
| `FrequencyWindowMinutes` | `60` | Time window for error frequency counting |

## What context is sent to the AI?

The package sends the log entry details (level, message, template, exception, properties), surrounding log entries, error frequency, and system diagnostics (Umbraco version, .NET, OS, database provider, hosting model, ModelsBuilder mode, and a focused list of relevant installed packages). No content data, user data, or credentials are sent. Prompts are kept token-efficient — static instructions and system context form a stable, cache-friendly prefix, and only the variable log data changes per request.

## Author

Created and maintained by [Justin Neville](https://www.nevitech.co.uk) at
[Nevitech IT Solutions Ltd](https://www.nevitech.co.uk).

## Documentation

Full documentation and source code available on [GitHub](https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser).

---

[Predict icons created by kerismaker - Flaticon](https://www.flaticon.com/free-icons/predict "predict icons")
