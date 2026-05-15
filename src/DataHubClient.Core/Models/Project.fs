namespace DataHubClient

open Fable.Core
open Thoth.Json.Core

/// <summary>
/// A DataHub project — the GitLab repository hosting a single ARC. See the
/// <see href="https://docs.gitlab.com/ee/api/projects.html">GitLab Projects API</see>.
/// </summary>
/// <param name="id">The project's unique, instance-wide numeric identifier.</param>
/// <param name="name">The project's display name.</param>
/// <param name="path">The project's URL slug within its namespace.</param>
/// <param name="pathWithNamespace">The full namespace-qualified path (e.g. <c>lab/my-arc</c>).</param>
/// <param name="visibility">The visibility level (<c>private</c>, <c>internal</c>, or <c>public</c>).</param>
/// <param name="webUrl">The URL of the project's web page.</param>
/// <param name="description">The project description, if one is set.</param>
/// <param name="defaultBranch">The name of the default branch, if the repository has one.</param>
[<AttachMembers>]
type Project
    (
        id: int,
        name: string,
        path: string,
        pathWithNamespace: string,
        visibility: string,
        webUrl: string,
        ?description: string,
        ?defaultBranch: string
    ) =
    /// The project's unique, instance-wide numeric identifier.
    member val Id = id with get, set
    /// The project's display name.
    member val Name = name with get, set
    /// The project's URL slug within its namespace.
    member val Path = path with get, set
    /// The full namespace-qualified path (e.g. <c>lab/my-arc</c>).
    member val PathWithNamespace = pathWithNamespace with get, set
    /// The visibility level (<c>private</c>, <c>internal</c>, or <c>public</c>).
    member val Visibility = visibility with get, set
    /// The URL of the project's web page.
    member val WebUrl = webUrl with get, set
    /// The project description, or <c>None</c> if none is set.
    member val Description : string option = description with get, set
    /// The name of the default branch, or <c>None</c> if the repository has none.
    member val DefaultBranch : string option = defaultBranch with get, set

    /// Decodes a <see cref="T:DataHubClient.Project"/> from its GitLab JSON representation.
    static member Decoder : Decoder<Project> =
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

    /// <summary>Encodes a <see cref="T:DataHubClient.Project"/> to its GitLab JSON representation.</summary>
    /// <param name="project">The project to encode.</param>
    static member Encoder(project: Project) : IEncodable =
        Encode.object [
            "id", Encode.int project.Id
            "name", Encode.string project.Name
            "path", Encode.string project.Path
            "path_with_namespace", Encode.string project.PathWithNamespace
            "visibility", Encode.string project.Visibility
            "web_url", Encode.string project.WebUrl
            "description", ThothExtensions.encodeOption Encode.string project.Description
            "default_branch", ThothExtensions.encodeOption Encode.string project.DefaultBranch ]
