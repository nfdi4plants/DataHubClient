# Integration tests

A live integration suite that exercises `DataHubClient` against a real ARC
DataHub instead of the in-memory `MockHttpClient`. It reuses the Pyxpecto
testing setup of the unit suite: one F# source tree (`LiveConfig.fs`,
`IntegrationTests.fs`, `Program.fs`) compiled by three parallel project files —
this `.NET` project plus the sibling `DataHubClient.Integration.JavaScript.Tests`
and `DataHubClient.Integration.Python.Tests` — so the same cases run on every
target.

The current cases are **read-only** (`List` / `Get` on projects, branches,
commits, issues, merge requests); none create or mutate resources.

## Configuration

The suite reads its target from environment variables:

| Variable | Required | Purpose |
| --- | --- | --- |
| `DATAHUB_TEST_URL` | yes | DataHub root URL, e.g. `https://gitlab.example.org` (no `/api/v4`). |
| `DATAHUB_TEST_TOKEN` | yes | A personal access token for that DataHub. |
| `DATAHUB_TEST_PROJECT` | no | A numeric project id; gates the project-scoped cases. |

When `DATAHUB_TEST_URL` or `DATAHUB_TEST_TOKEN` is unset, every case is
**skipped, not failed**, so the suite no-ops on local runs and fork PRs. When
`DATAHUB_TEST_PROJECT` is unset, only the instance-level case (`Projects.List`)
runs.

The lookup is target-specific (`System.Environment` on .NET, `process.env` on
JavaScript, `os.environ` on Python), selected by `#if` in `LiveConfig.fs`.

## Running

```bash
export DATAHUB_TEST_URL=https://gitlab.example.org
export DATAHUB_TEST_TOKEN=glpat-xxxxxxxxxxxxxxxxxxxx
export DATAHUB_TEST_PROJECT=42

./build.sh RunIntegrationTestsDotNet       # .NET
./build.sh RunIntegrationTestsJavaScript   # Fable -> node
./build.sh RunIntegrationTestsPython       # Fable -> python
./build.sh RunIntegrationTestsAll          # all three
```

These tasks are **not** part of `RunTestsAll` or the release flow — the live
suite must never gate a PR on a dev-instance outage.
