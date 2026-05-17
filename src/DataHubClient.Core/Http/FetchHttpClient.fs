namespace DataHubClient

open Fable.Core
open Fable.Core.JsInterop

/// Minimal binding to the WHATWG <c>Headers</c> object carried by a fetch response.
type private FetchHeaders =
    /// Invokes the callback once per header, passing the value then the name.
    abstract forEach: (string -> string -> unit) -> unit

/// Minimal binding to the WHATWG <c>Response</c> returned by <c>fetch</c>.
type private FetchResponse =
    /// The numeric HTTP status code.
    abstract status: int
    /// The response headers.
    abstract headers: FetchHeaders
    /// Reads the response body to a string.
    abstract text: unit -> JS.Promise<string>

/// <summary>
/// JavaScript/TypeScript implementation of <see cref="T:DataHubClient.IHttpClient"/>
/// backed by the global <c>fetch</c> function — built into modern browsers and
/// Node.js 18+. The implementation is transpiled from this F# source by Fable.
/// </summary>
[<AttachMembers>]
type FetchHttpClient() =

    [<Emit("fetch($0, $1)")>]
    static member private Fetch (url: string) (init: obj) : JS.Promise<FetchResponse> = jsNative

    interface IHttpClient with
        /// <summary>Sends a request through the global <c>fetch</c> function.</summary>
        /// <param name="request">The transport-agnostic request to send.</param>
        /// <returns>An async computation producing the transport-agnostic response.</returns>
        member _.SendAsync(request: HttpRequest) : Async<HttpResponse> =
            async {
                let headers = createObj [ for name, value in request.Headers -> name ==> value ]

                let init =
                    match request.Body with
                    | Some body -> createObj [ "method" ==> request.Method; "headers" ==> headers; "body" ==> body ]
                    | None -> createObj [ "method" ==> request.Method; "headers" ==> headers ]

                let! response = FetchHttpClient.Fetch request.Url init |> Async.AwaitPromise
                let! body = response.text () |> Async.AwaitPromise

                let collected = ResizeArray()
                response.headers.forEach (fun value name -> collected.Add(name, value))

                return HttpResponse(response.status, body, List.ofSeq collected)
            }
