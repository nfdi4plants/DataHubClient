# Validation results API (`client.Validation`)

## Context

DataHubClient is used by upstream projects to **visualize ARC validation pipeline results**. Every ARC on a DataHub has a `cqc` branch holding validation output as defined by the [validation output spec](https://nfdi4plants.github.io/nfdi4plants.knowledgebase/arc-validation/authoring-validation-packages/#validation-output). Its layout:

```text
<arc branch name (usually "main")>/
└── <package_name>@<semver>/
    ├── badge.svg                  (SVG, text)
    ├── validation_report.xml      (JUnit XML, text)
    └── validation_summary.json    (JSON summary)
```

The library needs built-in **discovery** (which branches were validated, which packages produced results) and **one retrieval function per file type**, with the ability to read from **any point in the cqc branch history** (via a `?ref` accepting a commit SHA).

Design decisions:

- Surface name: **`ValidationApi`**, exposed as **`client.Validation`**.
- `validation_summary.json` is decoded into a **typed `ValidationSummary` model**.
- Addressing is **discovery-based**: getters take a `ValidationPackageRef` object (there is a whole package ecosystem, not a fixed set of names) rather than name/version strings. `ValidationPackageRef.Create(name, version, ?branch)` covers callers that already know the coordinates.
- Retrieval uses GitLab's **raw file endpoint** (`.../repository/files/{path}/raw?ref=`), skipping the base64 round-trip of the Files API. Raw-file fetch and tree listing stay **internal** to `ValidationApi` (not public `FilesApi`/`RepositoryApi` methods).

## API surface

```fsharp
[<AttachMembers>]
type ValidationApi(baseUrl: string, auth: Authentication, http: IHttpClient) =
    // ---- Discovery ----
    /// Top-level folders of the cqc branch = ARC branches that have results (usually ["main"])
    member ListValidatedBranchesAsync (projectId: int, ?ref: string) // -> string[]
    /// Package result folders under one ARC branch, parsed from "<name>@<version>" folder names
    member ListPackagesAsync (projectId: int, ?branch: string, ?ref: string) // -> ValidationPackageRef[]
    /// Commits on the cqc branch (optionally scoped to one ARC branch folder); each SHA is usable as ?ref
    member ListHistoryAsync (projectId: int, ?branch: string, ?ref: string) // -> Commit[]

    // ---- Retrieval: one function per file type ----
    member GetSummaryAsync (projectId: int, package: ValidationPackageRef, ?ref: string) // -> ValidationSummary
    member GetReportAsync  (projectId: int, package: ValidationPackageRef, ?ref: string) // -> string (JUnit XML)
    member GetBadgeAsync   (projectId: int, package: ValidationPackageRef, ?ref: string) // -> string (SVG)

    // ---- Bulk ----
    /// Discovers packages for the branch, then fetches every summary (Async.Parallel)
    member GetAllSummariesAsync (projectId: int, ?branch: string, ?ref: string) // -> ValidationSummary[]
```

Defaults: `?ref = "cqc"` (branch head; pass a SHA for history, or another name for nonstandard cqc branches), `?branch = "main"`.

## Types

**`Models/ValidationPackageRef.fs`** — discovered result folder: `Name`, `Version`, `Branch`, computed `Path` = `{Branch}/{Name}@{Version}`, static `Create(name, version, ?branch)`. Named `…Ref` (not `ValidationPackage`) to avoid confusion with ARCtrl's `ValidationPackage` type.

**`Models/ValidationSummary.fs`** + **`Json/ValidationSummary.fs`** — typed per the authoritative source, [arc-validate `release` branch, `ARCExpect.Core/ValidationSummary.fs`](https://github.com/nfdi4plants/arc-validate/blob/release/src/ARCExpect.Core/ARCExpect.Core/ValidationSummary.fs) (verified 2026-07-13; JSON field names are PascalCase):

- `ValidationResult`: `HasFailures: bool`, `Total/Passed/Failed/Errored: int`
- `ValidationPackageSummary`: `Name/Version: string`, optional `Summary`/`Description`/`CQCHookEndpoint`
- `ValidationSummary`: `Critical`/`NonCritical: ValidationResult`, `ValidationPackage: ValidationPackageSummary`
- Upstream type names reused for ecosystem familiarity. Upstream's `Payload: Dictionary<string, obj> option` is **omitted in v1** — arbitrary JSON is not portably typeable via Thoth across the three runtimes; add later as a raw-JSON-string field if needed.
- The decoder is lenient: descriptive strings are `Optional`, unknown fields are ignored.

## Implementation stages

- [x] `Models/ValidationPackageRef.fs`, `Models/ValidationSummary.fs`, `Json/ValidationSummary.fs`, `Resources/ValidationApi.fs`
- [x] Registered in the target-agnostic `<Compile>` list of all three project files, `member _.Validation` on the facade
- [x] Unit tests: SampleData fixtures (tree listings, summary JSON, report XML, badge SVG) + `Validation.*` cases in `ResourceTests.fs`, round-trips in `ModelTests.fs`
- [x] Integration tests: two read-only cases in `projectCases`, gated on the project having a cqc branch (pass without assertion otherwise)
- [x] `RunTestsAll` green on .NET, JavaScript, and Python; integration suites transpile on both Fable targets
- [x] Transpiled TypeScript surface inspected: clean classes, `Promise`-returning methods, no mangled backing fields
- [x] Integration cases exercised against a live ARC with a cqc branch (`schneider.kev/test` on gitdev, `arc_specification@2.0.0-draft` results): 19/19 on .NET, JS, and Python, including summary/report/badge fetches and a historic-SHA refetch

Note: the 2026 dev-instance reset removed the canonical `integration_tests/test_1` fixture project. The `Fixture:` cases now additionally gate on the configured project's `PathWithNamespace` being `integration_tests/test_1` (no-op otherwise); recreate that fixture project to reactivate them.
