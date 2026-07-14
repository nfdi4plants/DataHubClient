namespace DataHubClient

open Fable.Core
open DataHubClient.Json

/// <summary>
/// Client for GitLab Package Registry operations, focused on generic packages.
/// See the <see href="https://docs.gitlab.com/ee/api/packages.html">GitLab Packages API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type PackagesApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    let packageSegments projectId =
        [ "projects"; string projectId; "packages" ]

    let genericFileSegments projectId packageName version fileName =
        [ "projects"; string projectId; "packages"; "generic"; packageName; version; fileName ]

    /// <summary>Lists packages for a project.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="packageType">Optional package type filter, such as <c>generic</c>.</param>
    /// <param name="packageName">Optional package-name filter.</param>
    member _.ListAsync(projectId: int, ?packageType: string, ?packageName: string) =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    (packageSegments projectId)
                    [ "package_name", packageName; "package_type", packageType ]

            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Package.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Gets a package by package identifier.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="packageId">The package identifier.</param>
    member _.GetAsync(projectId: int, packageId: int) =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" (packageSegments projectId @ [ string packageId ]) []
            let! response = http.SendAsync req
            return ResourceHelpers.decode Package.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Uploads a generic package file.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="packageName">The generic package name.</param>
    /// <param name="version">The generic package version.</param>
    /// <param name="fileName">The package file name.</param>
    /// <param name="content">The file content to upload.</param>
    member _.UploadGenericFileAsync(projectId: int, packageName: string, version: string, fileName: string, content: string) =
        async {
            let req =
                ResourceHelpers.textRequest
                    baseUrl
                    auth
                    "PUT"
                    (genericFileSegments projectId packageName version fileName)
                    []
                    content
                    "application/octet-stream"

            let! response = http.SendAsync req
            return ResourceHelpers.responseBody response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Downloads a generic package file as text.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="packageName">The generic package name.</param>
    /// <param name="version">The generic package version.</param>
    /// <param name="fileName">The package file name.</param>
    member _.DownloadGenericFileAsync(projectId: int, packageName: string, version: string, fileName: string) =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    (genericFileSegments projectId packageName version fileName)
                    []

            let! response = http.SendAsync req
            return ResourceHelpers.responseBody response
        }
        |> ResourceHelpers.toPublic
