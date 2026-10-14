module DataHubClient.DotNet.Tests.RedirectTests

open System
open System.IO
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Text
open System.Threading
open System.Threading.Tasks
open Fable.Pyxpecto
open DataHubClient

/// <summary>A request snapshot retained after the outgoing message is disposed.</summary>
/// <param name="method">The HTTP method.</param>
/// <param name="url">The absolute request URL.</param>
/// <param name="headers">Request and content headers.</param>
/// <param name="body">The request body, when present.</param>
type private RecordedRequest(method: string, url: string, headers: (string * string) list, body: string option) =
    /// <summary>The outgoing HTTP method.</summary>
    member _.Method = method
    /// <summary>The outgoing absolute URL.</summary>
    member _.Url = url
    /// <summary>The outgoing request headers.</summary>
    member _.Headers = headers
    /// <summary>The outgoing body.</summary>
    member _.Body = body

/// <summary>A deterministic handler that observes every manually followed hop.</summary>
/// <param name="responses">The response status, body and headers for each hop.</param>
type private RedirectHandler(responses: (int * string * (string * string) list) list) =
    inherit HttpMessageHandler()
    let requests = ResizeArray<RecordedRequest>()
    let remaining = System.Collections.Generic.Queue<_>(responses)

    /// <summary>Recorded request snapshots in order.</summary>
    member _.Requests = requests

    /// <summary>Records the request and returns the next prepared response.</summary>
    /// <param name="request">The outgoing request.</param>
    /// <param name="cancellationToken">Cancellation for the request.</param>
    override _.SendAsync(request: HttpRequestMessage, cancellationToken: CancellationToken) =
        task {
            let! body =
                task {
                    if isNull request.Content then return None
                    else
                        let! text = request.Content.ReadAsStringAsync(cancellationToken)
                        return Some text
                }
            let headers = [
                for header in request.Headers do
                    for value in header.Value do yield header.Key, value
                if not (isNull request.Content) then
                    for header in request.Content.Headers do
                        for value in header.Value do yield header.Key, value
            ]
            requests.Add(RecordedRequest(request.Method.Method, string request.RequestUri, headers, body))
            let status, body, headers = remaining.Dequeue()
            let response = new HttpResponseMessage(enum<HttpStatusCode> status)
            response.Content <- new StringContent(body)
            for name, value in headers do
                response.Headers.TryAddWithoutValidation(name, value) |> ignore
            return response
        }

/// <summary>A loopback HTTP server exercising the real default .NET handler.</summary>
/// <param name="route">Maps a path to response status, body and headers.</param>
type private LoopbackServer(route: string -> int * string * (string * string) list) =
    let listener = new TcpListener(IPAddress.Loopback, 0)
    let cancellation = new CancellationTokenSource()
    let requests = ResizeArray<RecordedRequest>()
    do listener.Start()
    let baseUrl = sprintf "http://127.0.0.1:%d" (listener.LocalEndpoint :?> IPEndPoint).Port
    let serving = task {
        try
            while not cancellation.IsCancellationRequested do
                use! connection = listener.AcceptTcpClientAsync(cancellation.Token)
                use stream = connection.GetStream()
                use reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true)
                let! firstLine = reader.ReadLineAsync(cancellation.Token)
                let parts = firstLine.Split(' ')
                let headers = ResizeArray<string * string>()
                let mutable readingHeaders = true
                while readingHeaders do
                    let! line = reader.ReadLineAsync(cancellation.Token)
                    if String.IsNullOrEmpty(line) then readingHeaders <- false
                    else
                        let separator = line.IndexOf(':')
                        headers.Add(line.Substring(0, separator), line.Substring(separator + 1).Trim())
                requests.Add(RecordedRequest(parts.[0], baseUrl + parts.[1], Seq.toList headers, None))
                let status, body, responseHeaders = route parts.[1]
                let payload = Encoding.UTF8.GetBytes(body)
                let headerLines =
                    responseHeaders
                    |> List.map (fun (name, value) -> name + ": " + value + "\r\n")
                    |> String.concat ""
                let header =
                    sprintf "HTTP/1.1 %d Test\r\nConnection: close\r\nContent-Length: %d\r\n%s\r\n"
                        status payload.Length headerLines
                do! stream.WriteAsync(Encoding.ASCII.GetBytes(header), cancellation.Token)
                do! stream.WriteAsync(payload, cancellation.Token)
        with
        | :? OperationCanceledException when cancellation.IsCancellationRequested -> ()
        | :? SocketException when cancellation.IsCancellationRequested -> ()
        | :? ObjectDisposedException when cancellation.IsCancellationRequested -> ()
    }

    /// <summary>The server's origin, using an available ephemeral port.</summary>
    member _.BaseUrl = baseUrl
    /// <summary>Requests received by the actual network server.</summary>
    member _.Requests = requests

    interface IDisposable with
        /// <summary>Stops the listener and waits for its request loop to exit.</summary>
        member _.Dispose() =
            cancellation.Cancel()
            listener.Stop()
            serving.GetAwaiter().GetResult()
            cancellation.Dispose()

/// <summary>Whether a request contains a named header, regardless of casing.</summary>
let private hasHeader name (request: RecordedRequest) =
    request.Headers |> List.exists (fun (key, _) -> String.Equals(key, name, StringComparison.OrdinalIgnoreCase))

/// <summary>Dummy credentials used to verify stripping without exposing a real secret.</summary>
let private credentials = [
    "pRiVaTe-ToKeN", "dummy-pat"
    "JoB-ToKeN", "dummy-job"
    "aUtHoRiZaTiOn", "Bearer dummy-oauth"
]

/// <summary>Redirect behavior and credential isolation through real HTTP and deterministic handlers.</summary>
let tests =
    testList "DotNetHttpClient redirects" [
        testCaseAsync "default transport removes credentials before a cross-origin network request" <| async {
            use storage = new LoopbackServer(fun _ -> 200, "{\"@graph\":[]}", [ "X-Object-Store", "fixture" ])
            use hub = new LoopbackServer(fun path ->
                if path = "/same-origin" then 302, "", [ "Location", storage.BaseUrl + "/crate.json" ]
                else 302, "", [ "Location", "/same-origin" ])
            let request = HttpRequest(hub.BaseUrl + "/package", "GET")
            request.Headers <- ResizeArray(credentials @ [ "X-Request-Id", "trace" ])

            let! response = (DotNetHttpClient() :> IHttpClient).SendAsync request

            Expect.equal response.StatusCode 200 "final network status"
            Expect.equal response.Body "{\"@graph\":[]}" "final network body"
            Expect.isTrue (Header.contains "X-Object-Store" "fixture" response.Headers) "final response headers"
            Expect.equal hub.Requests.Count 2 "relative same-origin redirect followed"
            Expect.equal storage.Requests.Count 1 "cross-origin redirect followed"
            for name, value in credentials do
                for received in hub.Requests do
                    Expect.isTrue (Header.contains name value received.Headers) "same-origin credentials retained"
                Expect.isFalse (hasHeader name storage.Requests.[0]) "credentials absent at the storage server"
            Expect.isTrue (Header.contains "X-Request-Id" "trace" storage.Requests.[0].Headers) "ordinary header retained"
            Expect.equal (Seq.toList request.Headers) (credentials @ [ "X-Request-Id", "trace" ]) "caller headers unchanged"
        }

        testCaseAsync "cross-origin return does not restore request credentials" <| async {
            use handler = new RedirectHandler([
                302, "", [ "Location", "https://storage.example/crate" ]
                307, "", [ "Location", "https://hub.example/return" ]
                200, "{}", []
            ])
            use http = new HttpClient(handler)
            let request = HttpRequest("https://hub.example/package", "GET")
            request.Headers <- ResizeArray credentials

            let! _ = (DotNetHttpClient(http) :> IHttpClient).SendAsync request

            for name, _ in credentials do
                Expect.isTrue (hasHeader name handler.Requests.[0]) "initial credentials included"
                for index in [ 1; 2 ] do
                    Expect.isFalse (hasHeader name handler.Requests.[index]) "stripped credentials never reappear"
        }

        testCaseAsync "real custom HttpClient defaults reject unsafe cross-origin redirects" <| async {
            use storage = new LoopbackServer(fun _ -> 200, "{}", [])
            use hub = new LoopbackServer(fun _ -> 302, "", [ "Location", storage.BaseUrl + "/crate" ])
            use http = new HttpClient(new HttpClientHandler(AllowAutoRedirect = false))
            for name, value in credentials do
                http.DefaultRequestHeaders.TryAddWithoutValidation(name, value) |> ignore

            try
                let! _ = (DotNetHttpClient(http) :> IHttpClient).SendAsync(HttpRequest(hub.BaseUrl + "/package", "GET"))
                failwith "Expected default-credential redirect rejection"
            with
            | :? HttpRequestException as error ->
                Expect.isTrue (error.Message.Contains("DefaultRequestHeaders")) "error explains the unsafe client configuration"

            for name, value in credentials do
                Expect.isTrue (Header.contains name value hub.Requests.[0].Headers) "initial defaults included"
                Expect.isTrue (http.DefaultRequestHeaders.Contains(name)) "shared client defaults unchanged"
            Expect.equal storage.Requests.Count 0 "no credential-bearing request can reach storage"
        }

        testCaseAsync "RO-Crate downloads preserve the final network error" <| async {
            use storage = new LoopbackServer(fun _ -> 404, "missing crate", [])
            use hub = new LoopbackServer(fun _ -> 302, "redirect body", [ "Location", storage.BaseUrl + "/missing" ])
            let client = DataHubClient(hub.BaseUrl, Authentication.Anonymous())
            try
                let! _ = client.Packages.DownloadROCrateAsync(1040)
                failwith "Expected NotFoundError"
            with
            | :? NotFoundError as error ->
                Expect.equal error.StatusCode 404 "final error status"
                Expect.equal error.Body "missing crate" "final error body"
        }

        testCaseAsync "normalized same-origin URLs retain tokens, but another port strips them" <| async {
            use handler = new RedirectHandler([
                301, "", [ "Location", "https://HUB.EXAMPLE:443/same" ]
                302, "", [ "Location", "https://hub.example:8443/other" ]
                200, "{}", []
            ])
            use http = new HttpClient(handler)
            let request = HttpRequest("https://hub.example/package", "GET")
            request.Headers <- ResizeArray credentials
            let! _ = (DotNetHttpClient(http) :> IHttpClient).SendAsync request
            for name, value in credentials do
                Expect.isTrue (Header.contains name value handler.Requests.[1].Headers) "default port and hostname casing normalize"
                Expect.isFalse (hasHeader name handler.Requests.[2]) "a different port is another origin"
        }

        testCaseAsync "redirect method rules preserve or discard the body as required" <| async {
            for status, method, expectedMethod in [
                301, "POST", "GET"; 302, "POST", "GET"; 303, "PUT", "GET"
                301, "PUT", "PUT"; 302, "PATCH", "PATCH"; 303, "HEAD", "HEAD"
                307, "POST", "POST"; 308, "PUT", "PUT"
            ] do
                use handler = new RedirectHandler([ status, "", [ "Location", "../final" ]; 200, "ok", [] ])
                use http = new HttpClient(handler)
                let request = HttpRequest("https://hub.example/packages/file", method)
                request.Body <- Some "{\"name\":\"crate\"}"
                request.Headers <- ResizeArray [ "Content-Type", "application/json"; "X-Request-Id", "trace" ]
                let! _ = (DotNetHttpClient(http) :> IHttpClient).SendAsync request
                let redirected = handler.Requests.[1]
                Expect.equal redirected.Url "https://hub.example/final" "relative Location resolved"
                Expect.equal redirected.Method expectedMethod "redirect method"
                let expectedBody = if expectedMethod = "GET" then None else request.Body
                Expect.equal redirected.Body expectedBody "redirect body"
                Expect.equal (hasHeader "Content-Type" redirected) expectedBody.IsSome "content headers follow the body"
                Expect.isTrue (Header.contains "X-Request-Id" "trace" redirected.Headers) "ordinary header survives method rewrite"
                Expect.equal request.Method method "caller method unchanged"
                Expect.equal request.Body (Some "{\"name\":\"crate\"}") "caller body unchanged"
        }

        testCaseAsync "a redirect without Location remains an HTTP response" <| async {
            use handler = new RedirectHandler([ 302, "no destination", [] ])
            use http = new HttpClient(handler)
            let! response = (DotNetHttpClient(http) :> IHttpClient).SendAsync(HttpRequest("https://hub.example/file", "GET"))
            Expect.equal response.StatusCode 302 "unfollowable status preserved"
            Expect.equal response.Body "no destination" "unfollowable body preserved"
            Expect.equal handler.Requests.Count 1 "no fabricated follow-up request"
        }

        testCaseAsync "redirect loops stop after twenty hops" <| async {
            use handler = new RedirectHandler(List.replicate 21 (302, "", [ "Location", "/loop" ]))
            use http = new HttpClient(handler)
            try
                let! _ = (DotNetHttpClient(http) :> IHttpClient).SendAsync(HttpRequest("https://hub.example/loop", "GET"))
                failwith "Expected redirect limit failure"
            with
            | :? HttpRequestException as error -> Expect.isTrue (error.Message.Contains("20 redirects")) "bounded redirect error"
            Expect.equal handler.Requests.Count 21 "initial request plus twenty redirects"
        }

        testCaseAsync "downgrades and non-HTTP destinations are rejected before sending" <| async {
            for location in [ "http://hub.example/insecure"; "file:///tmp/crate" ] do
                use handler = new RedirectHandler([ 302, "", [ "Location", location ] ])
                use http = new HttpClient(handler)
                try
                    let! _ = (DotNetHttpClient(http) :> IHttpClient).SendAsync(HttpRequest("https://hub.example/package", "GET"))
                    failwith "Expected unsafe redirect rejection"
                with
                | :? HttpRequestException -> ()
                Expect.equal handler.Requests.Count 1 "unsafe target never requested"
        }
    ]
