namespace DataHubClient

open Fable.Core

/// <summary>
/// A DataHub project — the GitLab repository hosting a single ARC. See the
/// <see href="https://docs.gitlab.com/ee/api/projects.html">GitLab Projects API</see>.
/// </summary>
/// <param name="id">The project's unique, instance-wide numeric identifier.</param>
/// <param name="name">The project's display name.</param>
/// <param name="path">The project's URL slug within its namespace.</param>
/// <param name="pathWithNamespace">The full namespace-qualified path (e.g. <c>lab/my-arc</c>).</param>
/// <param name="visibility">The visibility level (<c>private</c>, <c>internal</c>, or <c>public</c>).</param>
/// <param name="webUrl">The URL of the project's web page.</param>
/// <param name="description">The project description, if one is set.</param>
/// <param name="defaultBranch">The name of the default branch, if the repository has one.</param>
[<AttachMembers>]
type Project
    (
        id: int,
        name: string,
        path: string,
        pathWithNamespace: string,
        visibility: string,
        webUrl: string,
        ?description: string,
        ?defaultBranch: string
    ) =
    let mutable _id = id
    let mutable _name = name
    let mutable _path = path
    let mutable _pathWithNamespace = pathWithNamespace
    let mutable _visibility = visibility
    let mutable _webUrl = webUrl
    let mutable _description : string option = description
    let mutable _defaultBranch : string option = defaultBranch

    /// The project's unique, instance-wide numeric identifier.
    member _.Id with get () = _id and set value = _id <- value
    /// The project's display name.
    member _.Name with get () = _name and set value = _name <- value
    /// The project's URL slug within its namespace.
    member _.Path with get () = _path and set value = _path <- value
    /// The full namespace-qualified path (e.g. <c>lab/my-arc</c>).
    member _.PathWithNamespace with get () = _pathWithNamespace and set value = _pathWithNamespace <- value
    /// The visibility level (<c>private</c>, <c>internal</c>, or <c>public</c>).
    member _.Visibility with get () = _visibility and set value = _visibility <- value
    /// The URL of the project's web page.
    member _.WebUrl with get () = _webUrl and set value = _webUrl <- value
    /// The project description, or <c>None</c> if none is set.
    member _.Description with get () = _description and set value = _description <- value
    /// The name of the default branch, or <c>None</c> if the repository has none.
    member _.DefaultBranch with get () = _defaultBranch and set value = _defaultBranch <- value
