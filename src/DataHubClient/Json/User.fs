namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.User"/>. The JSON
/// modules live in the <c>DataHubClient.Json</c> namespace — separate from the
/// model classes in <c>DataHubClient</c> — mirroring ARCtrl's <c>ARCtrl.Json</c>.
module User =

    /// Decodes a <see cref="T:DataHubClient.User"/> from its GitLab JSON representation.
    let decoder : Decoder<User> =
        Decode.object (fun get ->
            User(
                get.Required.Field "id" Decode.int,
                get.Required.Field "username" Decode.string,
                get.Required.Field "name" Decode.string,
                get.Required.Field "state" Decode.string,
                get.Required.Field "web_url" Decode.string,
                ?avatarUrl = get.Optional.Field "avatar_url" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.User"/> to its GitLab JSON representation.
    let encoder (user: User) : IEncodable =
        Encode.object [
            "id", Encode.int user.Id
            "username", Encode.string user.Username
            "name", Encode.string user.Name
            "state", Encode.string user.State
            "web_url", Encode.string user.WebUrl
            "avatar_url", ThothExtensions.encodeOption Encode.string user.AvatarUrl ]
