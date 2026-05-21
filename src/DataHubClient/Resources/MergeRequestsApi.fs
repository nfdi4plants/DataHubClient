namespace DataHubClient

open Fable.Core
open DataHubClient.Json
open Thoth.Json.Core

/// <summary>
/// Client for GitLab merge request operations.
/// See the <see href="https://docs.gitlab.com/ee/api/merge_requests.html">GitLab Merge Requests API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type MergeRequestsApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    let mergeRequestSegments projectId =
        [ "projects"; string projectId; "merge_requests" ]

    let mergeRequestSegmentsWithIid projectId iid =
        mergeRequestSegments projectId @ [ string iid ]

    let noteSegments projectId iid =
        mergeRequestSegmentsWithIid projectId iid @ [ "notes" ]

    /// <summary>Lists merge requests for a project.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="state">Optional state filter, such as <c>opened</c>, <c>merged</c>, or <c>closed</c>.</param>
    member _.ListAsync(projectId: int, ?state: string) : Async<MergeRequest array> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (mergeRequestSegments projectId) [ "state", state ]
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray MergeRequest.decoder response
        }

    /// <summary>Gets a single merge request by project-local merge request number.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local merge request number.</param>
    member _.GetAsync(projectId: int, iid: int) : Async<MergeRequest> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (mergeRequestSegmentsWithIid projectId iid) []
            let! response = http.SendAsync req
            return ResourceHelpers.decode MergeRequest.decoder response
        }

    /// <summary>Creates a new merge request.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="sourceBranch">The branch containing proposed changes.</param>
    /// <param name="targetBranch">The branch that should receive the changes.</param>
    /// <param name="title">The merge request title.</param>
    /// <param name="description">Optional merge request description.</param>
    member _.CreateAsync(projectId: int, sourceBranch: string, targetBranch: string, title: string, ?description: string) : Async<MergeRequest> =
        async {
            let body =
                ThothExtensions.objectSkipNull [
                    "source_branch", Some(Encode.string sourceBranch)
                    "target_branch", Some(Encode.string targetBranch)
                    "title", Some(Encode.string title)
                    "description", description |> Option.map Encode.string
                ]

            let req = ResourceHelpers.jsonRequest baseUrl auth "POST" (mergeRequestSegments projectId) [] body
            let! response = http.SendAsync req
            return ResourceHelpers.decode MergeRequest.decoder response
        }

    /// <summary>Updates a merge request.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local merge request number.</param>
    /// <param name="title">Optional replacement title.</param>
    /// <param name="description">Optional replacement description.</param>
    /// <param name="stateEvent">Optional state transition, such as <c>close</c> or <c>reopen</c>.</param>
    member _.UpdateAsync(projectId: int, iid: int, ?title: string, ?description: string, ?stateEvent: string) : Async<MergeRequest> =
        async {
            let body =
                ThothExtensions.objectSkipNull [
                    "title", title |> Option.map Encode.string
                    "description", description |> Option.map Encode.string
                    "state_event", stateEvent |> Option.map Encode.string
                ]

            let req = ResourceHelpers.jsonRequest baseUrl auth "PUT" (mergeRequestSegmentsWithIid projectId iid) [] body
            let! response = http.SendAsync req
            return ResourceHelpers.decode MergeRequest.decoder response
        }

    /// <summary>Closes a merge request.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local merge request number.</param>
    member this.CloseAsync(projectId: int, iid: int) : Async<MergeRequest> =
        this.UpdateAsync(projectId, iid, stateEvent = "close")

    /// <summary>Lists notes on a merge request.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local merge request number.</param>
    member _.NotesAsync(projectId: int, iid: int) : Async<Note array> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (noteSegments projectId iid) []
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Note.decoder response
        }

    /// <summary>Creates a note on a merge request.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local merge request number.</param>
    /// <param name="body">The note text.</param>
    member _.CreateNoteAsync(projectId: int, iid: int, body: string) : Async<Note> =
        async {
            let req =
                ResourceHelpers.jsonRequest
                    baseUrl
                    auth
                    "POST"
                    (noteSegments projectId iid)
                    []
                    (Encode.object [ "body", Encode.string body ])

            let! response = http.SendAsync req
            return ResourceHelpers.decode Note.decoder response
        }
