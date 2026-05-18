namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.RepoFile"/>.
module RepoFile =

    /// Decodes a <see cref="T:DataHubClient.RepoFile"/> from its GitLab JSON representation.
    let decoder : Decoder<RepoFile> =
        Decode.object (fun get ->
            RepoFile(
                get.Required.Field "file_name" Decode.string,
                get.Required.Field "file_path" Decode.string,
                get.Required.Field "size" Decode.int,
                get.Required.Field "encoding" Decode.string,
                get.Required.Field "content" Decode.string,
                get.Required.Field "ref" Decode.string,
                get.Required.Field "blob_id" Decode.string,
                get.Required.Field "commit_id" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.RepoFile"/> to its GitLab JSON representation.
    let encoder (file: RepoFile) : IEncodable =
        Encode.object [
            "file_name", Encode.string file.FileName
            "file_path", Encode.string file.FilePath
            "size", Encode.int file.Size
            "encoding", Encode.string file.Encoding
            "content", Encode.string file.Content
            "ref", Encode.string file.Ref
            "blob_id", Encode.string file.BlobId
            "commit_id", Encode.string file.CommitId ]
