namespace DataHubClient

open Fable.Core
open DataHubClient.Json
open Thoth.Json.Core

/// <summary>
/// Client for GitLab issue operations.
/// See the <see href="https://docs.gitlab.com/ee/api/issues.html">GitLab Issues API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type IssuesApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    let issueSegments projectId =
        [ "projects"; string projectId; "issues" ]

    let issueSegmentsWithIid projectId iid =
        issueSegments projectId @ [ string iid ]

    let noteSegments projectId iid =
        issueSegmentsWithIid projectId iid @ [ "notes" ]

    /// <summary>Lists issues for a project.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="state">Optional state filter, such as <c>opened</c> or <c>closed</c>.</param>
    member _.ListAsync(projectId: int, ?state: string) =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (issueSegments projectId) [ "state", state ]
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Issue.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Gets a single issue by project-local issue number.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    member _.GetAsync(projectId: int, iid: int) =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (issueSegmentsWithIid projectId iid) []
            let! response = http.SendAsync req
            return ResourceHelpers.decode Issue.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Creates a new issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="title">The issue title.</param>
    /// <param name="description">Optional issue description.</param>
    member _.CreateAsync(projectId: int, title: string, ?description: string) =
        async {
            let body =
                ThothExtensions.objectSkipNull [
                    "title", Some(Encode.string title)
                    "description", description |> Option.map Encode.string
                ]

            let req = ResourceHelpers.jsonRequest baseUrl auth "POST" (issueSegments projectId) [] body
            let! response = http.SendAsync req
            return ResourceHelpers.decode Issue.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Updates an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    /// <param name="title">Optional replacement title.</param>
    /// <param name="description">Optional replacement description.</param>
    /// <param name="stateEvent">Optional state transition, such as <c>close</c> or <c>reopen</c>.</param>
    member _.UpdateAsync(projectId: int, iid: int, ?title: string, ?description: string, ?stateEvent: string) =
        async {
            let body =
                ThothExtensions.objectSkipNull [
                    "title", title |> Option.map Encode.string
                    "description", description |> Option.map Encode.string
                    "state_event", stateEvent |> Option.map Encode.string
                ]

            let req = ResourceHelpers.jsonRequest baseUrl auth "PUT" (issueSegmentsWithIid projectId iid) [] body
            let! response = http.SendAsync req
            return ResourceHelpers.decode Issue.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Closes an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    member this.CloseAsync(projectId: int, iid: int) =
        this.UpdateAsync(projectId, iid, stateEvent = "close")

    /// <summary>Lists notes on an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    member _.NotesAsync(projectId: int, iid: int) =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (noteSegments projectId iid) []
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Note.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Creates a note on an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    /// <param name="body">The note text.</param>
    member _.CreateNoteAsync(projectId: int, iid: int, body: string) =
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
        |> ResourceHelpers.toPublic
