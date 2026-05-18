# DataHubClient
A Polyglot client library for ARC Data Hubs.

## Development

Build and test orchestration runs through the FAKE pipeline — see [AGENTS.md](AGENTS.md) for the full layout and conventions.

```sh
./build.sh                 # build the solution   (build.cmd on Windows)
./build.sh RunTestsPython  # transpile + run the Python suite under uv/python
```

> **⚠️ The `.venv` is OS-specific — rebuild it when you switch OS.** `uv` creates a
> platform-specific virtual environment in `.venv` (it is `.gitignore`d). A `.venv`
> carried over from another OS — e.g. from the Linux devcontainer onto a Windows host —
> makes `uv run` and the `RunTestsPython` build target fail. Delete `.venv` and let
> `uv sync` / `uv run` rebuild it for the current OS.
