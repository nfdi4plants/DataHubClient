# DataHubClient

A polyglot client library for ARC DataHubs. Written once in F# and transpiled to .NET, JavaScript/TypeScript, and Python via [Fable](https://fable.io); published to NuGet, npm, and PyPI.

## Installation

| Target | Command |
|---|---|
| .NET   | `dotnet add package DataHubClient` |
| JS/TS  | `npm install @nfdi4plants/datahub-client` |
| Python | `pip install datahub-client` |

## Development

### Prerequisites

- .NET 10 SDK
- Python ≥ 3.12
- `uv` for Python environment management (see [uv docs](https://docs.astral.sh/uv/))
- `node` and `npm` for JavaScript

Run once after cloning (or after a devcontainer rebuild):

```sh
dotnet tool restore   # restores `fable` and `fsdocs`
uv sync               # creates .venv with httpx + fable-library
```

> **⚠️ The `.venv` is OS-specific — rebuild it when you switch OS.** `uv` creates
> a platform-specific virtual environment in `.venv` (it is `.gitignore`d). A
> `.venv` carried over from another OS — e.g. from the Linux devcontainer onto
> a Windows host — makes `uv run` and the `RunTestsPython` build target fail.
> Delete `.venv` and let `uv sync` / `uv run` rebuild it for the current OS.

### Tests

Build and test orchestration runs through the FAKE pipeline — see [AGENTS.md](AGENTS.md) for the full layout and conventions.

```sh
./build.sh                       # default target — builds the solution
./build.sh RunTestsDotNet        # .NET unit tests
./build.sh RunTestsJavaScript    # transpile + run the JS suite on node
./build.sh RunTestsPython        # transpile + run the Python suite under uv/python
./build.sh RunTestsAll           # all three
```

Use `build.cmd` on Windows with the same arguments.

### Documentation

The docs site pairs an [MkDocs Material](https://squidfunk.github.io/mkdocs-material/) guide with an [fsdocs](https://fsprojects.github.io/FSharp.Formatting/)-generated F# API reference (design record: [plans/docs.md](plans/docs.md)). Every code sample on the site is a real program under [docs/samples/](docs/samples/), shown per language in tabs and executed in CI against the freshly-packed NuGet/npm/PyPI artifacts — docs that drift from the code fail the build in all three languages.

```sh
./build.sh RunDocsSamples # pack + run every docs sample in all three languages
./build.sh BuildDocs      # build the full site into site/ (guide + API reference)
./build.sh WatchDocs      # live-reload preview of the guide at http://127.0.0.1:8000
./build.sh WatchApiDocs   # live-reload preview of the F# API reference at http://127.0.0.1:8901
```

CI deploys `site/` to GitHub Pages on every version tag.

### Packaging

A single F# source tree produces three published artifacts. Each language uses a different transpile-output location, manifest, and packer:

| Target | Fable output dir         | Manifest                                 | Built into `pkg/`                            |
|--------|--------------------------|------------------------------------------|----------------------------------------------|
| .NET   | n/a (compiled .NET assembly) | `src/DataHubClient/DataHubClient.fsproj` | `DataHubClient.<version>.nupkg`              |
| JS/TS  | `dist/js/`               | `src/DataHubClient/package.json`         | `nfdi4plants-datahub-client-<version>.tgz`   |
| Python | `src/DataHubClient/py/`  | root [pyproject.toml](pyproject.toml) (poetry-core) | `datahub_client-<version>-py3-none-any.whl` |

The version comes from [RELEASE_NOTES.md](RELEASE_NOTES.md): the FAKE build stamps it into each manifest before packing.

The **Python build mirrors ARCtrl's layout**:

- Fable transpiles into `src/DataHubClient/py/` (next to the `.fs` source, gitignored — it's a regenerated build artifact).
- A hand-written [src/DataHubClient/\_\_init\_\_.py](src/DataHubClient/__init__.py) lives next to the `.fs` source and re-exports `DataHubClient` and `Authentication`, so end users write `from datahub_client import DataHubClient, Authentication`.
- The root [pyproject.toml](pyproject.toml) uses `poetry-core` with `packages = [{ include = "**/*.py", from = "src/DataHubClient/", to = "datahub_client" }]` to rename `src/DataHubClient/` → `datahub_client/` inside the wheel, so the wheel installs a single package and the relative imports inside the Fable output resolve.
- `include = [{ path = "src/DataHubClient/py/**/*.py", format = ["sdist", "wheel"] }]` force-pulls the gitignored Fable output into the wheel. The explicit `format` is required — poetry-core 2.x silently drops gitignored files when `include` is given as a plain string.
- `[tool.uv] package = false` keeps `uv sync` from editable-installing the project during dev test runs; dev tests operate on Fable's separately-transpiled `dist/py-tests/` output, never on an installed `datahub_client`.

Build all three packages into `pkg/`:

```sh
./build.sh Pack           # production version from RELEASE_NOTES.md
./build.sh PackPrerelease # same, with a prerelease suffix
```

### Package smoke tests

`TestPackages` installs each freshly-packed artifact the way a real end user would — no Fable, no F# — and runs a one-line script that constructs `DataHubClient` to prove package metadata, entry points, and transitive deps resolve cleanly.

```sh
./build.sh TestPackages              # runs all three
./build.sh TestPackagesDotNet        # F# script via `dotnet fsi`, NuGet from local pkg/
./build.sh TestPackagesJavaScript    # `npm install <tarball>` + `node smoke.mjs`
./build.sh TestPackagesPython        # isolated venv + `pip install <wheel>` + `python smoke.py`
```

Each smoke regenerates its consumer script into `pkg/testScripts/{dotnet,js,py}/` on every run; the Python venv is created at `/tmp/datahubclient-py-smoke-venv` (outside the repo) because `uv venv` places a `lib64` symlink that FAKE's `Shell.cleanDirs` chokes on.

### Publishing

Releases are published by CI ([.github/workflows/ci.yml](.github/workflows/ci.yml)) on every version tag:

1. Add a `### X.Y.Z (Released ...)` entry at the top of [RELEASE_NOTES.md](RELEASE_NOTES.md) — the build derives the published version from it, and CI refuses tags that don't match it.
2. Commit, then tag and push:

   ```sh
   git tag X.Y.Z
   git push origin main X.Y.Z
   ```

CI then runs all three test suites, packs, and publishes `DataHubClient` to NuGet, `@nfdi4plants/datahub-client` to npm, and `datahub-client` to PyPI. The publish steps skip already-published versions, so re-running a failed release run is safe.

Publishing is keyless via **trusted publishing** (OIDC) on all three registries — no long-lived registry credentials are stored on the repo. The moving parts:

- Each registry has a trusted-publishing policy pointing at this repo, `ci.yml`, and the `release` GitHub environment: [nuget.org → Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing), [npm package settings → Trusted publisher](https://docs.npmjs.com/trusted-publishers/), [PyPI project settings → Publishing](https://docs.pypi.org/trusted-publishers/).
- The only Actions secret is `NUGET_USER` — the nuget.org profile name the NuGet policy belongs to (the `NuGet/login` action exchanges the job's OIDC token for a temporary API key under that account).
- The publish job runs in the `release` environment; adding required reviewers to that environment in the repo settings turns publishing into a manually approved step.

For local artifacts without publishing, use `./build.sh Pack` (see [Packaging](#packaging)).
