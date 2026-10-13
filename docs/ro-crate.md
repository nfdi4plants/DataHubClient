# Download an ARC RO-Crate

DataPLANT DataHubs publish a generated ARC RO-Crate metadata file in the
project's generic Package Registry. Download it with
`client.Packages.DownloadROCrateAsync(projectId)` (available since version
`0.5.0`). The method takes the numeric project ID and uses these built-in
package coordinates:

| Coordinate | Value |
| --- | --- |
| Package name | `isa_arc_json` |
| Package version | `0.0.1` |
| File name | `arc-ro-crate-metadata.json` |

This retrieves the **currently available generated main-branch export**.
It does not generate a new export, and the available file is not guaranteed to
represent the latest commit on `main`.

For a file stored under different package coordinates, use
[`DownloadGenericFileAsync`](resources/packages.md#downloadgenericfileasync).

## Download and save

Replace `42` with the ARC's numeric project ID. The example uses anonymous
access for a public export. For an export that requires credentials, replace
`Authentication.Anonymous()` with
`Authentication.PersonalAccessToken("your-datahub-pat")`.

The method returns JSON-LD text. These examples save that text as UTF-8 to
`arc-ro-crate-metadata.json` in the current working directory, replacing an
existing file with that name. The JavaScript example runs in Node.js. In an
existing Python async application, use `await download_ro_crate()` inside the
running event loop instead of `asyncio.run`.

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

=== "Python"

    ```python
    --8<-- "docs/samples/ro-crate/sample.py:setup"
    --8<-- "docs/samples/ro-crate/sample.py:download"
    ```

## Missing or inaccessible exports

A public project does not guarantee an available, anonymously readable export.
If the requested export is missing, the server returns HTTP `404`, exposed as
`NotFoundError`. HTTP `401` or `403` is exposed as `UnauthorizedError` and means
the request needs valid credentials or additional access. Check the project
and package visibility and use an appropriate token when needed.

The download raises an error in these cases; it does not return an empty
RO-Crate. Errors expose `StatusCode` and the server's response `Body` for
diagnostics.

!!! info "Offline verification"
    The [complete sample programs](https://github.com/nfdi4plants/DataHubClient/tree/main/docs/samples/ro-crate)
    replace HTTP access with a fixture, check the exact package endpoint and
    anonymous headers, and verify the saved UTF-8 content in a temporary
    working directory. CI needs no DataHub credentials or live export.

See [Packages](resources/packages.md) for the remaining Package Registry calls.
