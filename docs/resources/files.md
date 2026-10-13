# Files

Read and change repository files by numeric project ID and repository-relative path. Pass paths as ordinary strings; the client URL-encodes them.

## Setup

These snippets share this client. Replace the URL and token with your DataHub details. The complete sample files use offline responses when run by the documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/files/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/files/sample.mjs:setup"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/files/sample.ts:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/files/sample.py:setup"
    ```

## GetAsync

Read a file from a branch, tag, or commit SHA. `Content` is usually base64-encoded; check `Encoding` before decoding. These text-file examples use UTF-8; the JavaScript and TypeScript examples use Node.js `Buffer`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/files/sample.fsx:get"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/files/sample.mjs:get"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/files/sample.ts:get"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/files/sample.py:get"
    ```

## CreateAsync

Create a file and a commit on the target branch. Omit `encoding` for plain text; pass `"base64"` when the supplied content is encoded. The method reads the file back after the write and returns a `RepoFile`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/files/sample.fsx:create"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/files/sample.mjs:create"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/files/sample.ts:create"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/files/sample.py:create"
    ```

## UpdateAsync

Replace a file and commit the change. This example supplies base64 content. As with creation, a second request fetches the resulting `RepoFile`; a read-back failure is reported even if the write succeeded.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/files/sample.fsx:update"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/files/sample.mjs:update"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/files/sample.ts:update"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/files/sample.py:update"
    ```

## DeleteAsync

Delete a file and create a commit. Completion confirms success; the method has no result value.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/files/sample.fsx:delete"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/files/sample.mjs:delete"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/files/sample.ts:delete"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/files/sample.py:delete"
    ```
