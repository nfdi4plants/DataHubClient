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

There are **no separate per-target shim projects.** One `DataHubClient`
source tree is compiled by parallel project files, one per target; each sets its
own `FABLE_COMPILER*` constants and carries its own `IHttpClient` implementation
and Thoth.Json runtime. The single `DataHubClient` type is the entry point
everywhere — see *One DataHubClient, every language* below.

```
DataHubClient.slnx
src/
  DataHubClient/                  # the whole library — models, IHttpClient, resource APIs, transports
    DataHubClient.fsproj            # .NET build      (DotNetHttpClient, Thoth.Json.Newtonsoft)
    DataHubClient.Javascript.fsproj # Fable JS/TS build (FetchHttpClient,  Thoth.Json.Javascript)
    DataHubClient.Python.fsproj     # Fable Python build (HttpxHttpClient, Thoth.Json.Python) — Stage 6
    package.json                         # npm distribution metadata (Fable 5 needs no fable.config.json)
    Http/
      HttpRequest.fs                 # class, [<AttachMembers>]
      HttpResponse.fs
      IHttpClient.fs                 # interface
      Authentication.fs              # class w/ static factories (PAT / OAuth / JobToken)
      DotNetHttpClient.fs            # IHttpClient over System.Net.Http   — .NET fsproj only
      FetchHttpClient.fs             # IHttpClient over globalThis.fetch  — JS fsproj only
      HttpxHttpClient.fs             # IHttpClient over httpx.AsyncClient — Python fsproj only (Stage 6)
    Models/
      Project.fs                     # data-only class, [<AttachMembers>]
      User.fs / Branch.fs / Commit.fs / RepoFile.fs / Issue.fs / Note.fs
      MergeRequest.fs / Package.fs
      Errors.fs                      # DataHubError class hierarchy
    Json/                            # namespace DataHubClient.Json
      ThothExtensions.fs             # shared encoder helpers
      Project.fs / User.fs / ...     # module <Model> with let decoder / let encoder
    Resources/
      ResourceHelpers.fs             # URL builder, JSON runtime switch, error mapping
      ProjectsApi.fs                 # class taking (baseUrl, auth, http)
      RepositoryApi.fs / FilesApi.fs / IssuesApi.fs / MergeRequestsApi.fs / PackagesApi.fs
    DataHubClient.fs                 # top-level facade: .Projects, .Issues, ... + settable .Http

tests/
  DataHubClient.Tests/               # single Fable.Pyxpecto suite — transpiled to all three targets
  DataHubClient.DotNet.Tests/        # .NET-only suite for the System.Net.Http transport
  DataHubClient.JavaScript.Tests/    # JS-only suite (FetchHttpClient + transpiled shape) + the shared suite
  DataHubClient.Python.Tests/        # Python-only suite (HttpxHttpClient + transpiled shape) + the shared suite

.config/
  dotnet-tools.json                  # fable (fantomas, fsdocs added later)

build/                               # FAKE build pipeline (dotnet run-driven)
```

### One DataHubClient, every language

`DataHubClient` has a single public constructor, `(baseUrl, auth)`, so
`new DataHubClient(url, auth)` reads identically in .NET, JavaScript/TypeScript,
and Python. It defaults the `Http` transport per target via `#if`. A custom
`IHttpClient` is supplied by assigning the settable `Http` property — uniform on
every target — rather than a constructor overload: Fable lowers F# secondary
constructors to static factories (not `new`-able) and rejects the `as this … then`
form outright, so a single primary constructor is the only shape that stays
`new`-able everywhere.

### Why this shape

- **Core *is* the library.** Models, resource classes, all three HTTP transports,
  and the facade live in one source tree, compiled once per target. There is
  nothing downstream of Core to ship.
- **Parallel project files, not shims.** `DataHubClient.fsproj` /
  `.Javascript.fsproj` / `.Python.fsproj` each inline their own `<Compile>` list
  (a target-agnostic `<ItemGroup>` identical across all three, plus a
  target-specific one) and differ only in `<DefineConstants>`, the one
  `Http/*HttpClient.fs` they compile, and the Thoth.Json runtime they reference.
  This is the ARCtrl pattern; it is also how the per-target JSON runtime is
  selected (see Stage 5).
- **One `DataHubClient` type, `new`-able everywhere.** A caller on any target
  writes `new DataHubClient(url, auth)` and gets a working client; a custom
  `IHttpClient` goes in via the settable `Http` property (see *One DataHubClient,
  every language*).
- **Conditional compilation is confined** to `Http/`, `ResourceHelpers.fs`, and
  the `Http` default in `DataHubClient.fs` — everything else is target-agnostic.

## Key Conventions

### Classes over records, attached members

The model class is **data only** (see the *transpilation-first* property rule in
AGENTS.md — `let mutable` backing fields, not `member val`):

```fsharp
[<AttachMembers>]
type Project(id: int, name: string, path: string) =
    let mutable _id = id
    member _.Id with get () = _id and set value = _id <- value
    // … Name, Path …
```

Its JSON `decoder`/`encoder` live in a separate `module Project` under
`Json/Project.fs`, namespace `DataHubClient.Json`:

```fsharp
namespace DataHubClient.Json

module Project =
    let decoder : Decoder<Project> =
        Decode.object (fun get ->
            Project(
                get.Required.Field "id" Decode.int,
                get.Required.Field "name" Decode.string,
                get.Required.Field "path" Decode.string))

    let encoder (p: Project) : IEncodable =
        Encode.object [
            "id", Encode.int p.Id
            "name", Encode.string p.Name
            "path", Encode.string p.Path ]
```

- `[<AttachMembers>]` makes methods land on the JS/Python class prototype, so consumers call `project.method(...)` naturally.
- Coders are **module-level functions**, not static members — see *JSON via Thoth.Json* below for why this is mandatory on the Python target.

### Async at the boundary

All public methods return `Async<'T>`. The .NET shim exposes a `Task<'T>` wrapper for ergonomics; Fable's runtime already handles `Async` → `Promise` / `asyncio`-awaitable.

### JSON via Thoth.Json

Hand-written `decoder`/`encoder` as module-level `let` bindings in a `module <Model>`
under the `DataHubClient.Json` namespace — ARCtrl's `ARCtrl.Json` structure. They are
**not** static members on the model class: Fable's Python target miscompiles a
class-body static `Decoder : Decoder<T>` property into a self-reference to the
not-yet-bound class. See *Stage 6 → JSON coders as modules*.

## HTTP Abstraction

```fsharp
// DataHubClient/Http/IHttpClient.fs
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
dotnet pack -c Release src/DataHubClient
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

**Per-target HTTP shims are the exception to "write once."** `DotNetHttpClient`, `FetchHttpClient`, and `HttpxHttpClient` each wrap a concrete, non-transpilable HTTP library, so their tests cannot enter the shared Pyxpecto source. Each shim gets a small target-only test project — e.g. `tests/DataHubClient.DotNet.Tests`, a .NET Pyxpecto exe — exercising request/response mapping against a fake transport, never run through Fable.

- **Unit / mock-API tests** in `DataHubClient.Tests` exercise resource APIs against an in-memory `IHttpClient` (see below). They validate URL construction, header injection, JSON encoding/decoding, error mapping, and `Async` plumbing identically on every target.
- **Integration tests** in the same suite run against a **live ARC DataHub** — the public DataPLANT dev instance in CI — with the target supplied via the `DATAHUB_TEST_URL` / `DATAHUB_TEST_TOKEN` env vars and skipped when those are unset. See *Stage 8* for the rationale (Docker-stack vs. dev-instance) and the guardrails.
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
- **Text file content:** GitLab's file endpoint returns `content` base64-encoded,
  so every caller of `Files.Get` decodes it by hand (the integration suite's
  `decodeBase64` is a stopgap). Add `FilesApi.GetText(projectId, path, ref) :
  Async<string>` to Core — ideally backed by GitLab's raw endpoint
  (`GET /repository/files/:path/raw`), which returns the plain body and avoids
  decoding entirely. If it instead decodes `content`, it needs the per-target
  `#if` switch (`System.Text.Encoding` is broken on Fable Python — use an Emit
  `base64.b64decode` there), the same pattern `ResourceHelpers.fs` uses for the
  JSON runtime. Once shipped, the integration suite drops `decodeBase64`.
- **fsdocs site:** add later, once the API stabilizes.

## Implementation Stages

Tracked in dependency order. Each stage should leave the solution building and the
test suite green before the next begins. Check items off as they land.

### Stage 1 — Scaffold & HTTP foundation ✅ *done*

- [x] `DataHubClient.slnx` + BuildProject pipeline (`build/`, `build.sh`/`build.cmd`)
- [x] `.devcontainer` (.NET 10 / Node 20 / Python 3.11)
- [x] `DataHubClient` project (`netstandard2.0`, `Fable.Core`)
- [x] `Http/HttpRequest.fs`, `Http/HttpResponse.fs`, `Http/IHttpClient.fs`
- [x] `Http/Authentication.fs` (PAT / OAuth / JobToken factories)
- [x] Pyxpecto test harness (`tests/DataHubClient.Tests`) + `AuthenticationTests`

### Stage 2 — Core models & JSON ✅ *done*

- [x] Add `Thoth.Json.Core` package reference to `DataHubClient`
- [x] `Models/Errors.fs` — `DataHubError` class hierarchy
- [x] `Json/ThothExtensions.fs` — shared encoder/decoder helpers
- [x] Model classes with static `Decoder`/`Encoder`: `Project`, `User`, `Branch`,
      `Commit`, `RepoFile`, `Issue`, `Note`, `MergeRequest`, `Package`
- [x] Register all new `.fs` files in `DataHubClient.fsproj` (dependency order)
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

### Stage 4 — .NET shim ✅ *done*

- [x] `DataHubClient.DotNet` project referencing Core
- [x] `DotNetHttpClient.fs` — `IHttpClient` over `System.Net.Http.HttpClient`
- [x] `DataHubClient.DotNet.fs` — `DataHubClientDotNet`, a thin subclass of the
      Core `DataHubClient` whose constructors default the transport to
      `DotNetHttpClient`. C# and F# callers both just `new` it; no factory class
      or extension members. Overloads accept a custom `HttpClient` or `IHttpClient`.
- [x] `tests/DataHubClient.DotNet.Tests` — .NET-only Pyxpecto suite: `DotNetHttpClient`
      request/response mapping against a fake `HttpMessageHandler` (no network) and
      `DataHubClientDotNet` construction + transport wiring
- **Exit:** `./build.sh runtests` green on .NET — 22 tests in `DataHubClient.Tests`
  plus 5 in `DataHubClient.DotNet.Tests`. The shim's tests live in their own project,
  not the shared suite: `DotNetHttpClient` depends on `System.Net.Http`, which Fable
  cannot transpile, so it must never enter the Pyxpecto source.

### Stage 5 — JavaScript/TypeScript shim ✅ *done*

- [x] **Made Core transpilable (prerequisite).** Core could not transpile: it
      referenced `Thoth.Json.Newtonsoft`, whose `System.IO` use Fable rejects, and
      never referenced `Thoth.Json.JavaScript`. Fixed with the ARCtrl pattern —
      parallel project files over one source tree, see *JSON runtime selection*
      below: `DataHubClient.fsproj` (.NET, Newtonsoft) and the new
      `DataHubClient.Javascript.fsproj` (`FABLE_COMPILER*` constants,
      `Thoth.Json.Javascript`), each inlining its own `<Compile>` list. The
      `#if` switch in `ResourceHelpers.fs` was already correct.
- [x] `.config/dotnet-tools.json` pinning `fable` 5.0.0
- [x] `DataHubClient.JavaScript` project + `FetchHttpClient.fs` (`IHttpClient`
      over the global `fetch`) + `DataHubClientJavaScript` convenience subclass
- [x] `package.json` for the npm distribution. No `fable.config.json`: Fable 5
      has no config file — it is configured by CLI args, encoded in the
      `RunTestsJavaScript` build task.
- [x] Transpile verification: `dotnet fable --lang typescript` emits `class`
      definitions for the Core models, resource APIs, facade, and `FetchHttpClient`
- [x] Transpiled-shape smoke tests (`TranspileShapeTests.fs`): assert
      `[<AttachMembers>]` produced real class members — resource-API methods are
      `function`s, model scalars are readable properties. (The plan's
      `project.updateName` example does not apply: model classes carry properties
      and static coders, not instance mutators.)
- [x] Target-only `FetchHttpClient` test (`FetchHttpClientTests.fs`): request /
      response mapping against a fake global `fetch` — mirrors the
      `DataHubClient.DotNet.Tests` fake-handler pattern from Stage 4
- **Exit:** `./build.sh RunTestsJavaScript` green — the shared Pyxpecto suite
  (22 cases) plus the two JS-only suites (7 cases) transpile via Fable and pass
  under `node`, 29 in total. The JS projects are built only by Fable, never
  `dotnet build`: they stay out of `buildProjects` and the `.slnx` so the two
  Core project files (sharing a directory) never collide over `obj/`.

#### JSON runtime selection (ARCtrl pattern)

`Thoth.Json.Core` is target-agnostic (it yields `IEncodable` / `Decoder<'T>`);
the concrete runtime differs per target — `Newtonsoft` on .NET, `JavaScript` on
JS/TS, `Python` on Python — and Newtonsoft cannot transpile. Following ARCtrl,
one Core source tree is compiled by **parallel project files**, each setting its
own `<DefineConstants>` and referencing only its own runtime. `ResourceHelpers.fs`
switches on `#if FABLE_COMPILER_*`; the `.fsproj` decides which branch is live
and which package Fable sees. Stage 6 adds `DataHubClient.Python.fsproj`
the same way.

#### Stages 4–5 revised — shims folded into Core (post-Stage-5)

The separate `DataHubClient.DotNet` and `DataHubClient.JavaScript` shim projects
from Stages 4 and 5 were **dissolved** so that one `DataHubClient` type is the
entry point in every language (`new DataHubClient(url, auth)`), instead of
`DataHubClientDotNet` / `DataHubClientJavaScript`. Adopting ARCtrl's structure in
full: `DotNetHttpClient.fs` and `FetchHttpClient.fs` moved into `Core/Http/`,
each compiled only by its own `Core.*.fsproj`; `DataHubClient.fs` gained the
`#if`-selected default transport. Custom transports go in via the settable `Http`
property — F# secondary constructors do not survive Fable as `new`-able forms.
The Stage 4/5 checklists above describe the original shim layout; the *shape*
they delivered (transports, tests, ergonomic construction) still holds.

### Stage 6 — Python build ✅ *done*

- [x] **Moved JSON coders off the model classes (prerequisite).** Core could not
      transpile to Python: each model carried a `static member Decoder : Decoder<T>`,
      which Fable's Python target emits as `StaticLazyProperty[Decoder_1[T]]` inside
      the class body — a self-reference to the not-yet-bound class, so importing the
      module raised `NameError`. Fixed by adopting ARCtrl's structure in full: every
      `encoder`/`decoder` moved to a `module <Model>` under `Json/<Model>.fs` in a new
      `DataHubClient.Json` namespace (module-level `let` bindings, whose type is a
      deferred annotation). Models are now data-only; `ThothExtensions` moved into
      `DataHubClient.Json` too. Analogous to the Stage 5 "Made Core transpilable"
      prerequisite. See *JSON coders as modules* below.
- [x] `DataHubClient.Python.fsproj` — parallel project file, `FABLE_COMPILER`
      + `FABLE_COMPILER_PYTHON` constants, `Thoth.Json.Python` runtime
- [x] `Http/HttpxHttpClient.fs` — `IHttpClient` over `httpx.AsyncClient`,
      compiled only by the Python project file; wired as the `#if FABLE_COMPILER_PYTHON`
      default transport in `DataHubClient.fs`. The transport's `async` block awaits
      the native httpx coroutine via `Async.AwaitTask` (Fable's `await_task` bridges any
      Python awaitable).
- [x] `pyproject.toml` for the PyPI distribution (`src/DataHubClient/`), plus a
      root `pyproject.toml` for the uv-managed dev/test environment (`fable-library`,
      `httpx`; Python 3.12+, required by `fable-library`'s PEP 695 generics)
- [x] Transpile verification: `dotnet fable ... --lang python` emits `class` defs for
      the Core models, resource APIs, facade, and `HttpxHttpClient`; the `Json/`
      modules emit clean module-level `decoder`/`encoder` functions
- [x] `tests/DataHubClient.Python.Tests` — parallel test project: the shared
      Pyxpecto suite plus a target-only `HttpxHttpClient` test (fake `httpx.AsyncClient`)
      and a transpiled-shape smoke test (`callable(...)`), mirroring `DataHubClient.JavaScript.Tests`
- [x] `RunTestsPython` build task — Fable transpile + `uv run python`
- **Exit:** `./build.sh RunTestsPython` green — the shared Pyxpecto suite (22 cases)
  plus the two Python-only suites (7 cases) transpile via Fable and pass under
  `python`, 29 in total. The JS suite stays green after the JSON refactor.

#### JSON coders as modules

`Thoth.Json` `decoder`/`encoder` are **module-level `let` bindings** in a
`module <Model>` per `Json/<Model>.fs`, namespace `DataHubClient.Json` — *not*
static members on the model class. This is ARCtrl's `ARCtrl.Json` structure, and
it is mandatory on the Python target: a class-body static `Decoder` property
self-references the enclosing class in its type annotation and Fable Python
miscompiles it. The modules sit in `DataHubClient.Json` rather than the flat
`DataHubClient` namespace so a `module Commit` does not collide with `type Commit`.

### Stage 7 — CI & packaging

- [x] `.github/workflows/ci.yml` (dotnet-test, js-test, python-test; pack/publish only on release tags)
- [x] `pack` task produces `.nupkg`, `.tgz`, `.whl`
- [x] One generated `DataHubClientVersion` type feeds all three packages and request headers
- [x] Tag-triggered `publish` job (NuGet + npm + PyPI), guarded by tag/version equality with `RELEASE_NOTES.md`
- **Exit:** PR CI runs the three test lanes; release tags matching the top release-notes version produce package artifacts and publish them.

### Stage 8 — Integration tests

Integration tests run against a **live ARC DataHub**, not a Docker container stood
up in CI. Decided with the user:

- The DataHUB Docker stack ([`nfdi4plants/DataHUB`](https://github.com/nfdi4plants/DataHUB))
  *is* representative — it ships GitLab with the DataPLANT customizations baked in —
  but it is a full GitLab Omnibus image: minutes to a healthy state per run, plus
  non-declarative post-boot provisioning (root password, API token, test group,
  optional runner). Too heavy and fiddly to stand up per CI job.
- The **public DataPLANT dev instance** is the real customized GitLab, so it is
  both the lighter *and* the most representative CI target.
- The integration suite reads its target from `DATAHUB_TEST_URL` /
  `DATAHUB_TEST_TOKEN` env vars (trivial — `IHttpClient` already takes a base URL).
  CI points them at the dev instance; a developer can point the **same** suite at
  a local `docker-compose` DataHUB stack with no code change. The Docker stack
  stays a free opt-in local backstop without being a CI burden.

Tasks:

- [x] Integration suite reusing the Pyxpecto cases; target from `DATAHUB_TEST_URL` /
      `DATAHUB_TEST_TOKEN`, **skipped (not failed)** when unset — so it no-ops on
      fork PRs and on local runs without credentials. Three parallel project files
      over one source tree (`tests/DataHubClient.Integration.Tests` +
      `.JavaScript`/`.Python` variants), mirroring the unit suite; env vars are
      read per target by `LiveConfig.fs` via `#if`. Read-only for now (List/Get,
      no resource creation). A third var `DATAHUB_TEST_PROJECT` (numeric id) gates
      the project-scoped cases. Content-assertion cases that hardcode expected
      values for a deliberately-provisioned project (currently
      `integration_tests/test_1` on the DataPLANT dev instance) are gated by
      matching `DATAHUB_TEST_URL` against known fixture hosts — no extra env var.
      Build tasks: `RunIntegrationTests{DotNet,JavaScript,Python,All}`.
- The suite is **credential-agnostic**: it targets whatever PAT and project are
  supplied via `DATAHUB_TEST_TOKEN` / `DATAHUB_TEST_PROJECT`, stored as a GitHub
  Environment secret + variable. No dedicated bot account or test namespace —
  operators point it at a project they are comfortable exercising. Since the
  current cases are read-only, this is safe; introducing write/destructive cases
  later would revisit this.
- [ ] Tests are self-cleaning: write tests create resources with uuid/timestamp-
      suffixed names and tear them down in a `finally`, so parallel runs and
      leftover state don't collide; read-only and write/destructive cases separated.
- [x] Separate, **non-PR-gating** CI job — its own `.github/workflows/integration.yml`
      with `schedule` (weekly, Mondays 03:00 UTC) + `workflow_dispatch`, kept out of
      `ci.yml` so a dev-instance outage cannot turn unrelated PRs red. Three jobs
      (.NET / JS / Python), each bound to the `integration_tests` GitHub Environment;
      `DATAHUB_TEST_URL` / `DATAHUB_TEST_PROJECT` are environment **variables**,
      `DATAHUB_TEST_TOKEN` an environment **secret**. The optional `run-integration`
      PR label was not wired up.
- **Exit:** integration job green on all three targets against the dev instance;
  the same suite runnable locally against a docker-compose DataHUB by setting the
  two env vars.

## Verification

After the first implementation pass, this checks the design holds end-to-end:

1. `dotnet build` succeeds; `dotnet test` runs unit tests against an in-memory `IHttpClient`.
2. `dotnet fable ... --lang typescript` and `... --lang python` produce no errors and emit class definitions (grep for `class Project` in JS, `class Project:` in Python).
3. From `dist/js`: `import { DataHubClient, Authentication } from '...'; const c = new DataHubClient(url, Authentication.personalAccessToken('xxx'), new FetchHttpClient()); await c.projects.list();` runs against a local GitLab CE container and returns parsed objects.
4. Same flow in Python: `from datahub_client import DataHubClient, Authentication, HttpxHttpClient; await DataHubClient(...).projects.list()`.
5. `[<AttachMembers>]` proven correct: `typeof project.updateName === 'function'` in JS, `callable(project.update_name)` in Python.

## Critical Files (all new)

- `DataHubClient.sln`
- `src/DataHubClient/DataHubClient.fsproj`
- `src/DataHubClient/Http/IHttpClient.fs`
- `src/DataHubClient/Http/Authentication.fs`
- `src/DataHubClient/Models/Project.fs` (template for the other models)
- `src/DataHubClient/Resources/IssuesApi.fs` (template for other resource APIs)
- `src/DataHubClient/DataHubClient.fs` (facade, `#if`-selected default transport)
- `src/DataHubClient/DataHubClient.fsproj` / `.Javascript.fsproj` / `.Python.fsproj` (each inlines its own `<Compile>` list)
- `src/DataHubClient/Http/DotNetHttpClient.fs` / `FetchHttpClient.fs` / `HttpxHttpClient.fs`
- `src/DataHubClient/package.json` (npm) + `pyproject.toml` (PyPI, Stage 6)
- `.config/dotnet-tools.json`
- `.github/workflows/ci.yml`
