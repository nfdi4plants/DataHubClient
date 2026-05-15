namespace DataHubClient

open Fable.Core

/// <summary>
/// A transport-agnostic HTTP response returned by an
/// <see cref="T:DataHubClient.IHttpClient"/> implementation. Resource APIs
/// inspect the status code and decode the body into model classes.
/// </summary>
/// <param name="statusCode">The numeric HTTP status code of the response.</param>
/// <param name="body">The raw response body as a string (typically JSON).</param>
/// <param name="headers">The response headers as name/value pairs.</param>
[<AttachMembers>]
type HttpResponse(statusCode: int, body: string, headers: (string * string) list) =
    /// The numeric HTTP status code of the response.
    member _.StatusCode = statusCode
    /// The raw response body as a string (typically JSON).
    member _.Body = body
    /// The response headers as name/value pairs.
    member _.Headers = headers
