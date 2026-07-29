namespace DataHubClient

open Fable.Core

/// <summary>
/// A DataHub user, as returned embedded in issues, merge requests, notes, and
/// commits. See the <see href="https://docs.gitlab.com/ee/api/users.html">GitLab Users API</see>.
/// </summary>
/// <param name="id">The user's unique, instance-wide numeric identifier.</param>
/// <param name="username">The user's login handle.</param>
/// <param name="name">The user's display name.</param>
/// <param name="state">The account state (e.g. <c>active</c>, <c>blocked</c>).</param>
/// <param name="webUrl">The URL of the user's profile page.</param>
/// <param name="avatarUrl">The URL of the user's avatar image, if one is set.</param>
/// <param name="email">
/// The user's primary email address. GitLab returns this only for the user the
/// credential authenticates as — i.e. from <c>UsersApi.GetCurrentAsync</c> — so it
/// is <c>None</c> on users embedded in issues, merge requests, notes, and commits.
/// </param>
[<AttachMembers>]
type User(id: int, username: string, name: string, state: string, webUrl: string, ?avatarUrl: string, ?email: string) =
    let mutable _id = id
    let mutable _username = username
    let mutable _name = name
    let mutable _state = state
    let mutable _webUrl = webUrl
    let mutable _avatarUrl : string option = avatarUrl
    let mutable _email : string option = email

    /// The user's unique, instance-wide numeric identifier.
    member _.Id with get () = _id and set value = _id <- value
    /// The user's login handle.
    member _.Username with get () = _username and set value = _username <- value
    /// The user's display name.
    member _.Name with get () = _name and set value = _name <- value
    /// The account state (e.g. <c>active</c>, <c>blocked</c>).
    member _.State with get () = _state and set value = _state <- value
    /// The URL of the user's profile page.
    member _.WebUrl with get () = _webUrl and set value = _webUrl <- value
    /// The URL of the user's avatar image, or <c>None</c> if none is set.
    member _.AvatarUrl with get () = _avatarUrl and set value = _avatarUrl <- value
    /// <summary>
    /// The user's primary email address, or <c>None</c>. Only populated for the user the
    /// credential authenticates as; embedded users never carry it.
    /// </summary>
    member _.Email with get () = _email and set value = _email <- value
