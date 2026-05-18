namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.Issue"/>.
module Issue =

    /// Decodes an <see cref="T:DataHubClient.Issue"/> from its GitLab JSON representation.
    let decoder : Decoder<Issue> =
        Decode.object (fun get ->
            Issue(
                get.Required.Field "id" Decode.int,
                get.Required.Field "iid" Decode.int,
                get.Required.Field "project_id" Decode.int,
                get.Required.Field "title" Decode.string,
                get.Required.Field "state" Decode.string,
                get.Required.Field "author" User.decoder,
                get.Required.Field "assignees" (Decode.array User.decoder),
                get.Required.Field "labels" (Decode.array Decode.string),
                get.Required.Field "web_url" Decode.string,
                get.Required.Field "created_at" Decode.string,
                get.Required.Field "updated_at" Decode.string,
                ?description = get.Optional.Field "description" Decode.string))

    /// Encodes an <see cref="T:DataHubClient.Issue"/> to its GitLab JSON representation.
    let encoder (issue: Issue) : IEncodable =
        Encode.object [
            "id", Encode.int issue.Id
            "iid", Encode.int issue.Iid
            "project_id", Encode.int issue.ProjectId
            "title", Encode.string issue.Title
            "state", Encode.string issue.State
            "author", User.encoder issue.Author
            "assignees", Encode.array (Array.map User.encoder issue.Assignees)
            "labels", Encode.array (Array.map Encode.string issue.Labels)
            "web_url", Encode.string issue.WebUrl
            "created_at", Encode.string issue.CreatedAt
            "updated_at", Encode.string issue.UpdatedAt
            "description", ThothExtensions.encodeOption Encode.string issue.Description ]
