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
- Returns a plain-language summary, potential cause and suggested next steps
- Includes system diagnostics (Umbraco version, .NET version, OS, loaded assemblies) for richer analysis
- Works with any AI provider configured via Umbraco.AI

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
4. View the AI-generated summary in the modal dialog

## Contributing

Contributions to this package are most welcome! Please read the [Contributing Guidelines](CONTRIBUTING.md).

## Acknowledgments

- Scaffolded with the [Umbraco Opinionated Package Starter Template](https://github.com/jcdcdev/Umbraco.Community.Templates)
- Built with [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI) for AI provider abstraction
- Uses [Lit](https://lit.dev/) for the frontend web components