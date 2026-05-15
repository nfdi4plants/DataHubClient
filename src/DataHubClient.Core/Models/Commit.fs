namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

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
    /// The full 40-character commit SHA.
    member val Id = id with get, set
    /// The abbreviated commit SHA.
    member val ShortId = shortId with get, set
    /// The first line of the commit message.
    member val Title = title with get, set
    /// The full commit message.
    member val Message = message with get, set
    /// The display name of the commit author.
    member val AuthorName = authorName with get, set
    /// The email address of the commit author.
    member val AuthorEmail = authorEmail with get, set
    /// The commit timestamp as an ISO 8601 string.
    member val CreatedAt = createdAt with get, set
    /// The URL of the commit's web page, or <c>None</c> if not available.
    member val WebUrl : string option = webUrl with get, set

    /// Decodes a <see cref="T:DataHubClient.Commit"/> from its GitLab JSON representation.
    static member Decoder : Decoder<Commit> =
        Decode.object (fun get ->
            Commit(
                get.Required.Field "id" Decode.string,
                get.Required.Field "short_id" Decode.string,
                get.Required.Field "title" Decode.string,
                get.Required.Field "message" Decode.string,
                get.Required.Field "author_name" Decode.string,
                get.Required.Field "author_email" Decode.string,
                get.Required.Field "created_at" Decode.string,
                ?webUrl = get.Optional.Field "web_url" Decode.string))

    /// <summary>Encodes a <see cref="T:DataHubClient.Commit"/> to its GitLab JSON representation.</summary>
    /// <param name="commit">The commit to encode.</param>
    static member Encoder(commit: Commit) : IEncodable =
        Encode.object [
            "id", Encode.string commit.Id
            "short_id", Encode.string commit.ShortId
            "title", Encode.string commit.Title
            "message", Encode.string commit.Message
            "author_name", Encode.string commit.AuthorName
            "author_email", Encode.string commit.AuthorEmail
            "created_at", Encode.string commit.CreatedAt
            "web_url", ThothExtensions.encodeOption Encode.string commit.WebUrl ]
