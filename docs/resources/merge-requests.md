# Merge requests

Use `client.MergeRequests` to manage merge requests and their notes. The `iid` is the merge request number within a project (for example, 8), not its global `Id`.

List methods return the server's current page; they do not fetch every page automatically. Create, update, close, and note creation write to the selected project.

## Setup

Replace the example token and project ID with your own. Python examples call each API from an async function, then run that function with `asyncio.run`. In an existing async application, call the API with `await` inside its running event loop. The repository runs these exact snippets against offline fixtures during documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:setup"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:setup"
    ```

## ListAsync

List merge requests, optionally filtered by state. Omit `state` to use the server default.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:list"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:list"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:list"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:list"
    ```

## GetAsync

Read one merge request by its project-local number.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:get"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:get"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:get"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:get"
    ```

## CreateAsync

Propose changes from a source branch into a target branch. Both branches must already exist. The returned `Iid` identifies the merge request for subsequent calls.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:create"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:create"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:create"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:create"
    ```

## UpdateAsync

Change the title or description. Omitted fields remain unchanged. Use `stateEvent = "reopen"` (Python: `state_event="reopen"`) to reopen a closed merge request; JavaScript and TypeScript take this as the fifth argument.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:update"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:update"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:update"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:update"
    ```

## CloseAsync

Close a merge request and return its updated state. Closing a merge request does not merge its source branch.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:close"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:close"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:close"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:close"
    ```

## NotesAsync

Read the notes on a merge request. `System` distinguishes automatically generated notes from user comments.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:notes"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:notes"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:notes"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:notes"
    ```

## CreateNoteAsync

Add a Markdown comment and return the created note.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/merge-requests/sample.fsx:create-note"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/merge-requests/sample.mjs:create-note"
    ```

=== "TypeScript"

    ```typescript
    --8<-- "docs/samples/merge-requests/sample.ts:create-note"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/merge-requests/sample.py:create-note"
    ```
