# Getting started

## Install

=== "F# / .NET"

    ```sh
    dotnet add package DataHubClient
    ```

=== "JavaScript"

    ```sh
    npm install @nfdi4plants/datahub-client
    ```

=== "Python"

    ```sh
    pip install datahub-client
    ```

## Create a client

A `DataHubClient` needs the DataHub root URL (without the `/api/v4` suffix) and
an `Authentication` credential:

=== "F# / .NET"

    ```fsharp
    --8<-- "docs/samples/getting-started/sample.fsx"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/getting-started/sample.mjs"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/getting-started/sample.py"
    ```

## Authentication

`Authentication` reduces a GitLab credential to the single HTTP header it
contributes to every request. Three factory methods cover the
[GitLab credential types](https://docs.gitlab.com/ee/api/rest/authentication.html):

| Factory                        | Header                  | Use for                        |
|--------------------------------|-------------------------|--------------------------------|
| `Authentication.PersonalAccessToken` | `PRIVATE-TOKEN`   | personal access tokens         |
| `Authentication.OAuthToken`    | `Authorization: Bearer` | OAuth 2.0 access tokens        |
| `Authentication.JobToken`      | `JOB-TOKEN`             | CI/CD job tokens               |

## Resource APIs

The client is a facade over one API object per GitLab resource area:
`client.Projects`, `client.Repository`, `client.Files`, `client.Issues`,
`client.MergeRequests`, `client.Packages`, and `client.Validation` (ARC
validation results). Every call is async — `Async` in F#, `Promise` in
JavaScript, `awaitable` in Python.

See the [API reference](reference.md) for the full surface.
