namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.Note"/>.
module Note =

    /// Decodes a <see cref="T:DataHubClient.Note"/> from its GitLab JSON representation.
    let decoder : Decoder<Note> =
        Decode.object (fun get ->
            Note(
                get.Required.Field "id" Decode.int,
                get.Required.Field "body" Decode.string,
                get.Required.Field "author" User.decoder,
                get.Required.Field "system" Decode.bool,
                get.Required.Field "created_at" Decode.string,
                get.Required.Field "updated_at" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.Note"/> to its GitLab JSON representation.
    let encoder (note: Note) : IEncodable =
        Encode.object [
            "id", Encode.int note.Id
            "body", Encode.string note.Body
            "author", User.encoder note.Author
            "system", Encode.bool note.System
            "created_at", Encode.string note.CreatedAt
            "updated_at", Encode.string note.UpdatedAt ]
