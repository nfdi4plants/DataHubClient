namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

/// <summary>
/// An entry in a project's generic Package Registry. See the
/// <see href="https://docs.gitlab.com/ee/api/packages.html">GitLab Packages API</see>.
/// </summary>
/// <param name="id">The package's unique, instance-wide numeric identifier.</param>
/// <param name="name">The package name.</param>
/// <param name="version">The package version string.</param>
/// <param name="packageType">The package format (e.g. <c>generic</c>).</param>
/// <param name="status">The package status (e.g. <c>default</c>, <c>hidden</c>).</param>
/// <param name="createdAt">The creation timestamp as an ISO 8601 string.</param>
/// <param name="webUrl">The URL of the package's web page, if available.</param>
[<AttachMembers>]
type Package
    (
        id: int,
        name: string,
        version: string,
        packageType: string,
        status: string,
        createdAt: string,
        ?webUrl: string
    ) =
    /// The package's unique, instance-wide numeric identifier.
    member val Id = id with get, set
    /// The package name.
    member val Name = name with get, set
    /// The package version string.
    member val Version = version with get, set
    /// The package format (e.g. <c>generic</c>).
    member val PackageType = packageType with get, set
    /// The package status (e.g. <c>default</c>, <c>hidden</c>).
    member val Status = status with get, set
    /// The creation timestamp as an ISO 8601 string.
    member val CreatedAt = createdAt with get, set
    /// The URL of the package's web page, or <c>None</c> if not available.
    member val WebUrl : string option = webUrl with get, set

    /// Decodes a <see cref="T:DataHubClient.Package"/> from its GitLab JSON representation.
    static member Decoder : Decoder<Package> =
        Decode.object (fun get ->
            Package(
                get.Required.Field "id" Decode.int,
                get.Required.Field "name" Decode.string,
                get.Required.Field "version" Decode.string,
                get.Required.Field "package_type" Decode.string,
                get.Required.Field "status" Decode.string,
                get.Required.Field "created_at" Decode.string,
                ?webUrl = get.Optional.Field "web_url" Decode.string))

    /// <summary>Encodes a <see cref="T:DataHubClient.Package"/> to its GitLab JSON representation.</summary>
    /// <param name="package">The package to encode.</param>
    static member Encoder(package: Package) : IEncodable =
        Encode.object [
            "id", Encode.int package.Id
            "name", Encode.string package.Name
            "version", Encode.string package.Version
            "package_type", Encode.string package.PackageType
            "status", Encode.string package.Status
            "created_at", Encode.string package.CreatedAt
            "web_url", ThothExtensions.encodeOption Encode.string package.WebUrl ]
