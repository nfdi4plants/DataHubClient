### 0.8.0 (Released 2026-10-14)

* Fix Python package and RO-Crate downloads failing with HTTP `302` by enabling HTTPX redirects. Exercise the real HTTPX redirect implementation in tests and the packaged documentation sample.
* Remove `Authorization`, `PRIVATE-TOKEN`, and `JOB-TOKEN` on cross-origin redirects in the default transports. Credentials remain removed when a later redirect returns to the original origin.
* **Browser compatibility change:** Requests using `PRIVATE-TOKEN` or `JOB-TOKEN` now fail on redirects, including same-origin redirects, because browsers hide the destination needed to remove custom credentials safely. Anonymous and OAuth bearer requests retain native redirect handling. Verify successful OAuth downloads and saved RO-Crate files in Chromium and Firefox with native modules and Vite development/production builds.
* **Custom .NET transport requirement:** Injected `HttpClient` instances must disable automatic redirects and provide credentials per request. Cross-origin redirects fail when credentials are configured in `DefaultRequestHeaders`, which would otherwise restore them.
* Add live RO-Crate and generic-package download coverage, explicit integration fixtures, and required CI configuration checks. Document the Git dev object-storage setup needed to exercise real redirects and verify credential removal.

### 0.7.0 (Released 2026-10-13)

* **Breaking change:** Public collection results, constructor arguments, and properties now use `ResizeArray<T>` consistently across targets, including resource lists, issue assignees and labels, and HTTP headers. F#/.NET callers should use `.Count` instead of `.Length` and construct collections with `ResizeArray [ ... ]`.
* TypeScript collection declarations now use ordinary `T[]`, supporting `map`, `push`, and other native array operations without `Array.from` workarounds. Python receives native lists.
* Update documentation examples, custom transport fixtures, and cross-target tests for the collection signatures; verify mutable collections, JSON round trips, and browser consumers.
* Require `ResizeArray` for public collection arguments and return values in the Fable development guidance.

### 0.6.0 (Released 2026-10-13)

* Bundle TypeScript declarations with the npm package's JavaScript modules, preserving existing import paths for JavaScript and TypeScript consumers.
* Add TypeScript tabs throughout the documentation, backed by strictly compiled examples tested against the packaged library.
* Separate RO-Crate metadata retrieval from Node.js filesystem saving and provide browser download examples for JavaScript and TypeScript.
* Verify the npm package in Chromium and Firefox with native ES modules, Vite development, and Vite production builds, including real fetch, CORS, and file downloads.
* Upgrade Fable and the Python Fable runtime to 5.18.0 for corrected TypeScript generation.

### 0.5.1 (Released 2026-10-13)

* Include the repository README in the npm package so the npm landing page can display installation instructions, documentation links, and registry badges.

### 0.5.0 (Released 2026-10-13)

* `client.Packages.DownloadROCrateAsync(projectId)` — download an ARC's available generated RO-Crate metadata as JSON-LD text, using the DataHub's standard generic-package coordinates.
* RO-Crate download-and-save guide with verified F#, JavaScript, and Python examples using the new method, linked from the README and Packages API page.
* Resource API documentation appears under a fixed desktop sidebar heading with navigation expansion enabled.

### 0.4.0 (Released 2026-10-13)

* `Authentication.Anonymous()` — access public resources without sending an authentication header.
* `client.Projects.ListAllAsync()` — retrieve every matching project in ascending ID order using keyset pagination, with optional search, compact representation, and visibility filters. Invalid ordering and failed pages raise errors instead of returning partial results.
* Fixed `Files.CreateAsync()` and `Files.UpdateAsync()` failing to decode successful GitLab write responses. Both now read back the file from the target branch to return its contents and metadata; a read-back failure is reported even if the write succeeded.
* Documentation examples for every method across all eight resource APIs, verified against the .NET, JavaScript, and Python packages.
* Documentation sample runner supports the Windows npm launcher, Python environments, and NuGet cache paths.
* README links to the published guide and API reference, with version badges linking to NuGet, npm, and PyPI.

### 0.3.0 (Released 2026-7-29)

* `client.Users` — user API: `GetCurrentAsync()` (GitLab `GET /user`) resolves the identity behind the configured credential, `GetAsync(userId)` fetches a user by numeric id.
* `User.Email` — optional, and populated only by `GetCurrentAsync()`: GitLab omits the address on users embedded in issues, merge requests, notes, and commits.

### 0.2.0 (Released 2026-7-14)

* `client.Validation` — ARC validation (CQC) results API: discover validated branches and packages on the `cqc` branch (`ListValidatedBranchesAsync`, `ListPackagesAsync`), fetch each result file (`GetSummaryAsync` typed via the arc-validate `ValidationSummary` schema, `GetReportAsync` JUnit XML, `GetBadgeAsync` SVG), bulk `GetAllSummariesAsync`, and `ListHistoryAsync` for reading results at any commit of the branch history.
* New models: `ValidationPackageRef`, `ValidationSummary`, `ValidationResult`, `ValidationPackageSummary`.
* Resource API methods now return host-native awaitables on every target — a JS `Promise`, a Python `Task`/coroutine, F# `Async` on .NET — so `await client.Projects.ListAsync()` reads natively everywhere.

### 0.1.0 (Released 2026-5-21)

Initial release. Single F# source, transpiled to .NET, JavaScript/TypeScript, and Python — same `DataHubClient(baseUrl, auth)` surface on every target.

* Resource APIs: `Projects`, `Repository` (branches, commits), `Files`, `Issues`, `MergeRequests`, `Packages` (generic upload/download).
* `Authentication` factories for personal access, OAuth, and CI job tokens.
* Pluggable `IHttpClient` with per-runtime defaults: `DotNetHttpClient` (.NET), `FetchHttpClient` (JS/TS), `HttpxHttpClient` (Python).
* Thoth-based JSON runtime selected per target, no npm runtime dependencies on the JS distribution.
* FAKE build pipeline with unit tests via Fable.Pyxpecto and a GitLab-backed integration suite.
