namespace DataHubClient

open Fable.Core

/// <summary>
/// A transport-agnostic HTTP request. Resource APIs assemble these and hand
/// them to an <see cref="T:DataHubClient.IHttpClient"/> implementation, keeping
/// <c>DataHubClient.Core</c> free of any concrete HTTP library.
/// </summary>
/// <param name="url">The fully-qualified request URL, including query string.</param>
/// <param name="method">The HTTP verb (e.g. <c>GET</c>, <c>POST</c>, <c>PUT</c>, <c>DELETE</c>).</param>
[<AttachMembers>]
type HttpRequest(url: string, method: string) =
    /// The fully-qualified request URL, including any query string.
    member val Url = url with get, set
    /// The HTTP verb to use for the request.
    member val Method = method with get, set
    /// Request headers as name/value pairs. Authentication and content headers
    /// are appended here by the resource APIs before the request is sent.
    member val Headers : (string * string) list = [] with get, set
    /// The request body, or <c>None</c> for verbs that carry no payload.
    member val Body : string option = None with get, set
