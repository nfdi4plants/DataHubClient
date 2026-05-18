namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.Commit"/>.
module Commit =

    /// Decodes a <see cref="T:DataHubClient.Commit"/> from its GitLab JSON representation.
    let decoder : Decoder<Commit> =
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

    /// Encodes a <see cref="T:DataHubClient.Commit"/> to its GitLab JSON representation.
    let encoder (commit: Commit) : IEncodable =
        Encode.object [
            "id", Encode.string commit.Id
            "short_id", Encode.string commit.ShortId
            "title", Encode.string commit.Title
            "message", Encode.string commit.Message
            "author_name", Encode.string commit.AuthorName
            "author_email", Encode.string commit.AuthorEmail
            "created_at", Encode.string commit.CreatedAt
            "web_url", ThothExtensions.encodeOption Encode.string commit.WebUrl ]
