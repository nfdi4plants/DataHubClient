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
/// </summary>
[<AttachMembers>]
type HttpxHttpClient() =

    /// Creates a fresh <c>httpx.AsyncClient</c>.
    [<Emit("__import__('httpx').AsyncClient()")>]
    static member private NewClient() : HttpxAsyncClient = nativeOnly

    /// Issues the request through the client, yielding the awaitable httpx response.
    [<Emit("$0.request($1, $2, headers=$3, content=$4)")>]
    static member private Request
        (client: HttpxAsyncClient, method': string, url: string, headers: obj, content: obj)
        : System.Threading.Tasks.Task<HttpxResponse> = nativeOnly

    /// Closes the client, releasing its connection pool.
    [<Emit("$0.aclose()")>]
    static member private Close(client: HttpxAsyncClient) : System.Threading.Tasks.Task<unit> = nativeOnly

    /// Reads the response headers as a sequence of name/value pairs.
    [<Emit("list($0.headers.items())")>]
    static member private HeaderItems(response: HttpxResponse) : seq<string * string> = nativeOnly

    interface IHttpClient with
        /// <summary>Sends a request through <c>httpx.AsyncClient</c>.</summary>
        /// <param name="request">The transport-agnostic request to send.</param>
        /// <returns>An async computation producing the transport-agnostic response.</returns>
        member _.SendAsync(request: HttpRequest) : Async<HttpResponse> =
            async {
                let client = HttpxHttpClient.NewClient()

                let headers: obj = box (ResizeArray [ for name, value in request.Headers -> name, value ])

                let content: obj =
                    match request.Body with
                    | Some body -> box body
                    | None -> null

                let! response =
                    HttpxHttpClient.Request(client, request.Method, request.Url, headers, content)
                    |> Async.AwaitTask

                do! HttpxHttpClient.Close client |> Async.AwaitTask

                let collected = [ for name, value in HttpxHttpClient.HeaderItems response -> name, value ]

                return HttpResponse(response.status_code, response.text, collected)
            }
