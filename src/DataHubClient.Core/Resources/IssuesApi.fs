namespace DataHubClient

open Fable.Core
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
    member _.List(projectId: int, ?state: string) : Async<Issue array> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (issueSegments projectId) [ "state", state ]
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Issue.Decoder response
        }

    /// <summary>Gets a single issue by project-local issue number.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    member _.Get(projectId: int, iid: int) : Async<Issue> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (issueSegmentsWithIid projectId iid) []
            let! response = http.SendAsync req
            return ResourceHelpers.decode Issue.Decoder response
        }

    /// <summary>Creates a new issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="title">The issue title.</param>
    /// <param name="description">Optional issue description.</param>
    member _.Create(projectId: int, title: string, ?description: string) : Async<Issue> =
        async {
            let body =
                ThothExtensions.objectSkipNull [
                    "title", Some(Encode.string title)
                    "description", description |> Option.map Encode.string
                ]

            let req = ResourceHelpers.jsonRequest baseUrl auth "POST" (issueSegments projectId) [] body
            let! response = http.SendAsync req
            return ResourceHelpers.decode Issue.Decoder response
        }

    /// <summary>Updates an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    /// <param name="title">Optional replacement title.</param>
    /// <param name="description">Optional replacement description.</param>
    /// <param name="stateEvent">Optional state transition, such as <c>close</c> or <c>reopen</c>.</param>
    member _.Update(projectId: int, iid: int, ?title: string, ?description: string, ?stateEvent: string) : Async<Issue> =
        async {
            let body =
                ThothExtensions.objectSkipNull [
                    "title", title |> Option.map Encode.string
                    "description", description |> Option.map Encode.string
                    "state_event", stateEvent |> Option.map Encode.string
                ]

            let req = ResourceHelpers.jsonRequest baseUrl auth "PUT" (issueSegmentsWithIid projectId iid) [] body
            let! response = http.SendAsync req
            return ResourceHelpers.decode Issue.Decoder response
        }

    /// <summary>Closes an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    member this.Close(projectId: int, iid: int) : Async<Issue> =
        this.Update(projectId, iid, stateEvent = "close")

    /// <summary>Lists notes on an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    member _.Notes(projectId: int, iid: int) : Async<Note array> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (noteSegments projectId iid) []
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Note.Decoder response
        }

    /// <summary>Creates a note on an issue.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="iid">The project-local issue number.</param>
    /// <param name="body">The note text.</param>
    member _.CreateNote(projectId: int, iid: int, body: string) : Async<Note> =
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
            return ResourceHelpers.decode Note.Decoder response
        }
