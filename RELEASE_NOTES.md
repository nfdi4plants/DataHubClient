### 0.1.0 (Released 2026-5-21)

Initial release. Single F# source, transpiled to .NET, JavaScript/TypeScript, and Python — same `DataHubClient(baseUrl, auth)` surface on every target.

* Resource APIs: `Projects`, `Repository` (branches, commits), `Files`, `Issues`, `MergeRequests`, `Packages` (generic upload/download).
* `Authentication` factories for personal access, OAuth, and CI job tokens.
* Pluggable `IHttpClient` with per-runtime defaults: `DotNetHttpClient` (.NET), `FetchHttpClient` (JS/TS), `HttpxHttpClient` (Python).
* Thoth-based JSON runtime selected per target, no npm runtime dependencies on the JS distribution.
* FAKE build pipeline with unit tests via Fable.Pyxpecto and a GitLab-backed integration suite.
