namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

/// <summary>
/// An issue on a DataHub project. <c>Iid</c> is the per-project number shown in
/// the UI; <c>Id</c> is globally unique across the instance. See the
/// <see href="https://docs.gitlab.com/ee/api/issues.html">GitLab Issues API</see>.
/// </summary>
/// <param name="id">The issue's unique, instance-wide numeric identifier.</param>
/// <param name="iid">The per-project issue number shown in the UI.</param>
/// <param name="projectId">The identifier of the project the issue belongs to.</param>
/// <param name="title">The issue title.</param>
/// <param name="state">The issue state (e.g. <c>opened</c>, <c>closed</c>).</param>
/// <param name="author">The user who opened the issue.</param>
/// <param name="assignees">The users currently assigned to the issue.</param>
/// <param name="labels">The label names applied to the issue.</param>
/// <param name="webUrl">The URL of the issue's web page.</param>
/// <param name="createdAt">The creation timestamp as an ISO 8601 string.</param>
/// <param name="updatedAt">The last-update timestamp as an ISO 8601 string.</param>
/// <param name="description">The issue description, if one is set.</param>
[<AttachMembers>]
type Issue
    (
        id: int,
        iid: int,
        projectId: int,
        title: string,
        state: string,
        author: User,
        assignees: User array,
        labels: string array,
        webUrl: string,
        createdAt: string,
        updatedAt: string,
        ?description: string
    ) =
    let mutable _id = id
    let mutable _iid = iid
    let mutable _projectId = projectId
    let mutable _title = title
    let mutable _state = state
    let mutable _author = author
    let mutable _assignees = assignees
    let mutable _labels = labels
    let mutable _webUrl = webUrl
    let mutable _createdAt = createdAt
    let mutable _updatedAt = updatedAt
    let mutable _description : string option = description

    /// The issue's unique, instance-wide numeric identifier.
    member _.Id with get () = _id and set value = _id <- value
    /// The per-project issue number shown in the UI.
    member _.Iid with get () = _iid and set value = _iid <- value
    /// The identifier of the project the issue belongs to.
    member _.ProjectId with get () = _projectId and set value = _projectId <- value
    /// The issue title.
    member _.Title with get () = _title and set value = _title <- value
    /// The issue state (e.g. <c>opened</c>, <c>closed</c>).
    member _.State with get () = _state and set value = _state <- value
    /// The user who opened the issue.
    member _.Author with get () = _author and set value = _author <- value
    /// The users currently assigned to the issue.
    member _.Assignees with get () = _assignees and set value = _assignees <- value
    /// The label names applied to the issue.
    member _.Labels with get () = _labels and set value = _labels <- value
    /// The URL of the issue's web page.
    member _.WebUrl with get () = _webUrl and set value = _webUrl <- value
    /// The creation timestamp as an ISO 8601 string.
    member _.CreatedAt with get () = _createdAt and set value = _createdAt <- value
    /// The last-update timestamp as an ISO 8601 string.
    member _.UpdatedAt with get () = _updatedAt and set value = _updatedAt <- value
    /// The issue description, or <c>None</c> if none is set.
    member _.Description with get () = _description and set value = _description <- value

    /// Decodes an <see cref="T:DataHubClient.Issue"/> from its GitLab JSON representation.
    static member Decoder : Decoder<Issue> =
        Decode.object (fun get ->
            Issue(
                get.Required.Field "id" Decode.int,
                get.Required.Field "iid" Decode.int,
                get.Required.Field "project_id" Decode.int,
                get.Required.Field "title" Decode.string,
                get.Required.Field "state" Decode.string,
                get.Required.Field "author" User.Decoder,
                get.Required.Field "assignees" (Decode.array User.Decoder),
                get.Required.Field "labels" (Decode.array Decode.string),
                get.Required.Field "web_url" Decode.string,
                get.Required.Field "created_at" Decode.string,
                get.Required.Field "updated_at" Decode.string,
                ?description = get.Optional.Field "description" Decode.string))

    /// <summary>Encodes an <see cref="T:DataHubClient.Issue"/> to its GitLab JSON representation.</summary>
    /// <param name="issue">The issue to encode.</param>
    static member Encoder(issue: Issue) : IEncodable =
        Encode.object [
            "id", Encode.int issue.Id
            "iid", Encode.int issue.Iid
            "project_id", Encode.int issue.ProjectId
            "title", Encode.string issue.Title
            "state", Encode.string issue.State
            "author", User.Encoder issue.Author
            "assignees", Encode.array (Array.map User.Encoder issue.Assignees)
            "labels", Encode.array (Array.map Encode.string issue.Labels)
            "web_url", Encode.string issue.WebUrl
            "created_at", Encode.string issue.CreatedAt
            "updated_at", Encode.string issue.UpdatedAt
            "description", ThothExtensions.encodeOption Encode.string issue.Description ]
