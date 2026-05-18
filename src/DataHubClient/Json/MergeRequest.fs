namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.MergeRequest"/>.
module MergeRequest =

    /// Decodes a <see cref="T:DataHubClient.MergeRequest"/> from its GitLab JSON representation.
    let decoder : Decoder<MergeRequest> =
        Decode.object (fun get ->
            MergeRequest(
                get.Required.Field "id" Decode.int,
                get.Required.Field "iid" Decode.int,
                get.Required.Field "project_id" Decode.int,
                get.Required.Field "title" Decode.string,
                get.Required.Field "state" Decode.string,
                get.Required.Field "source_branch" Decode.string,
                get.Required.Field "target_branch" Decode.string,
                get.Required.Field "author" User.decoder,
                get.Required.Field "web_url" Decode.string,
                get.Required.Field "created_at" Decode.string,
                get.Required.Field "updated_at" Decode.string,
                ?description = get.Optional.Field "description" Decode.string,
                ?mergeStatus = get.Optional.Field "merge_status" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.MergeRequest"/> to its GitLab JSON representation.
    let encoder (mr: MergeRequest) : IEncodable =
        Encode.object [
            "id", Encode.int mr.Id
            "iid", Encode.int mr.Iid
            "project_id", Encode.int mr.ProjectId
            "title", Encode.string mr.Title
            "state", Encode.string mr.State
            "source_branch", Encode.string mr.SourceBranch
            "target_branch", Encode.string mr.TargetBranch
            "author", User.encoder mr.Author
            "web_url", Encode.string mr.WebUrl
            "created_at", Encode.string mr.CreatedAt
            "updated_at", Encode.string mr.UpdatedAt
            "description", ThothExtensions.encodeOption Encode.string mr.Description
            "merge_status", ThothExtensions.encodeOption Encode.string mr.MergeStatus ]
