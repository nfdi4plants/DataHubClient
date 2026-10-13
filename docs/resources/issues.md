# Issues

Use `client.Issues` to manage issues and their notes. The `iid` is the issue number within a project (for example, 3), not its global `Id`.

List methods return the server's current page; they do not fetch every page automatically. Create, update, close, and note creation write to the selected project.

## Setup

Replace the example token and project ID with your own. Python examples call each API from an async function, then run that function with `asyncio.run`. In an existing async application, call the API with `await` inside its running event loop. The repository runs these exact snippets against offline fixtures during documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:setup"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:setup"
    ```

## ListAsync

List issues, optionally filtered by state. Omit `state` to use the server default.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:list"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:list"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:list"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:list"
    ```

## GetAsync

Read one issue by its project-local number.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:get"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:get"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:get"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:get"
    ```

## CreateAsync

Create an issue with a title and optional Markdown description. The returned `Iid` identifies it for subsequent calls.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:create"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:create"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:create"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:create"
    ```

## UpdateAsync

Change the title or description. Omitted fields remain unchanged. Use `stateEvent = "reopen"` (Python: `state_event="reopen"`) to reopen a closed issue; JavaScript and TypeScript take this as the fifth argument.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:update"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:update"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:update"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:update"
    ```

## CloseAsync

Close an issue and return its updated state.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:close"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:close"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:close"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:close"
    ```

## NotesAsync

Read the notes on an issue. `System` distinguishes automatically generated notes from user comments.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:notes"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:notes"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:notes"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:notes"
    ```

## CreateNoteAsync

Add a Markdown comment and return the created note.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/issues/sample.fsx:create-note"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/issues/sample.mjs:create-note"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/issues/sample.ts:create-note"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/issues/sample.py:create-note"
    ```
