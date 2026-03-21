# AI.Log Analyser

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.AI.LogAnalyser?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.AI.LogAnalyser?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser)
[![GitHub license](https://img.shields.io/github/license/justin-nevitech/Umbraco.Community.AI.LogAnalyser?color=8AB803)](https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser/blob/main/LICENSE)

An Umbraco package that adds AI-powered log analysis to the backoffice log viewer. Each log entry gets an "Analyse with AI" button that returns a concise, actionable summary using your configured AI provider.

Supports any AI provider available through [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI), including OpenAI, Anthropic, Google, Amazon Bedrock and Microsoft AI Foundry.

<img alt="AI Log Analysis modal showing a summary, cause and recommended action for an error log entry" src="https://raw.githubusercontent.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser/main/docs/screenshot.png" width="700">

## Quick Start

```
dotnet add package Umbraco.Community.AI.LogAnalyser
```

You will also need at least one Umbraco.AI provider package installed and configured (e.g. `Umbraco.AI.OpenAI`).

Then navigate to **Settings > Log Viewer > Search** in the backoffice and click the AI icon on any log entry.

## Features

- One-click AI analysis of any log entry from the backoffice log viewer
- Structured response with summary, likely cause and recommended action
- Includes surrounding log entries for sequence-of-events context
- Detects error frequency to distinguish one-off vs systemic issues
- Includes system diagnostics (Umbraco version, .NET, OS, database provider, hosting model, assemblies) for environment-aware analysis
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

The package sends the log entry details (level, message, template, exception, properties), surrounding log entries, error frequency, and system diagnostics (Umbraco version, .NET, OS, database provider, hosting model, ModelsBuilder mode, loaded assemblies). No content data, user data, or credentials are sent.

## Documentation

Full documentation and source code available on [GitHub](https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser).

---

[Predict icons created by kerismaker - Flaticon](https://www.flaticon.com/free-icons/predict "predict icons")
