# DataHubClient

**DataHubClient** is a polyglot client library for **DataPLANT ARC DataHubs** —
heavily customized GitLab CE instances that host ARCs (Annotated Research
Contexts).

The library is **written once in F#** and transpiled to JavaScript/TypeScript
and Python via [Fable](https://fable.io), then distributed to NuGet, npm, and
PyPI. A single source tree produces a native package for each ecosystem, so the
API reads the same in .NET, JS/TS, and Python.

Head to [Getting started](getting-started.md) to install the package for your
language and create your first client.

!!! info "Samples on this site are executed, not transcribed"
    Every code sample is a real program from the repository's
    [`docs/samples/`](https://github.com/nfdi4plants/DataHubClient/tree/main/docs/samples)
    tree. CI installs the freshly-built NuGet, npm, and PyPI packages the way an
    end user would and runs each sample in all three languages on every change —
    a sample that renders here is a sample that works.

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

## Further reading

- [Getting started](getting-started.md) — install and create a client
- [API reference](reference.md) — the generated F# API reference
- [AGENTS.md](https://github.com/nfdi4plants/DataHubClient/blob/main/AGENTS.md) — repo layout, conventions, and the transpilation-first F# style
- [GitLab REST API](https://docs.gitlab.com/ee/api/api_resources.html) — the API surface the client wraps
