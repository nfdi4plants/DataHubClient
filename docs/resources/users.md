# Users

Resolve the identity behind a credential or look up a user by numeric ID.

## Setup

These snippets share this client. Replace the URL and token with your DataHub details. The complete sample files use offline responses when run by the documentation checks.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/users/sample.fsx:setup"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/users/sample.mjs:setup"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/users/sample.py:setup"
    ```

## GetCurrentAsync

Get the user authenticated by the configured token. `Email` is optional and is populated only by this endpoint.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/users/sample.fsx:get-current"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/users/sample.mjs:get-current"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/users/sample.py:get-current"
    ```

## GetAsync

Get a user by their instance-wide numeric ID.

=== "F#"

    ```fsharp
    --8<-- "docs/samples/users/sample.fsx:get"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/users/sample.mjs:get"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/users/sample.py:get"
    ```
