# Repository

Read branches and commits using a numeric project ID. List methods return one server page.

## Setup

These snippets share this client. Replace the URL and token with your DataHub details. The complete sample files use offline responses when run by the documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/repository/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/repository/sample.mjs:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/repository/sample.py:setup"
    ```

## ListBranchesAsync

List branches, optionally filtering their names with `search`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/repository/sample.fsx:list-branches"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/repository/sample.mjs:list-branches"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/repository/sample.py:list-branches"
    ```

## GetBranchAsync

Get a branch by its name.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/repository/sample.fsx:get-branch"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/repository/sample.mjs:get-branch"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/repository/sample.py:get-branch"
    ```

## ListCommitsAsync

List commits, optionally choosing a branch, tag, or SHA with `refName` (`ref_name` in Python), and restricting the history to `path`.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/repository/sample.fsx:list-commits"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/repository/sample.mjs:list-commits"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/repository/sample.py:list-commits"
    ```

## GetCommitAsync

Read a single commit by SHA.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/repository/sample.fsx:get-commit"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/repository/sample.mjs:get-commit"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/repository/sample.py:get-commit"
    ```
