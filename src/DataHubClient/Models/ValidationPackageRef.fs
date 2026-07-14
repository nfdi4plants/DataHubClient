namespace DataHubClient

open Fable.Core

/// <summary>
/// A reference to one validation package's result folder on an ARC's cqc
/// branch, named <c>&lt;name&gt;@&lt;version&gt;</c> on disk. Instances are
/// usually discovered via <c>ValidationApi.ListPackagesAsync</c>; use
/// <see cref="M:DataHubClient.ValidationPackageRef.Create"/> when the
/// coordinates are already known. See the
/// <see href="https://nfdi4plants.github.io/nfdi4plants.knowledgebase/arc-validation/authoring-validation-packages/#validation-output">validation output specification</see>.
/// </summary>
/// <param name="name">The validation package name (e.g. <c>invenio</c>).</param>
/// <param name="version">The validation package version (semantic version string).</param>
/// <param name="branch">The validated ARC branch whose results the folder holds (e.g. <c>main</c>).</param>
[<AttachMembers>]
type ValidationPackageRef(name: string, version: string, branch: string) =
    let mutable _name = name
    let mutable _version = version
    let mutable _branch = branch

    /// The validation package name (e.g. <c>invenio</c>).
    member _.Name with get () = _name and set value = _name <- value
    /// The validation package version (semantic version string).
    member _.Version with get () = _version and set value = _version <- value
    /// The validated ARC branch whose results the folder holds (e.g. <c>main</c>).
    member _.Branch with get () = _branch and set value = _branch <- value
    /// The result folder's path on the cqc branch, <c>{Branch}/{Name}@{Version}</c>.
    member _.Path = _branch + "/" + _name + "@" + _version

    /// <summary>Creates a reference from known coordinates, skipping discovery.</summary>
    /// <param name="name">The validation package name.</param>
    /// <param name="version">The validation package version.</param>
    /// <param name="branch">The validated ARC branch. Defaults to <c>main</c>.</param>
    static member Create(name: string, version: string, ?branch: string) =
        ValidationPackageRef(name, version, defaultArg branch "main")
