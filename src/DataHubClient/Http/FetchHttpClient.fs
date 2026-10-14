namespace DataHubClient

open Fable.Core
open Fable.Core.JsInterop

/// Minimal binding to the WHATWG <c>Headers</c> object carried by a fetch response.
type private FetchHeaders =
    /// Invokes the callback once per header, passing the value then the name.
    abstract forEach: (string -> string -> unit) -> unit
    /// Reads a nullable header value from the native Headers object.
    abstract get: string -> obj

/// Minimal binding to the WHATWG <c>Response</c> returned by <c>fetch</c>.
type private FetchResponse =
    /// The numeric HTTP status code.
    abstract status: int
    /// The response type, including opaque redirects returned by browsers.
    abstract ``type``: string
    /// The nullable response stream, used to release intermediate responses.
    abstract body: obj
    /// The response headers.
    abstract headers: FetchHeaders
    /// Reads the response body to a string.
    abstract text: unit -> JS.Promise<string>

/// <summary>
/// JavaScript/TypeScript implementation of <see cref="T:DataHubClient.IHttpClient"/>
/// backed by the global <c>fetch</c> function — built into modern browsers and
/// Node.js 18+. The implementation is transpiled from this F# source by Fable.
/// Redirects carrying GitLab token headers are followed manually to prevent
/// forwarding credentials to another origin. Browsers hide those redirect
/// destinations, so such redirects fail before the destination is contacted.
/// </summary>
[<AttachMembers>]
type FetchHttpClient() =

    [<Emit("fetch($0, $1)")>]
    static member private Fetch (url: string) (init: obj) : JS.Promise<FetchResponse> = jsNative

    /// Resolves an absolute or relative redirect location against the current URL.
    [<Emit("new URL($0, $1).href")>]
    static member private ResolveUrl(location: string, currentUrl: string) : string = jsNative

    /// Reads the normalized origin (scheme, host, and effective port) of a URL.
    [<Emit("new URL($0).origin")>]
    static member private Origin(url: string) : string = jsNative

    /// Allows HTTP(S) redirect targets without URL credentials or HTTPS downgrades.
    [<Emit("((u, previous) => (u.protocol === 'https:' || u.protocol === 'http:') && !u.username && !u.password && !(previous.protocol === 'https:' && u.protocol === 'http:'))(new URL($1), new URL($0))")>]
    static member private AllowedRedirect(currentUrl: string, nextUrl: string) : bool = jsNative

    /// Reads the redirect location, returning an empty string when it is absent.
    [<Emit("$0.headers.get('location') ?? ''")>]
    static member private Location(response: FetchResponse) : string = jsNative

    /// Releases a redirect response body before requesting the next location.
    [<Emit("($0.body ? $0.body.cancel() : Promise.resolve())")>]
    static member private CancelBody(response: FetchResponse) : JS.Promise<unit> = jsNative

    interface IHttpClient with
        /// <summary>Sends a request through the global <c>fetch</c> function.</summary>
        /// <param name="request">The transport-agnostic request to send.</param>
        /// <returns>An async computation producing the transport-agnostic response.</returns>
        member _.SendAsync(request: HttpRequest) : Async<HttpResponse> =
            async {
                let originalHeaders = request.Headers |> Seq.toList
                let hasGitLabToken =
                    originalHeaders
                    |> List.exists (fun (name, _) ->
                        let name = name.ToLowerInvariant()
                        name = "private-token" || name = "job-token")

                let rec send (url: string) (method': string) (body: string option) (headers: (string * string) list) redirectCount = async {
                    let init =
                        createObj [
                            "method" ==> method'
                            "headers" ==> createObj [ for name, value in headers -> name ==> value ]
                            "redirect" ==> (if hasGitLabToken then "manual" else "follow")
                            match body with
                            | Some content -> "body" ==> content
                            | None -> ()
                        ]

                    let! response = FetchHttpClient.Fetch url init |> Async.AwaitPromise

                    if response.``type`` = "opaqueredirect" then
                        return failwith "Cannot safely follow a browser redirect with PRIVATE-TOKEN or JOB-TOKEN: fetch hides the redirect destination. Use anonymous access or Authorization-based authentication, or perform the request on a server."
                    else
                        let isRedirect =
                            match response.status with
                            | 301 | 302 | 303 | 307 | 308 -> true
                            | _ -> false

                        if hasGitLabToken && isRedirect then
                            let location = FetchHttpClient.Location response
                            if System.String.IsNullOrEmpty location then
                                return response
                            else
                                do! FetchHttpClient.CancelBody response |> Async.AwaitPromise
                                if redirectCount >= 20 then
                                    return failwith "HTTP redirect limit exceeded (20 redirects)."
                                else
                                    let nextUrl = FetchHttpClient.ResolveUrl(location, url)
                                    if not (FetchHttpClient.AllowedRedirect(url, nextUrl)) then
                                        return failwith "Refusing an HTTP redirect with an unsupported scheme, URL credentials, or an HTTPS downgrade."
                                    else
                                        let changesOrigin = FetchHttpClient.Origin(url) <> FetchHttpClient.Origin(nextUrl)
                                        let becomesGet =
                                            ((response.status = 301 || response.status = 302) && method' = "POST")
                                            || (response.status = 303 && method' <> "GET" && method' <> "HEAD")
                                        let nextHeaders =
                                            headers
                                            |> List.filter (fun (name, _) ->
                                                let name = name.ToLowerInvariant()
                                                let credential = name = "authorization" || name = "private-token" || name = "job-token"
                                                let contentHeader =
                                                    name = "content-type" || name = "content-length" || name = "content-encoding"
                                                    || name = "content-language" || name = "content-location"
                                                not (changesOrigin && credential) && not (becomesGet && contentHeader))
                                        return! send nextUrl (if becomesGet then "GET" else method') (if becomesGet then None else body) nextHeaders (redirectCount + 1)
                        else
                            return response
                }

                let! response = send request.Url (request.Method.ToUpperInvariant()) request.Body originalHeaders 0
                let! body = response.text () |> Async.AwaitPromise

                let collected = ResizeArray()
                response.headers.forEach (fun value name -> collected.Add(name, value))

                return HttpResponse(response.status, body, collected)
            }
