# AGENTS.md

Guidance for AI coding agents working in this repo.

## What this repo is

`DataHubClient` is a polyglot client library for **DataPLANT ARC DataHubs** — heavily customized GitLab CE instances that host ARCs (Annotated Research Contexts). The library is **written once in F#** and transpiled to JavaScript/TypeScript and Python via **[Fable](https://fable.io)**, then distributed to NuGet, npm, and PyPI.

The full design plan lives at [plans/mvp.md](plans/mvp.md) — read it before making non-trivial changes. It ends with an **Implementation Stages** checklist; tick items off there as you complete them so progress stays visible across sessions.

## Architecture (short version)

```
src/DataHubClient.Core                  the whole library — models, IHttpClient, resource APIs, transports, facade
  DataHubClient.Core.fsproj               .NET build       (DotNetHttpClient + Thoth.Json.Newtonsoft)
  DataHubClient.Core.Javascript.fsproj    Fable JS/TS build (FetchHttpClient  + Thoth.Json.Javascript)
  DataHubClient.Core.Python.fsproj        Fable Python build (HttpxHttpClient + Thoth.Json.Python) — planned
  DataHubClient.Core.Compile.props        target-agnostic <Compile> list, shared by all three project files
tests/DataHubClient.Tests               single Fable.Pyxpecto suite, runs on all targets
tests/DataHubClient.DotNet.Tests        .NET-only suite for the System.Net.Http transport
tests/DataHubClient.JavaScript.Tests    JS-only suite + the shared suite, transpiled and run under node
build                                   FAKE-based BuildProject orchestration
plans/mvp.md                            full implementation plan
```

- **Core is the whole library.** Models, resource classes, all three HTTP transports, and the facade live in one source tree. There are **no separate per-target shim projects** — instead, parallel project files (`DataHubClient.Core.*.fsproj`) share `DataHubClient.Core.Compile.props` and differ only in `<DefineConstants>`, the one `Http/*HttpClient.fs` they compile, and the Thoth.Json runtime they reference. This is the ARCtrl pattern.
- **One `DataHubClient` type on every target.** `new DataHubClient(url, auth)` reads identically in .NET, JS/TS, and Python; the default transport is `#if`-selected. A custom `IHttpClient` goes in via the settable `Http` property — not a constructor overload (Fable lowers F# secondary constructors to non-`new`-able static factories).
- **HTTP transport is abstracted** via `IHttpClient` (see [src/DataHubClient.Core/Http/IHttpClient.fs](src/DataHubClient.Core/Http/IHttpClient.fs)). The concrete transports (`DotNetHttpClient`, `FetchHttpClient`, `HttpxHttpClient`) live in `Core/Http/`, but each is compiled only by its own per-target project file, behind `#if`. Target-agnostic Core files never import a concrete HTTP library.
- **Public namespace is flat: `DataHubClient`.** Folder structure (Http/, Models/, Resources/) is for F# file organisation only — don't reflect it in namespaces.

## F# code conventions (transpilation-first)

These conventions are non-negotiable because the F# is consumed from JS and Python via Fable, where idiomatic F# constructs often transpile to awkward shapes.

- **Classes, not records.** Record `{ with = … }` syntax doesn't exist in JS/Python consumers.
- **`[<AttachMembers>]` on every public class.** Without it, Fable emits members as free functions and JS/Python consumers can't do `instance.method()`.
- **Static members on classes instead of modules with functions.** Modules transpile to nested namespace objects; static members become natural class methods.
- **`Async<'T>` at the public API boundary.** Fable maps Async to JS Promise and Python awaitable. Avoid `Task<'T>` in Core.
- **No reflection-based JSON.** Use [Thoth.Json](https://github.com/thoth-org/Thoth.Json) with hand-written `Decoder`/`Encoder` as static members on each model class.
- **Avoid F#-only types (Option, Result, DU) in the public signature where a string/enum/class would do** — they transpile but are clunky to construct from JS/Python.
- **For a settable property, use an explicit `let mutable` backing field + a hand-written `member` — not `member val … with get, set`.** Fable renders an auto-property's compiler-generated backing field as `this["Name@"]`; the `@` is not a valid JS identifier, so it surfaces as an ugly string-keyed access. **The backing field must not shadow a constructor parameter** — `let mutable id = id` makes F# disambiguate the field as `id@30` (binding name + line), just as bad. Prefix the field with `_`: `let mutable _id = id` paired with `member _.Id with get () = _id and set value = _id <- value` transpiles to a clean `this._id`. Use a plain get-only `member _.X = …` for computed/immutable properties (no backing field at all).
- **After a large addition, transpile and read the generated code.** Run `dotnet fable src/DataHubClient.Core/DataHubClient.Core.Javascript.fsproj --lang typescript -o dist/js` and inspect the emitted classes for awkward API surfaces — `@`-suffixed backing fields, free functions where instance methods were expected, mangled or duplicated names. Fable produces these silently; only reading the output catches them. A clean transpiled surface is part of "done", not a follow-up.
- **Full XML doc comments on all new code.** Every public type, member, constructor parameter, and static `Decoder`/`Encoder` gets a `///` comment — `<summary>` plus `<param>` tags on the primary constructor. Model classes must additionally link the relevant GitLab REST API page in their `<summary>` via `<see href="https://docs.gitlab.com/ee/api/...">`. Docs are part of every change, not a follow-up.

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
./build.sh                     # default target: buildSolution
./build.sh runtests            # build + run the .NET test projects
./build.sh RunTestsJavaScript  # Fable-transpile the suite and run it under node
./build.sh pack                # nuget pack
./build.sh release             # full release (clean, build, test, pack, tag, publish, docs)
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

The shared suite (`tests/DataHubClient.Tests`) runs on .NET directly. For Fable
targets it is compiled by a parallel test project — `tests/DataHubClient.JavaScript.Tests`
(and, planned, `DataHubClient.Python.Tests`) — which pulls in the shared `.fs`
files plus its own target-only suites, then is transpiled and run:

```
.NET:    dotnet run --project tests/DataHubClient.Tests
JS:      dotnet fable tests/DataHubClient.JavaScript.Tests --lang javascript -o dist/js-tests && node dist/js-tests/Program.js
```

The `RunTestsJavaScript` task in [build/TestTasks.fs](build/TestTasks.fs) wraps the JS flow (it also writes the `{"type":"module"}` marker node needs).

## When adding a new resource API (Issues, MergeRequests, …)

1. Add the model under `src/DataHubClient.Core/Models/` as a `[<AttachMembers>]` class with static `Decoder`/`Encoder`.
2. Add the resource API under `src/DataHubClient.Core/Resources/` as a `[<AttachMembers>]` class taking `(baseUrl, auth, http)` in its constructor.
3. Expose it as a property on the top-level `DataHubClient` facade.
4. Add a unit test suite using an in-memory `IHttpClient` stub that records the outgoing request and returns canned JSON.
5. Register the new `.fs` files in `DataHubClient.Core.Compile.props` (the shared `<Compile>` list, not the individual `.fsproj` files) in **dependency order** (F# files are compiled top-down).
6. Transpile and inspect the output (see the F# code conventions above) before considering the addition done.

## Things to NOT do

- ❌ Don't use `dotnet test` for Pyxpecto suites — it silently no-ops under .NET 10 SDK's new test platform.
- ❌ Don't add Expecto / Fable.Mocha / YoloDev packages.
- ❌ Don't use records in the public API surface.
- ❌ Don't write modules-with-functions for the public surface — use classes with static members.
- ❌ Don't put concrete HTTP library calls in target-agnostic Core files — they belong in `Http/*HttpClient.fs`, each compiled only by its own per-target `.fsproj`, behind `#if`.
- ❌ Don't expose a target-specific entry type (`DataHubClientDotNet`, etc.) — there is one `DataHubClient`; default the transport per target with `#if`.
- ❌ Don't reflect F# folder structure into the namespace; everything public lives directly in `DataHubClient`.

## Useful pointers

- Plan: [plans/mvp.md](plans/mvp.md)
- BuildProject template README: <https://github.com/kMutagene/BuildProjects.NET>
- Fable.Pyxpecto README: <https://github.com/Freymaurer/Fable.Pyxpecto>
- GitLab REST API: <https://docs.gitlab.com/ee/api/api_resources.html>
