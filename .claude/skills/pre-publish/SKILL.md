---
name: pre-publish
description: Run the full pre-publish checklist for the Umbraco.Community.AI.LogAnalyser package.
---

Run the full pre-publish checklist for the Umbraco.Community.AI.LogAnalyser package.

The package ships **two variants from one set of sources** — `.v17` (Umbraco 17) and `.v18`
(Umbraco 18) — under a single PackageId, version-aligned so the package major matches the Umbraco
major. Check both. See `docs/BUILDING.md`.

## 1. Build Solution
```
dotnet build src/Umbraco.Community.AI.LogAnalyser.sln -c Release
```
- Must be 0 errors
- Report any code warnings (ignore NuGet vulnerability warnings from Umbraco's own transitive
  dependencies, e.g. NU1902/NU1903)

## 2. Run Tests
```
dotnet test src/Umbraco.Community.AI.LogAnalyser.sln -c Release
```
- Runs the shared suite against **both** majors (`Tests.v17` and `Tests.v18`)
- All tests must pass in both runs
- The v17 run reports one more test than v18: a Swashbuckle-only assertion guarded by `#if`

## 3. Build Frontend
```
cd src/Umbraco.Community.AI.LogAnalyser/Client && npm run build
```
- Must complete with 0 errors
- Verify built assets exist in
  `src/Umbraco.Community.AI.LogAnalyser/wwwroot/App_Plugins/Umbraco.Community.AI.LogAnalyser/`
  (a hash-named `ai-log-analyser-*.js`, a hash-named `log-ai-summary-dialog.element-*.js`, and
  `umbraco-package.json`)
- These assets are committed to the repo, and the release workflow packs without running npm — so
  confirm `git status` is clean afterwards. A dirty tree means the committed assets are stale and
  must be committed before tagging.

## 4. Pack and Inspect
Pack both variants (to separate folders — they share a filename when unversioned):
```
dotnet pack src/Umbraco.Community.AI.LogAnalyser.v17/Umbraco.Community.AI.LogAnalyser.v17.csproj -c Release -o ./artifacts/v17 -p:Version=17.0.0
dotnet pack src/Umbraco.Community.AI.LogAnalyser.v18/Umbraco.Community.AI.LogAnalyser.v18.csproj -c Release -o ./artifacts/v18 -p:Version=18.0.0
```
Verify each nupkg contains:
- `lib/net10.0/Umbraco.Community.AI.LogAnalyser.dll`
- `staticwebassets/App_Plugins/Umbraco.Community.AI.LogAnalyser/` frontend assets (5 files)
- `README_nuget.md`
- `icon.png`
- Correct nuspec metadata:
  - PackageId: `Umbraco.Community.AI.LogAnalyser`
  - Title: `AI Log Analyser`
  - Authors: `Justin Neville`
  - License: MIT
  - Tags include: `umbraco`, `umbraco-marketplace`
  - RepositoryUrl: `https://github.com/justin-nevitech/Umbraco.Community.AI.LogAnalyser`
- Dependency group for net10.0 includes `Umbraco.Cms.Web.Website` and `Umbraco.AI`, with the
  ranges matching the variant: `[17.4.0, 18.0.0)` / `[17.1.0, 18.0.0)` for v17, `[18.0.0, 19.0.0)`
  for v18

## 5. Verify Documentation
Check these files exist and are not stale:
- `.github/README.md` — feature description, compatibility table, pin-the-major install guidance,
  configuration example, screenshot reference, Author section
- `docs/README_nuget.md` — NuGet-facing readme with install instructions and Author section
- `docs/BUILDING.md` — dual-major build, test and packaging mechanics
- `umbraco-marketplace.json` — category, description, tags, screenshots, author details
- `CLAUDE.md` — architecture and commands accurate for the current state of the codebase

## 6. Verify CI/CD
- `.github/workflows/release.yml` exists
- Selects the package **and** test project from the tag's leading major (17 → `.v17`, 18 → `.v18`),
  and fails on any other major
- Runs `dotnet test` for that major before packing
- Version is injected via `/p:Version=${{github.ref_name}}`
- `NUGET_API_KEY` secret is referenced
- `dotnet-version` matches the project's target framework (10.0.x)

## 7. Report
Summarize the results as a checklist with pass/fail for each item. Flag any issues that would block
a release.
