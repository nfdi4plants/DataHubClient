namespace DataHubClient

/// <summary>
/// The JavaScript/TypeScript entry point for <see cref="T:DataHubClient.DataHubClient"/>:
/// a thin subclass whose two-argument constructor defaults the HTTP transport to
/// <see cref="T:DataHubClient.FetchHttpClient"/>. A JS/TS caller builds a working
/// client from just a URL and credentials, while the three-argument constructor
/// still accepts a custom <see cref="T:DataHubClient.IHttpClient"/> for retries,
/// proxies, or tests. An instance is a full <see cref="T:DataHubClient.DataHubClient"/>;
/// every resource property is inherited.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
type DataHubClientJavaScript(baseUrl: string, auth: Authentication, http: IHttpClient) =
    inherit DataHubClient(baseUrl, auth, http)

    /// <summary>
    /// Creates a client using the global <c>fetch</c> transport.
    /// </summary>
    /// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
    /// <param name="auth">The authentication header applied to every request.</param>
    new(baseUrl: string, auth: Authentication) =
        DataHubClientJavaScript(baseUrl, auth, FetchHttpClient() :> IHttpClient)
