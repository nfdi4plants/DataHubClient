namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

/// <summary>
/// A merge request on a DataHub project. <c>Iid</c> is the per-project number
/// shown in the UI; <c>Id</c> is globally unique across the instance. See the
/// <see href="https://docs.gitlab.com/ee/api/merge_requests.html">GitLab Merge Requests API</see>.
/// </summary>
/// <param name="id">The merge request's unique, instance-wide numeric identifier.</param>
/// <param name="iid">The per-project merge request number shown in the UI.</param>
/// <param name="projectId">The identifier of the project the merge request belongs to.</param>
/// <param name="title">The merge request title.</param>
/// <param name="state">The state (e.g. <c>opened</c>, <c>merged</c>, <c>closed</c>).</param>
/// <param name="sourceBranch">The branch whose changes are proposed for merging.</param>
/// <param name="targetBranch">The branch the changes would be merged into.</param>
/// <param name="author">The user who opened the merge request.</param>
/// <param name="webUrl">The URL of the merge request's web page.</param>
/// <param name="createdAt">The creation timestamp as an ISO 8601 string.</param>
/// <param name="updatedAt">The last-update timestamp as an ISO 8601 string.</param>
/// <param name="description">The merge request description, if one is set.</param>
/// <param name="mergeStatus">The mergeability status (e.g. <c>can_be_merged</c>), if reported.</param>
[<AttachMembers>]
type MergeRequest
    (
        id: int,
        iid: int,
        projectId: int,
        title: string,
        state: string,
        sourceBranch: string,
        targetBranch: string,
        author: User,
        webUrl: string,
        createdAt: string,
        updatedAt: string,
        ?description: string,
        ?mergeStatus: string
    ) =
    /// The merge request's unique, instance-wide numeric identifier.
    member val Id = id with get, set
    /// The per-project merge request number shown in the UI.
    member val Iid = iid with get, set
    /// The identifier of the project the merge request belongs to.
    member val ProjectId = projectId with get, set
    /// The merge request title.
    member val Title = title with get, set
    /// The state (e.g. <c>opened</c>, <c>merged</c>, <c>closed</c>).
    member val State = state with get, set
    /// The branch whose changes are proposed for merging.
    member val SourceBranch = sourceBranch with get, set
    /// The branch the changes would be merged into.
    member val TargetBranch = targetBranch with get, set
    /// The user who opened the merge request.
    member val Author = author with get, set
    /// The URL of the merge request's web page.
    member val WebUrl = webUrl with get, set
    /// The creation timestamp as an ISO 8601 string.
    member val CreatedAt = createdAt with get, set
    /// The last-update timestamp as an ISO 8601 string.
    member val UpdatedAt = updatedAt with get, set
    /// The merge request description, or <c>None</c> if none is set.
    member val Description : string option = description with get, set
    /// The mergeability status (e.g. <c>can_be_merged</c>), or <c>None</c> if not reported.
    member val MergeStatus : string option = mergeStatus with get, set

    /// Decodes a <see cref="T:DataHubClient.MergeRequest"/> from its GitLab JSON representation.
    static member Decoder : Decoder<MergeRequest> =
        Decode.object (fun get ->
            MergeRequest(
                get.Required.Field "id" Decode.int,
                get.Required.Field "iid" Decode.int,
                get.Required.Field "project_id" Decode.int,
                get.Required.Field "title" Decode.string,
                get.Required.Field "state" Decode.string,
                get.Required.Field "source_branch" Decode.string,
                get.Required.Field "target_branch" Decode.string,
                get.Required.Field "author" User.Decoder,
                get.Required.Field "web_url" Decode.string,
                get.Required.Field "created_at" Decode.string,
                get.Required.Field "updated_at" Decode.string,
                ?description = get.Optional.Field "description" Decode.string,
                ?mergeStatus = get.Optional.Field "merge_status" Decode.string))

    /// <summary>Encodes a <see cref="T:DataHubClient.MergeRequest"/> to its GitLab JSON representation.</summary>
    /// <param name="mr">The merge request to encode.</param>
    static member Encoder(mr: MergeRequest) : IEncodable =
        Encode.object [
            "id", Encode.int mr.Id
            "iid", Encode.int mr.Iid
            "project_id", Encode.int mr.ProjectId
            "title", Encode.string mr.Title
            "state", Encode.string mr.State
            "source_branch", Encode.string mr.SourceBranch
            "target_branch", Encode.string mr.TargetBranch
            "author", User.Encoder mr.Author
            "web_url", Encode.string mr.WebUrl
            "created_at", Encode.string mr.CreatedAt
            "updated_at", Encode.string mr.UpdatedAt
            "description", ThothExtensions.encodeOption Encode.string mr.Description
            "merge_status", ThothExtensions.encodeOption Encode.string mr.MergeStatus ]
