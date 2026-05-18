namespace DataHubClient

open Fable.Core

/// <summary>
/// A single commit in a DataHub repository. See the
/// <see href="https://docs.gitlab.com/ee/api/commits.html">GitLab Commits API</see>.
/// </summary>
/// <param name="id">The full 40-character commit SHA.</param>
/// <param name="shortId">The abbreviated commit SHA.</param>
/// <param name="title">The first line of the commit message.</param>
/// <param name="message">The full commit message.</param>
/// <param name="authorName">The display name of the commit author.</param>
/// <param name="authorEmail">The email address of the commit author.</param>
/// <param name="createdAt">The commit timestamp as an ISO 8601 string.</param>
/// <param name="webUrl">The URL of the commit's web page, if available.</param>
[<AttachMembers>]
type Commit
    (
        id: string,
        shortId: string,
        title: string,
        message: string,
        authorName: string,
        authorEmail: string,
        createdAt: string,
        ?webUrl: string
    ) =
    let mutable _id = id
    let mutable _shortId = shortId
    let mutable _title = title
    let mutable _message = message
    let mutable _authorName = authorName
    let mutable _authorEmail = authorEmail
    let mutable _createdAt = createdAt
    let mutable _webUrl : string option = webUrl

    /// The full 40-character commit SHA.
    member _.Id with get () = _id and set value = _id <- value
    /// The abbreviated commit SHA.
    member _.ShortId with get () = _shortId and set value = _shortId <- value
    /// The first line of the commit message.
    member _.Title with get () = _title and set value = _title <- value
    /// The full commit message.
    member _.Message with get () = _message and set value = _message <- value
    /// The display name of the commit author.
    member _.AuthorName with get () = _authorName and set value = _authorName <- value
    /// The email address of the commit author.
    member _.AuthorEmail with get () = _authorEmail and set value = _authorEmail <- value
    /// The commit timestamp as an ISO 8601 string.
    member _.CreatedAt with get () = _createdAt and set value = _createdAt <- value
    /// The URL of the commit's web page, or <c>None</c> if not available.
    member _.WebUrl with get () = _webUrl and set value = _webUrl <- value
