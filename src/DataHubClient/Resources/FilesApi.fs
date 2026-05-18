namespace DataHubClient

open Fable.Core
open DataHubClient.Json
open Thoth.Json.Core

/// <summary>
/// Client for GitLab repository file operations.
/// See the <see href="https://docs.gitlab.com/ee/api/repository_files.html">GitLab Repository Files API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type FilesApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    let fileSegments projectId filePath =
        [ "projects"; string projectId; "repository"; "files"; filePath ]

    let writeBody branch content commitMessage encoding =
        ThothExtensions.objectSkipNull [
            "branch", Some(Encode.string branch)
            "content", Some(Encode.string content)
            "commit_message", Some(Encode.string commitMessage)
            "encoding", encoding |> Option.map Encode.string
        ]

    /// <summary>Gets a repository file from a branch, tag, or commit SHA.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="filePath">The repository-relative file path.</param>
    /// <param name="refName">The branch, tag, or commit SHA to read from.</param>
    member _.Get(projectId: int, filePath: string, refName: string) : Async<RepoFile> =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    (fileSegments projectId filePath)
                    [ "ref", Some refName ]

            let! response = http.SendAsync req
            return ResourceHelpers.decode RepoFile.decoder response
        }

    /// <summary>Creates a repository file.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="filePath">The repository-relative file path.</param>
    /// <param name="branch">The target branch.</param>
    /// <param name="content">The file content.</param>
    /// <param name="commitMessage">The commit message GitLab should create.</param>
    /// <param name="encoding">Optional content encoding, such as <c>base64</c>.</param>
    member _.Create(projectId: int, filePath: string, branch: string, content: string, commitMessage: string, ?encoding: string) : Async<RepoFile> =
        async {
            let req =
                ResourceHelpers.jsonRequest
                    baseUrl
                    auth
                    "POST"
                    (fileSegments projectId filePath)
                    []
                    (writeBody branch content commitMessage encoding)

            let! response = http.SendAsync req
            return ResourceHelpers.decode RepoFile.decoder response
        }

    /// <summary>Updates a repository file.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="filePath">The repository-relative file path.</param>
    /// <param name="branch">The target branch.</param>
    /// <param name="content">The replacement file content.</param>
    /// <param name="commitMessage">The commit message GitLab should create.</param>
    /// <param name="encoding">Optional content encoding, such as <c>base64</c>.</param>
    member _.Update(projectId: int, filePath: string, branch: string, content: string, commitMessage: string, ?encoding: string) : Async<RepoFile> =
        async {
            let req =
                ResourceHelpers.jsonRequest
                    baseUrl
                    auth
                    "PUT"
                    (fileSegments projectId filePath)
                    []
                    (writeBody branch content commitMessage encoding)

            let! response = http.SendAsync req
            return ResourceHelpers.decode RepoFile.decoder response
        }

    /// <summary>Deletes a repository file.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="filePath">The repository-relative file path.</param>
    /// <param name="branch">The target branch.</param>
    /// <param name="commitMessage">The commit message GitLab should create.</param>
    member _.Delete(projectId: int, filePath: string, branch: string, commitMessage: string) : Async<unit> =
        async {
            let body =
                Encode.object [
                    "branch", Encode.string branch
                    "commit_message", Encode.string commitMessage
                ]

            let req =
                ResourceHelpers.jsonRequest
                    baseUrl
                    auth
                    "DELETE"
                    (fileSegments projectId filePath)
                    []
                    body

            let! response = http.SendAsync req
            ResourceHelpers.ensureSuccess response
        }
