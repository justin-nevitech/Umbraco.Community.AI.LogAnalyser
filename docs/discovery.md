# Development Discovery Log

Findings, issues encountered, and lessons learned during development of the AI.LogAnalyser test project and tooling.

## Umbraco v17 API Differences

### Attempt<T> Has Two Type Parameters

**Issue**: Umbraco v17's `ILogViewerService.GetPagedLogsAsync` returns `Attempt<PagedModel<ILogEntry>, LogViewerOperationStatus>` — a two-type-parameter struct — not the single-parameter `Attempt<T?>` from older Umbraco versions.

**Symptoms**: NSubstitute `.Returns()` calls failed with type conversion errors because `Attempt<PagedModel<ILogEntry>?>` doesn't implicitly convert to `Attempt<PagedModel<ILogEntry>, LogViewerOperationStatus>`.

**Fix**: Use the factory methods that produce the correct generic type:
```csharp
// Success
Attempt.SucceedWithStatus(LogViewerOperationStatus.Success, pagedModel);

// Failure
Attempt.FailWithStatus(LogViewerOperationStatus.CancelledByLogsSizeValidation, new PagedModel<ILogEntry>(0, []));
```

### LogViewerOperationStatus Enum Values

**Issue**: The enum values aren't documented. Had to build a throwaway console app to enumerate them.

**Values discovered**:
```
Success = 0
NotFoundLogSearch = 1
DuplicateLogSearch = 2
CancelledByLogsSizeValidation = 3
```

The initially guessed value `CancelledByNotification` does not exist on this enum — it exists on other Umbraco operation status enums but not this one.

### ILogEntry.Level Is Umbraco.Cms.Core.Logging.LogLevel, Not Serilog

**Issue**: Assumed `ILogEntry.Level` was `Serilog.Events.LogEventLevel` since Umbraco uses Serilog internally. It's actually `Umbraco.Cms.Core.Logging.LogLevel`, a separate enum.

**Values**:
```
Verbose = 0
Debug = 1
Information = 2
Warning = 3
Error = 4
Fatal = 5
```

**Lesson**: Umbraco wraps Serilog types with its own abstractions at the service layer. Don't assume the internal logging library types leak through to the service interfaces.

## Microsoft.Extensions.AI Version Conflict

**Issue**: Referencing `Microsoft.Extensions.AI` version `9.*` caused a `NU1605` package downgrade error because `Umbraco.AI 1.6.0` → `Umbraco.AI.Core 1.6.0` transitively requires `Microsoft.Extensions.AI >= 10.2.0`.

**Fix**: Pin the test project to `Microsoft.Extensions.AI` version `10.*` to match the transitive dependency chain.

**Lesson**: When targeting .NET 10 with Umbraco 17.x packages, the Microsoft.Extensions.AI dependency is v10+, not v9. Always check the transitive dependency chain when adding a direct reference to a package that's already pulled in transitively.

## ChatResponse.Text Returns Empty String, Not Null

**Issue**: The controller uses `response.Text ?? "No summary available."` as a null-coalescing fallback. Testing revealed that `ChatResponse.Text` returns `""` (empty string), never `null`, even when constructed with `new ChatMessage(ChatRole.Assistant, (string?)null)`.

**Root cause**: `Microsoft.Extensions.AI`'s `ChatResponse.Text` aggregates content items from the message. When no text content exists, it returns empty string rather than null.

**Impact**: The `?? "No summary available."` fallback in the controller never triggers. An empty AI response will produce an empty summary in the UI, not the "No summary available." placeholder.

**Recommendation**: If the fallback is important, change to `string.IsNullOrEmpty(response.Text) ? "No summary available." : response.Text`.

## FluentAssertions 8.x API Changes

**Issue**: `HaveCountLessOrEqualTo()` doesn't exist in FluentAssertions 8.x.

**Fix**: Use `HaveCountLessThanOrEqualTo()` instead. The method was renamed for consistency with other comparison methods in the 8.x release.

## IAIChatService Method Signature

**Issue**: The `Umbraco.AI.Core.Chat.IAIChatService` interface is not publicly documented. The `GetChatResponseAsync` method signature had to be inferred from the controller's call site:

```csharp
await _chatService.GetChatResponseAsync(messages, cancellationToken: ct);
```

The named `cancellationToken:` parameter indicates there are intermediate optional parameters (likely `ChatOptions? options = null`). NSubstitute mocking required matching on `Arg.Any<IList<ChatMessage>>()` and `cancellationToken: Arg.Any<CancellationToken>()` without specifying the middle parameters.

**Return type**: `Task<ChatResponse>` from `Microsoft.Extensions.AI` (not `ChatCompletion`, which was the pre-v10 name).

## Test Infrastructure Observations

### xUnit 2.9.3 Works on .NET 10

No issues with xUnit 2.x on `net10.0`. The `xunit` (2.9.3) and `xunit.runner.visualstudio` (2.8.2) packages resolved and ran fine. No need to upgrade to xUnit v3.

### NSubstitute Nullable Warnings with Attempt

When mocking `FailWithStatus` and passing `(PagedModel<ILogEntry>?)null` as the result, NSubstitute produces `CS8620` nullable warnings because the mock's return type inference doesn't perfectly align with the two-parameter `Attempt<T, TStatus>` struct's nullability annotations. These are warnings only — the mocks work correctly at runtime.

### Testing Private Static Methods Through the Public API

The controller's `Truncate`, `FormatSurroundingLogs`, and `FormatEntries` methods are `private static`. Rather than making them `internal` with `[InternalsVisibleTo]`, they were tested indirectly through the `Analyse` endpoint by asserting on the prompt content passed to the AI service mock. This keeps the production code's encapsulation intact while still achieving coverage.

## Test Project Package Versions

Final working versions for `net10.0`:

| Package | Version |
|---------|---------|
| Microsoft.NET.Test.Sdk | 18.9.0 |
| xunit | 2.* |
| xunit.runner.visualstudio | 4.0.0 |
| NSubstitute | 6.* |
| FluentAssertions | 8.* |
| Microsoft.Extensions.AI | 10.* |

## Umbraco 18 Dual-Major Support

**Finding**: Supporting Umbraco 17 and 18 from one codebase needed a non-standard multi-targeting approach.

- **Both majors are `net10.0`**, so the usual TFM-based multi-targeting (`net6.0`/`net8.0`) does not apply. An `UmbracoMajor` MSBuild switch on a single project was tried first and abandoned: **NuGet restore evaluates each project once with its default properties**, ignoring `AdditionalProperties` on a `ProjectReference`, so one solution could never restore the project at both majors and the two test sites could not coexist (`NU1107`). The shipped design is instead **two wrapper projects** (`.v17` / `.v18`) that compile the same shared sources from `src/Umbraco.Community.AI.LogAnalyser` (not a project — just a source folder) via `LogAnalyser.Shared.props`; only the Umbraco/Umbraco.AI ranges and the `UMBRACO_18` symbol differ. See [BUILDING.md](BUILDING.md).
- **Umbraco 18 removed Swashbuckle** for OpenAPI in favour of `Microsoft.AspNetCore.OpenApi`. The types the v17 composer relied on (`SwaggerGenOptions.SwaggerDoc`, `OpenApiInfo`, `BackOfficeSecurityRequirementsOperationFilterBase`, `IOperationIdHandler`/`OperationIdHandler`) are gone. The v18 replacement is `builder.AddBackOfficeOpenApiDocument(name, doc => doc.WithTitle(...).WithBackOfficeAuthentication().ConfigureOpenApiOptions(o => o.AddOperationTransformer(...)))`. This is the only code behind `#if UMBRACO_18`. Exact namespaces were confirmed by reading the `18.0.0-rc1` assembly metadata: `AddBackOfficeOpenApiDocument`/`WithTitle`/`ConfigureOpenApiOptions` live in `Umbraco.Cms.Api.Common.OpenApi`, `WithBackOfficeAuthentication` in `Umbraco.Cms.Api.Management.OpenApi`.
- **`Umbraco.AI` once blocked Umbraco 18 — resolved.** Its old `1.x` line pinned `Umbraco.Cms.Core` to `[17.4.0, 17.999.999)`, so an Umbraco 18 consumer site (and `TestSite.v18`) failed to restore with a hard `NU1107` conflict. `Umbraco.AI` has since moved to the same version-aligned convention as this package (`17.x` / `18.x` lines superseding `1.x`), so the 18.x variant now depends on `Umbraco.AI [18.0.0, 19.0.0)` and both majors are publishable. The 17.x floor of `17.4.0` is inherited from the lowest version-aligned `Umbraco.AI` 17.x release.

## Prompt Token Optimisation

**Finding**: The system-context assembly inventory was by far the largest, lowest-value part of every prompt.

- On a real site, `AppDomain.CurrentDomain.GetAssemblies()` through the old framework-denylist still yielded **~183 entries** (cloud SDKs, Lucene internals, serialisation libs, etc.) — roughly 1,500–2,200 tokens per request of transitive dependencies that are version-locked to the CMS and carry no independent diagnostic value.
- Switched to an **allowlist**: anything containing "umbraco", the application's own assembly, and a curated `RelevantAssemblyPrefixes` set of commonly-implicated infrastructure (search/db/email/imaging/etc.). Then **collapsed same-version sub-assemblies** by package family (first three name segments) into one line. Result: ~183 → ~40 lines.
- **`Properties` truncation** lowered from 8 KB to 2 KB (largest, lowest-signal field).
- **Restructured the messages for prompt caching**: all static content (instructions + diagnostics) is now a single `ChatRole.System` prefix; only the variable log data is in the user message; any tone addendum is appended last. This lets OpenAI/Anthropic prompt caching reuse the prefix across requests. Note: anything excluded from the assembly list still appears in the exception **stack trace** when it actually throws, so diagnostic coverage is preserved.

## Tooling: Stale `bin` After Renaming a Project

**Issue**: After renaming a test site project/assembly, Umbraco startup threw `FileNotFoundException: Could not load file or assembly '...TestSite'` from `FindAssembliesWithReferencesTo.ResolveAssemblies()`.

**Cause**: MSBuild does not remove differently-named outputs, so the old-named assembly lingered in `bin` alongside the new one. Umbraco's `TypeFinder` scans every assembly in `bin`, hit the orphan, and failed to load it (it is not listed in the new app's `.deps.json`).

**Fix**: Delete `bin`/`obj` once after any project/assembly rename and rebuild — `dotnet clean` and incremental builds do not purge old-named artifacts.

## Test Coverage Summary

153 tests, run against **both** Umbraco majors (152 on v18 — one assertion is Swashbuckle-specific
and guarded by `#if`). The test sources live once in a shared folder and are compiled by the
`.v17` / `.v18` wrapper projects, mirroring the package layout.

- **LogContextProviderTests** (31 tests): surrounding log retrieval, current entry exclusion, deduplication (consecutive same level+message, different messages, different levels, after entries), chronological ordering, empty/failed/null responses, max entries, field mapping, cancellation, frequency counting (exact match, case sensitivity, configuration).
- **AILogAnalyserApiControllerTests** (60 tests): validation, successful analysis, null response handling, optional fields, truncation, surrounding context formatting, frequency notes, timestamp handling, graceful degradation, system diagnostics, AI failure → 502, prompt structure (roles, headings, message count), and configurable per-request prompt options.
- **SystemDiagnosticsProviderTests** (53 tests): environment facts, ModelsBuilder mode presence/absence, `Lazy<string>` caching, connection-string redaction (the provider name is reported, the credentials never are), provider inference for SQLite/SQL Server/unknown, the relevant-package allowlist (kept vs. dropped families, entry-assembly matching), build-metadata stripping, and family collapsing (same-version sub-assemblies collapse, differing versions do not).
- **AILogAnalyserApiComposerTests** (9 tests): DI registrations and their lifetimes, settings binding and defaults, the inline-chat alias guard, and the OpenAPI registration — the last of which is the only `#if UMBRACO_18` branch, so running the suite from both wrappers exercises Swashbuckle on 17 and `Microsoft.AspNetCore.OpenApi` on 18.
