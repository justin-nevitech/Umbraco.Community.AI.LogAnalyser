---
name: pre-publish
description: Run the full pre-publish checklist for the Umbraco.Community.AI.LogAnalyser package.
---

Run the full pre-publish checklist for the Umbraco.Community.AI.LogAnalyser package.

## 1. Build Solution
```
dotnet build src/Umbraco.Community.Umbraco.Community.AI.LogAnalyser.sln -c Release
```
- Must be 0 errors
- Report any code warnings (ignore NuGet vulnerability warnings from Umbraco dependencies, e.g. NU1902)

## 2. Run Tests
```
dotnet test src/Umbraco.Community.Umbraco.Community.AI.LogAnalyser.Tests/Umbraco.Community.AI.LogAnalyser.Tests.csproj
```
- All tests must pass

## 3. Build Frontend
```
cd src/Umbraco.Community.AI.LogAnalyser/Client && npm run build
```
- Must complete with 0 errors
- Verify built assets exist in `src/Umbraco.Community.AI.LogAnalyser/wwwroot/App_Plugins/AILogAnalyser/` (at minimum `ai-log-analyser.js` and `umbraco-package.json`)

## 4. Pack and Inspect
```
dotnet pack src/Umbraco.Community.AI.LogAnalyser/AI.LogAnalyser.csproj -c Release -o /tmp/nupkg-check
```
Verify the nupkg contains:
- `lib/net10.0/` DLL
- `staticwebassets/` with `App_Plugins/AILogAnalyser/` frontend assets
- `README_nuget.md`
- `icon.png`
- Correct nuspec metadata:
  - PackageId: `Umbraco.Community.AI.LogAnalyser`
  - Title: `AI Log Analyser`
  - Authors: `Justin Neville`
  - License: MIT
  - Tags include: `umbraco`, `umbraco-marketplace`
  - RepositoryUrl: `https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser`
- Dependency group for net10.0 includes `Umbraco.Cms.Web.Website`, `Umbraco.AI`

## 5. Verify Documentation
Check these files exist and are not stale:
- `.github/README.md` — Contains feature description, configuration example, screenshot reference
- `docs/README_nuget.md` — NuGet-facing readme with install instructions
- `umbraco-marketplace.json` — Has category, description, tags, screenshots, author details
- `CLAUDE.md` — Architecture and commands are accurate for current state of codebase

## 6. Verify CI/CD
- `.github/workflows/release.yml` exists
- References correct .csproj path: `src/Umbraco.Community.AI.LogAnalyser/AI.LogAnalyser.csproj`
- Version is injected via `/p:Version=${{github.ref_name}}`
- `NUGET_API_KEY` secret is referenced
- `dotnet-version` matches the project's target framework (10.0.x)

## 7. Report
Summarize the results as a checklist with pass/fail for each item. Flag any issues that would block a release.
