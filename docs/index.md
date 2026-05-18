---
title: Documentation
category: Documentation
categoryindex: 1
index: 1
---

# DataHubClient

**DataHubClient** is a polyglot client library for **DataPLANT ARC DataHubs** —
heavily customized GitLab CE instances that host ARCs (Annotated Research
Contexts).

The library is **written once in F#** and transpiled to JavaScript/TypeScript
and Python via [Fable](https://fable.io), then distributed to NuGet, npm, and
PyPI. A single source tree produces a native package for each ecosystem, so the
API reads the same in .NET, JS/TS, and Python:

```fsharp
let client = DataHubClient(url, auth)
```

## How it works

One source tree (`src/DataHubClient`) holds the whole library — models,
resource APIs, HTTP transports, and the top-level facade. Three parallel
project files build it for each target, differing only in compile constants,
the HTTP transport they include, and the [Thoth.Json](https://github.com/thoth-org/Thoth.Json)
runtime they reference:

| Target        | Project file                      | HTTP transport     |
|---------------|-----------------------------------|--------------------|
| .NET          | `DataHubClient.fsproj`            | `DotNetHttpClient` |
| JavaScript/TS | `DataHubClient.Javascript.fsproj` | `FetchHttpClient`  |
| Python        | `DataHubClient.Python.fsproj`     | `HttpxHttpClient`  |

HTTP access is abstracted behind an `IHttpClient` interface; each target gets a
sensible default transport and callers can supply their own.

## Getting started

### Prerequisites

- .NET 10 SDK
- Python ≥ 3.12 with [`uv`](https://docs.astral.sh/uv/) for environment management
- `node` and `npm`

Run once:

```sh
uv sync
npm install
```

### Build and test

Build and test orchestration runs through the FAKE pipeline:

```sh
./build.sh                     # build the solution   (build.cmd on Windows)
./build.sh runtests            # build + run the .NET test suite
./build.sh RunTestsJavaScript  # transpile + run the suite under node
./build.sh RunTestsPython      # transpile + run the suite under uv/python
```

Tests use [Fable.Pyxpecto](https://github.com/Freymaurer/Fable.Pyxpecto), a
polyglot test library that runs the same F# suite on all three targets.

## Further reading

- [AGENTS.md](https://github.com/kMutagene/DataHubClient/blob/main/AGENTS.md) — repo layout, conventions, and the transpilation-first F# style
- [plans/mvp.md](https://github.com/kMutagene/DataHubClient/blob/main/plans/mvp.md) — full design plan and implementation stages
- [GitLab REST API](https://docs.gitlab.com/ee/api/api_resources.html) — the API surface the client wraps

Run `dotnet fsdocs watch` to preview this site locally.
