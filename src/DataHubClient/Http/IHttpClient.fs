namespace DataHubClient

/// <summary>
/// The HTTP transport abstraction for <c>DataHubClient</c>. Each distribution
/// target supplies its own implementation — <c>System.Net.Http</c> on .NET,
/// <c>fetch</c> on JavaScript/TypeScript, <c>httpx</c> on Python — so that
/// <c>DataHubClient</c> never depends on a concrete HTTP library.
/// Callers may also inject a custom implementation for retries, proxies, or tests.
/// </summary>
type IHttpClient =
    /// <summary>Sends an HTTP request and asynchronously yields the response.</summary>
    /// <param name="request">The request to send.</param>
    /// <returns>An async computation producing the <see cref="T:DataHubClient.HttpResponse"/>.</returns>
    abstract SendAsync : request: HttpRequest -> Async<HttpResponse>
