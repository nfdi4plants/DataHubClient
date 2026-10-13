# DataHubClient API coverage and browser support plan

Date: 2026-10-13. Baseline: 0.5.1.

DataHubClient supports DataHub discovery, reading, individual file edits, and
basic collaboration. The next additions should make common ARC browsing,
editing, and collaboration workflows complete across .NET, JavaScript, and
Python. This plan records the repository audit and proposed implementation
priorities; it does not authorize implementing every item immediately.

The current RO-Crate download is sufficient. Export freshness checks are out
of scope. TypeScript packaging is the first implementation step below.

## Audit findings

### Browser compatibility

The JavaScript library is compatible with modern browsers by implementation:
its default transport uses global `fetch`, public resource methods return
native Promises, and the library has no Node-specific API dependencies.

- [FetchHttpClient.fs](../src/DataHubClient/Http/FetchHttpClient.fs) maps requests
  to `fetch` and reads responses as text.
- [ResourceHelpers.fs](../src/DataHubClient/Resources/ResourceHelpers.fs)
  explicitly converts F# async computations to JavaScript Promises.
- The npm package contains ES modules. A frontend bundler can resolve npm
  imports; direct browser use requires serving/resolving the module files.
- Every request includes `X-DataHubClient-Version`, so anonymous cross-origin
  GETs also require CORS preflight. Token headers must be allowed as well.
- Shared JavaScript tests and integration tests run under Node. The separate
  `RunTestsBrowser` target installs the npm tarball and runs Chromium and Firefox
  against native ES modules, Vite development, and Vite production output.
- Some documentation examples are Node-specific: RO-Crate saving uses
  `node:fs/promises`, and file decoding examples use `Buffer`. RO-Crate examples
  now distinguish Node.js from the browser and include a tested Blob download.

During the audit, `build.cmd RunTestsJavaScript` passed all 53 tests. An HTTP
check against `https://git.nfdi4plants.org/api/v4/projects?per_page=1`, using
origin `http://localhost:5173`, confirmed successful anonymous GET and
preflight responses. Preflights allowed the version header and the combination
of version and `PRIVATE-TOKEN` headers. That initial probe did not perform an
authenticated request or actual browser execution. This is evidence for that
endpoint on the audit date, not a guarantee for every DataHub instance.

Browser verification added on 2026-10-13 passed all six browser/mode combinations
for both the published npm `0.5.1` package and the current TypeScript/Vite build.
The checks use real fetch against a second local origin: authenticated and
anonymous requests, CORS preflights and denials, pagination, models, HTTP error
classes, Unicode repository-file decoding, package upload, and RO-Crate download
with byte-for-byte UTF-8 verification. They require no live DataHub or credentials.

The reported filesystem-import failure is reproducible by copying the Node.js
RO-Crate example into a browser application. Vite development rejects access to
`node:fs/promises.writeFile`; its production build reports that `writeFile` is
not exported by the browser-externalized module. Neither inspected npm artifact
imports Node filesystem APIs. The browser documentation now uses
[a Blob/download-link helper](../docs/samples/ro-crate/browser.mjs) instead.
The user clarified that the download snippet was used without the save part.
Retaining the now-unused filesystem import still reproduces the native-browser
and Vite-development failure; Vite production can optimize away the unused
access and succeed. The exact original import/build setup remains unknown.
The docs now separate metadata retrieval from platform-specific saving. Browser
tests execute the exact metadata-only snippet, substituting only the local
fixture URL, so its import list is covered as well as the client method.

### Accepted RO-Crate download behavior

[Packages.DownloadROCrateAsync](../src/DataHubClient/Resources/PackagesApi.fs)
downloads JSON-LD metadata with this request:

```text
GET /api/v4/projects/{id}/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json
```

It retrieves the published metadata at the standard coordinates. It does not
generate an export or select a source commit. The documentation's warning that
the file may not represent the latest commit is a description of this behavior,
not evidence that the downloaded file is outdated or that the method is faulty.
The fixed package version does not itself establish the age of the content.

**Decision:** retain the current download behavior. Do not add source-SHA
comparison, freshness status, provenance verification, or automatic regeneration
as requirements of this plan. The package will be versioned in the future;
decide how the convenience method selects a version when that publishing
contract is defined. `DownloadGenericFileAsync` already accepts explicit
package coordinates.

### API coverage

The facade exposes eight resource classes and 40 methods. The most consequential
gaps are incomplete pagination, missing repository browsing, and the absence of
atomic multi-file writes.

| Workflow | Current limitation |
| --- | --- |
| Complete listings | Only `Projects.ListAllAsync` traverses all pages. Other resource list and note methods expose neither page controls nor continuation metadata. Validation tree discovery requests only 100 entries. |
| Browse an ARC | No public repository-tree API. `Files.GetAsync` requires a known path and returns encoded content and metadata. Tree/raw-file calls exist privately within validation. |
| Save related ARC files | Create/update/delete calls commit individual files. No batch commit, move/rename action, or `last_commit_id` concurrency guard. |
| Create and locate projects | No project creation, forking, settings updates, groups/namespaces, membership information, writable-project filtering, or lookup by namespace/path. |
| Complete a review workflow | Branches are read-only. MRs can be created, edited, closed, and commented on, but cannot be merged through the client. Diffs, comparison, and review discussions are absent. |
| Transfer datasets | Generic package transfers and the HTTP body contract use strings. No binary package or repository archive download API. Repository files can still carry binary content through base64. |
| Manage validation jobs | Validation reads existing results. No pipeline/job inspection, logs, artifacts, or execution controls. |
| Coordinate work | Missing issue label/assignee/milestone updates, user search, MR reviewers, comment editing/deletion, tags/releases, and package-file management. |

Relevant implementation: [ProjectsApi](../src/DataHubClient/Resources/ProjectsApi.fs),
[RepositoryApi](../src/DataHubClient/Resources/RepositoryApi.fs),
[FilesApi](../src/DataHubClient/Resources/FilesApi.fs),
[IssuesApi](../src/DataHubClient/Resources/IssuesApi.fs),
[MergeRequestsApi](../src/DataHubClient/Resources/MergeRequestsApi.fs),
[PackagesApi](../src/DataHubClient/Resources/PackagesApi.fs),
[ValidationApi](../src/DataHubClient/Resources/ValidationApi.fs).

## Implementation stages

These stages are proposed priorities for common ARC application workflows.
Browser verification can proceed alongside the resource additions. Tick items
only after their implementation and relevant verification are complete.

### Stage 1 Complete pagination

- [ ] Choose a consistent cross-target contract for page controls, continuation
      information, and complete traversal without breaking existing methods.
- [ ] Apply it to branches, commits, issues, MRs, notes, packages, and validation
      history and discovery.
- [ ] Cover empty results, multiple pages, short intermediate pages where the
      server indicates continuation, and errors on later pages.

Acceptance: callers can retrieve every matching item through public APIs,
including validation folders beyond the first 100. Complete-list operations
must not silently return partial results when a later request fails.

### Stage 2 Repository browsing and file transfer

- [ ] Add public repository-tree listing with path, ref, recursion, and pagination.
- [ ] Add convenient raw text and binary file reads, preserving existing file
      metadata and base64 APIs.
- [ ] Define a binary-capable HTTP contract for all three transports.
- [ ] Add repository archive downloads and binary generic-package transfers.
- [ ] Define LFS behavior for applicable file/archive operations.

Acceptance: an application can discover an ARC's files without knowing their
paths in advance. Binary downloads and uploads preserve exact bytes. Document
buffering or streaming limits explicitly.

### Stage 3 Atomic ARC edits

- [ ] Add commit creation with multiple create/update/delete/move actions.
- [ ] Expose GitLab's applicable `last_commit_id` guards and required metadata.
- [ ] Return the resulting commit identity so callers can read the committed
      state without relying on a subsequent branch-head read.

Acceptance: related file edits can form one commit, and stale-edit guards
surface conflicts. Verify atomic failure behavior against a controlled live
project. Existing single-file APIs remain available; their documented
write-success/read-back-failure behavior must remain explicit.

### Stage 4 Project and collaboration workflows

- [ ] Add project creation, forking, settings updates, and namespace/path lookup.
- [ ] Add group/namespace discovery, membership/permission information, and
      filters for selecting projects the caller can edit.
- [ ] Add branch creation/deletion, repository comparison, and MR diffs.
- [ ] Add MR merging and review discussion operations.

Acceptance: a client can choose a destination, create or fork an ARC, create a
branch, commit changes, open and review an MR, and merge it with appropriate
server permissions and conflict handling.

### Stage 5 Operational APIs and routine coordination

- [ ] Add pipeline/job inspection, logs, artifacts, and appropriate run/retry/
      cancel operations for validation and other CI workflows.
- [ ] Add issue label/assignee/milestone updates, user search, MR reviewers, and
      comment editing/deletion.
- [ ] Add tag/release operations and package-file enumeration, checksums, and
      deletion as concrete consuming workflows require them.

Acceptance: applications can report job progress and failures and perform the
selected coordination operations without constructing raw HTTP requests.
These APIs do not introduce an RO-Crate freshness requirement.

### Browser support and reliability

- [x] Add a real browser smoke test that imports the distributed package and
      exercises resource methods and browser download behavior.
- [x] Cover browser CORS behavior using controlled origins; keep live-instance
      checks distinct from deterministic browser tests.
- [x] Add a tested browser example for JSON-LD downloads and distinguish it from
      the Node.js filesystem example.
- [ ] Add browser file-decoding examples to the resource guide (the browser
      test fixture already verifies `atob` plus `TextDecoder` with UTF-8).
- [ ] Define cancellation, timeout, and rate-limit handling across transports,
      including access to relevant response headers such as `Retry-After`.
- [ ] Add controlled, self-cleaning live write tests alongside the existing
      read-only integration suite.

Acceptance: browser compatibility is exercised in a browser, and common
failure modes have documented behavior. Live tests must isolate their resources
and clean up after themselves; credentials must remain outside fixtures.

## TypeScript declarations

Ship JavaScript and generated declarations together so JavaScript and TypeScript
consumers use the same package and import paths. Following
[ARCtrl's library build](https://github.com/nfdi4plants/ARCtrl/blob/main/vite.config.mts),
Fable produces intermediate TypeScript and Vite with `vite-plugin-dts` produces
ES modules and adjacent `.d.ts` files. The package remains executable JavaScript;
consumers need no TypeScript runtime.

- [x] Update Fable to 5.18.0 to fix invalid generic parameters emitted by the old
      TypeScript compiler target; align the Python runtime with that compiler.
- [x] Transpile into `dist/ts`, check from the `DataHubClient.ts` entry point,
      and emit JavaScript and declarations into `dist/js` with Vite.
- [x] Preserve existing root and deep imports and include required runtime
      declarations in the package; advertise the declaration entry with `types`.
- [x] Pin build dependencies and integrate the build and packed-consumer checks
      with FAKE and CI, including a standalone `PackJavaScript` target.
- [x] Verify every declaration in a locally installed tarball, strict NodeNext
      and Bundler consumers, negative type checks, offline runtime requests,
      and a Vite browser-bundle build. The bundle runtime check runs in Node.
- [x] Document the shared JS/TS imports and Fable's `MutableArray<T>` list-result
      types; use `Array.from` when a standard JavaScript array is needed.
- [x] Add dedicated TypeScript tabs throughout the examples, backed by strict
      `.ts` sources and packaged execution checks. All 44 sample programs and
      six browser cases pass; all 54 TypeScript tabs render correctly in the
      built guide (2026-10-13).
- [x] Verify the compiler upgrade with the existing suites and packaged samples
      on all three targets: 51 .NET, 53 JavaScript, and 53 Python tests passed,
      followed by all documentation samples. `PackJavaScript` also passed with
      all 78 declarations checked (2026-10-13).

`tsconfig.json` selects the generated facade as its root, as ARCtrl selects its
barrel. Imported modules remain part of the TypeScript program even when their
directory is excluded from root discovery. Declaration emission also includes
those imported runtime types so the installed package is self-contained. Vite's
declaration plugin receives a generated configuration containing the reachable
source files, including dependencies imported only as types; unused Fable
runtime sources are not additional compilation roots.

## Cross-target delivery requirements

Follow [AGENTS.md](../AGENTS.md) and the architecture in [mvp.md](mvp.md):
attached public classes, explicit backing fields, native public async behavior,
hand-written JSON modules, and XML documentation. Register new source files in
all three project files in matching dependency order.

Public collection returns, method/function arguments, constructor parameters,
and properties must use `ResizeArray<'T>` on every target. This produces native
TypeScript `T[]` declarations and supersedes the initial `MutableArray<T>` /
`Array.from` guidance above. Internal arrays, lists, and sequences may remain
implementation details. The migration changes the .NET collection signatures,
including resource results, issue assignees/labels, and request/response headers.

For substantive additions, run the relevant shared and transport-specific
Pyxpecto suites through the build pipeline, inspect generated JavaScript and
Python API surfaces, and update packaged documentation examples. Add live tests
where server behavior cannot be established by fixtures. Keep this checklist
and any corresponding MVP implementation checklist entries synchronized as work
is completed.

## References

- [GitLab pagination](https://docs.gitlab.com/api/rest/#pagination)
- [Repository trees and archives](https://docs.gitlab.com/api/repositories/)
- [Commit creation and actions](https://docs.gitlab.com/api/commits/#create-a-commit)
- [Repository file updates](https://docs.gitlab.com/api/repository_files/#update-existing-file-in-repository)
- [Generic packages](https://docs.gitlab.com/user/packages/generic_packages/)
- [Fetch CORS protocol](https://fetch.spec.whatwg.org/#http-cors-protocol)
