namespace DataHubClient

open Fable.Core

/// <summary>
/// Base error for every failed DataHub operation. Wraps the raw HTTP status and
/// body so callers get a consistent, cross-language error shape instead of a
/// target-specific HTTP exception.
/// </summary>
/// <param name="statusCode">The HTTP status code returned by the DataHub.</param>
/// <param name="message">A short, human-readable description of the failure.</param>
/// <param name="body">The raw response body, preserved for diagnostics.</param>
[<AttachMembers>]
type DataHubError(statusCode: int, message: string, body: string) =
    inherit System.Exception(message)

    /// The HTTP status code returned by the DataHub.
    member _.StatusCode = statusCode
    /// The raw response body, preserved for diagnostics.
    member _.Body = body

    /// <summary>Maps an unsuccessful <see cref="T:DataHubClient.HttpResponse"/> to the most specific error subclass.</summary>
    /// <param name="response">The failed HTTP response to classify.</param>
    /// <returns>An <see cref="T:DataHubClient.UnauthorizedError"/>, <see cref="T:DataHubClient.NotFoundError"/>, <see cref="T:DataHubClient.RateLimitedError"/>, <see cref="T:DataHubClient.ServerError"/>, or a plain <c>DataHubError</c>.</returns>
    static member FromResponse(response: HttpResponse) : DataHubError =
        match response.StatusCode with
        | 401
        | 403 -> UnauthorizedError(response.StatusCode, response.Body) :> DataHubError
        | 404 -> NotFoundError(response.Body) :> DataHubError
        | 429 -> RateLimitedError(response.Body) :> DataHubError
        | code when code >= 500 -> ServerError(code, response.Body) :> DataHubError
        | code -> DataHubError(code, "HTTP " + string code, response.Body)

/// <summary>401/403 — missing, invalid, or insufficiently scoped credentials.</summary>
/// <param name="statusCode">The HTTP status code (<c>401</c> or <c>403</c>).</param>
/// <param name="body">The raw response body, preserved for diagnostics.</param>
and [<AttachMembers>] UnauthorizedError(statusCode: int, body: string) =
    inherit DataHubError(statusCode, "Unauthorized", body)

/// <summary>404 — the requested project, branch, issue, … does not exist.</summary>
/// <param name="body">The raw response body, preserved for diagnostics.</param>
and [<AttachMembers>] NotFoundError(body: string) =
    inherit DataHubError(404, "Not found", body)

/// <summary>429 — the DataHub rejected the request for exceeding its rate limit.</summary>
/// <param name="body">The raw response body, preserved for diagnostics.</param>
and [<AttachMembers>] RateLimitedError(body: string) =
    inherit DataHubError(429, "Rate limited", body)

/// <summary>5xx — the DataHub failed to process an otherwise valid request.</summary>
/// <param name="statusCode">The <c>5xx</c> HTTP status code.</param>
/// <param name="body">The raw response body, preserved for diagnostics.</param>
and [<AttachMembers>] ServerError(statusCode: int, body: string) =
    inherit DataHubError(statusCode, "Server error", body)
