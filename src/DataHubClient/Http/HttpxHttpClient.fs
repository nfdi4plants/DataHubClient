namespace DataHubClient

open Fable.Core

/// Minimal binding to the <c>httpx.Response</c> object returned by a request.
type private HttpxResponse =
    /// The numeric HTTP status code.
    abstract status_code: int
    /// The response body decoded to a string.
    abstract text: string

/// Opaque handle to an <c>httpx.AsyncClient</c> instance.
type private HttpxAsyncClient = interface end

/// <summary>
/// Python implementation of <see cref="T:DataHubClient.IHttpClient"/> backed by
/// <c>httpx.AsyncClient</c> — the de-facto async HTTP library for Python. The
/// implementation is transpiled from this F# source by Fable; <c>httpx</c> must
/// be installed in the host environment (it is declared in <c>pyproject.toml</c>).
/// Follows HTTP redirects, including Package Registry redirects to object storage.
/// Authentication headers are removed when a redirect changes the URL's origin.
/// </summary>
[<AttachMembers>]
type HttpxHttpClient() =

    /// Creates a fresh <c>httpx.AsyncClient</c> with redirect following enabled.
    [<Emit("__import__('httpx').AsyncClient(follow_redirects=True, event_hooks={'request': [$0]})")>]
    static member private NewClient(onRequest: obj -> System.Threading.Tasks.Task<unit>) : HttpxAsyncClient = nativeOnly

    /// Parses the original URL so redirect origins use HTTPX's normalized scheme, host, and port.
    [<Emit("__import__('httpx').URL($0)")>]
    static member private ParseUrl(url: string) : obj = nativeOnly

    /// Checks whether the outgoing HTTPX request still targets the original origin.
    [<Emit("($0.url.scheme, $0.url.host, $0.url.port) == ($1.scheme, $1.host, $1.port)")>]
    static member private SameOrigin(request: obj, originalUrl: obj) : bool = nativeOnly

    /// Removes GitLab credentials before a redirected request reaches another origin.
    [<Emit("$0.headers.pop('PRIVATE-TOKEN', None); $0.headers.pop('JOB-TOKEN', None); $0.headers.pop('Authorization', None)")>]
    static member private RemoveCredentials(request: obj) : unit = nativeOnly

    /// Issues the request through the client, yielding the awaitable httpx response.
    [<Emit("$0.request($1, $2, headers=$3, content=$4)")>]
    static member private Request
        (client: HttpxAsyncClient, method': string, url: string, headers: obj, content: obj)
        : System.Threading.Tasks.Task<HttpxResponse> = nativeOnly

    /// Closes the client, releasing its connection pool.
    [<Emit("$0.aclose()")>]
    static member private Close(client: HttpxAsyncClient) : System.Threading.Tasks.Task<unit> = nativeOnly

    /// Reads the response headers as a resizable collection of name/value pairs.
    [<Emit("list($0.headers.items())")>]
    static member private HeaderItems(response: HttpxResponse) : ResizeArray<string * string> = nativeOnly

    interface IHttpClient with
        /// <summary>Sends a request through <c>httpx.AsyncClient</c>.</summary>
        /// <param name="request">The transport-agnostic request to send.</param>
        /// <returns>An async computation producing the transport-agnostic response.</returns>
        member _.SendAsync(request: HttpRequest) : Async<HttpResponse> =
            async {
                let originalUrl = HttpxHttpClient.ParseUrl request.Url

                let onRequest redirected =
                    async {
                        if not (HttpxHttpClient.SameOrigin(redirected, originalUrl)) then
                            HttpxHttpClient.RemoveCredentials redirected
                    }
                    |> Async.StartAsTask

                let client = HttpxHttpClient.NewClient onRequest

                let headers: obj = box request.Headers

                let content: obj =
                    match request.Body with
                    | Some body -> box body
                    | None -> null

                let! response =
                    HttpxHttpClient.Request(client, request.Method, request.Url, headers, content)
                    |> Async.AwaitTask

                do! HttpxHttpClient.Close client |> Async.AwaitTask

                let collected = HttpxHttpClient.HeaderItems response

                return HttpResponse(response.status_code, response.text, collected)
            }
