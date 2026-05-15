namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

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
[<AttachMembers>]
type User(id: int, username: string, name: string, state: string, webUrl: string, ?avatarUrl: string) =
    /// The user's unique, instance-wide numeric identifier.
    member val Id = id with get, set
    /// The user's login handle.
    member val Username = username with get, set
    /// The user's display name.
    member val Name = name with get, set
    /// The account state (e.g. <c>active</c>, <c>blocked</c>).
    member val State = state with get, set
    /// The URL of the user's profile page.
    member val WebUrl = webUrl with get, set
    /// The URL of the user's avatar image, or <c>None</c> if none is set.
    member val AvatarUrl : string option = avatarUrl with get, set

    /// Decodes a <see cref="T:DataHubClient.User"/> from its GitLab JSON representation.
    static member Decoder : Decoder<User> =
        Decode.object (fun get ->
            User(
                get.Required.Field "id" Decode.int,
                get.Required.Field "username" Decode.string,
                get.Required.Field "name" Decode.string,
                get.Required.Field "state" Decode.string,
                get.Required.Field "web_url" Decode.string,
                ?avatarUrl = get.Optional.Field "avatar_url" Decode.string))

    /// <summary>Encodes a <see cref="T:DataHubClient.User"/> to its GitLab JSON representation.</summary>
    /// <param name="user">The user to encode.</param>
    static member Encoder(user: User) : IEncodable =
        Encode.object [
            "id", Encode.int user.Id
            "username", Encode.string user.Username
            "name", Encode.string user.Name
            "state", Encode.string user.State
            "web_url", Encode.string user.WebUrl
            "avatar_url", ThothExtensions.encodeOption Encode.string user.AvatarUrl ]
