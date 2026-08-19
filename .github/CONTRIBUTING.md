# Contributing Guidelines

Contributions to this package are most welcome!

## Getting Started

There is a test site in the solution to make working with this repository easier. It is configured to do an unattended install, check `appSettings.json` for the login details.

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) (LTS version recommended)
- An Umbraco.AI provider package and API key for testing AI analysis

### Running the Test Site

1. Clone the repository
2. Open the solution in your IDE
3. Set `Umbraco.Community.AI.LogAnalyser.TestSite.v17` as the startup project
4. Run the project — it will perform an unattended Umbraco install on first run
5. Log in with the credentials from `appSettings.json`

> The package supports both Umbraco 17 and 18 from one set of sources, via two wrapper package projects that compile the same files (`Umbraco.Community.AI.LogAnalyser.v17` and `Umbraco.Community.AI.LogAnalyser.v18`). The sources themselves live in `Umbraco.Community.AI.LogAnalyser`, which is a shared source folder rather than a project — add new files there and both variants pick them up automatically. Both test sites are in the solution and can run at the same time (v17 on `https://localhost:44300`, v18 on `https://localhost:44301`). See [docs/BUILDING.md](../docs/BUILDING.md) for the dual-major build details.

### Building the Frontend

The frontend client is in `src/Umbraco.Community.AI.LogAnalyser/Client/`. To build:

```bash
cd src/Umbraco.Community.AI.LogAnalyser/Client
npm install
npm run build
```

The built output goes to `src/Umbraco.Community.AI.LogAnalyser/wwwroot/App_Plugins/Umbraco.Community.AI.LogAnalyser/`.

## Project Structure

```
src/
  Umbraco.Community.AI.LogAnalyser/                    # Shared sources (NOT a project - no .csproj)
    Client/                          # Frontend (Lit web components, TypeScript)
      src/
        index.ts                     # Entry point, LogViewerEnhancer
        log-ai-summary-dialog.element.ts  # Modal dialog component
        log-ai-summary.modal-token.ts     # Modal token & data types
    Controllers/                     # Backoffice API controllers
    Models/                          # Request/response models
    Services/                        # System diagnostics, log context provider
    Composers/                       # DI registration
    wwwroot/                         # Built static assets
  Umbraco.Community.AI.LogAnalyser.v17/                # Umbraco 17 package variant (wrapper, no sources)
  Umbraco.Community.AI.LogAnalyser.v18/                # Umbraco 18 package variant (wrapper, no sources)
  Umbraco.Community.AI.LogAnalyser.Tests/              # Unit tests (xUnit)
  Umbraco.Community.AI.LogAnalyser.TestSite.v17/       # Umbraco 17 test site (https://localhost:44300)
  Umbraco.Community.AI.LogAnalyser.TestSite.v18/       # Umbraco 18 test site (https://localhost:44301)
```

## Guidelines

- Keep the AI prompt concise and structured — changes to the prompt affect the quality of every analysis
- Test with different log levels (Error, Warning, Information, Debug) to ensure the AI response quality is appropriate for each
- Frontend changes should work within Umbraco's shadow DOM architecture
- Follow the existing code style and patterns
