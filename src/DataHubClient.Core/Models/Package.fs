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
    let mutable _id = id
    let mutable _name = name
    let mutable _version = version
    let mutable _packageType = packageType
    let mutable _status = status
    let mutable _createdAt = createdAt
    let mutable _webUrl : string option = webUrl

    /// The package's unique, instance-wide numeric identifier.
    member _.Id with get () = _id and set value = _id <- value
    /// The package name.
    member _.Name with get () = _name and set value = _name <- value
    /// The package version string.
    member _.Version with get () = _version and set value = _version <- value
    /// The package format (e.g. <c>generic</c>).
    member _.PackageType with get () = _packageType and set value = _packageType <- value
    /// The package status (e.g. <c>default</c>, <c>hidden</c>).
    member _.Status with get () = _status and set value = _status <- value
    /// The creation timestamp as an ISO 8601 string.
    member _.CreatedAt with get () = _createdAt and set value = _createdAt <- value
    /// The URL of the package's web page, or <c>None</c> if not available.
    member _.WebUrl with get () = _webUrl and set value = _webUrl <- value

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
