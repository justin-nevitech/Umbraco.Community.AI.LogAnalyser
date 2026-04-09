---
name: security-scan
description: Security audit of all source files in the AI.LogAnalyser package — backend (C#) and frontend (TypeScript).
---

Perform a security review of ALL source files in `src/Umbraco.Community.AI.LogAnalyser/` (C# backend) and `src/Umbraco.Community.AI.LogAnalyser/Client/src/` (TypeScript frontend).

## 1. Dependency Review
Run `dotnet list src/Umbraco.Community.AI.LogAnalyser/AI.LogAnalyser.csproj package --vulnerable` and report any known CVEs.
Also run `cd src/Umbraco.Community.AI.LogAnalyser/Client && npm audit` and report any npm advisories.

## 2. Prompt Injection

### User Content in AI Prompt
- User-supplied fields (`Message`, `MessageTemplate`, `Exception`, `Properties`) are embedded directly in the AI prompt — check whether a crafted log message could override the system prompt instructions (e.g. "Ignore previous instructions and...")
- Surrounding log entries from `ILogViewerService` are also embedded — these come from Serilog and could contain attacker-controlled content logged from HTTP requests
- Verify the system prompt and user prompt are sent as separate `ChatMessage` objects with correct roles (`System` vs `User`)

### System Context Leakage
- `SystemDiagnosticsProvider.GetContext()` sends to the AI provider: Umbraco version, .NET version, OS, database provider, connection string provider name, environment name, hosting model, loaded assembly names+versions
- Check whether any of this constitutes sensitive infrastructure disclosure if the AI provider is a third-party cloud service
- Verify the full connection string itself is NOT sent — only the inferred provider type

## 3. Cross-Site Scripting (XSS)

### Markdown Rendering with `unsafeHTML`
- `log-ai-summary-dialog.element.ts` uses `unsafeHTML(marked.parse(...))` to render the AI response as HTML
- The AI response is controlled by the AI provider, not the user directly, but could contain malicious HTML/JS if the AI model is tricked via prompt injection
- Verify `marked` is configured to strip or neutralize `<script>`, `<img onerror>`, `<iframe>`, `<svg onload>`, and other XSS vectors
- Verify the custom `link` renderer blocks `javascript:` protocol URLs
- Check if `marked` sanitizes HTML tags by default or if a separate sanitizer (e.g. DOMPurify) is needed

### Log Message Display
- The log message is rendered in the dialog via Lit's `html` template (`${this.data?.message}`) — verify Lit auto-escapes this (it should, since it's not `unsafeHTML`)
- The level badge uses `style="background:${levelColor}"` — verify `levelColor` only comes from the hardcoded switch/case, not from user input
- Error messages displayed via `${this._error}` — verify error text from the API cannot contain executable HTML

### SVG Injection
- `button.innerHTML = AI_ICON_SVG` — verify the SVG constant is hardcoded and not derived from external input

## 4. Authentication & Authorization

### Backoffice API Protection
- Verify `[Authorize(Policy = AuthorizationPolicies.SectionAccessSettings)]` is applied on the base controller — this restricts to backoffice users with Settings section access
- Verify the endpoint is not accessible to anonymous users or front-end website visitors
- Check that the `[BackOfficeRoute]` prefix prevents collision with public Umbraco routes

### Bearer Token Handling (Frontend)
- Verify the token is obtained fresh via `authContext.getLatestToken()` on each request, not cached
- Verify the token is sent only to the same-origin `/umbraco/` path, not to external URLs
- Check for token leakage in error messages or console logs

## 5. Information Disclosure

### Error Responses
- API returns `StatusCode(502, "AI provider unavailable...")` on AI failure — verify no stack trace, inner exception, or provider details are leaked to the client
- API returns `BadRequest("Message and Level are required.")` — verify this is a safe static string
- Frontend truncates error text to 200 chars (`text.substring(0, 200)`) — verify server error responses cannot leak sensitive details within that window

### Logging
- `LogWarning(ex, ...)` logs the full exception for context fetch failures — verify this goes to Serilog (server-side only), not returned to the client
- `LogError(ex, ...)` logs the full exception for AI failures — same check
- `LogInformation` logs prompt length and timing — verify no user content (log messages, exceptions) is logged at Information level
- Verify `SystemDiagnosticsProvider` does not log the database connection string (it should only log the inferred provider name)

## 6. Denial of Service

### Unbounded Input
- `Message`, `Exception`, `Properties` fields have no max-length validation on the request DTO — a malicious client could send multi-MB strings
- `Exception` and `Properties` are truncated to 8192 chars before prompt embedding, but `Message` and `MessageTemplate` are NOT truncated — check if an extremely large message could cause memory pressure or exceed AI token limits
- `Level` and `Timestamp` fields have no length validation

### AI Provider Abuse
- Each click sends a full AI request — verify there's no rate limiting (note: this may be acceptable for a backoffice-only endpoint, but flag it)
- The prompt includes system context, surrounding logs, and frequency data — verify the total prompt size is bounded

### Log Viewer Service Queries
- `FrequencyMaxScan` defaults to 500 — verify this caps the number of log entries scanned
- `MaxSurroundingEntries` defaults to 10 — verify this caps before/after entries
- Check if misconfigured settings (e.g. `FrequencyMaxScan = int.MaxValue`) could cause the log viewer service to return excessive data

### Recursive Shadow DOM Traversal
- `_findElement` recursively traverses all shadow roots in the document — verify this terminates (no circular references in shadow DOM) and doesn't cause stack overflow on deeply nested UIs

## 7. CSRF / Request Forgery
- The endpoint uses `[HttpPost]` with `[Authorize]` and Bearer token — verify this is sufficient CSRF protection (Bearer tokens in `Authorization` header are not automatically attached by browsers, unlike cookies)
- Verify the frontend uses `fetch` with explicit `Authorization` header, not cookie-based auth

## 8. OWASP Considerations

### Injection
- User input is passed to `ILogViewerService.GetPagedLogsAsync` only as `DateTimeOffset` and `string` parameters — verify no Serilog/Lucene query injection is possible through the message string
- User input is embedded in the AI prompt as plain text — not executed as code, but check for prompt injection (covered above)

### Broken Access Control
- Verify the endpoint cannot be accessed by Umbraco members (front-end users), only backoffice users
- Verify there's no path to access another user's log analysis results (the endpoint is stateless, so this should be N/A)

### Security Misconfiguration
- Swagger doc is registered for the API group — verify Swagger UI is not exposed in production (this is typically handled by Umbraco's middleware, but flag if the package adds its own Swagger endpoint)

Report findings with severity (**Critical**/**High**/**Medium**/**Low**), file path, line number, and recommended fix.
