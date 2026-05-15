# DataHubClient — Initial Implementation Plan

## Context

`DataHubClient` is a greenfield polyglot client library for **DataPLANT ARC DataHubs** — heavily customized GitLab CE instances used to host ARCs (Annotated Research Contexts). The library is written in F# and transpiled to JavaScript/TypeScript and Python via **Fable**, then distributed via NuGet, npm, and PyPI.

The repo currently contains only a README, LICENSE, .gitignore, and a devcontainer (`.NET 10 SDK`, `Node 20`, `Python 3.11 + uv`, Ionide). Everything below is new construction.

Design priorities locked with the user:
- **MVP surface:** repos/branches/commits, repository files, issues + merge requests, generic Package Registry.
- **HTTP layer:** `IHttpClient` interface in Core + a per-target implementation (System.Net.Http on .NET, `fetch` on JS/TS, `httpx` on Python). Mirrors the ARCtrl pattern.
- **Async style:** `Async<'T>` in Core. Fable maps Async → JS Promise and Python awaitable; .NET callers get `Async.StartAsTask` for free.
- **Distribution:** all three registries (NuGet + npm + PyPI) from day one.
- **F# conventions:** transpilation-first — classes with `[<AttachMembers>]`, static members in lieu of modules, no record types in the public surface.

## Solution Layout

```
DataHubClient.sln
src/
  DataHubClient.Core/                # data models + IHttpClient + resource APIs (transpiled)
    DataHubClient.Core.fsproj
    Http/
      HttpRequest.fs                 # class, [<AttachMembers>]
      HttpResponse.fs
      IHttpClient.fs                 # interface
      Authentication.fs              # class w/ static factories (PAT / OAuth / JobToken)
    Models/
      Project.fs                     # all class, [<AttachMembers>], static Encoder/Decoder
      User.fs
      Branch.fs
      Commit.fs
      RepoFile.fs
      Issue.fs
      Note.fs
      MergeRequest.fs
      Package.fs
      Errors.fs                      # DataHubError class hierarchy
    Json/
      ThothExtensions.fs             # shared encoder helpers
    Resources/
      ProjectsApi.fs                 # class taking (baseUrl, auth, http)
      RepositoryApi.fs               # branches, commits, tree
      FilesApi.fs
      IssuesApi.fs
      MergeRequestsApi.fs
      PackagesApi.fs
    DataHubClient.fs                 # top-level facade: properties .Projects, .Issues, ...

  DataHubClient.DotNet/              # .NET-only shim: HttpClient impl + convenience ctor
    DataHubClient.DotNet.fsproj
    DotNetHttpClient.fs              # IHttpClient over System.Net.Http.HttpClient
    DataHubClient.DotNet.fs          # subclass / factory wrapping Core with default HTTP

  DataHubClient.JavaScript/          # Fable JS/TS target: fetch impl
    DataHubClient.JavaScript.fsproj
    FetchHttpClient.fs               # IHttpClient over globalThis.fetch
    package.json
    fable.config.json                # --lang typescript

  DataHubClient.Python/              # Fable Python target: httpx impl
    DataHubClient.Python.fsproj
    HttpxHttpClient.fs               # IHttpClient over httpx.AsyncClient
    pyproject.toml

tests/
  DataHubClient.Tests/               # single Fable.Pyxpecto suite — transpiled to all three targets

.config/
  dotnet-tools.json                  # fable, fantomas, fsdocs

build.fsproj                         # FAKE-style script with dotnet run for build orchestration
```

### Why this shape

- **Core carries the whole API surface** (models + resource classes) so business logic transpiles once and the per-target packages stay tiny.
- **Per-target package = HTTP impl + ergonomic constructor.** A user on .NET writes `DataHubClient.Create(url, auth)` and gets a working client with `HttpClient` injected; same in JS/Python via the equivalent shim. They can still inject a custom `IHttpClient` for retries/proxy/tests.
- **No conditional compilation in Core.** Cleaner reads, simpler Fable runs.

## Key Conventions

### Classes over records, attached members

```fsharp
[<AttachMembers>]
type Project(id: int, name: string, path: string) =
    member val Id = id with get, set
    member val Name = name with get, set
    member val Path = path with get, set

    static member Decoder : Decoder<Project> =
        Decode.object (fun get ->
            Project(
                get.Required.Field "id" Decode.int,
                get.Required.Field "name" Decode.string,
                get.Required.Field "path" Decode.string))

    static member Encoder (p: Project) : JsonValue =
        Encode.object [
            "id", Encode.int p.Id
            "name", Encode.string p.Name
            "path", Encode.string p.Path ]
```

- `[<AttachMembers>]` makes methods land on the JS/Python class prototype, so consumers call `project.updateName(...)` naturally.
- Static `Decoder`/`Encoder` members keep serialization discoverable per type, no module hunting.
- Mutable `with get, set` is acceptable here — feels native in JS/Python and avoids `with`-record syntax that doesn't exist in those ecosystems.

### Async at the boundary

All public methods return `Async<'T>`. The .NET shim exposes a `Task<'T>` wrapper for ergonomics; Fable's runtime already handles `Async` → `Promise` / `asyncio`-awaitable.

### JSON via Thoth.Json

Hand-written encoders/decoders on each model class. Avoids reflection-based serialization (which is brittle under Fable) and is the de-facto choice in ARCtrl.

## HTTP Abstraction

```fsharp
// DataHubClient.Core/Http/IHttpClient.fs
[<AttachMembers>]
type HttpRequest(url: string, methd: string) =
    member val Url = url with get, set
    member val Method = methd with get, set
    member val Headers : (string * string) list = [] with get, set
    member val Body : string option = None with get, set

[<AttachMembers>]
type HttpResponse(statusCode: int, body: string, headers: (string * string) list) =
    member val StatusCode = statusCode
    member val Body = body
    member val Headers = headers

type IHttpClient =
    abstract SendAsync : HttpRequest -> Async<HttpResponse>
```

Per-target impls live in their own shim projects and depend only on Core.

### Suggested HTTP libs

| Target | Library | Notes |
|---|---|---|
| .NET | `System.Net.Http.HttpClient` | Ship a singleton instance behind the impl; consumers can pass their own. |
| JS/TS | `globalThis.fetch` | Zero deps in modern Node 18+/browsers. Optional `cross-fetch` polyfill for older Node. |
| Python | `httpx.AsyncClient` | First-class async, sync façade available, works with Fable.Python's asyncio mapping. |

Optional later: a `Fable.SimpleHttp`-based universal impl for users who don't want per-target deps — but not needed for MVP.

## Authentication

```fsharp
[<AttachMembers>]
type Authentication private (header: string, value: string) =
    member _.Header = header
    member _.Value = value

    static member PersonalAccessToken(token: string) =
        Authentication("PRIVATE-TOKEN", token)
    static member OAuthToken(token: string) =
        Authentication("Authorization", "Bearer " + token)
    static member JobToken(token: string) =
        Authentication("JOB-TOKEN", token)
```

Applied automatically by every resource class when assembling requests.

## Resource API Shape

Resource classes follow GitLab's URL grouping. Example:

```fsharp
[<AttachMembers>]
type IssuesApi(baseUrl: string, auth: Authentication, http: IHttpClient) =
    member this.List(projectId: int) : Async<Issue array> = ...
    member this.Get(projectId: int, iid: int) : Async<Issue> = ...
    member this.Create(projectId: int, title: string, ?description: string) : Async<Issue> = ...
    member this.Update(projectId: int, iid: int, patch: Issue) : Async<Issue> = ...
    member this.Close(projectId: int, iid: int) : Async<Issue> = ...
    member this.Notes(projectId: int, iid: int) : Async<Note array> = ...
```

Top-level facade:

```fsharp
[<AttachMembers>]
type DataHubClient(baseUrl: string, auth: Authentication, http: IHttpClient) =
    member val Projects   = ProjectsApi(baseUrl, auth, http)
    member val Repository = RepositoryApi(baseUrl, auth, http)
    member val Files      = FilesApi(baseUrl, auth, http)
    member val Issues     = IssuesApi(baseUrl, auth, http)
    member val MergeRequests = MergeRequestsApi(baseUrl, auth, http)
    member val Packages   = PackagesApi(baseUrl, auth, http)
```

## Build & Transpilation

`.config/dotnet-tools.json` pins `fable`, `fantomas`, `fsdocs`.

Build commands (encoded in `build.fsproj` or a simple script):

```bash
# .NET build + test + pack
dotnet build
dotnet test
dotnet pack -c Release src/DataHubClient.Core
dotnet pack -c Release src/DataHubClient.DotNet

# JS/TS transpile
dotnet fable src/DataHubClient.JavaScript --lang typescript -o dist/js
npm --prefix dist/js pack

# Python transpile
dotnet fable src/DataHubClient.Python --lang python -o dist/py
uv build dist/py
```

Wire matching scripts into the post-create hook (currently commented out) and a `build.fsproj` `dotnet run` entrypoint.

## Testing

Tests are written **once in F#** using **[Fable.Pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto)** (the polyglot testing lib maintained by the DataPLANT side) and run on all three targets from the same source — .NET as a `dotnet run` executable, JS/TS and Python via the Fable-transpiled suite.

- **Unit / mock-API tests** in `DataHubClient.Tests` exercise resource APIs against an in-memory `IHttpClient` (see below). They validate URL construction, header injection, JSON encoding/decoding, error mapping, and `Async` plumbing identically on every target.
- **Integration tests** in the same suite run against a docker-composed GitLab CE container with seeded data. The fixture stands up the container once per CI job; each transpiled output runs the same test cases against it.
- **Transpiled-shape smoke tests** assert `[<AttachMembers>]` produced real prototype/class methods on the JS/Python outputs (e.g. `typeof project.updateName === 'function'`).

### Mock API testing strategy

Because every HTTP call goes through `IHttpClient`, the test double is itself plain F# that transpiles with the suite. The same mock, canned payloads, and assertions therefore run on .NET, JS, and Python — so **mock API tests double as transpilation conformance tests**: a Fable bug that only surfaces on Python is caught here.

- **`MockHttpClient`** (test-only, `tests/DataHubClient.Tests/Mock/`) — a route-table `IHttpClient`. Tests register `(method, url) → canned HttpResponse`; it records every outgoing `HttpRequest` and **throws with the attempted URL on an unmatched route**, so a wrongly-built URL fails loudly instead of silently passing.
- **`SampleData`** — hand-written GitLab JSON literals shaped like real responses, including fields the models ignore (proves decoders tolerate extra fields). Fixtures are *not* generated from the encoders, so a decoder/encoder bug cannot mask itself.
- **Each test asserts both sides:** the recorded request (path, query params as an order-independent map, verb, injected auth header, and create/update bodies decoded back through a decoder — never string-compared) and the decoded response (model scalar properties, or error subclass for non-2xx).

Cross-language rules these tests must follow:

- Resource-API tests use Pyxpecto's **`testCaseAsync`** — never `Async.RunSynchronously`, which blocks or no-ops under Fable JS/Python.
- Assert **scalar properties, not whole model objects** — model classes have reference equality, so `Expect.equal` on two instances fails under Fable.
- Resource APIs build **query strings deterministically** (sorted keys) so URL assertions hold identically on every runtime.
- No reflection and no culture-sensitive `DateTime` parsing in the mock or fixtures.

Test commands (`dotnet run`, not `dotnet test` — Pyxpecto is a plain executable):

```bash
dotnet run --project tests/DataHubClient.Tests                                                          # .NET
dotnet fable tests/DataHubClient.Tests --lang javascript -o dist/js-tests && node dist/js-tests/Main.js  # JS
dotnet fable tests/DataHubClient.Tests --lang python     -o dist/py-tests && python dist/py-tests/main.py # Python
```

## CI

GitHub Actions:
1. `lint` — `fantomas --check`.
2. `dotnet-test` — runs the Pyxpecto suite via Expecto.
3. `transpile` — run Fable for JS/TS and Python, cache artifacts (both library and test suite).
4. `js-test` — Mocha on the transpiled Pyxpecto suite.
5. `python-test` — pytest on the transpiled Pyxpecto suite.
6. `pack` — produces `.nupkg`, `.tgz`, and a `.whl` as workflow artifacts.
7. `publish` (tag-triggered) — pushes to NuGet, npm, PyPI.

## Open Suggestions / Decisions to Make Later

- **Pagination:** GitLab uses `Link` headers + `page`/`per_page`. Suggest a `Paginated<'T>` helper exposing `AsArray`/`Iterate` that callers can ignore for simple cases.
- **Error model:** custom `DataHubError` subclasses (`Unauthorized`, `NotFound`, `RateLimited`, `Server`) wrapping the raw response — better cross-language ergonomics than throwing raw HTTP exceptions.
- **Retry/backoff:** keep out of Core. Provide a decorator `IHttpClient` impl (`RetryingHttpClient(inner, policy)`) ship-able per target.
- **Logging:** inject an `ILogger`-style minimal interface; default no-op.
- **fsdocs site:** add later, once the API stabilizes.

## Implementation Stages

Tracked in dependency order. Each stage should leave the solution building and the
test suite green before the next begins. Check items off as they land.

### Stage 1 — Scaffold & HTTP foundation ✅ *done*

- [x] `DataHubClient.slnx` + BuildProject pipeline (`build/`, `build.sh`/`build.cmd`)
- [x] `.devcontainer` (.NET 10 / Node 20 / Python 3.11)
- [x] `DataHubClient.Core` project (`netstandard2.0`, `Fable.Core`)
- [x] `Http/HttpRequest.fs`, `Http/HttpResponse.fs`, `Http/IHttpClient.fs`
- [x] `Http/Authentication.fs` (PAT / OAuth / JobToken factories)
- [x] Pyxpecto test harness (`tests/DataHubClient.Tests`) + `AuthenticationTests`

### Stage 2 — Core models & JSON ✅ *done*

- [x] Add `Thoth.Json.Core` package reference to `DataHubClient.Core`
- [x] `Models/Errors.fs` — `DataHubError` class hierarchy
- [x] `Json/ThothExtensions.fs` — shared encoder/decoder helpers
- [x] Model classes with static `Decoder`/`Encoder`: `Project`, `User`, `Branch`,
      `Commit`, `RepoFile`, `Issue`, `Note`, `MergeRequest`, `Package`
- [x] Register all new `.fs` files in `DataHubClient.Core.fsproj` (dependency order)
- [x] Encoder/decoder round-trip tests for each model
- **Exit:** `./build.sh runtests` green on .NET (13 tests). JS/Python runtime is
  wired in `tests/.../TestJson.fs` via `#if` but only verified once Fable runs
  in Stages 5–6. Note: Thoth.Json 0.9 encoders return `IEncodable` (not `Json`)
  and have no `Encode.option` — use `ThothExtensions.encodeOption`.

### Stage 3 — Resource APIs & facade ✅ *done*

- [x] `MockHttpClient` (route-table `IHttpClient`) + `SampleData` JSON fixtures
      in `tests/DataHubClient.Tests/Mock/` — see *Mock API testing strategy*
- [x] Deterministic (sorted-key) query-string builder shared across resource APIs
- [x] `Resources/ProjectsApi.fs`, `RepositoryApi.fs`, `FilesApi.fs`
- [x] `Resources/IssuesApi.fs`, `MergeRequestsApi.fs`, `PackagesApi.fs`
- [x] `DataHubClient.fs` top-level facade exposing all resource properties
- [x] Per-resource mock-API tests (`testCaseAsync`): assert request path / query /
      verb / auth header / body **and** decoded response + error mapping
- **Exit:** every resource API exercised against `MockHttpClient`; `./build.sh runtests`
  green on .NET (22 tests). The FAKE `BuildSolution` target now builds the
  source/test project list directly because `dotnet build DataHubClient.slnx`
  fails in the current .NET 10.0.300 container during solution restore with no
  diagnostics, while the individual projects build successfully.

### Stage 4 — .NET shim

- [ ] `DataHubClient.DotNet` project referencing Core
- [ ] `DotNetHttpClient.fs` — `IHttpClient` over `System.Net.Http.HttpClient`
- [ ] `DataHubClient.DotNet.fs` — `Create(url, auth)` convenience ctor
- **Exit:** `dotnet build` succeeds; a .NET caller can construct a working client.

### Stage 5 — JavaScript/TypeScript shim

- [ ] `DataHubClient.JavaScript` project + `FetchHttpClient.fs`
- [ ] `package.json`, `fable.config.json` (`--lang typescript`)
- [ ] Transpile verification: `dotnet fable ... --lang typescript` emits classes
- [ ] Transpiled-shape smoke test (`typeof project.updateName === 'function'`)
- **Exit:** transpiled suite passes under `node`/`tsx`.

### Stage 6 — Python shim

- [ ] `DataHubClient.Python` project + `HttpxHttpClient.fs`
- [ ] `pyproject.toml`
- [ ] Transpile verification: `dotnet fable ... --lang py` emits classes
- [ ] Transpiled-shape smoke test (`callable(project.update_name)`)
- **Exit:** transpiled suite passes under `pytest`.

### Stage 7 — CI & packaging

- [ ] `.github/workflows/ci.yml` (lint, dotnet-test, transpile, js-test, python-test, pack)
- [ ] `pack` task produces `.nupkg`, `.tgz`, `.whl`
- [ ] Tag-triggered `publish` job (NuGet + npm + PyPI)
- **Exit:** CI green on a PR; artifacts downloadable from the run.

### Stage 8 — Integration tests

- [ ] docker-compose GitLab CE container with seeded ARC data
- [ ] Integration suite reusing the Pyxpecto cases against the live container
- [ ] Wire the container fixture into CI
- **Exit:** integration job green on all three targets.

## Verification

After the first implementation pass, this checks the design holds end-to-end:

1. `dotnet build` succeeds; `dotnet test` runs unit tests against an in-memory `IHttpClient`.
2. `dotnet fable ... --lang typescript` and `... --lang python` produce no errors and emit class definitions (grep for `class Project` in JS, `class Project:` in Python).
3. From `dist/js`: `import { DataHubClient, Authentication } from '...'; const c = new DataHubClient(url, Authentication.personalAccessToken('xxx'), new FetchHttpClient()); await c.projects.list();` runs against a local GitLab CE container and returns parsed objects.
4. Same flow in Python: `from datahub_client import DataHubClient, Authentication, HttpxHttpClient; await DataHubClient(...).projects.list()`.
5. `[<AttachMembers>]` proven correct: `typeof project.updateName === 'function'` in JS, `callable(project.update_name)` in Python.

## Critical Files (all new)

- `DataHubClient.sln`
- `src/DataHubClient.Core/DataHubClient.Core.fsproj`
- `src/DataHubClient.Core/Http/IHttpClient.fs`
- `src/DataHubClient.Core/Http/Authentication.fs`
- `src/DataHubClient.Core/Models/Project.fs` (template for the other models)
- `src/DataHubClient.Core/Resources/IssuesApi.fs` (template for other resource APIs)
- `src/DataHubClient.Core/DataHubClient.fs`
- `src/DataHubClient.DotNet/DotNetHttpClient.fs`
- `src/DataHubClient.JavaScript/FetchHttpClient.fs` + `package.json` + `fable.config.json`
- `src/DataHubClient.Python/HttpxHttpClient.fs` + `pyproject.toml`
- `.config/dotnet-tools.json`
- `.github/workflows/ci.yml`
