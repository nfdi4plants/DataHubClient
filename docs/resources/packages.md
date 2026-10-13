# Packages

Use `client.Packages` to browse a project's Package Registry, transfer generic package files, and download an ARC's generated RO-Crate. `packageId` is the numeric registry package ID; generic file operations use the package name, version, and file name instead.

Upload and download currently accept or return **text strings**. These methods do not provide a binary byte-stream API. Listing returns the server's current page.

## Setup

Replace the example token and project ID with your own. Python examples call each API from an async function, then run that function with `asyncio.run`. In an existing async application, call the API with `await` inside its running event loop. The repository runs these exact snippets against offline fixtures during documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/packages/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/packages/sample.mjs:setup"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/packages/sample.ts:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/packages/sample.py:setup"
    ```

## ListAsync

Filter the registry by package type and name. Both filters are optional.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/packages/sample.fsx:list"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/packages/sample.mjs:list"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/packages/sample.ts:list"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/packages/sample.py:list"
    ```

## GetAsync

Read the registry metadata for a package ID returned by `ListAsync`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/packages/sample.fsx:get"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/packages/sample.mjs:get"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/packages/sample.ts:get"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/packages/sample.py:get"
    ```

## UploadGenericFileAsync

Upload a text file under a generic package name and version. This writes to the registry and returns the server's response body as a string.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/packages/sample.fsx:upload"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/packages/sample.mjs:upload"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/packages/sample.ts:upload"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/packages/sample.py:upload"
    ```

## DownloadGenericFileAsync

Read a generic package file as text; parse it separately when it contains JSON or another structured format.

For the DataHub's generated ARC export, see
[`DownloadROCrateAsync`](#downloadrocrateasync), which supplies the standard
package coordinates for you.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/packages/sample.fsx:download"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/packages/sample.mjs:download"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/packages/sample.ts:download"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/packages/sample.py:download"
    ```

## DownloadROCrateAsync

Download an ARC's currently available generated main-branch RO-Crate as JSON-LD
text. This method takes only the numeric project ID and fetches
`isa_arc_json/0.0.1/arc-ro-crate-metadata.json` from the generic Package Registry.
It is available since version `0.5.0`.

The following examples create an anonymous client. Replace the project ID and
use a token if the export requires credentials. The JavaScript and TypeScript
examples retrieve JSON-LD text in either a browser or Node.js; the F# and Python
examples also save the text as UTF-8. See [Download and save](../ro-crate.md#download-and-save)
for Node.js filesystem and browser download-link examples.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/ro-crate/sample.fsx:setup"
    --8<-- "docs/samples/ro-crate/sample.fsx:download"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/ro-crate/sample.mjs:setup"
    --8<-- "docs/samples/ro-crate/sample.mjs:download"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/ro-crate/sample.ts:setup"
    --8<-- "docs/samples/ro-crate/sample.ts:download"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/ro-crate/sample.py:setup"
    --8<-- "docs/samples/ro-crate/sample.py:download"
    ```

The export is not guaranteed to represent the latest commit. See
[Download an ARC RO-Crate](../ro-crate.md) for export freshness and handling
missing or inaccessible exports.
