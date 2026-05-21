# AGENTS.md

Guidance for AI coding agents working in this repo.

## What this repo is

`DataHubClient` is a polyglot client library for **DataPLANT ARC DataHubs** — heavily customized GitLab CE instances that host ARCs (Annotated Research Contexts). The library is **written once in F#** and transpiled to JavaScript/TypeScript and Python via **[Fable](https://fable.io)**, then distributed to NuGet, npm, and PyPI.

The full design plan lives at [plans/mvp.md](plans/mvp.md) — read it before making non-trivial changes. It ends with an **Implementation Stages** checklist; tick items off there as you complete them so progress stays visible across sessions.

## Architecture (short version)

```
src/DataHubClient                       the whole library — models, IHttpClient, resource APIs, transports, facade
  DataHubClient.fsproj                    .NET build       (DotNetHttpClient + Thoth.Json.Newtonsoft)
  DataHubClient.Javascript.fsproj         Fable JS/TS build (FetchHttpClient  + Thoth.Json.Javascript)
  DataHubClient.Python.fsproj             Fable Python build (HttpxHttpClient + Thoth.Json.Python)
tests/DataHubClient.Tests               single Fable.Pyxpecto suite, runs on all targets
tests/DataHubClient.DotNet.Tests        .NET-only suite for the System.Net.Http transport
tests/DataHubClient.JavaScript.Tests    JS-only suite + the shared suite, transpiled and run under node
tests/DataHubClient.Python.Tests        Python-only suite + the shared suite, transpiled and run under uv/python
build                                   FAKE-based BuildProject orchestration
plans/mvp.md                            full implementation plan
```

- **One source tree is the whole library.** Models, resource classes, all three HTTP transports, and the facade live in `src/DataHubClient`. There are **no separate per-target shim projects** — instead, parallel project files (`DataHubClient.fsproj`, `DataHubClient.Javascript.fsproj`, `DataHubClient.Python.fsproj`) differ only in `<DefineConstants>`, the one `Http/*HttpClient.fs` they compile, and the Thoth.Json runtime they reference. This is the ARCtrl pattern.
- **Each project file inlines its own `<Compile>` list — there is no shared `.props`.** Every `.fsproj` carries two `<ItemGroup>`s: a **target-agnostic** list (identical and in identical order across all three — F# compilation is order-sensitive) and a **target-specific** list (the one `Http/*HttpClient.fs` plus `DataHubClient.fs`). The agnostic list is deliberately duplicated rather than factored into a shared `DataHubClient.Compile.props` `<Import>`: a shared `.props` of `<Compile>` items desyncs from per-project evaluation and breaks downstream NuGet restore / project references. **When adding a model or resource, update all three project files' agnostic `<ItemGroup>` in lockstep.**
- **One `DataHubClient` type on every target.** `new DataHubClient(url, auth)` reads identically in .NET, JS/TS, and Python; the default transport is `#if`-selected. A custom `IHttpClient` goes in via the settable `Http` property — not a constructor overload (Fable lowers F# secondary constructors to non-`new`-able static factories).
- **HTTP transport is abstracted** via `IHttpClient` (see [src/DataHubClient/Http/IHttpClient.fs](src/DataHubClient/Http/IHttpClient.fs)). The concrete transports (`DotNetHttpClient`, `FetchHttpClient`, `HttpxHttpClient`) live in `Http/`, but each is compiled only by its own per-target project file, behind `#if`. Target-agnostic files never import a concrete HTTP library.
- **Public namespace is flat: `DataHubClient`.** Folder structure (Http/, Models/, Resources/) is for F# file organisation only — don't reflect it in namespaces. The **one exception is `Json/`**: each model's JSON `decoder`/`encoder` lives in a `module <Model>` under the `DataHubClient.Json` namespace, because a module cannot share the flat namespace with its same-named model type (this mirrors ARCtrl's `ARCtrl.Json`). See the JSON convention below.

## F# code conventions (transpilation-first)

These conventions are non-negotiable because the F# is consumed from JS and Python via Fable, where idiomatic F# constructs often transpile to awkward shapes.

- **Classes, not records.** Record `{ with = … }` syntax doesn't exist in JS/Python consumers.
- **`[<AttachMembers>]` on every public class.** Without it, Fable emits members as free functions and JS/Python consumers can't do `instance.method()`.
- **Static members on classes instead of modules with functions.** Modules transpile to nested namespace objects; static members become natural class methods. (The JSON `decoder`/`encoder` are the one deliberate exception — see the no-reflection-JSON rule below.)
- **`Async<'T>` at the public API boundary, with the `Async` suffix on every method that returns one.** Fable maps Async to JS Promise and Python awaitable. Avoid `Task<'T>` in the public API. Every resource API method is async — `client.Projects.ListAsync()`, `client.Files.GetAsync(...)`, `client.Issues.CloseAsync(...)`. The suffix makes the async-ness visible at the call site even though there is no sync sibling (there cannot be: Node.js has no synchronous HTTP, and browser `XMLHttpRequest` with `async=false` is deprecated).
- **No reflection-based JSON.** Use [Thoth.Json](https://github.com/thoth-org/Thoth.Json) with hand-written `decoder`/`encoder` as **module-level `let` bindings** in a `module <Model>` under `Json/<Model>.fs` (namespace `DataHubClient.Json`) — *not* as static members on the model class. Fable's Python target miscompiles a class-level static `Decoder` property: it emits `StaticLazyProperty[Decoder_1[Model]]` inside the class body, where `Model` is not yet bound, so importing the module raises `NameError`. A module-level `let decoder : Decoder<Model>` transpiles to a module global whose type is a deferred annotation — no self-reference at class-eval time. This is exactly ARCtrl's `ARCtrl.Json` structure.
- **Avoid F#-only types (Option, Result, DU) in the public signature where a string/enum/class would do** — they transpile but are clunky to construct from JS/Python.
- **For a settable property, use an explicit `let mutable` backing field + a hand-written `member` — not `member val … with get, set`.** Fable renders an auto-property's compiler-generated backing field as `this["Name@"]`; the `@` is not a valid JS identifier, so it surfaces as an ugly string-keyed access. **The backing field must not shadow a constructor parameter** — `let mutable id = id` makes F# disambiguate the field as `id@30` (binding name + line), just as bad. Prefix the field with `_`: `let mutable _id = id` paired with `member _.Id with get () = _id and set value = _id <- value` transpiles to a clean `this._id`. Use a plain get-only `member _.X = …` for computed/immutable properties (no backing field at all).
- **After a large addition, transpile and read the generated code.** Run `dotnet fable src/DataHubClient/DataHubClient.Javascript.fsproj --lang typescript -o dist/js` and inspect the emitted classes for awkward API surfaces — `@`-suffixed backing fields, free functions where instance methods were expected, mangled or duplicated names. Fable produces these silently; only reading the output catches them. A clean transpiled surface is part of "done", not a follow-up.
- **Full XML doc comments on all new code.** Every public type, member, constructor parameter, and JSON `decoder`/`encoder` gets a `///` comment — `<summary>` plus `<param>` tags on the primary constructor. Model classes must additionally link the relevant GitLab REST API page in their `<summary>` via `<see href="https://docs.gitlab.com/ee/api/...">`. Docs are part of every change, not a follow-up.

Example pattern (see [src/DataHubClient/Http/Authentication.fs](src/DataHubClient/Http/Authentication.fs)):

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
./build.sh RunTestsPython      # Fable-transpile the suite and run it under uv/python
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
and `tests/DataHubClient.Python.Tests` — which pulls in the shared `.fs`
files plus its own target-only suites, then is transpiled and run:

```
.NET:    dotnet run --project tests/DataHubClient.Tests
JS:      dotnet fable tests/DataHubClient.JavaScript.Tests --lang javascript -o dist/js-tests && node dist/js-tests/Program.js
Python:  dotnet fable tests/DataHubClient.Python.Tests --lang python -o dist/py-tests && uv run python dist/py-tests/program.py
```

The `RunTestsJavaScript` task in [build/TestTasks.fs](build/TestTasks.fs) wraps the JS flow (it also writes the `{"type":"module"}` marker node needs); `RunTestsPython` wraps the Python flow. The Python target needs the uv-managed dev environment — a `uv sync` from the repository root (`pyproject.toml`) installs `fable-library` and `httpx`; it requires Python 3.12+ because `fable-library` uses PEP 695 generics.

> **⚠️ The `.venv` is OS-specific — rebuild it when you switch OS.** `uv` creates a platform-specific virtual environment in `.venv` (it is `.gitignore`d). A `.venv` carried over from another OS — e.g. from the Linux devcontainer onto a Windows host — makes `uv run` (and `RunTestsPython`) fail: uv tries to recreate the env and cannot delete the foreign layout (on Windows, the Linux `lib64` reparse point throws `Access is denied`). Fix: delete `.venv` and let `uv sync` / `uv run` rebuild it for the current OS.

## When adding a new resource API (Issues, MergeRequests, …)

1. Add the model under `src/DataHubClient/Models/` as a `[<AttachMembers>]` class — data only, no JSON members.
2. Add its JSON `decoder`/`encoder` under `src/DataHubClient/Json/<Model>.fs` as a `module <Model>` in namespace `DataHubClient.Json` (see the no-reflection-JSON rule above).
3. Add the resource API under `src/DataHubClient/Resources/` as a `[<AttachMembers>]` class taking `(baseUrl, auth, http)` in its constructor; `open DataHubClient.Json` to reach the coders.
4. Expose it as a property on the top-level `DataHubClient` facade.
5. Add a unit test suite using an in-memory `IHttpClient` stub that records the outgoing request and returns canned JSON.
6. Register the new `.fs` files in the target-agnostic `<ItemGroup>` of **all three** project files (`DataHubClient.fsproj`, `DataHubClient.Javascript.fsproj`, `DataHubClient.Python.fsproj`) — identically and in **dependency order** (F# files are compiled top-down), the `Json/` file after every model it references.
7. Transpile and inspect the output (see the F# code conventions above) before considering the addition done.

## Things to NOT do

- ❌ Don't use `dotnet test` for Pyxpecto suites — it silently no-ops under .NET 10 SDK's new test platform.
- ❌ Don't add Expecto / Fable.Mocha / YoloDev packages.
- ❌ Don't use records in the public API surface.
- ❌ Don't write modules-with-functions for the public surface — use classes with static members.
- ❌ Don't put concrete HTTP library calls in target-agnostic files — they belong in `Http/*HttpClient.fs`, each compiled only by its own per-target `.fsproj`, behind `#if`.
- ❌ Don't expose a target-specific entry type (`DataHubClientDotNet`, etc.) — there is one `DataHubClient`; default the transport per target with `#if`.
- ❌ Don't reflect F# folder structure into the namespace; everything public lives directly in `DataHubClient` — the sole exception is the JSON `decoder`/`encoder` modules in `DataHubClient.Json`.
- ❌ Don't put JSON `Decoder`/`Encoder` as static members on a model class — Fable's Python target miscompiles a self-referential static decoder property; use a `module <Model>` in `DataHubClient.Json` instead.

## Useful pointers

- Plan: [plans/mvp.md](plans/mvp.md)
- BuildProject template README: <https://github.com/kMutagene/BuildProjects.NET>
- Fable.Pyxpecto README: <https://github.com/Freymaurer/Fable.Pyxpecto>
- GitLab REST API: <https://docs.gitlab.com/ee/api/api_resources.html>
