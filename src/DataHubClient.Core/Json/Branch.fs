namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.Branch"/>.
module Branch =

    /// Decodes a <see cref="T:DataHubClient.Branch"/> from its GitLab JSON representation.
    let decoder : Decoder<Branch> =
        Decode.object (fun get ->
            Branch(
                get.Required.Field "name" Decode.string,
                get.Required.Field "default" Decode.bool,
                get.Required.Field "protected" Decode.bool,
                get.Required.Field "merged" Decode.bool,
                get.Required.Field "commit" Commit.decoder))

    /// Encodes a <see cref="T:DataHubClient.Branch"/> to its GitLab JSON representation.
    let encoder (branch: Branch) : IEncodable =
        Encode.object [
            "name", Encode.string branch.Name
            "default", Encode.bool branch.Default
            "protected", Encode.bool branch.Protected
            "merged", Encode.bool branch.Merged
            "commit", Commit.encoder branch.Commit ]
