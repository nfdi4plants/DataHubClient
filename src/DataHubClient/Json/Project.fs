namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.Project"/>.
module Project =

    /// Decodes a <see cref="T:DataHubClient.Project"/> from its GitLab JSON representation.
    let decoder : Decoder<Project> =
        Decode.object (fun get ->
            Project(
                get.Required.Field "id" Decode.int,
                get.Required.Field "name" Decode.string,
                get.Required.Field "path" Decode.string,
                get.Required.Field "path_with_namespace" Decode.string,
                get.Required.Field "visibility" Decode.string,
                get.Required.Field "web_url" Decode.string,
                ?description = get.Optional.Field "description" Decode.string,
                ?defaultBranch = get.Optional.Field "default_branch" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.Project"/> to its GitLab JSON representation.
    let encoder (project: Project) : IEncodable =
        Encode.object [
            "id", Encode.int project.Id
            "name", Encode.string project.Name
            "path", Encode.string project.Path
            "path_with_namespace", Encode.string project.PathWithNamespace
            "visibility", Encode.string project.Visibility
            "web_url", Encode.string project.WebUrl
            "description", ThothExtensions.encodeOption Encode.string project.Description
            "default_branch", ThothExtensions.encodeOption Encode.string project.DefaultBranch ]
