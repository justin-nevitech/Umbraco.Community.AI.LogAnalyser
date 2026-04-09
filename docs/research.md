# Technical Decisions & Rationale

This document explains the key technical decisions behind the AI.LogAnalyser implementation, the alternatives that were considered, and why each approach was chosen.

## AI Provider Abstraction

**Decision**: Depend on `Umbraco.AI` (`IAIChatService`) rather than integrating directly with any AI provider SDK.

**Rationale**: Umbraco.AI provides a unified abstraction over OpenAI, Anthropic, Google, Amazon Bedrock, and Microsoft AI Foundry. Coupling to a specific provider would limit the package's audience and require maintaining provider-specific configuration. By depending on the abstraction, the package works with whichever provider the site already has configured — zero additional API key setup for users who already use Umbraco.AI elsewhere.

**Trade-off**: The package cannot use provider-specific features (function calling, structured output, streaming). It's limited to the `GetChatResponseAsync` text-in/text-out interface. This is acceptable because log analysis is a straightforward prompt-and-response task.

## Prompt Engineering

### Structured Response Format

**Decision**: The prompt instructs the AI to respond with three markdown headings: Summary, Cause, and Recommended Action.

**Rationale**: Structured headings make the response scannable. Developers debugging production issues need actionable information fast — not a wall of text. The three-section format mirrors how a senior developer would mentally triage a log entry: "what happened, why, and what do I do about it." Markdown was chosen because the Umbraco backoffice already ships `marked.js`, so no additional dependency is needed.

**Alternative considered**: JSON-structured responses parsed into UI sections. Rejected because it would require prompt complexity to ensure valid JSON, error handling for malformed responses, and would break if the AI returns free-text. Markdown is more resilient — even a poorly formatted response is still readable.

### System Prompt Separation

**Decision**: The system prompt is sent as a `ChatRole.System` message, and the user prompt (log entry + context) as a `ChatRole.User` message.

**Rationale**: Separating roles gives the AI model clear instruction boundaries. The system message establishes the persona ("expert Umbraco CMS diagnostics assistant") and behavior constraints ("be concise, technical, and actionable"), while the user message contains only the data to analyse. This is the standard pattern recommended by all major AI providers for consistent behavior.

### Umbraco-Specific System Prompt

**Decision**: The system prompt explicitly references Umbraco concepts: "composition failures, content resolution, Examine indexing, middleware pipeline, dependency injection."

**Rationale**: Without domain-specific prompting, generic AI models produce generic advice. By naming common Umbraco failure modes, the AI is more likely to identify Umbraco-specific root causes (e.g., recognising a `BootFailedException` as a composition issue rather than a generic startup failure). This is the core value proposition — a developer could paste a log entry into ChatGPT themselves, but they wouldn't get Umbraco-aware analysis without crafting the prompt manually.

## Surrounding Log Context

### Why Include Surrounding Entries

**Decision**: Fetch up to 10 log entries before and after the selected entry within a 5-minute window and include them in the prompt.

**Rationale**: A single log entry often lacks the context needed for root-cause analysis. Errors are frequently preceded by warning signs (failed dependency resolution, timeout warnings, connection drops) and followed by cascading failures. By showing the AI what happened before and after, it can identify patterns like "the database connection timed out 2 seconds before this NullReferenceException, suggesting the error is caused by a failed data fetch."

**Alternative considered**: Fetching only the entry itself. This would be simpler and cheaper (fewer tokens) but produces significantly less useful analysis for the most complex errors — which are exactly the ones where AI analysis is most valuable.

### Configurable Window

**Decision**: Both the entry count (`MaxSurroundingEntries`) and time window (`SurroundingWindowMinutes`) are configurable via `appsettings.json`.

**Rationale**: Different environments have different log volumes. A high-traffic production site might log thousands of entries per minute, making 10 entries within 5 minutes a tiny window. A development site might log one entry per minute, making the same window too wide. Configurability lets operators tune the context to their environment. The defaults (10 entries, 5 minutes) are conservative — enough context for most scenarios without sending excessive data to the AI provider.

### Deduplication

**Decision**: Consecutive log entries with identical level and message are collapsed into a single entry with a repeat count (e.g., `(x5)`).

**Rationale**: Many Umbraco errors repeat rapidly (retry loops, recurring scheduled task failures, health check pings). Sending 10 identical entries wastes tokens and dilutes the context. Collapsing them to `[Error] (x5) Connection refused` preserves the frequency signal while keeping the prompt compact. Deduplication is consecutive-only — if entries A, B, A appear, they're kept as three entries because the interleaving pattern may be significant.

### Current Entry Exclusion

**Decision**: The selected log entry is excluded from the surrounding context by matching on both timestamp and rendered message.

**Rationale**: Including the selected entry in the "before" or "after" list would confuse the AI and waste tokens. The dual-match on timestamp + message is necessary because Serilog can produce multiple entries at the same timestamp (within the same millisecond), and different messages at the same timestamp should not be excluded.

### Before-Entries Reversal

**Decision**: Before-entries are fetched in descending order (newest first for efficiency) then reversed into chronological order before embedding in the prompt.

**Rationale**: `ILogViewerService.GetPagedLogsAsync` with `Direction.Descending` returns the most recent entries first, which is the most efficient way to get entries closest to the selected timestamp. But chronological order is natural for the AI to reason about cause-and-effect, so the entries are reversed before formatting. After-entries are fetched ascending and don't need reversal.

## Error Frequency Detection

**Decision**: Count how many times the exact same rendered message appeared in the last hour (configurable), and include this in the prompt with an escalation note for counts > 10.

**Rationale**: A one-off error and a recurring error require different responses. A `NullReferenceException` that appeared once might be a transient data issue; the same exception appearing 500 times in an hour is a systemic problem requiring immediate attention. The frequency signal helps the AI calibrate its severity assessment and recommendations (e.g., "consider adding a circuit breaker" vs. "check if the data exists").

**Design choice — ordinal string matching**: Uses `StringComparison.Ordinal` for exact matching rather than fuzzy/template matching. This means parameterised messages like `"Failed to load item 42"` and `"Failed to load item 99"` count as different messages. This is intentional — the rendered message is what the developer sees, and distinguishing between instances may be important (one item might be corrupted while others are fine). Template-based matching would require parsing Serilog message templates, which adds complexity for marginal benefit.

## System Diagnostics

### What's Included

**Decision**: Send Umbraco version, .NET runtime, OS, database provider, environment name, hosting model, ModelsBuilder mode, application start time, and non-framework assembly names/versions to the AI provider.

**Rationale**: Many Umbraco errors are environment-specific. A `SqlException` on SQLite requires different advice than one on SQL Server. An error on Azure App Service might relate to connection limits that don't apply on Kestrel. Assembly versions reveal package conflicts (e.g., an old version of Examine alongside a new Umbraco core). Without this context, the AI gives generic advice; with it, the AI can reference the specific Umbraco version's known issues.

### What's Excluded

**Decision**: Framework assemblies (`System.*`, `Microsoft.Extensions.*`, etc.) are filtered from the assembly list. Connection strings are not sent — only the inferred provider type ("SQLite", "SQL Server").

**Rationale**: Framework assemblies add noise without diagnostic value — every Umbraco site has them and their versions are determined by the .NET runtime. Connection strings would leak credentials to the AI provider, which is unacceptable. Inferring "SQLite" vs "SQL Server" from the connection string format gives the AI enough to tailor its advice without leaking infrastructure details.

### Lazy Initialisation

**Decision**: `SystemDiagnosticsProvider` is a singleton with `Lazy<string>` initialisation.

**Rationale**: System context (Umbraco version, loaded assemblies, hosting model) doesn't change during the application's lifetime. Building it once and caching avoids redundant reflection and environment variable lookups on every request. The `Lazy<T>` wrapper ensures thread safety without explicit locking — the first call triggers `BuildContext`, and all subsequent calls return the cached string.

### Hosting Model Detection

**Decision**: Detect hosting model by checking environment variables and process name in priority order: Azure App Service → Docker → IIS → IIS Express → Kestrel (default).

**Rationale**: Each hosting model has different constraints (connection limits, file system access, process recycling) that affect troubleshooting advice. The detection order prioritises cloud/container environments where the process name alone would be ambiguous (Azure runs `w3wp` under the hood, but the Azure-specific advice is more relevant). Kestrel is the fallback because it's the default hosting model for .NET apps.

## Field Truncation

**Decision**: Exception and Properties fields are truncated to 8192 characters. Message and MessageTemplate are not truncated.

**Rationale**: Stack traces and serialised property bags can be enormous (multi-MB in extreme cases), which would blow out AI token limits and increase costs. 8 KB captures the full exception type, message, and several hundred frames of stack trace — more than enough for diagnosis. The truncation marker (`... [truncated]`) tells the AI that information was cut. Message and MessageTemplate are typically short (under 1 KB) and are the primary diagnostic data, so truncating them would harm analysis quality.

## Frontend Architecture

### DOM Polling vs MutationObserver

**Decision**: Use `setInterval` polling every 1 second to detect and enhance log viewer rows, rather than `MutationObserver`.

**Rationale**: The Umbraco backoffice uses web components with shadow DOM. `MutationObserver` cannot observe changes inside shadow roots that it isn't explicitly attached to, and the shadow root hierarchy (`umb-log-viewer-messages-list` → `umb-log-viewer-message` → `summary`) would require attaching observers at each level as elements appear. Polling is simpler, more reliable, and the 1-second interval has negligible performance impact. The `_isLogViewerPage()` guard ensures polling is effectively no-op when the user is on other pages.

### WeakSet for Tracking Enhanced Elements

**Decision**: Use a `WeakSet<Element>` to track which log rows have already been enhanced.

**Rationale**: `WeakSet` allows the garbage collector to reclaim DOM elements that are removed from the page (e.g., when the user navigates or paginates). A regular `Set` would retain references to detached DOM nodes, causing memory leaks over long backoffice sessions. The CSS class guard (`.log-ai-cell`) provides a secondary check in case elements are recycled.

### Element Caching with `isConnected`

**Decision**: Cache the `umb-log-viewer-messages-list` element reference and only re-search when it's disconnected from the DOM.

**Rationale**: `_findElement` recursively traverses shadow DOM boundaries, which is expensive. Caching the reference and checking `isConnected` avoids re-traversal on every tick while correctly handling page navigation (where the cached element is removed from the DOM).

### Shadow DOM Traversal

**Decision**: Implement a recursive `_findElement` that walks through shadow roots to locate `umb-log-viewer-messages-list`.

**Rationale**: Umbraco's backoffice is built entirely from web components. Standard `document.querySelector` cannot reach inside shadow roots. The recursive approach handles arbitrary nesting depth — the log viewer component might be 4-5 levels deep in the shadow DOM tree depending on the Umbraco version. The function terminates on first match for efficiency.

### Markdown Rendering with `unsafeHTML`

**Decision**: Use Lit's `unsafeHTML` directive to render `marked.parse()` output as HTML.

**Rationale**: Markdown-to-HTML requires inserting raw HTML into the DOM. Lit's default templating escapes HTML, which would show raw `<h2>` tags. `unsafeHTML` is the standard Lit pattern for this. XSS risk is mitigated by the custom `marked` link renderer that blocks `javascript:` URLs and adds `rel="noopener"` to all links. The AI response is the only content rendered this way — user-supplied log data (level, message, timestamp) uses Lit's safe auto-escaping.

### `marked` from Umbraco Backoffice

**Decision**: Import `marked` from `@umbraco-cms/backoffice/external/marked` rather than bundling it.

**Rationale**: Umbraco's backoffice already ships `marked.js`. Bundling a separate copy would increase the package size and potentially cause version conflicts. Using Umbraco's instance ensures compatibility and keeps the JavaScript bundle small (~35 KB total).

### Vite External Pattern

**Decision**: All `@umbraco-cms/*` imports are externalised in Vite (`external: [/^@umbraco/]`).

**Rationale**: These modules are provided by the Umbraco backoffice at runtime. Bundling them would massively inflate the output, cause version conflicts, and break when Umbraco updates its internal dependencies. The regex pattern future-proofs against new Umbraco modules without listing each one.

## API Design

### Single Endpoint

**Decision**: One POST endpoint (`/umbraco/ailoganalyser/api/v1.0/analyse`) that accepts a log entry and returns an AI analysis.

**Rationale**: The package has exactly one user interaction: "analyse this log entry." A single endpoint keeps the API surface minimal, the frontend simple, and the Swagger documentation concise. There's no need for separate endpoints for context, frequency, or diagnostics — the backend orchestrates all of these internally and returns a single combined response.

### 502 for AI Failures

**Decision**: Return HTTP 502 (Bad Gateway) when the AI provider fails, not 500 (Internal Server Error).

**Rationale**: 502 semantically means "the server got an invalid response from an upstream server." The AI provider is the upstream server, and its failure is not a bug in the package. This distinction helps operators differentiate between package errors (500) and provider issues (502) in monitoring dashboards. The error message ("AI provider unavailable. Please check your AI provider configuration.") directs the developer to the root cause without leaking internal details.

### Bearer Token Authentication

**Decision**: The frontend obtains a fresh Bearer token from `UMB_AUTH_CONTEXT.getLatestToken()` for every API call, sent via the `Authorization` header.

**Rationale**: Umbraco's backoffice API uses OAuth2/OIDC tokens, not cookie-based authentication. Getting a fresh token on each request avoids stale-token issues (especially relevant since AI analysis can take 10-30 seconds, during which a token might expire). Using the `Authorization` header instead of cookies also provides inherent CSRF protection — browsers don't automatically attach `Authorization` headers to cross-origin requests.

## Service Lifetimes

### SystemDiagnosticsProvider as Singleton

**Decision**: Registered as singleton in DI.

**Rationale**: The system context (Umbraco version, OS, assemblies) doesn't change at runtime. Building it once avoids repeated reflection. The `Lazy<string>` ensures the singleton is thread-safe without per-request overhead. All injected dependencies (`IUmbracoVersion`, `IRuntimeState`, `IHostEnvironment`, `IConfiguration`) are themselves singletons or effectively immutable, so capturing them in a singleton is safe.

### LogContextProvider as Transient

**Decision**: Registered as transient in DI.

**Rationale**: `LogContextProvider` depends on `ILogViewerService`, which reads from Serilog's log store. This service may have request-scoped or transient-scoped dependencies in Umbraco's DI container. Using transient ensures the provider gets a fresh `ILogViewerService` instance per request, avoiding scoped-service-in-singleton pitfalls. The provider holds no state between calls — `_settings` is read once in the constructor from `IOptions<T>.Value`, which is safe.

## Performance Diagnostics

**Decision**: Log context-gathering time, AI response time, total request time, and prompt length at `Information` level using structured Serilog templates.

**Rationale**: AI response times vary dramatically (1-30 seconds depending on provider, model, and prompt size). Logging these metrics lets operators identify slow queries, monitor cost (prompt length correlates with token usage), and diagnose whether latency is in context gathering or AI inference. Structured templates (not string interpolation) enable machine-readable metrics extraction by log aggregation tools. Context failures are logged at `Warning` because they're non-fatal — the analysis proceeds without context.

## Configuration Defaults

| Setting | Default | Rationale |
|---------|---------|-----------|
| `MaxSurroundingEntries` | 10 | Enough context for most error patterns without overwhelming the prompt. 10 before + 10 after = 20 entries max, typically under 2 KB. |
| `SurroundingWindowMinutes` | 5 | Balances relevance (entries close in time) with coverage. Entries from 30 minutes ago are rarely relevant to a specific error. |
| `FrequencyMaxScan` | 500 | Limits the log query to prevent performance degradation on high-volume sites. 500 entries at typical log rates covers several hours. |
| `FrequencyWindowMinutes` | 60 | One hour is the natural "is this a recurring problem?" window. Shorter windows miss intermittent issues; longer windows dilute the signal. |
