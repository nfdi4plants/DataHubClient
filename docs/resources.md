# Resource API

The client is a facade over one API object per GitLab resource area. Each page
below has a short F#, JavaScript, TypeScript, and Python example for every method
in that area. Start with [Getting started](getting-started.md) to install a
package and choose an authentication method.

For a complete download-and-save example, see [Download an RO-Crate](ro-crate.md).

| Client property | Examples |
| --- | --- |
| [`client.Users`](resources/users.md) | Current identity and user lookup |
| [`client.Projects`](resources/projects.md) | List one page or every project, search, and project lookup |
| [`client.Repository`](resources/repository.md) | Branch and commit lists and lookups |
| [`client.Files`](resources/files.md) | Read, create, update, and delete repository files |
| [`client.Issues`](resources/issues.md) | List, read, create, update, close, and comment on issues |
| [`client.MergeRequests`](resources/merge-requests.md) | List, read, create, update, close, and comment on merge requests |
| [`client.Packages`](resources/packages.md) | Package metadata, generic file upload/download, and ARC RO-Crate export |
| [`client.Validation`](resources/validation.md) | Validated branches, packages, history, summaries, reports, and badges |

Every call is async: F# examples run an `Async` computation, JavaScript and
TypeScript use `await`, and Python uses `await` inside an async function or
`asyncio.run` in a script. JavaScript and TypeScript optional arguments are
positional; pass `undefined` to skip an argument. F# and Python support named
arguments.

`projectId` is a numeric GitLab project ID. Issue and merge request methods use
the project-local `iid`, rather than the instance-wide ID. Replace example
identifiers, branch names, and paths with values from your DataHub. Methods that
create, update, delete, close, comment, or upload change the selected project
and require a credential with appropriate permissions.

List methods return one server page unless explicitly documented otherwise.
`Projects.ListAllAsync` traverses all project pages. Validation branch and package
discovery reads up to 100 repository-tree entries per call; validation history
also returns one server page.

The [complete sample programs](https://github.com/nfdi4plants/DataHubClient/tree/main/docs/samples)
include offline HTTP fixtures and assertions. CI runs them against all three
packed distributions; the code shown on these pages is included directly from
those programs. See the [API reference](reference.md) for full signatures and
model properties.
