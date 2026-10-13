# Validation

Use `client.Validation` to read ARC validation (CQC) results. Results are stored on the `cqc` branch under `<validated-branch>/<package-name>@<version>/`.

`branch` selects the validated ARC branch (default: `main`); `ref` selects the results branch or historical commit SHA (default: `cqc`). These are separate coordinates. Tree discovery reads up to 100 entries per call; history returns the server's current page.

## Setup

Replace the example token and project ID with your own. Python examples call each API from an async function, then run that function with `asyncio.run`. In an existing async application, call the API with `await` inside its running event loop. The repository runs these exact snippets against offline fixtures during documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:setup"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:setup"
    ```

## ListValidatedBranchesAsync

Discover ARC branches that have validation results. Files at the root of the results branch are skipped.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:branches"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:branches"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:branches"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:branches"
    ```

## ListPackagesAsync

Discover validation packages for an ARC branch. Each `ValidationPackageRef` contains `Name`, `Version`, `Branch`, and the results folder `Path`. Folders without a version suffix are skipped.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:packages"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:packages"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:packages"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:packages"
    ```

## ListHistoryAsync

Read validation commits, newest first. The optional `branch` scopes history to that ARC branch's results folder. Pass a returned commit `Id` as `ref` to read a historical summary, report, or badge.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:history"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:history"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:history"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:history"
    ```

## GetSummaryAsync

Read `validation_summary.json` as a `ValidationSummary`. `Critical` and `NonCritical` expose pass/fail/error counts; `ValidationPackage` identifies the validator. Create a reference when its coordinates are known, or use one returned by `ListPackagesAsync`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:summary"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:summary"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:summary"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:summary"
    ```

## GetReportAsync

Read the raw JUnit XML report using the package reference from the summary example. For a historical result, also pass `ref` (`GetReportAsync(projectId, packageRef, sha)` in JavaScript or TypeScript).

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:report"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:report"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:report"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:report"
    ```

## GetBadgeAsync

Read the package's validation badge as raw SVG text.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:badge"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:badge"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:badge"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:badge"
    ```

## GetAllSummariesAsync

Discover the packages for an ARC branch, then fetch each summary. The returned collection contains one `ValidationSummary` per discovered package; an empty package list returns an empty collection.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/validation/sample.fsx:all-summaries"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/validation/sample.mjs:all-summaries"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/validation/sample.ts:all-summaries"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/validation/sample.py:all-summaries"
    ```
