# Building & dual-major (Umbraco 17 / 18) support

This package supports Umbraco **17** and **18** from a single set of sources. Both Umbraco
majors run on `net10.0`, so the usual TFM-based multi-targeting (`net6.0`/`net8.0`) does not
apply. Instead there are **two package projects that compile the same source files** against
different Umbraco package ranges.

## Layout

| Project | Umbraco | Umbraco.AI | Symbol | Produces |
| ------- | ------- | ---------- | ------ | -------- |
| `src/Umbraco.Community.AI.LogAnalyser.v17` | `[17.4.0, 18.0.0)` | `[17.1.0, 18.0.0)` | — | package `17.x` |
| `src/Umbraco.Community.AI.LogAnalyser.v18` | `[18.0.0, 19.0.0)` | `[18.0.0, 19.0.0)` | `UMBRACO_18` | package `18.x` |

**`src/Umbraco.Community.AI.LogAnalyser` is not a project — it is the shared source folder.**
It holds all the C# sources, the TypeScript client and the built backoffice assets
(`wwwroot/App_Plugins`), but no `.csproj`.

The two wrapper projects above own no sources of their own. Each imports
`LogAnalyser.Shared.props` and `SharedBackofficeAssets.targets` from that folder, which declare
the shared compile items, backoffice assets and package metadata using
`$(MSBuildThisFileDirectory)`-anchored paths. Add new files to the shared folder and both
variants pick them up automatically. Both produce the same assembly name and the same
`PackageId`; the *only* differences are the package ranges above and the `UMBRACO_18` symbol.

The Umbraco 17 floor is `17.4.0` because that is what the lowest version-aligned `Umbraco.AI`
17.x release requires.

Build/pack each variant:

```bash
# Umbraco 17
dotnet build src/Umbraco.Community.AI.LogAnalyser.v17/Umbraco.Community.AI.LogAnalyser.v17.csproj -c Release
dotnet pack  src/Umbraco.Community.AI.LogAnalyser.v17/Umbraco.Community.AI.LogAnalyser.v17.csproj -c Release /p:Version=17.0.0

# Umbraco 18
dotnet build src/Umbraco.Community.AI.LogAnalyser.v18/Umbraco.Community.AI.LogAnalyser.v18.csproj -c Release
dotnet pack  src/Umbraco.Community.AI.LogAnalyser.v18/Umbraco.Community.AI.LogAnalyser.v18.csproj -c Release /p:Version=18.0.0
```

The version ranges flow into the packed `.nuspec` dependency nodes, so a `17.x` release
depends on Umbraco `[17.4.0, 18.0.0)` and an `18.x` release on `[18.0.0, 19.0.0)`.

## Why two projects instead of one project with a build switch

Umbraco 18 removed Swashbuckle (it ships only `Swashbuckle.AspNetCore.SwaggerUI`, not
`SwaggerGen`), so the OpenAPI registration must be **compiled** differently per major — see
`#if UMBRACO_18` in `Composers/AILogAnalyserApiComposer.cs`. A single project could only
express that with an MSBuild property such as `-p:UmbracoMajor=18`, and that fails for the
development workflow: **NuGet restore evaluates each project once with its default
properties**, ignoring `AdditionalProperties` metadata on a `ProjectReference`. One solution
could therefore never restore the shared project at both majors simultaneously, and the v17
and v18 test sites could not coexist in the solution (`NU1107`).

Two project files give each major its own restore, so the whole solution — both package
variants and both test sites — builds together and both sites can run side by side.

> Packages that have **no** version-specific code can avoid this entirely by declaring a single
> wide range such as `[17.0.0, 19.0.0)`, so one assembly serves both majors. That is not an
> option here because of the OpenAPI difference above.

## Packaging model: version-aligned, one PackageId

There is **one** PackageId (`Umbraco.Community.AI.LogAnalyser`). The package major tracks the
Umbraco major:

- Install on Umbraco 17 → `17.x` of this package.
- Install on Umbraco 18 → `18.x` of this package.

`Umbraco.AI` follows the same convention — its old `1.x` line was superseded by version-aligned
`17.x` / `18.x` releases.

The release workflow (`.github/workflows/release.yml`) selects the project to pack from the
leading major of the pushed tag, so tagging `17.2.3` packs the `.v17` project and `18.0.0`
packs the `.v18` project. A tag whose major is neither 17 nor 18 fails the build rather than publishing
the wrong variant.

## What differs between the two variants

Only the OpenAPI/Swagger registration. Umbraco 18 dropped Swashbuckle in favour of
`Microsoft.AspNetCore.OpenApi`, removing the types the v17 composer used
(`SwaggerGenOptions.SwaggerDoc`, `OpenApiInfo`, `BackOfficeSecurityRequirementsOperationFilterBase`,
`IOperationIdHandler`/`OperationIdHandler`). The difference is isolated behind `#if UMBRACO_18`
in `Composers/AILogAnalyserApiComposer.cs`:

- **v17** — registers a Swagger doc, applies `BackOfficeSecurityRequirementsOperationFilterBase`,
  and emits clean operation IDs via an `IOperationIdHandler`.
- **v18** — `builder.AddBackOfficeOpenApiDocument(name, doc => doc.WithTitle(...)
  .WithBackOfficeAuthentication().ConfigureOpenApiOptions(o => o.AddOperationTransformer(...)))`.

Everything else (controllers, `ILogViewerService`, `IUmbracoVersion`, `IRuntimeState`,
`[BackOfficeRoute]`/`[MapToApi]`/`[Authorize]`, the services and models) is shared and uses
APIs that are unchanged across the two majors.

### OpenAPI document location differs

The generated document moved between majors:

| Umbraco | OpenAPI document URL                          |
| ------- | --------------------------------------------- |
| 17      | `/umbraco/swagger/ailoganalyser/swagger.json` |
| 18      | `/umbraco/openapi/ailoganalyser.json`         |

Only the document *URL* differs. The document contents are identical on both majors: same
title, the same path `/umbraco/ailoganalyser/api/v1/analyse`, the same `Analyse` operation id
and the same `Backoffice-User` security scheme.

Note that both majors normalise the API version to `v1` in the document even though the
controller declares `[ApiVersion("1.0")]`. The route constraint accepts **both** `v1` and
`v1.0` at runtime, so the client's hard-coded `/umbraco/ailoganalyser/api/v1.0/analyse` works
on both. Keep it in mind if you regenerate the client from either document.

## Test sites

Both test sites are in the solution and can run at the same time:

| Site | Umbraco | URL | References |
| ---- | ------- | --- | ---------- |
| `TestSite.v17` | 17 | `https://localhost:44300` | `Umbraco.Community.AI.LogAnalyser.v17` |
| `TestSite.v18` | 18 | `https://localhost:44301` | `Umbraco.Community.AI.LogAnalyser.v18` |

```bash
dotnet run --project src/Umbraco.Community.AI.LogAnalyser.TestSite.v17
dotnet run --project src/Umbraco.Community.AI.LogAnalyser.TestSite.v18
```

Both use the same unattended-install credentials (admin@example.com / 1234567890) and a local
SQLite database, and listen on different ports so both can run at once.
