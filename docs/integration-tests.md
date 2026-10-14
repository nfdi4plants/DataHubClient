# Live integration tests

The live suite runs the same F# tests against a real DataHub on .NET, Node.js,
and Python. Every request is read-only. Unit tests and packaged documentation
samples use controlled responses; passing those does not prove that a deployed
DataHub's package storage and redirect configuration work.

## Run the suite

Set these environment variables before running a build target:

| Variable | Purpose |
| --- | --- |
| `DATAHUB_TEST_URL` | DataHub root, such as `https://gitdev.nfdi4plants.org`. |
| `DATAHUB_TEST_TOKEN` | Personal access token permitted to read the selected project and packages. |
| `DATAHUB_TEST_PROJECT` | Positive numeric project ID for repository, issue, merge request, package-list, and validation reads. |
| `DATAHUB_TEST_REQUIRED` | Set to `true` to fail if URL, authentication, or project configuration is missing. The integration workflow always sets this. |
| `DATAHUB_TEST_ANONYMOUS` | Set to `true` explicitly to exercise public resources without authentication. Overrides the token locally; the workflow does not enable it. |
| `DATAHUB_TEST_ROCRATE_PROJECT` | Optional positive project ID whose `isa_arc_json/0.0.1/arc-ro-crate-metadata.json` export must be downloadable. Can differ from the general test project. |
| `DATAHUB_TEST_FIXTURE` | Optional `dataplant-dev` to require the canonical fixture described below. Leave unset for other projects. |

With no local configuration the suite skips. Invalid explicit configuration
fails, and required mode cannot silently pass with zero tests. The RO-Crate
cases run only when their project is configured; they check parsed JSON for
`@context` and a nonempty `@graph` whose entries have `@id`. These are structural
checks, not full RO-Crate semantic validation. A missing export, denied access, HTML
login page, or download error fails the test.

```powershell
$env:DATAHUB_TEST_URL = "https://gitdev.nfdi4plants.org"
$env:DATAHUB_TEST_ANONYMOUS = "true"
$env:DATAHUB_TEST_PROJECT = "38"
$env:DATAHUB_TEST_ROCRATE_PROJECT = "38"
$env:DATAHUB_TEST_REQUIRED = "true"
.\build.cmd RunIntegrationTestsAll
```

Use `./build.sh` on Linux/macOS. Individual targets are
`RunIntegrationTestsDotNet`, `RunIntegrationTestsJavaScript`, and
`RunIntegrationTestsPython`. Python needs `uv sync` and Python 3.12 or later.
For a download-only smoke test, omit `DATAHUB_TEST_PROJECT` and
`DATAHUB_TEST_REQUIRED`; instance listing and the configured download cases run.

## GitHub Actions setup

The separate [integration workflow](https://github.com/nfdi4plants/DataHubClient/blob/main/.github/workflows/integration.yml)
runs weekly and on demand, outside pull-request CI. All three jobs use the
`integration_tests` GitHub Environment. Store `DATAHUB_TEST_TOKEN` as an
environment secret and the URL/project/RO-Crate/fixture settings as environment
variables. The workflow sets required mode itself. A token secret's existence
does not prove its validity; an authenticated run checks that.

If GitHub reports `disabled_inactivity`, an administrator must re-enable the
workflow in Actions, or run `gh workflow enable integration.yml`, then dispatch
it. See [GitHub's workflow enablement documentation](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/disable-and-enable-workflows).
Check the case counts and selected configuration as well as the green badge.
Issue/merge-request detail cases require at least one corresponding resource;
validation detail cases require populated `cqc` results. Empty fixtures do not
exercise those paths.

## Make Git dev reproduce package redirects

A direct `200` package response tests downloading but cannot catch the original
HTTPX `302` failure. Prepare a stable ARC export in a fixture project, enable
its package registry, and ensure its file is stored in external object storage.
The package coordinates must be:

```text
GET /api/v4/projects/<id>/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json
```

For GitLab's consolidated object-storage configuration, configure the packages
bucket and use `proxy_download = false` for packages so clients retrieve files
from the storage service. Existing local package files must be migrated or the
fixture republished into object storage. The exact configuration depends on the
GitLab version/deployment; use its matching [object storage documentation](https://docs.gitlab.com/administration/object_storage/)
and [package storage migration instructions](https://docs.gitlab.com/administration/packages/#migrate-to-object-storage).
The observed direct `200` alone does not identify whether the current dev
server uses local storage or proxies an object store.

Check the first response with redirects disabled: it must be a redirect with
a `Location` pointing to a different origin, and that destination must be
reachable from GitHub runners with a valid TLS certificate. For a public
fixture, this probe prints only the status and destination origin, keeping
signed URL query parameters out of logs:

```python
import os
from urllib.parse import urljoin, urlsplit
import httpx

base = os.environ["DATAHUB_TEST_URL"].rstrip("/")
project = int(os.environ["DATAHUB_TEST_ROCRATE_PROJECT"])
url = f"{base}/api/v4/projects/{project}/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json"
response = httpx.get(url, follow_redirects=False, timeout=30)
assert response.status_code in (301, 302, 303, 307, 308)
target = urlsplit(urljoin(url, response.headers["location"]))
source = urlsplit(url)
def origin(parts):
    return parts.scheme, parts.hostname, parts.port or (443 if parts.scheme == "https" else 80)

assert origin(target) != origin(source)
print(response.status_code, target.scheme, target.hostname, target.port)
```

Set `DATAHUB_TEST_ROCRATE_PROJECT` in the GitHub environment and run all three
integration jobs after establishing that redirect. Successful downloads prove
the deployed redirect path works. They do **not** prove that credentials were
removed: an object store can ignore and accept leaked GitLab headers.

To prove non-forwarding against Git dev, the receiving storage endpoint or a
controlled redirect receiver must record **presence only**, or reject requests
containing `PRIVATE-TOKEN`, `JOB-TOKEN`, or `Authorization`. Correlate a test
request with that observation. Never record token values or signed URL query
strings. The local transport regression tests provide deterministic header
assertions; the shared live download suite cannot see the remote receiver's
headers. Testing each authentication mode live also requires valid PAT,
OAuth, and GitLab job credentials; the GitHub suite currently uses a PAT.

The live JavaScript suite runs in Node. Browser coverage additionally needs
appropriate CORS behavior and browser tests; Node success does not establish
browser redirect support.

`RunTestsBrowser` installs the packed npm artifact and runs native fetch in
Chromium and Firefox, each with native ES modules, Vite development, and Vite
production. Its local authenticated package fixture requires a valid dummy
OAuth bearer token before issuing a redirect; missing and incorrect tokens
must return `401` without reaching storage. Same-origin destinations require
the token, while cross-origin destinations and later return hops reject all
GitLab credential headers. Successful cases verify exact UTF-8 RO-Crate content
through both download APIs and files saved by the documented browser helper.
These tests establish supported bearer-authenticated browser downloads against
controlled servers; they do not validate deployed DataHub credentials or CORS.
The separate browser cases for `PRIVATE-TOKEN` and `JOB-TOKEN` still require
redirects to be blocked, as described in [RO-Crate downloads](ro-crate.md#redirects-and-authentication).

## Optional canonical content fixture

`DATAHUB_TEST_FIXTURE=dataplant-dev` requires the public
`integration_tests/test_1` project on `https://gitdev.nfdi4plants.org`, named
`Test_1`, with protected default branch `main`. Its `README.md` must contain
`Hello DataHubClient!`; issue IID 1 must be open, titled `Test_Issue_1`, described
as `This is an issue for DataHubClient integration tests!`, and labeled
`integration_tests`. Point `DATAHUB_TEST_PROJECT` at that project's actual ID.
A missing or different project now fails instead of silently passing the
fixture cases. These content assertions are independent of the RO-Crate
fixture, which can use another project.

## Audit on 2026-10-14

- The [last recorded integration run](https://github.com/nfdi4plants/DataHubClient/actions/runs/36404537280)
  was on September 28, with **19 tests executed and passed on each target** at
  commit `4826e22c25101b6a545aeeb732fa4ae2eb92cb81`. These included only package
  listing, not package downloads.
- GitHub reported the workflow as `disabled_inactivity`; no October 5 or
  October 12 runs were listed. Re-enabling it is still an administrator action.
- The GitHub environment contained the dev URL, project ID `38`, and a token
  secret. Secret contents are unavailable through GitHub's read API, so this
  audit could not validate the stored token.
- Anonymous reads identified project `38` as `schneider.kev/test`, with `main`
  and `cqc` branches, no issues or merge requests, and the standard RO-Crate
  package. The canonical fixture path returned `404` anonymously.
- The previous suite selected canonical fixture tests by hostname, then
  silently skipped their assertions for a different project. Those cases
  could appear as passed without checking fixture content. Fixture selection
  is now explicit and a mismatch fails.
- The dev RO-Crate returned **direct `200`**, with 12 graph entities. This
  fixture can exercise download parsing now, but needs an actual cross-origin
  storage redirect to reproduce the production failure.
- The updated suite passed anonymously on all three targets against dev
  project 38 (17 cases per target, including both downloads). The empty issue
  and merge-request lists still leave their detail paths unexercised.
  Download-only runs also passed on all targets against production project
  1040 (three cases each), whose export redirects to object storage. These
  runs do not validate the GitHub token or the optional canonical fixture.
- No remote workflow, token, project, package, or storage configuration was
  changed during the audit.
