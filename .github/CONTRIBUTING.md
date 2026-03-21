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
3. Set `AI.LogAnalyser.TestSite` as the startup project
4. Run the project — it will perform an unattended Umbraco install on first run
5. Log in with the credentials from `appSettings.json`

### Building the Frontend

The frontend client is in `src/AI.LogAnalyser/Client/`. To build:

```bash
cd src/AI.LogAnalyser/Client
npm install
npm run build
```

The built output goes to `src/AI.LogAnalyser/wwwroot/App_Plugins/AILogAnalyser/`.

## Project Structure

```
src/
  AI.LogAnalyser/                    # Main package project
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
  AI.LogAnalyser.TestSite/           # Test Umbraco site
```

## Guidelines

- Keep the AI prompt concise and structured — changes to the prompt affect the quality of every analysis
- Test with different log levels (Error, Warning, Information, Debug) to ensure the AI response quality is appropriate for each
- Frontend changes should work within Umbraco's shadow DOM architecture
- Follow the existing code style and patterns
