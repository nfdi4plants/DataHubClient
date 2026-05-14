# AGENTS.md

Guidance for AI coding agents working in this repo.

## What this repo is

`DataHubClient` is a polyglot client library for **DataPLANT ARC DataHubs** — heavily customized GitLab CE instances that host ARCs (Annotated Research Contexts). The library is **written once in F#** and transpiled to JavaScript/TypeScript and Python via **[Fable](https://fable.io)**, then distributed to NuGet, npm, and PyPI.

The full design plan lives at [plans/mvp.md](plans/mvp.md) — read it before making non-trivial changes.

## Architecture (short version)

```
src/DataHubClient.Core         models + IHttpClient interface + resource APIs (transpiled)
src/DataHubClient.DotNet       .NET HttpClient impl of IHttpClient (planned)
src/DataHubClient.JavaScript   fetch impl + npm packaging (planned)
src/DataHubClient.Python       httpx impl + PyPI packaging (planned)
tests/DataHubClient.Tests      single Fable.Pyxpecto suite, runs on all targets
build                          FAKE-based BuildProject orchestration
plans/mvp.md                   full implementation plan
```

- **Core carries the whole API surface** (models + resource classes). Per-target packages are thin shims that supply an `IHttpClient` implementation and an ergonomic constructor.
- **HTTP transport is abstracted** via `IHttpClient` (see [src/DataHubClient.Core/Http/IHttpClient.fs](src/DataHubClient.Core/Http/IHttpClient.fs)). Core never imports a concrete HTTP library.
- **Public namespace is flat: `DataHubClient`.** Folder structure (Http/, Models/, Resources/) is for F# file organisation only — don't reflect it in namespaces.

## F# code conventions (transpilation-first)

These conventions are non-negotiable because the F# is consumed from JS and Python via Fable, where idiomatic F# constructs often transpile to awkward shapes.

- **Classes, not records.** Record `{ with = … }` syntax doesn't exist in JS/Python consumers.
- **`[<AttachMembers>]` on every public class.** Without it, Fable emits members as free functions and JS/Python consumers can't do `instance.method()`.
- **Static members on classes instead of modules with functions.** Modules transpile to nested namespace objects; static members become natural class methods.
- **`Async<'T>` at the public API boundary.** Fable maps Async to JS Promise and Python awaitable. Avoid `Task<'T>` in Core.
- **No reflection-based JSON.** Use [Thoth.Json](https://github.com/thoth-org/Thoth.Json) with hand-written `Decoder`/`Encoder` as static members on each model class.
- **Avoid F#-only types (Option, Result, DU) in the public signature where a string/enum/class would do** — they transpile but are clunky to construct from JS/Python.

Example pattern (see [src/DataHubClient.Core/Http/Authentication.fs](src/DataHubClient.Core/Http/Authentication.fs)):

```fsharp
namespace DataHubClient

open Fable.Core

[<AttachMembers>]
type Authentication private (header: string, value: string) =
    member _.Header = header
    member _.Value = value
    static member PersonalAccessToken(token: string) =
        Authentication("PRIVATE-TOKEN", token)
```

## Build & test

All build orchestration goes through the FAKE/BuildProject pipeline under [build/](build/), invoked via the [build.sh](build.sh) / [build.cmd](build.cmd) wrappers. Targets are defined in [build/Build.fs](build/Build.fs).

```
./build.sh                  # default target: buildSolution
./build.sh runtests         # build + run tests
./build.sh pack             # nuget pack
./build.sh release          # full release (clean, build, test, pack, tag, publish, docs)
```

On Windows, use `build.cmd` with the same arguments.

For quick local iteration outside the pipeline: `dotnet build` and `dotnet run --project tests/DataHubClient.Tests` work the same way the pipeline invokes them.

**Solution file is `DataHubClient.slnx`** (XML solution format, .NET 10 SDK default), not `.sln`. `build/ProjectInfo.fs` references `slnx`.

## Testing — Fable.Pyxpecto only

Tests use **[Fable.Pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto)** — a polyglot test library that runs the same F# source on all three targets. Maintained on the DataPLANT side.

**Rules:**
- The test project has **exactly one** test-related package: `Fable.Pyxpecto`. Do NOT add `Expecto`, `Fable.Mocha`, `YoloDev.Expecto.TestSdk`, or `Microsoft.NET.Test.Sdk` — Pyxpecto has zero deps and provides a unified API.
- Test files use a single `open Fable.Pyxpecto`. `testList`, `testCase`, `Expect`, `ftestCase`, `ptestCase` all come from there. **Do not** wrap opens in `#if FABLE_COMPILER_*`.
- The entry point uses the `!!` cast pattern (see [tests/DataHubClient.Tests/Program.fs](tests/DataHubClient.Tests/Program.fs)).
- Tests run via **`dotnet run`**, NOT `dotnet test`. Pyxpecto's runner is a regular `[<EntryPoint>]` exe, not a VSTest adapter. The `runTests` task in [build/TestTasks.fs](build/TestTasks.fs) reflects this.

Per-target invocation:

```
.NET:    dotnet run --project tests/DataHubClient.Tests
JS:      dotnet fable tests/DataHubClient.Tests -o dist/js-tests && node dist/js-tests/Main.js
TS:      dotnet fable tests/DataHubClient.Tests --lang ts -o dist/ts-tests && npx tsx dist/ts-tests/Main.ts
Python:  dotnet fable tests/DataHubClient.Tests --lang py -o dist/py-tests && python dist/py-tests/main.py
```

## When adding a new resource API (Issues, MergeRequests, …)

1. Add the model under `src/DataHubClient.Core/Models/` as a `[<AttachMembers>]` class with static `Decoder`/`Encoder`.
2. Add the resource API under `src/DataHubClient.Core/Resources/` as a `[<AttachMembers>]` class taking `(baseUrl, auth, http)` in its constructor.
3. Expose it as a property on the top-level `DataHubClient` facade.
4. Add a unit test suite using an in-memory `IHttpClient` stub that records the outgoing request and returns canned JSON.
5. Register the new `.fs` files in `DataHubClient.Core.fsproj` in **dependency order** (F# files are compiled top-down).

## Things to NOT do

- ❌ Don't use `dotnet test` for Pyxpecto suites — it silently no-ops under .NET 10 SDK's new test platform.
- ❌ Don't add Expecto / Fable.Mocha / YoloDev packages.
- ❌ Don't use records in the public API surface.
- ❌ Don't write modules-with-functions for the public surface — use classes with static members.
- ❌ Don't put concrete HTTP library calls in `DataHubClient.Core` — they belong in the per-target shim projects.
- ❌ Don't reflect F# folder structure into the namespace; everything public lives directly in `DataHubClient`.

## Useful pointers

- Plan: [plans/mvp.md](plans/mvp.md)
- BuildProject template README: <https://github.com/kMutagene/BuildProjects.NET>
- Fable.Pyxpecto README: <https://github.com/Freymaurer/Fable.Pyxpecto>
- GitLab REST API: <https://docs.gitlab.com/ee/api/api_resources.html>
