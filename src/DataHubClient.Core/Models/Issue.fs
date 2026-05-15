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
    /// The issue's unique, instance-wide numeric identifier.
    member val Id = id with get, set
    /// The per-project issue number shown in the UI.
    member val Iid = iid with get, set
    /// The identifier of the project the issue belongs to.
    member val ProjectId = projectId with get, set
    /// The issue title.
    member val Title = title with get, set
    /// The issue state (e.g. <c>opened</c>, <c>closed</c>).
    member val State = state with get, set
    /// The user who opened the issue.
    member val Author = author with get, set
    /// The users currently assigned to the issue.
    member val Assignees = assignees with get, set
    /// The label names applied to the issue.
    member val Labels = labels with get, set
    /// The URL of the issue's web page.
    member val WebUrl = webUrl with get, set
    /// The creation timestamp as an ISO 8601 string.
    member val CreatedAt = createdAt with get, set
    /// The last-update timestamp as an ISO 8601 string.
    member val UpdatedAt = updatedAt with get, set
    /// The issue description, or <c>None</c> if none is set.
    member val Description : string option = description with get, set

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
