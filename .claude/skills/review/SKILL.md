---
name: review
description: Deep code review of all source files in the AI.LogAnalyser package for correctness, security, and reliability.
---

Review ALL source files in `src/Umbraco.Community.AI.LogAnalyser/` (C# backend) and `src/Umbraco.Community.AI.LogAnalyser/Client/src/` (TypeScript frontend) for the issues below.

## Null Safety & Defensive Coding
- No unguarded `.` access on nullable references (especially `result.Result`, `response.Text`, `entry.Exception`)
- `DateTimeOffset.TryParse` used for external timestamp input (not `Parse`)
- Null/empty checks before string operations like `.Split()`, `.Contains()`, `.Length`
- `IOptions<T>.Value` accessed safely — no risk of unbound configuration causing runtime nulls
- Collection access (`.Items`, list indexing) guarded against empty/null

## Exception Handling
- No generic `throw new Exception()` — use specific types
- AI service failures must not crash the endpoint (catch and return 502)
- Log context fetch failures must not block AI analysis (catch, log warning, continue)
- `CancellationToken` respected — `ThrowIfCancellationRequested()` at async method entry points
- `Task.WhenAll` failures handled (if one task throws, the other's result is still accessible)

## Umbraco API Correctness
- `Attempt<T, TStatus>` return values: `.Success` checked AND `.Result` null-checked before accessing `.Items`
- `ILogViewerService.GetPagedLogsAsync` called with correct parameter order (startDate, endDate, skip, take, orderDirection)
- Backoffice auth policy (`AuthorizationPolicies.SectionAccessSettings`) applied on base controller
- `IComposer` registers services with correct lifetimes: singleton for stateless/cached (`ISystemDiagnosticsProvider`), transient for per-request (`ILogContextProvider`)
- Swagger doc group name matches `[MapToApi]` and `[ApiExplorerSettings(GroupName)]` values

## AI Prompt & Response Handling
- Large user-supplied fields (exception, properties) truncated before embedding in prompt to prevent token overflow
- System prompt and user prompt are clearly separated as distinct `ChatMessage` entries
- AI response `.Text` null-coalesced to a fallback value
- No user-supplied content injected into the system prompt (prompt injection risk)
- Prompt does not leak sensitive system information beyond what's diagnostically useful

## Log Context & Deduplication Logic
- Current log entry correctly excluded from surrounding context (matched by timestamp + rendered message)
- Before entries reversed into chronological order after descending fetch
- After entries remain in ascending order (no reversal needed)
- Deduplication only collapses consecutive entries with identical level AND message (not just message)
- `Count` field incremented correctly on the previous entry (not the current)
- Frequency count uses exact ordinal string match (no accidental partial matching)
- `FrequencyMaxScan` and `MaxSurroundingEntries` settings respected in service calls

## Frontend Security & Reliability
- `unsafeHTML` used with `marked.parse` output — verify marked is configured to block `javascript:` URLs
- Bearer token obtained from `UMB_AUTH_CONTEXT` before each API call (not cached/stale)
- `AbortController` used to cancel in-flight requests on dialog close or re-analyse
- Abort errors (`DOMException` with name `AbortError`) silenced, not shown to user
- `response.ok` checked before parsing JSON (non-2xx responses handled as errors)
- Error text truncated (`substring(0, 200)`) to prevent massive error display
- DOM polling (`setInterval`) cleaned up — `stop()` called before `start()` to prevent duplicate intervals
- `WeakSet` used for enhanced elements — no memory leaks from retained DOM references
- Shadow DOM traversal (`_findElement`) handles missing shadow roots gracefully

## Structured Logging
- Serilog message templates use named placeholders (`{ContextMs}`) — no string interpolation in log calls
- Log levels appropriate: `Information` for performance metrics, `Warning` for non-fatal failures, `Error` for request-blocking failures
- Performance stopwatches log elapsed time at correct points (after awaited operations, not before)
- No sensitive data (user content, tokens, full exceptions) logged at Information level

## DI & Service Lifetime
- `SystemDiagnosticsProvider` is singleton with `Lazy<string>` — verify injected dependencies (`IUmbracoVersion`, `IRuntimeState`, `IHostEnvironment`, `IConfiguration`) are safe to capture in a singleton (must not be scoped)
- `LogContextProvider` is transient — verify it does not hold state between calls
- Controller dependencies are all interface-based (testable with NSubstitute mocks)

## Configuration
- `LogContextSettings` defaults are sensible (10 entries, 5 min window, 500 max scan, 60 min frequency window)
- Settings bound from `AILogAnalyser:LogContext` section — config path matches documentation in CLAUDE.md and README
- No risk of negative or zero values causing divide-by-zero or infinite loops

Report ALL findings with file paths and line numbers. Flag severity as **Critical**, **High**, **Medium**, or **Low**.
