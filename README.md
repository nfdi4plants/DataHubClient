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

The release pipeline runs clean → build → test → pack → tag → publish:

```sh
./build.sh Release        # full release: NuGet publish + docs
./build.sh ReleaseNoDocs  # same, no docs
./build.sh PreRelease     # prerelease variant
```

The pipeline only auto-publishes the NuGet package. After `./build.sh Pack`, publish the others manually:

```sh
# npm — requires `npm login`
npm publish ./pkg/nfdi4plants-datahub-client-<version>.tgz

# PyPI — project-scoped token from https://pypi.org/manage/account/token/
uv publish pkg/datahub_client-<version>-py3-none-any.whl \
  --token "$PYPI_TOKEN"

# TestPyPI uses a separate account + token
uv publish --publish-url https://test.pypi.org/legacy/ \
  pkg/datahub_client-<version>-py3-none-any.whl \
  --token "$TEST_PYPI_TOKEN"
```

Bump [RELEASE_NOTES.md](RELEASE_NOTES.md) before tagging — the build derives the published version from it.
