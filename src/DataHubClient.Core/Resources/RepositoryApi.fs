namespace DataHubClient

open Fable.Core
open DataHubClient.Json

/// <summary>
/// Client for repository branch and commit operations.
/// See the <see href="https://docs.gitlab.com/ee/api/repositories.html">GitLab Repositories API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type RepositoryApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    /// <summary>Lists branches for a project.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="search">Optional branch-name search term.</param>
    member _.ListBranches(projectId: int, ?search: string) : Async<Branch array> =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    [ "projects"; string projectId; "repository"; "branches" ]
                    [ "search", search ]

            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Branch.decoder response
        }

    /// <summary>Gets a single branch by name.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="branch">The branch name.</param>
    member _.GetBranch(projectId: int, branch: string) : Async<Branch> =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    [ "projects"; string projectId; "repository"; "branches"; branch ]
                    []

            let! response = http.SendAsync req
            return ResourceHelpers.decode Branch.decoder response
        }

    /// <summary>Lists commits for a project.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="refName">Optional branch, tag, or commit SHA to list from.</param>
    /// <param name="path">Optional repository path used to filter commits.</param>
    member _.ListCommits(projectId: int, ?refName: string, ?path: string) : Async<Commit array> =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    [ "projects"; string projectId; "repository"; "commits" ]
                    [ "path", path; "ref_name", refName ]

            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Commit.decoder response
        }

    /// <summary>Gets a single commit by SHA.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="sha">The commit SHA.</param>
    member _.GetCommit(projectId: int, sha: string) : Async<Commit> =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    [ "projects"; string projectId; "repository"; "commits"; sha ]
                    []

            let! response = http.SendAsync req
            return ResourceHelpers.decode Commit.decoder response
        }
