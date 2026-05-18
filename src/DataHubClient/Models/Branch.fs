namespace DataHubClient

open Fable.Core

/// <summary>
/// A branch in a DataHub repository, together with the commit it points at.
/// See the <see href="https://docs.gitlab.com/ee/api/branches.html">GitLab Branches API</see>.
/// </summary>
/// <param name="name">The branch name.</param>
/// <param name="isDefault">Whether this is the repository's default branch.</param>
/// <param name="isProtected">Whether the branch is protected against force-pushes and deletion.</param>
/// <param name="merged">Whether the branch has been merged into the default branch.</param>
/// <param name="commit">The commit the branch currently points at.</param>
[<AttachMembers>]
type Branch(name: string, isDefault: bool, isProtected: bool, merged: bool, commit: Commit) =
    let mutable _name = name
    let mutable _isDefault = isDefault
    let mutable _isProtected = isProtected
    let mutable _merged = merged
    let mutable _commit = commit

    /// The branch name.
    member _.Name with get () = _name and set value = _name <- value
    /// Whether this is the repository's default branch.
    member _.Default with get () = _isDefault and set value = _isDefault <- value
    /// Whether the branch is protected against force-pushes and deletion.
    member _.Protected with get () = _isProtected and set value = _isProtected <- value
    /// Whether the branch has been merged into the default branch.
    member _.Merged with get () = _merged and set value = _merged <- value
    /// The commit the branch currently points at.
    member _.Commit with get () = _commit and set value = _commit <- value
