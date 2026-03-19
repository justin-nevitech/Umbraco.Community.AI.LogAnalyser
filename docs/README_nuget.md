# AI.Log Analyser 

[![Downloads](https://img.shields.io/nuget/dt/Umbraco.Community.AI.LogAnalyser?color=cc9900)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser/)
[![NuGet](https://img.shields.io/nuget/vpre/Umbraco.Community.AI.LogAnalyser?color=0273B3)](https://www.nuget.org/packages/Umbraco.Community.AI.LogAnalyser)
[![GitHub license](https://img.shields.io/github/license/justin-nevitech/Umbraco.Community.AI.LogAnalyser?color=8AB803)](https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser/blob/main/LICENSE)

An Umbraco package that adds AI-powered log analysis to the backoffice log viewer. Each log entry gets an "Analyse with AI" button that returns a concise, actionable summary using your configured AI provider.

Supports any AI provider available through [Umbraco.AI](https://www.nuget.org/packages/Umbraco.AI), including OpenAI, Anthropic, Google, Amazon Bedrock and Microsoft AI Foundry.

## Quick Start

```
dotnet add package Umbraco.Community.AI.LogAnalyser
```

You will also need at least one Umbraco.AI provider package installed and configured (e.g. `Umbraco.AI.OpenAI`).

Then navigate to **Settings > Log Viewer > Search** in the backoffice and click the AI icon on any log entry.