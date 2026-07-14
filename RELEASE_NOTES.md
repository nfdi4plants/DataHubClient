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
