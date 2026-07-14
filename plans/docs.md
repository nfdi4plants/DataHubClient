# Documentation site

## Context

DataHubClient is one F# source tree shipped to three ecosystems, so the docs
must show every sample in all three languages **and** CI must fail when any of
the three drifts from the code base. `fsdocs --eval` only ever verifies the F#
column; JavaScript and Python prose samples would rot silently.

## Design decisions

- **Samples are runnable files, not prose.** Each doc sample lives as a real
  program per language under `docs/samples/<topic>/sample.{fsx,mjs,py}`. A FAKE
  target `RunDocsSamples` (depends on `Pack`) executes every one of them against
  the freshly-packed artifacts using the `TestPackages` consumer pattern —
  `dotnet fsi` + local NuGet source, `npm install <tarball>` + node, venv +
  wheel + python. Doc pages *include* the files by reference, so what renders is
  byte-identical to what ran. This is stronger verification than fsdocs eval:
  it exercises the published import surface (`from datahub_client import …`,
  `@nfdi4plants/datahub-client`), not the source tree.
- **MkDocs Material renders the guide.** Native content tabs (`=== "F#"`)
  with `content.tabs.link` so selecting a language switches every tab group on
  the page, and native file inclusion via `pymdownx.snippets`
  (`--8<-- "docs/samples/<topic>/sample.py"`). No hand-rolled tab JS or include
  preprocessor to maintain. Installed via a `docs` uv dependency group.
- **fsdocs stays, scoped to the generated F# API reference.** Input moves to
  `docs/fsdocs/` (the guide markdown is pymdownx-flavored and must stay out of
  fsdocs' hands), output lands in `site/fsdocs/` inside the MkDocs site, cracked
  only from `DataHubClient.fsproj`. The guide links to `fsdocs/reference/`.
  `--eval` remains available for future literate F# pages but carries no
  correctness burden — the samples pipeline does.
- **Samples are offline-by-construction.** PR CI has no live DataHub, so
  samples never hit the network: construction/serialization-level code, or (in
  later stages) an inline stub `IHttpClient` — which doubles as documentation of
  the transport injection point. Live-server walkthroughs stay in the
  (non-gating) integration suites.
- **CI:** a `docs` job on every PR/push runs `RunDocsSamples` + `BuildDocs`, so
  out-of-sync docs fail PRs in all three languages. On version tags (and
  `workflow_dispatch`, for bootstrap) the built `site/` deploys to GitHub Pages
  via `actions/deploy-pages` — docs versions track package releases.
- **Legacy interactive release tasks are retired.** `ReleaseTasks.fs`
  (prompt-driven tagging, `YOUR_KEY_HERE` NuGet push, gh-pages clone/push),
  the `Release*` meta-targets, and `MessagePrompts.fs` predate CI trusted
  publishing and would silently break once docs output moves to `site/`.
  Publishing is tags + CI; docs deploy is Pages.

## Mechanics

### Running the committed sample files

- **F#** — the committed `sample.fsx` opens with `#r "nuget: DataHubClient"`
  (exactly what a script user writes). The runner copies it to a scratch dir,
  replacing that line with `#i "nuget: <abs pkg/>"` + a versioned `#r`, wipes
  `~/.nuget/packages/datahubclient/<version>` (same shadowing hazard as
  `TestPackagesDotNet`), and runs `dotnet fsi`.
- **JS** — scratch dir with a private `package.json`, `npm install` the packed
  tarball, run the committed `sample.mjs` unmodified.
- **Python** — venv outside the repo (FAKE `Shell.cleanDirs` chokes on `lib64`
  symlinks), `pip install` the wheel, run the committed `sample.py` unmodified.

Scratch roots live under `pkg/docsSamples/` next to `pkg/testScripts/`.
Shared process/scratch helpers move from `TestPackageTasks.fs` into
`Helpers.fs`.

### Page syntax

```markdown
=== "F#"

    ```fsharp
    --8<-- "docs/samples/getting-started/sample.fsx"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/getting-started/sample.mjs"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/getting-started/sample.py"
    ```
```

When a sample later needs a hidden harness (stub transport, assertions), mark
the shown region with `--8<-- [start:…]` / `[end:…]` section comments and
include `sample.py:section` instead of the whole file.

### Layout

```text
mkdocs.yml                    # Material theme, tabs, snippets, exclude samples/+fsdocs/
docs/
  index.md                    # guide landing page (mkdocs)
  getting-started.md          # tabbed install + first-client page
  reference.md                # links into the fsdocs-generated API reference
  samples/<topic>/sample.*    # verified polyglot samples (excluded from site nav)
  fsdocs/index.md             # fsdocs input shell → site/fsdocs/
site/                         # build output (gitignored)
```

## Implementation stages

- [x] Plan written up (this document)
- [x] `docs/samples/getting-started/` triple + `build/DocsSampleTasks.fs`
      (`RunDocsSamples`), shared helpers lifted into `Helpers.fs`
- [x] MkDocs Material site: `docs` uv dependency group, `mkdocs.yml`,
      `index.md` / `getting-started.md` / `reference.md`, `site/` gitignored
- [x] fsdocs input → `docs/fsdocs/`, output → `site/fsdocs/`;
      `DocumentationTasks.fs` rewritten (`BuildDocs` = mkdocs build + fsdocs
      build, `WatchDocs` = mkdocs serve); `ReleaseTasks.fs`/`MessagePrompts.fs`
      and `Release*` meta-targets removed
- [x] CI: gating `docs` job (samples + site build) on PR/push; Pages deploy
      job on version tags / `workflow_dispatch`
- [x] Verified locally: `./build.sh RunDocsSamples` and `./build.sh BuildDocs`
      green; all three samples executed against the packed artifacts and
      rendered as synced tabs (no unexpanded snippet markers) in the built site
- [ ] Later: richer topic samples (stub-transport `client.Http` injection,
      validation results walkthrough) using section markers
