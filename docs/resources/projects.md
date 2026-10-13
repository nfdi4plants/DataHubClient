# Projects

Find projects visible to the configured credential. Use `Authentication.Anonymous()` in the setup below to browse public projects without a token.

## Setup

These snippets share this client. Replace the URL and token with your DataHub details. The complete sample files use offline responses when run by the documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/projects/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/projects/sample.mjs:setup"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/projects/sample.ts:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/projects/sample.py:setup"
    ```

## ListAsync

Get one page of projects. Both `search` and `simple` are optional; omit them to use the server defaults.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/projects/sample.fsx:list"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/projects/sample.mjs:list"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/projects/sample.ts:list"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/projects/sample.py:list"
    ```

## ListAllAsync

Get every matching project in ascending ID order. Optional `visibility` accepts `public`, `internal`, or `private`. Pages are requested until an empty page; invalid ordering or a failed page raises an error without returning partial results.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/projects/sample.fsx:list-all"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/projects/sample.mjs:list-all"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/projects/sample.ts:list-all"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/projects/sample.py:list-all"
    ```

## GetAsync

Get one project by numeric ID.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/projects/sample.fsx:get"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/projects/sample.mjs:get"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/projects/sample.ts:get"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/projects/sample.py:get"
    ```

## SearchAsync

Search by name with the one-page convenience method. Use `ListAllAsync(search = ...)` in F#, `ListAllAsync("plant arc")` in JavaScript or TypeScript, or `ListAllAsync(search=...)` in Python when every page is needed.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/projects/sample.fsx:search"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/projects/sample.mjs:search"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/projects/sample.ts:search"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/projects/sample.py:search"
    ```
