namespace DataHubClient

open Fable.Core

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
    let mutable _id = id
    let mutable _iid = iid
    let mutable _projectId = projectId
    let mutable _title = title
    let mutable _state = state
    let mutable _sourceBranch = sourceBranch
    let mutable _targetBranch = targetBranch
    let mutable _author = author
    let mutable _webUrl = webUrl
    let mutable _createdAt = createdAt
    let mutable _updatedAt = updatedAt
    let mutable _description : string option = description
    let mutable _mergeStatus : string option = mergeStatus

    /// The merge request's unique, instance-wide numeric identifier.
    member _.Id with get () = _id and set value = _id <- value
    /// The per-project merge request number shown in the UI.
    member _.Iid with get () = _iid and set value = _iid <- value
    /// The identifier of the project the merge request belongs to.
    member _.ProjectId with get () = _projectId and set value = _projectId <- value
    /// The merge request title.
    member _.Title with get () = _title and set value = _title <- value
    /// The state (e.g. <c>opened</c>, <c>merged</c>, <c>closed</c>).
    member _.State with get () = _state and set value = _state <- value
    /// The branch whose changes are proposed for merging.
    member _.SourceBranch with get () = _sourceBranch and set value = _sourceBranch <- value
    /// The branch the changes would be merged into.
    member _.TargetBranch with get () = _targetBranch and set value = _targetBranch <- value
    /// The user who opened the merge request.
    member _.Author with get () = _author and set value = _author <- value
    /// The URL of the merge request's web page.
    member _.WebUrl with get () = _webUrl and set value = _webUrl <- value
    /// The creation timestamp as an ISO 8601 string.
    member _.CreatedAt with get () = _createdAt and set value = _createdAt <- value
    /// The last-update timestamp as an ISO 8601 string.
    member _.UpdatedAt with get () = _updatedAt and set value = _updatedAt <- value
    /// The merge request description, or <c>None</c> if none is set.
    member _.Description with get () = _description and set value = _description <- value
    /// The mergeability status (e.g. <c>can_be_merged</c>), or <c>None</c> if not reported.
    member _.MergeStatus with get () = _mergeStatus and set value = _mergeStatus <- value
