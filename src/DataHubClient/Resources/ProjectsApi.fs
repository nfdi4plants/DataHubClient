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

    /// <summary>Lists one page of visible projects. Use <c>ListAllAsync</c> to retrieve every page.</summary>
    /// <param name="search">Optional search term used to filter projects.</param>
    /// <param name="simple">Whether GitLab should return the compact project representation.</param>
    member _.ListAsync(?search: string, ?simple: bool) =
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
        |> ResourceHelpers.toPublic

    /// <summary>
    /// Lists every visible project in ascending ID order using keyset pagination.
    /// Anonymous requests return only public projects. This method continues until
    /// an empty page and fails if the server returns non-increasing IDs or any page fails.
    /// </summary>
    /// <param name="search">Optional search term used to filter projects.</param>
    /// <param name="simple">Whether GitLab should return the compact project representation.</param>
    /// <param name="visibility">Optional visibility filter: <c>public</c>, <c>internal</c>, or <c>private</c>.</param>
    /// <returns>All matching projects in ascending ID order.</returns>
    member _.ListAllAsync(?search: string, ?simple: bool, ?visibility: string) =
        let rec listPage (lastId: int option) pages =
            async {
                let query =
                    [
                        "pagination", Some "keyset"
                        "order_by", Some "id"
                        "sort", Some "asc"
                        "per_page", Some "100"
                        "id_after", lastId |> Option.map string
                        "search", search
                        "simple", simple |> Option.map (fun (value: bool) -> value.ToString().ToLowerInvariant())
                        "visibility", visibility
                    ]

                let req = ResourceHelpers.emptyRequest baseUrl auth "GET" [ "projects" ] query
                let! response = http.SendAsync req
                let projects = ResourceHelpers.decodeArray Project.decoder response

                if Array.isEmpty projects then
                    return pages |> List.rev |> Array.concat
                else
                    let mutable previousId = lastId

                    for project in projects do
                        match previousId with
                        | Some id when project.Id <= id ->
                            raise (DataHubError(response.StatusCode, "Project pagination did not return strictly increasing IDs.", response.Body))
                        | _ -> previousId <- Some project.Id

                    return! listPage previousId (projects :: pages)
            }

        listPage None []
        |> ResourceHelpers.toPublic

    /// <summary>Gets a project by numeric project identifier.</summary>
    /// <param name="projectId">The project identifier.</param>
    member _.GetAsync(projectId: int) =
        async {
            let req = ResourceHelpers.emptyRequest baseUrl auth "GET" [ "projects"; string projectId ] []
            let! response = http.SendAsync req
            return ResourceHelpers.decode Project.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Searches projects visible to the authenticated user.</summary>
    /// <param name="search">The search term used to filter projects.</param>
    member this.SearchAsync(search: string) =
        this.ListAsync(search = search)
