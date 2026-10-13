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
an `Authentication` value:

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

`Authentication` selects the authentication header for each request. Use
`Anonymous()` for public resources without credentials, or a factory for one of
the [GitLab credential types](https://docs.gitlab.com/ee/api/rest/authentication.html).
Anonymous requests omit the authentication header and retain the client-version
header.

| Factory                        | Header                  | Use for                        |
|--------------------------------|-------------------------|--------------------------------|
| `Authentication.Anonymous`     | none                    | public resources               |
| `Authentication.PersonalAccessToken` | `PRIVATE-TOKEN`   | personal access tokens         |
| `Authentication.OAuthToken`    | `Authorization: Bearer` | OAuth 2.0 access tokens        |
| `Authentication.JobToken`      | `JOB-TOKEN`             | CI/CD job tokens               |

## Resource APIs

The client is a facade over one API object per GitLab resource area:
`client.Users`, `client.Projects`, `client.Repository`, `client.Files`,
`client.Issues`, `client.MergeRequests`, `client.Packages`, and
`client.Validation` (ARC validation results). Every call is async — `Async` in
F#, `Promise` in JavaScript, `awaitable` in Python.

`client.Users.GetCurrentAsync()` resolves the identity behind the configured
credential, and is the only call that populates `User.Email` — GitLab omits it
on users embedded in issues, merge requests, notes, and commits.

See [Resource examples](resources.md) for calls in all three languages and the
[API reference](reference.md) for the full surface.

## List every public project

`Projects.ListAllAsync` retrieves all matching projects in ascending ID order
using keyset pagination. It accepts optional `search`, `simple`, and `visibility`
filters. Request failures or nonadvancing project IDs raise an error rather than
returning an incomplete list. It requests pages of up to 100 projects and
continues until the server returns an empty page. `Projects.ListAsync` still
retrieves one page.

Anonymous clients can only see public projects. With a credential, omit
`visibility` to list every project visible to that credential, or use `"public"`,
`"internal"`, or `"private"` to restrict the results. `search` filters project
names and paths; `simple` requests the compact project representation.

=== "F# / .NET"

    ```fsharp
    --8<-- "docs/samples/public-projects/sample.fsx:setup"
    --8<-- "docs/samples/public-projects/sample.fsx:list"
    ```

=== "JavaScript"

    ```javascript
    --8<-- "docs/samples/public-projects/sample.mjs:setup"
    --8<-- "docs/samples/public-projects/sample.mjs:list"
    ```

=== "Python"

    ```python
    --8<-- "docs/samples/public-projects/sample.py:setup"
    --8<-- "docs/samples/public-projects/sample.py:list"
    ```

In JavaScript, use `undefined` for optional parameters you want to skip. Python
supports named arguments; F# uses named optional arguments as shown above.

!!! info "How this example is checked"
    CI runs these samples with canned HTTP responses for two project pages and
    an empty final page, so the checks need no DataHub credentials or network
    access. The
    [complete sample files](https://github.com/nfdi4plants/DataHubClient/tree/main/docs/samples/public-projects)
    include the offline setup and verify pagination and anonymous requests.
