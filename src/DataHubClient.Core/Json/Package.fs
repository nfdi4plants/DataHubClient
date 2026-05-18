namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.Package"/>.
module Package =

    /// Decodes a <see cref="T:DataHubClient.Package"/> from its GitLab JSON representation.
    let decoder : Decoder<Package> =
        Decode.object (fun get ->
            Package(
                get.Required.Field "id" Decode.int,
                get.Required.Field "name" Decode.string,
                get.Required.Field "version" Decode.string,
                get.Required.Field "package_type" Decode.string,
                get.Required.Field "status" Decode.string,
                get.Required.Field "created_at" Decode.string,
                ?webUrl = get.Optional.Field "web_url" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.Package"/> to its GitLab JSON representation.
    let encoder (package: Package) : IEncodable =
        Encode.object [
            "id", Encode.int package.Id
            "name", Encode.string package.Name
            "version", Encode.string package.Version
            "package_type", Encode.string package.PackageType
            "status", Encode.string package.Status
            "created_at", Encode.string package.CreatedAt
            "web_url", ThothExtensions.encodeOption Encode.string package.WebUrl ]
