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
| Microsoft.NET.Test.Sdk | 18.6.0 |
| xunit | 2.* |
| xunit.runner.visualstudio | 3.1.5 |
| NSubstitute | 5.* |
| FluentAssertions | 8.* |
| Microsoft.Extensions.AI | 10.* |

## Umbraco 18 Dual-Major Support

**Finding**: Supporting Umbraco 17 and 18 from one codebase needed a non-standard multi-targeting approach.

- **Both majors are `net10.0`**, so the usual TFM-based multi-targeting (`net6.0`/`net8.0`) does not apply. Used a single `UmbracoMajor` MSBuild switch that selects the Umbraco package range and defines `UMBRACO_18`.
- **Umbraco 18 removed Swashbuckle** for OpenAPI in favour of `Microsoft.AspNetCore.OpenApi`. The types the v17 composer relied on (`SwaggerGenOptions.SwaggerDoc`, `OpenApiInfo`, `BackOfficeSecurityRequirementsOperationFilterBase`, `IOperationIdHandler`/`OperationIdHandler`) are gone. The v18 replacement is `builder.AddBackOfficeOpenApiDocument(name, doc => doc.WithTitle(...).WithBackOfficeAuthentication().ConfigureOpenApiOptions(o => o.AddOperationTransformer(...)))`. This is the only code behind `#if UMBRACO_18`. Exact namespaces were confirmed by reading the `18.0.0-rc1` assembly metadata: `AddBackOfficeOpenApiDocument`/`WithTitle`/`ConfigureOpenApiOptions` live in `Umbraco.Cms.Api.Common.OpenApi`, `WithBackOfficeAuthentication` in `Umbraco.Cms.Api.Management.OpenApi`.
- **`Umbraco.AI` blocks Umbraco 18 today.** The latest release (`1.14.0`) pins `Umbraco.Cms.Core` to `[17.4.0, 17.999.999)`. The package *project* still compiles in `UmbracoMajor=18` mode because its direct `Umbraco.Cms.*` references out-rank Umbraco.AI's constraint as an `NU1608` warning — but a real Umbraco 18 consumer site (and `TestSite.v18`) fails to restore with a hard `NU1107` conflict (no direct-reference winner). So the 18.x package cannot be published until a v18-compatible `Umbraco.AI` ships. Full details and the steps to finish v18 support are in [BUILDING.md](BUILDING.md).

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

89 tests total (all passing):

- **LogContextProviderTests** (31 tests): surrounding log retrieval, current entry exclusion, deduplication (consecutive same level+message, different messages, different levels, after entries), chronological ordering, empty/failed/null responses, max entries, field mapping, cancellation, frequency counting (exact match, case sensitivity, configuration).
- **AILogAnalyserApiControllerTests** (58 tests): validation, successful analysis, null response handling, optional fields, truncation, surrounding context formatting, frequency notes, timestamp handling, graceful degradation, system diagnostics, AI failure → 502, prompt structure (roles, headings, message count), and configurable per-request prompt options.
