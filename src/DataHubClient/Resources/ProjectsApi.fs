namespace DataHubClient

open Fable.Core
open DataHubClient.Json

/// <summary>
/// Client for the GitLab Projects API used by DataHubs.
/// See the <see href="https://docs.gitlab.com/ee/api/projects.html">GitLab Projects API</see>.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type ProjectsApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    /// <summary>Lists projects visible to the authenticated user.</summary>
    /// <param name="search">Optional search term used to filter projects.</param>
    /// <param name="simple">Whether GitLab should return the compact project representation.</param>
    member _.ListAsync(?search: string, ?simple: bool) : Async<Project array> =
        async {
            let query =
                [
                    "search", search
                    "simple", simple |> Option.map (fun v -> v.ToString().ToLowerInvariant())
                ]

            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" [ "projects" ] query
            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Project.decoder response
        }

    /// <summary>Gets a project by numeric project identifier.</summary>
    /// <param name="projectId">The project identifier.</param>
    member _.GetAsync(projectId: int) : Async<Project> =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" [ "projects"; string projectId ] []
            let! response = http.SendAsync req
            return ResourceHelpers.decode Project.decoder response
        }

    /// <summary>Searches projects visible to the authenticated user.</summary>
    /// <param name="search">The search term used to filter projects.</param>
    member this.SearchAsync(search: string) : Async<Project array> =
        this.ListAsync(search = search)
