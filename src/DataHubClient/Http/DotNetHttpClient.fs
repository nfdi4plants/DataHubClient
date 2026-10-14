namespace DataHubClient

open System
open System.Net.Http
open System.Net.Http.Headers
open Fable.Core

/// <summary>
/// .NET implementation of <see cref="T:DataHubClient.IHttpClient"/> backed by
/// <see cref="T:System.Net.Http.HttpClient"/>.
/// Follows up to twenty redirects and removes authentication headers when the
/// destination changes origin. HTTPS-to-HTTP redirects are rejected.
/// </summary>
/// <param name="httpClient">
/// The client used to send requests. Its handler must have automatic redirects
/// disabled (<c>HttpClientHandler.AllowAutoRedirect = false</c>) so this transport
/// can remove credentials before following redirects. Custom handlers must not
/// reinsert credentials on redirected requests. Supply authentication on each
/// request rather than in <c>DefaultRequestHeaders</c>: cross-origin redirects
/// are rejected when those defaults contain credentials. The caller owns this client.
/// </param>
[<AttachMembers>]
type DotNetHttpClient(httpClient: HttpClient) =

    static let sharedHttpClient =
        lazy (new HttpClient(new HttpClientHandler(AllowAutoRedirect = false)))

    /// Headers that must not leave the origin to which the caller supplied them.
    static let credentialHeaders = [ "Authorization"; "PRIVATE-TOKEN"; "JOB-TOKEN" ]

    /// Compares HTTP header names without depending on their casing.
    static let sameHeader left right = String.Equals(left, right, StringComparison.OrdinalIgnoreCase)

    /// Compares normalized URI origins, including effective default ports.
    static let sameOrigin (left: Uri) (right: Uri) =
        left.Scheme = right.Scheme && left.IdnHost = right.IdnHost && left.Port = right.Port

    /// <summary>
    /// Creates a transport using a shared <see cref="T:System.Net.Http.HttpClient"/> instance.
    /// </summary>
    new() = DotNetHttpClient(sharedHttpClient.Value)

    /// <summary>The underlying client, whose handler must keep automatic redirects disabled.</summary>
    member _.HttpClient = httpClient

    interface IHttpClient with
        /// <summary>Sends a request through <see cref="T:System.Net.Http.HttpClient"/>.</summary>
        /// <param name="request">The transport-agnostic request to send.</param>
        /// <returns>An async computation producing the transport-agnostic response.</returns>
        member _.SendAsync(request: HttpRequest) : Async<HttpResponse> =
            async {
                let mutable url = Uri(request.Url, UriKind.Absolute)
                let mutable method = request.Method
                let mutable contentBody = request.Body
                let mutable headers = request.Headers |> Seq.toList
                let mutable redirects = 0
                let mutable result = None

                while result.IsNone do
                    use message = new HttpRequestMessage(new HttpMethod(method), url)

                    match contentBody with
                    | Some body -> message.Content <- new StringContent(body)
                    | None -> ()

                    for name, value in headers do
                        if sameHeader name "Content-Type" then
                            match message.Content with
                            | null -> ()
                            | content -> content.Headers.ContentType <- MediaTypeHeaderValue.Parse(value)
                        elif not (message.Headers.TryAddWithoutValidation(name, value)) then
                            match message.Content with
                            | null -> ()
                            | content -> content.Headers.TryAddWithoutValidation(name, value) |> ignore

                    use! response = httpClient.SendAsync(message) |> Async.AwaitTask
                    let status = int response.StatusCode
                    let isRedirect = List.contains status [ 301; 302; 303; 307; 308 ]
                    let location = response.Headers.Location

                    if isRedirect && not (isNull location) then
                        if redirects >= 20 then
                            raise (HttpRequestException("The request exceeded the limit of 20 redirects."))

                        let nextUrl = Uri(url, location)

                        if nextUrl.Scheme <> Uri.UriSchemeHttp && nextUrl.Scheme <> Uri.UriSchemeHttps then
                            raise (HttpRequestException("The redirect destination must use HTTP or HTTPS."))

                        if url.Scheme = Uri.UriSchemeHttps && nextUrl.Scheme = Uri.UriSchemeHttp then
                            raise (HttpRequestException("Refusing an HTTPS-to-HTTP redirect."))

                        if not (sameOrigin url nextUrl) then
                            // HttpClient reapplies defaults on each SendAsync. Mutating them
                            // would race with concurrent callers, so refuse this unsafe setup.
                            if credentialHeaders |> List.exists httpClient.DefaultRequestHeaders.Contains then
                                raise (HttpRequestException(
                                    "Cannot follow a cross-origin redirect with credentials in HttpClient.DefaultRequestHeaders. "
                                    + "Supply authentication on individual requests instead."))

                            headers <-
                                headers
                                |> List.filter (fun (name, _) ->
                                    credentialHeaders |> List.exists (sameHeader name) |> not)

                        let rewriteToGet =
                            ((status = 301 || status = 302) && sameHeader method "POST")
                            || (status = 303 && not (sameHeader method "GET" || sameHeader method "HEAD"))

                        if rewriteToGet then
                            method <- "GET"
                            contentBody <- None
                            headers <-
                                headers
                                |> List.filter (fun (name, _) ->
                                    not (name.StartsWith("Content-", StringComparison.OrdinalIgnoreCase))
                                    && not (sameHeader name "Transfer-Encoding"))

                        url <- nextUrl
                        redirects <- redirects + 1
                    else
                        let! body = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                        let responseHeaders =
                            ResizeArray [
                                for header in response.Headers do
                                    for value in header.Value do
                                        header.Key, value

                                if not (isNull response.Content) then
                                    for header in response.Content.Headers do
                                        for value in header.Value do
                                            header.Key, value
                            ]

                        result <- Some (HttpResponse(status, body, responseHeaders))

                return result.Value
            }
