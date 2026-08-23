# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Umbraco v17.4+ backoffice package that adds AI-powered log analysis to the log viewer. Users click "Analyse with AI" on any log entry to get a structured analysis (Summary, Cause, Recommended Action) via the Umbraco.AI abstraction layer. Published as NuGet package `Umbraco.Community.AI.LogAnalyser`.

### Dual-major (Umbraco 17 / 18) support

Both majors are `net10.0`, so TFM multi-targeting is not used. Instead there are **two wrapper package projects compiling the same sources**: `Umbraco.Community.AI.LogAnalyser.v17` (Umbraco `[17.4.0, 18.0.0)`) and `Umbraco.Community.AI.LogAnalyser.v18` (Umbraco `[18.0.0, 19.0.0)`, defines `UMBRACO_18`). `Umbraco.Community.AI.LogAnalyser` is **not a project** — it is the shared source folder (C# sources, TypeScript client, built `wwwroot/App_Plugins`), and both wrappers import `LogAnalyser.Shared.props` and `SharedBackofficeAssets.targets` from it, so new files are picked up automatically. Packaging is version-aligned under one PackageId: the package major tracks the Umbraco major, and `Umbraco.AI` follows the same convention (its old `1.x` line was superseded by `17.x`/`18.x`). The only version-specific code is the OpenAPI registration in `Composers/AILogAnalyserApiComposer.cs` (Umbraco 18 removed Swashbuckle) — which is *why* two projects are needed: NuGet restore evaluates each project once with its default properties, so a single project with an MSBuild switch could not restore at both majors in one solution. Full details in `docs/BUILDING.md`.

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
Output goes to `src/Umbraco.Community.AI.LogAnalyser/wwwroot/App_Plugins/Umbraco.Community.AI.LogAnalyser/`.

### Generate OpenAPI Client
```bash
cd src/Umbraco.Community.AI.LogAnalyser/Client
npm run generate-client  # Requires test site running on https://localhost:44300
```

### Backend
```bash
cd src
dotnet build Umbraco.Community.AI.LogAnalyser.sln
dotnet run --project Umbraco.Community.AI.LogAnalyser.TestSite.v17  # Run Umbraco 17 test site
```
Test site login: admin@example.com / 1234567890 (SQLite, unattended install). There are two test sites, both in the solution and runnable at the same time: `TestSite.v17` (Umbraco 17, `https://localhost:44300`, references `Umbraco.Community.AI.LogAnalyser.v17`) and `TestSite.v18` (Umbraco 18, `https://localhost:44301`, references `Umbraco.Community.AI.LogAnalyser.v18`). See `docs/BUILDING.md`.

### Tests
```bash
cd src
dotnet test Umbraco.Community.AI.LogAnalyser.sln                 # Both majors
dotnet test Umbraco.Community.AI.LogAnalyser.Tests.v17           # Umbraco 17 only
dotnet test Umbraco.Community.AI.LogAnalyser.Tests.v18           # Umbraco 18 only
dotnet test Umbraco.Community.AI.LogAnalyser.Tests.v17 --filter "ClassName~LogContextProviderTests"  # Single test class
dotnet test Umbraco.Community.AI.LogAnalyser.Tests.v17 --filter "Name~GetErrorFrequency"            # Tests matching name
```
Uses xUnit, NSubstitute, FluentAssertions.

The test suite mirrors the package layout: `Umbraco.Community.AI.LogAnalyser.Tests` is a **shared
source folder, not a project**, and the `.v17` / `.v18` wrappers compile the same test files against
their respective package variant (the `.v18` wrapper defines `UMBRACO_18`). Every test therefore runs
on both majors, so the `#if UMBRACO_18` branch is executed rather than merely compiled. Add new tests
to the shared folder; both wrappers pick them up. Pure helpers on the services are `internal` and
exposed to both test assemblies via `InternalsVisibleTo` in `LogAnalyser.Shared.props`.

### Package
```bash
# Umbraco 17 variant
dotnet pack src/Umbraco.Community.AI.LogAnalyser.v17/Umbraco.Community.AI.LogAnalyser.v17.csproj -c Release
# Umbraco 18 variant
dotnet pack src/Umbraco.Community.AI.LogAnalyser.v18/Umbraco.Community.AI.LogAnalyser.v18.csproj -c Release
```

## Architecture

### Backend (.NET, Razor SDK)
- **Controllers**: `AILogAnalyserApiController` — single POST endpoint at `/umbraco/ailoganalyser/api/v1.0/analyse`. Builds the AI prompt from the log entry, surrounding context, error frequency, and system diagnostics, then returns markdown. Static content (instructions + diagnostics) goes in the system message as a cache-friendly stable prefix; only the variable log data goes in the user message.
- **Services**: `LogContextProvider` fetches surrounding log entries (with deduplication) and error frequency via `ILogViewerService`. `SystemDiagnosticsProvider` gathers system context (Umbraco version, .NET, OS, database, and a curated subset of relevant installed packages — Umbraco-related + key infra, sub-assemblies collapsed) — lazy-initialized singleton.
- **Composers**: `AILogAnalyserApiComposer` registers DI services, binds settings from `appsettings.json` under the `AILogAnalyser` section, and configures the backoffice OpenAPI document (Swashbuckle on Umbraco 17, `Microsoft.AspNetCore.OpenApi` on Umbraco 18 — the only code behind `#if UMBRACO_18`).
- **Models**: DTOs for request/response and log context. Large fields are truncated (8 KB for message/exception; properties get a tighter 2 KB cap).

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

Tagging a version (e.g., `17.2.3` or `18.0.0`, or a prerelease like `18.1.0-rc1`) triggers the GitHub Actions workflow (`.github/workflows/release.yml`) which packs and pushes to NuGet. Packaging is version-aligned: the workflow selects the package project from the tag's leading major number (`17.x` → the `.v17` project, `18.x` → the `.v18` project) and fails on any other major. See `docs/BUILDING.md`.

## Key Conventions

- C# uses file-scoped namespaces, nullable reference types, async/await, structured Serilog logging
- TypeScript strict mode with Lit decorators (`@customElement`, `@state`)
- Frontend integrates via Umbraco's backoffice extension point system (`umbraco-package.json` manifest)
- AI provider is abstracted through `Umbraco.AI` — the package itself is provider-agnostic

## Umbraco Best Practices

- **DI registration**: Use `IComposer` implementations (not `Startup.cs`) to register services via `IUmbracoBuilder`. Singletons for stateless/cached services, transient for services with per-request Umbraco dependencies like `ILogViewerService`.
- **Backoffice API controllers**: Inherit from a base controller with `[BackOfficeRoute]`, `[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]`, and `[MapToApi]`. Use `[ApiVersion]` on the concrete controller. This ensures correct routing, auth, and Swagger grouping.
- **Swagger/OpenAPI**: This is the one area that differs between Umbraco majors, isolated behind `#if UMBRACO_18` in the composer. On **Umbraco 17** (Swashbuckle): register a `SwaggerDoc` per API group, subclass `BackOfficeSecurityRequirementsOperationFilterBase` for auth, and use a custom `OperationIdHandler` for clean generated client method names. On **Umbraco 18** (`Microsoft.AspNetCore.OpenApi`, Swashbuckle removed): use `builder.AddBackOfficeOpenApiDocument(name, doc => doc.WithTitle(...).WithBackOfficeAuthentication().ConfigureOpenApiOptions(o => o.AddOperationTransformer(...)))`. The document name must match `[MapToApi(Constants.ApiName)]` on the controller. See `docs/BUILDING.md`.
- **Umbraco service return types**: `ILogViewerService.GetPagedLogsAsync` returns `Attempt<PagedModel<T>, TStatus>` — always check `.Success` and null-check `.Result` before accessing `.Items`.
- **Umbraco configuration**: Bind settings via `builder.Config.GetSection()` into an `IOptions<T>` wrapper. Use `builder.Services.Configure<T>()` in the composer, inject `IOptions<T>` in consuming services.

## .NET Best Practices

- **CancellationToken**: Accept `CancellationToken` on all async public methods and pass it through to downstream calls. Use `ct.ThrowIfCancellationRequested()` at the start of CPU-bound or long-running methods.
- **Structured logging**: Use Serilog message templates with named placeholders (`{ContextMs}`, `{FrequencyCount}`) — never string interpolation in log calls. Log at appropriate levels: `Information` for performance metrics, `Warning` for non-fatal failures (e.g. context fetch), `Error` for request-blocking failures.
- **Parallel async work**: Use `Task.WhenAll()` when multiple independent I/O operations can run concurrently (see `GetSurroundingLogsAsync` and the controller's context+frequency fetch).
- **Options pattern**: Use `IOptions<T>` for configuration binding. Read `.Value` once in the constructor for transient services. Define a `const string SectionName` on the settings class for the config path.
- **Defensive string handling**: Use `StringComparison.Ordinal` for case-sensitive log message matching, `StringComparison.OrdinalIgnoreCase` for infrastructure strings. Use `string.IsNullOrWhiteSpace()` for user-facing input validation.
- **Interface-based design**: Define `I*Provider`/`I*Service` interfaces for all services to enable DI and unit testing with NSubstitute mocks. Keep static helper methods (formatting, truncation) as `private static` on the consuming class.
