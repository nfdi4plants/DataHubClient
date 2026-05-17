namespace DataHubClient

open System.Net.Http

/// <summary>
/// The .NET entry point for <see cref="T:DataHubClient.DataHubClient"/>: a thin
/// subclass whose constructors default the HTTP transport to
/// <see cref="T:DataHubClient.DotNetHttpClient"/>. A .NET caller builds a working
/// client from just a URL and credentials, with <c>new</c> from C# and F# alike —
/// no factory class or extension members. An instance is a full
/// <see cref="T:DataHubClient.DataHubClient"/>; every resource property is inherited.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
type DataHubClientDotNet(baseUrl: string, auth: Authentication, http: IHttpClient) =
    inherit DataHubClient(baseUrl, auth, http)

    /// <summary>
    /// Creates a client using the shared default <see cref="T:System.Net.Http.HttpClient"/> transport.
    /// </summary>
    /// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
    /// <param name="auth">The authentication header applied to every request.</param>
    new(baseUrl: string, auth: Authentication) =
        DataHubClientDotNet(baseUrl, auth, DotNetHttpClient() :> IHttpClient)

    /// <summary>
    /// Creates a client backed by a caller-supplied <see cref="T:System.Net.Http.HttpClient"/>,
    /// for control over handlers, proxies, or timeouts.
    /// </summary>
    /// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
    /// <param name="auth">The authentication header applied to every request.</param>
    /// <param name="httpClient">The <see cref="T:System.Net.Http.HttpClient"/> instance used to send requests.</param>
    new(baseUrl: string, auth: Authentication, httpClient: HttpClient) =
        DataHubClientDotNet(baseUrl, auth, DotNetHttpClient(httpClient) :> IHttpClient)
