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

## Download the metadata

Replace `42` with the ARC's numeric project ID. The example uses anonymous
access for a public export. For an export that requires credentials, replace
`Authentication.Anonymous()` with
`Authentication.PersonalAccessToken("your-datahub-pat")`.

This shared JavaScript / TypeScript example retrieves the JSON-LD text in a browser or
Node.js. It does not write a file and needs no filesystem import. Frontend npm
imports are resolved by your application's bundler, such as Vite.

=== "JavaScript / TypeScript"

    ```javascript
    --8<-- "docs/samples/ro-crate/sample.mjs:setup"
    --8<-- "docs/samples/ro-crate/sample.mjs:download"
    ```

## Download and save

The method returns JSON-LD text. The F#, Node.js, and Python examples save that
text as UTF-8 to `arc-ro-crate-metadata.json` in the current working directory,
replacing an existing file with that name. The browser example starts a file
download through the browser. In an existing Python async application, use
`await download_ro_crate()` inside the running event loop instead of
`asyncio.run`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/ro-crate/sample.fsx:setup"
    --8<-- "docs/samples/ro-crate/sample.fsx:download"
    ```

=== "JavaScript / TypeScript (Node.js)"

    ```javascript
    --8<-- "docs/samples/ro-crate/sample.mjs:setup"
    --8<-- "docs/samples/ro-crate/sample.mjs:download"
    --8<-- "docs/samples/ro-crate/sample.mjs:save"
    ```

=== "JavaScript / TypeScript (Browser)"

    Save this JavaScript helper as `browser.mjs` in your application. It uses a
    `Blob` and a download link to save the returned text:

    ```javascript
    --8<-- "docs/samples/ro-crate/browser.mjs"
    ```

    For a `.ts` helper with explicit parameter types, see
    [TypeScript browser downloads](typescript.md#browser-downloads).

    Call it from a button in your frontend module. These npm imports are
    resolved by your application's bundler, such as Vite:

    ```javascript
    import { DataHubClient } from "@nfdi4plants/datahub-client";
    import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
    import { downloadROCrate } from "./browser.mjs";

    const client = new DataHubClient("https://git.nfdi4plants.org", Authentication.Anonymous());
    const button = document.createElement("button");
    button.textContent = "Download RO-Crate";
    button.addEventListener("click", () => {
      downloadROCrate(client, 42).catch(console.error);
    });
    document.body.append(button);
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/ro-crate/sample.py:setup"
    --8<-- "docs/samples/ro-crate/sample.py:download"
    ```

### Browser imports and CORS

`node:fs/promises` is Node.js's filesystem module; it cannot be imported into a
browser application. It appears in the Node.js example to write the downloaded
text to disk. `DataHubClient` itself uses `fetch` and returns a string; use the
browser example above to save that string as a file.

When adapting the Node.js save example for a browser, remove its filesystem
import as well as the `writeFile` call. A retained static import can prevent the
module from loading even when the imported function is never called. The
metadata-only snippet above contains just the client imports and download.

When the DataHub and your application have different origins, the DataHub must
allow your application's origin through
[CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CORS). Requests
include `X-DataHubClient-Version`, so even anonymous downloads need a successful
preflight that allows this header. Authenticated downloads also need the
authentication header allowed, such as `PRIVATE-TOKEN`. A rejected CORS request
is a separate problem from an unsupported `node:fs/promises` import.

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
    The F#, JavaScript, TypeScript, and Python
    [sample programs](https://github.com/nfdi4plants/DataHubClient/tree/main/docs/samples/ro-crate)
    replace HTTP access with a fixture, check the exact package endpoint and
    anonymous headers, and verify the saved UTF-8 content in a temporary
    working directory. The browser checks exercise the helper above with a
    local HTTP server and verify the downloaded file. These checks need no
    DataHub credentials or live export.

See [Packages](resources/packages.md) for the remaining Package Registry calls.
