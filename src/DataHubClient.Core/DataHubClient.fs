namespace DataHubClient

open Fable.Core

/// <summary>
/// Top-level facade for DataHub resource APIs. Construct it with a DataHub root
/// URL, an authentication strategy, and a target-specific HTTP client.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type DataHubClient(baseUrl: string, auth: Authentication, http: IHttpClient) =
    /// Project operations.
    member val Projects = ProjectsApi(baseUrl, auth, http)
    /// Repository branch and commit operations.
    member val Repository = RepositoryApi(baseUrl, auth, http)
    /// Repository file operations.
    member val Files = FilesApi(baseUrl, auth, http)
    /// Issue operations.
    member val Issues = IssuesApi(baseUrl, auth, http)
    /// Merge request operations.
    member val MergeRequests = MergeRequestsApi(baseUrl, auth, http)
    /// Package Registry operations.
    member val Packages = PackagesApi(baseUrl, auth, http)
