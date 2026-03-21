# AI.Log Analyser

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.AI.LogAnalyser?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.AI.LogAnalyser?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser)
[![GitHub license](https://img.shields.io/github/license/justin-nevitech/Umbraco.Community.AI.LogAnalyser?color=8AB803)](../LICENSE)

An Umbraco package that adds AI-powered log analysis to the backoffice log viewer. Each log entry gets an "Analyse with AI" button that sends the log message, level, timestamp, exception and properties to a configured AI provider and returns a concise, actionable summary directly in the backoffice.

Supports any AI provider available through the [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI) abstraction layer, including OpenAI, Anthropic, Google, Amazon Bedrock and Microsoft AI Foundry.

<img alt="AI Log Analysis modal showing a summary, cause and recommended action for an error log entry" src="https://raw.githubusercontent.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser/main/docs/screenshot.png" width="700">

## Features

- Adds an **AI** column to the log viewer search results
- One-click analysis of any log entry with context-aware prompts
- Returns a structured analysis with a plain-language summary, likely cause and suggested next steps
- Includes **surrounding log entries** (before and after) so the AI can understand the sequence of events
- Detects **error frequency** — tells the AI how many times the same error appeared in the last hour
- Includes **system diagnostics** (Umbraco version, .NET version, OS, database provider, hosting model, ModelsBuilder mode, loaded assemblies) for richer, environment-aware analysis
- Logs **performance diagnostics** (context gathering time, AI response time, total request time) to the Umbraco log
- Deduplicates repeated log entries in the context window to keep prompts concise
- Truncates oversized exception and property fields to prevent excessive AI prompt sizes
- Works with any AI provider configured via Umbraco.AI
- Renders AI responses as formatted markdown with syntax highlighting, links and code blocks
- Fully configurable context window and frequency scan settings via `appsettings.json`

## Installation

Add the package to an existing Umbraco website (v17.2+) from NuGet:

```
dotnet add package Umbraco.Community.AI.LogAnalyser
```

You will also need at least one Umbraco.AI provider package installed and configured, for example:

```
dotnet add package Umbraco.AI.OpenAI
```

Configure your chosen AI provider in `appsettings.json` as per the [Umbraco.AI documentation](https://docs.umbraco.com/umbraco-cms/reference/artificial-intelligence).

## Usage

1. Navigate to **Settings > Log Viewer** in the Umbraco backoffice
2. Switch to the **Search** tab
3. Click the AI icon on any log entry
4. View the AI-generated analysis in the modal dialog
5. Click **Re-analyse** to get a fresh analysis if needed

## Configuration

The package works out of the box with sensible defaults. You can optionally customise the log context behaviour in `appsettings.json`:

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
| `MaxSurroundingEntries` | `10` | Maximum number of log entries to fetch before and after the selected entry |
| `SurroundingWindowMinutes` | `5` | Time window (in minutes) to search for surrounding entries |
| `FrequencyMaxScan` | `500` | Maximum number of log entries to scan when calculating error frequency |
| `FrequencyWindowMinutes` | `60` | Time window (in minutes) to look back when counting how often the same message appeared |

All settings are optional. If the `AILogAnalyser:LogContext` section is omitted entirely, the defaults above are used.

## What context is sent to the AI?

When you click the AI button, the following information is sent to your configured AI provider:

| Data | Source |
|------|--------|
| Log level, timestamp, rendered message | The selected log entry |
| Message template | Serilog structured logging template (e.g. `"Failed to resolve content by route '{Route}'"`) |
| Exception & stack trace | If present on the log entry (truncated to 8 KB) |
| Structured properties | Key-value pairs from the Serilog log event (truncated to 8 KB) |
| Surrounding log entries | Configurable entries before and after (default: 10, within a 5-minute window), deduplicated |
| Error frequency | How many times the exact same message appeared in the last hour (configurable) |
| Umbraco version | e.g. `17.2.2` |
| .NET runtime version | e.g. `.NET 10.0.0` |
| Operating system | e.g. `Windows 11`, `Linux 6.x` |
| Environment name | `Development`, `Staging`, `Production` |
| Runtime mode | Umbraco runtime level |
| Database provider | SQL Server, SQLite, or as configured |
| ModelsBuilder mode | e.g. `InMemoryAuto`, `SourceCodeManual` |
| Hosting model | Azure App Service, Docker/Container, IIS, IIS Express, or Kestrel |
| Application start time | When the process was started |
| Loaded assemblies | Non-framework assemblies and their versions (Umbraco packages, custom code, etc.) |

No content data, user data, or credentials are sent. All requests go directly from your server to your configured AI provider.

## Performance Diagnostics

The package logs timing information to the Umbraco log at `Information` level so you can monitor performance:

- **Context gathering**: Time taken to fetch surrounding logs and error frequency, plus entry counts
- **AI response**: Time for the AI provider to respond, total request duration, and prompt length
- **AI failures**: Logged at `Error` level with elapsed time

These entries are searchable in the log viewer under the `AI.LogAnalyser.Controllers.AILogAnalyserApiController` source.

## Architecture

```
Browser (Backoffice)                     Server
+---------------------------+            +-----------------------------------+
| LogViewerEnhancer         |            | AILogAnalyserApiController        |
|  - Polls log viewer DOM   |   POST     |  - Validates & truncates request  |
|  - Injects AI buttons     | ---------> |  - Fetches surrounding logs       |
|  - Opens modal dialog     |            |  - Counts error frequency         |
|                           |            |  - Builds structured prompt       |
| LogAiSummaryDialog        |   JSON     |  - Calls IAIChatService           |
|  - Renders markdown       | <--------- |  - Returns markdown response      |
|  - Shows level badge      |            |  - Logs performance diagnostics   |
|  - Re-analyse button      |            |                                   |
+---------------------------+            | SystemDiagnosticsProvider         |
                                         |  - Environment info (cached)      |
                                         |                                   |
                                         | LogContextProvider                |
                                         |  - Surrounding entries            |
                                         |  - Error frequency counting       |
                                         |  - Deduplication                  |
                                         |  - Configurable via appsettings   |
                                         +-----------------------------------+
```

## Troubleshooting

**"AI provider unavailable" error**
- Ensure you have an AI provider package installed (e.g. `Umbraco.AI.OpenAI`)
- Check your `appsettings.json` has valid AI provider configuration
- Verify your API key is correct and has available quota

**AI button doesn't appear**
- Make sure you're on the **Search** tab of the log viewer (not the Overview tab)
- Check the browser console for any JavaScript errors
- The package requires Umbraco v17.2 or later

**Analysis is slow**
- Response time depends on your AI provider and model choice
- Larger log entries with long stack traces take longer to analyse
- Reduce `MaxSurroundingEntries` or `SurroundingWindowMinutes` to send less context
- Check the performance diagnostics in the log viewer for timing breakdowns

## Contributing

Contributions to this package are most welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md).

## Acknowledgments

- Scaffolded with the [Umbraco Opinionated Package Starter Template](https://github.com/jcdcdev/Umbraco.Community.Templates)
- Built with [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI) for AI provider abstraction
- Uses [Lit](https://lit.dev/) for the frontend web components
- [Predict icons created by kerismaker - Flaticon](https://www.flaticon.com/free-icons/predict "predict icons")
