# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Umbraco v17.2+ backoffice package that adds AI-powered log analysis to the log viewer. Users click "Analyse with AI" on any log entry to get a structured analysis (Summary, Cause, Recommended Action) via the Umbraco.AI abstraction layer. Published as NuGet package `Umbraco.Community.AI.LogAnalyser`.

## Build & Development Commands

### Prerequisites
- .NET 10.0 SDK
- Node.js LTS 20.17.0+

### Frontend (Client)
```bash
cd src/Umbraco.Community.AI.LogAnalyser/Client
npm install
npm run build          # TypeScript compile + Vite build
npm run watch          # Dev mode with file watching
```
Output goes to `src/Umbraco.Community.AI.LogAnalyser/wwwroot/App_Plugins/AILogAnalyser/`.

### Generate OpenAPI Client
```bash
cd src/Umbraco.Community.AI.LogAnalyser/Client
npm run generate-client  # Requires test site running on https://localhost:44300
```

### Backend
```bash
cd src
dotnet build Umbraco.Community.AI.LogAnalyser.sln
dotnet run --project Umbraco.Community.AI.LogAnalyser.TestSite  # Run test Umbraco site
```
Test site login: admin@example.com / 1234567890 (SQLite, unattended install).

### Tests
```bash
cd src
dotnet test Umbraco.Community.AI.LogAnalyser.Tests              # Run all tests
dotnet test Umbraco.Community.AI.LogAnalyser.Tests --filter "ClassName~LogContextProviderTests"  # Single test class
dotnet test Umbraco.Community.AI.LogAnalyser.Tests --filter "Name~GetErrorFrequency"            # Tests matching name
```
Uses xUnit, NSubstitute, FluentAssertions.

### Package
```bash
dotnet pack src/Umbraco.Community.AI.LogAnalyser/Umbraco.Community.AI.LogAnalyser.csproj -c Release
```

## Architecture

### Backend (.NET, Razor SDK)
- **Controllers**: `AILogAnalyserApiController` — single POST endpoint at `/umbraco/ailoganalyser/api/v1.0/analyse`. Builds an AI prompt with log entry, surrounding context, error frequency, and system diagnostics, then returns markdown.
- **Services**: `LogContextProvider` fetches surrounding log entries (with deduplication) and error frequency via `ILogViewerService`. `SystemDiagnosticsProvider` gathers system context (Umbraco version, .NET, OS, database, assemblies) — lazy-initialized singleton.
- **Composers**: `AILogAnalyserApiComposer` registers DI services, binds `LogContextSettings` from `appsettings.json` under `AILogAnalyser:LogContext`, and configures Swagger.
- **Models**: DTOs for request/response and log context. Large fields truncated to 8192 chars.

### Frontend (TypeScript, Lit, Vite)
- **`index.ts`**: `LogViewerEnhancer` polls the backoffice DOM every 1s, injects AI buttons into log rows through shadow DOM boundaries. Uses WeakSet to track enhanced rows.
- **`log-ai-summary-dialog.element.ts`**: Lit web component (`<log-ai-summary-dialog>`) extending `UmbModalBaseElement`. Fetches analysis, renders markdown via marked.js, handles loading/error states.
- **`log-ai-summary.modal-token.ts`**: Umbraco modal token definition.
- All `@umbraco-cms/*` packages are externalized in Vite (provided by the backoffice at runtime).

### Configuration
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

## Release Process

Tagging a semantic version (e.g., `1.2.3`) triggers the GitHub Actions workflow (`.github/workflows/release.yml`) which packs and pushes to NuGet.

## Key Conventions

- C# uses file-scoped namespaces, nullable reference types, async/await, structured Serilog logging
- TypeScript strict mode with Lit decorators (`@customElement`, `@state`)
- Frontend integrates via Umbraco's backoffice extension point system (`umbraco-package.json` manifest)
- AI provider is abstracted through `Umbraco.AI` — the package itself is provider-agnostic

## Umbraco Best Practices

- **DI registration**: Use `IComposer` implementations (not `Startup.cs`) to register services via `IUmbracoBuilder`. Singletons for stateless/cached services, transient for services with per-request Umbraco dependencies like `ILogViewerService`.
- **Backoffice API controllers**: Inherit from a base controller with `[BackOfficeRoute]`, `[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]`, and `[MapToApi]`. Use `[ApiVersion]` on the concrete controller. This ensures correct routing, auth, and Swagger grouping.
- **Swagger/OpenAPI**: Register a `SwaggerDoc` per API group in the composer. Subclass `BackOfficeSecurityRequirementsOperationFilterBase` for auth requirements. Use a custom `OperationIdHandler` scoped to this package's namespace for clean generated TypeScript client method names.
- **Umbraco service return types**: `ILogViewerService.GetPagedLogsAsync` returns `Attempt<PagedModel<T>, TStatus>` — always check `.Success` and null-check `.Result` before accessing `.Items`.
- **Umbraco configuration**: Bind settings via `builder.Config.GetSection()` into an `IOptions<T>` wrapper. Use `builder.Services.Configure<T>()` in the composer, inject `IOptions<T>` in consuming services.

## .NET Best Practices

- **CancellationToken**: Accept `CancellationToken` on all async public methods and pass it through to downstream calls. Use `ct.ThrowIfCancellationRequested()` at the start of CPU-bound or long-running methods.
- **Structured logging**: Use Serilog message templates with named placeholders (`{ContextMs}`, `{FrequencyCount}`) — never string interpolation in log calls. Log at appropriate levels: `Information` for performance metrics, `Warning` for non-fatal failures (e.g. context fetch), `Error` for request-blocking failures.
- **Parallel async work**: Use `Task.WhenAll()` when multiple independent I/O operations can run concurrently (see `GetSurroundingLogsAsync` and the controller's context+frequency fetch).
- **Options pattern**: Use `IOptions<T>` for configuration binding. Read `.Value` once in the constructor for transient services. Define a `const string SectionName` on the settings class for the config path.
- **Defensive string handling**: Use `StringComparison.Ordinal` for case-sensitive log message matching, `StringComparison.OrdinalIgnoreCase` for infrastructure strings. Use `string.IsNullOrWhiteSpace()` for user-facing input validation.
- **Interface-based design**: Define `I*Provider`/`I*Service` interfaces for all services to enable DI and unit testing with NSubstitute mocks. Keep static helper methods (formatting, truncation) as `private static` on the consuming class.
