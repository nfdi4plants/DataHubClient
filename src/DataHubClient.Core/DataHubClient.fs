namespace DataHubClient

open Fable.Core

/// <summary>
/// Top-level facade for DataHub resource APIs — the single public entry point on
/// every target. <c>new DataHubClient(baseUrl, auth)</c> reads identically in
/// .NET, JavaScript/TypeScript, and Python: the constructor defaults the
/// <see cref="P:DataHubClient.DataHubClient.Http"/> transport to the one built for
/// the host runtime — <c>DotNetHttpClient</c> on .NET, <c>FetchHttpClient</c> on
/// JavaScript/TypeScript, <c>HttpxHttpClient</c> on Python. Assign <c>Http</c> to
/// substitute a transport for retries, proxies, or tests.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
[<AttachMembers>]
type DataHubClient(baseUrl: string, auth: Authentication) =

    let mutable http : IHttpClient =
        #if FABLE_COMPILER_PYTHON
        HttpxHttpClient() :> IHttpClient
        #endif
        #if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
        FetchHttpClient() :> IHttpClient
        #endif
        #if !FABLE_COMPILER
        DotNetHttpClient() :> IHttpClient
        #endif

    /// The HTTP transport every resource call is routed through. It defaults to
    /// the host runtime's transport; reassign it — e.g. to a retrying or proxying
    /// decorator, or a test double — and resource calls made afterwards use the
    /// new value.
    member _.Http
        with get () = http
        and set value = http <- value

    /// Project operations.
    member _.Projects = ProjectsApi(baseUrl, auth, http)
    /// Repository branch and commit operations.
    member _.Repository = RepositoryApi(baseUrl, auth, http)
    /// Repository file operations.
    member _.Files = FilesApi(baseUrl, auth, http)
    /// Issue operations.
    member _.Issues = IssuesApi(baseUrl, auth, http)
    /// Merge request operations.
    member _.MergeRequests = MergeRequestsApi(baseUrl, auth, http)
    /// Package Registry operations.
    member _.Packages = PackagesApi(baseUrl, auth, http)
