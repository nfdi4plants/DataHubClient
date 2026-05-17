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
    let mutable _url = url
    let mutable _method = method
    let mutable _headers : (string * string) list = []
    let mutable _body : string option = None

    /// The fully-qualified request URL, including any query string.
    member _.Url with get () = _url and set value = _url <- value
    /// The HTTP verb to use for the request.
    member _.Method with get () = _method and set value = _method <- value
    /// Request headers as name/value pairs. Authentication and content headers
    /// are appended here by the resource APIs before the request is sent.
    member _.Headers with get () = _headers and set value = _headers <- value
    /// The request body, or <c>None</c> for verbs that carry no payload.
    member _.Body with get () = _body and set value = _body <- value
