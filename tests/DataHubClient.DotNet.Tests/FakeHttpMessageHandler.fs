namespace DataHubClient.DotNet.Tests

open System
open System.Net
open System.Net.Http
open System.Threading
open System.Threading.Tasks

/// <summary>
/// A test <see cref="T:System.Net.Http.HttpMessageHandler"/> that records the
/// request it receives and returns a canned response, so <c>DotNetHttpClient</c>
/// can be exercised end to end without a network connection.
/// </summary>
/// <param name="status">The HTTP status code the handler returns.</param>
/// <param name="responseBody">The response body the handler returns.</param>
/// <param name="responseHeaders">Response headers to attach, as name/value pairs.</param>
type FakeHttpMessageHandler
    (status: HttpStatusCode, responseBody: string, responseHeaders: (string * string) list) =
    inherit HttpMessageHandler()

    let mutable requestMethod = ""
    let mutable requestUrl = ""
    let mutable requestHeaders: (string * string) list = []
    let mutable requestContentType: string option = None
    let mutable requestBody: string option = None

    /// The HTTP verb of the request the handler last received.
    member _.RequestMethod = requestMethod
    /// The absolute URL of the request the handler last received.
    member _.RequestUrl = requestUrl
    /// The request headers (content headers excluded) the handler last received.
    member _.RequestHeaders = requestHeaders
    /// The <c>Content-Type</c> of the last request, or <c>None</c> if it carried no content.
    member _.RequestContentType = requestContentType
    /// The body of the last request, or <c>None</c> if it carried no content.
    member _.RequestBody = requestBody

    /// <summary>Records the incoming request and returns the canned response.</summary>
    /// <param name="request">The request sent by <see cref="T:System.Net.Http.HttpClient"/>.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>A task producing the canned response.</returns>
    override _.SendAsync
        (request: HttpRequestMessage, cancellationToken: CancellationToken)
        : Task<HttpResponseMessage> =
        task {
            requestMethod <- request.Method.Method
            requestUrl <- string request.RequestUri

            requestHeaders <-
                [ for header in request.Headers do
                      for value in header.Value do
                          header.Key, value ]

            match request.Content with
            | null -> ()
            | content ->
                let! body = content.ReadAsStringAsync()
                requestBody <- Some body
                requestContentType <-
                    match content.Headers.ContentType with
                    | null -> None
                    | mediaType -> Some mediaType.MediaType

            let response = new HttpResponseMessage(status)
            response.Content <- new StringContent(responseBody)

            for name, value in responseHeaders do
                if not (response.Headers.TryAddWithoutValidation(name, value)) then
                    response.Content.Headers.TryAddWithoutValidation(name, value) |> ignore

            return response
        }

/// Header-list assertions for the .NET shim tests.
module Header =

    /// True if <paramref name="headers"/> carries <paramref name="name"/> — matched
    /// case-insensitively, as HTTP requires — with exactly <paramref name="value"/>.
    /// System.Net.Http canonicalises some header names (e.g. <c>X-Request-Id</c> to
    /// <c>X-Request-ID</c>), so an exact-case match would be transport-fragile.
    let contains (name: string) (value: string) (headers: (string * string) list) : bool =
        headers
        |> List.exists (fun (headerName, headerValue) ->
            String.Equals(headerName, name, StringComparison.OrdinalIgnoreCase)
            && headerValue = value)
