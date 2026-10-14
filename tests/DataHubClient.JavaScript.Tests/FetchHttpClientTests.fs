module DataHubClient.JavaScript.Tests.FetchHttpClientTests

open Fable.Core
open Fable.Core.JsInterop
open Fable.Pyxpecto
open DataHubClient

// FetchHttpClient cannot enter the shared Pyxpecto suite: it calls the global
// `fetch`, which has no .NET equivalent. This target-only suite stands in a fake
// `fetch` — mirroring the fake HttpMessageHandler in DataHubClient.DotNet.Tests —
// records the outgoing call, and asserts the request/response mapping.

[<Emit("Promise.resolve($0)")>]
let private resolved (value: 'T) : JS.Promise<'T> = jsNative

[<Emit("new Headers($0)")>]
let private makeHeaders (init: obj) : obj = jsNative

[<Emit("globalThis.fetch = $0")>]
let private setFetch (fetch: System.Func<string, obj, JS.Promise<obj>>) : unit = jsNative

/// <summary>Captures the native fetch so each redirect fixture can restore it.</summary>
[<Emit("globalThis.fetch")>]
let private getFetch () : System.Func<string, obj, JS.Promise<obj>> = jsNative

let private nativeFetch = getFetch ()

let mutable private lastUrl = ""
let mutable private lastInit: obj = null

/// Installs a fake global `fetch` that records its arguments and resolves to a
/// canned response built from the given status, body, and headers.
let private installFetch (status: int) (body: string) (headers: (string * string) list) : unit =
    let response =
        createObj [
            "status" ==> status
            "headers" ==> makeHeaders (createObj [ for name, value in headers -> name ==> value ])
            "text" ==> (fun () -> resolved body)
        ]

    setFetch (
        System.Func<string, obj, JS.Promise<obj>>(fun url init ->
            lastUrl <- url
            lastInit <- init
            resolved response)
    )

/// <summary>Runs redirect cases against a sequence of responses and records every hop.</summary>
let private withRedirects
    (responses: (int * string * (string * string) list) list)
    (work: ResizeArray<string * obj> -> Async<unit>) = async {
    let requests = ResizeArray<string * obj>()
    let remaining = ResizeArray(responses)
    setFetch (System.Func<string, obj, JS.Promise<obj>>(fun url init ->
        requests.Add(url, init)
        if remaining.Count = 0 then failwith "Unexpected redirect request"
        let status, responseType, headers = remaining.[0]
        remaining.RemoveAt 0
        resolved (createObj [
            "status" ==> status
            "type" ==> responseType
            "headers" ==> makeHeaders (createObj [ for name, value in headers -> name ==> value ])
            "text" ==> (fun () -> resolved "final-body")
        ])))
    try
        do! work requests
    finally
        setFetch nativeFetch
}

let tests =
    testList "FetchHttpClient" [

        testCaseAsync "maps method, url and request headers onto the fetch call" <| async {
            installFetch 200 "ok" []
            let transport = FetchHttpClient() :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects", "GET")
            request.Headers <- ResizeArray([ "PRIVATE-TOKEN", "secret-token" ])

            let! _ = transport.SendAsync request

            let method': string = lastInit?method
            let authHeader: string = lastInit?headers?("PRIVATE-TOKEN")
            Expect.equal lastUrl "https://hub.example/api/v4/projects" "request url"
            Expect.equal method' "GET" "request method"
            Expect.equal authHeader "secret-token" "auth header survives"
        }

        testCaseAsync "sends the request body through fetch" <| async {
            installFetch 201 "{}" []
            let transport = FetchHttpClient() :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects/42/issues", "POST")
            request.Headers <- ResizeArray([ "Content-Type", "application/json" ])
            request.Body <- Some """{"title":"bug"}"""

            let! _ = transport.SendAsync request

            let body: string = lastInit?body
            let contentType: string = lastInit?headers?("Content-Type")
            Expect.equal body """{"title":"bug"}""" "request body survives"
            Expect.equal contentType "application/json" "content type survives"
        }

        testCaseAsync "maps the fetch response status, body and headers to HttpResponse" <| async {
            installFetch 201 "created-body" [ "x-request-id", "req-7" ]
            let transport = FetchHttpClient() :> IHttpClient

            let! response = transport.SendAsync(HttpRequest("https://hub.example/api/v4/projects", "GET"))

            Expect.equal response.StatusCode 201 "status code"
            Expect.equal response.Body "created-body" "response body"
            Expect.isTrue
                (response.Headers |> Seq.contains ("x-request-id", "req-7"))
                "response header survives"
        }

        testCaseAsync "custom tokens select manual redirects and are stripped permanently across origins" <|
            withRedirects [
                302, "basic", [ "location", "/same" ]
                302, "basic", [ "location", "https://storage.example/file" ]
                302, "basic", [ "location", "https://hub.example/back" ]
                200, "basic", []
            ] (fun requests -> async {
                let request = HttpRequest("https://hub.example/start", "GET")
                request.Headers <- ResizeArray [ "pRiVaTe-ToKeN", "secret"; "JOB-TOKEN", "job"; "Authorization", "Bearer oauth"; "X-Ordinary", "kept" ]
                let! response = (FetchHttpClient() :> IHttpClient).SendAsync request
                Expect.equal response.Body "final-body" "final body"
                Expect.equal requests.Count 4 "all hops"
                for i in 0 .. 3 do
                    let _, init = requests.[i]
                    Expect.equal (init?redirect : string) "manual" "manual for the whole chain"
                    let headers: obj = init?headers
                    Expect.equal (headers?("X-Ordinary") : string) "kept" "ordinary header preserved"
                    for name in [ "pRiVaTe-ToKeN"; "JOB-TOKEN"; "Authorization" ] do
                        let present: bool = emitJsExpr (headers, name) "Object.hasOwn($0, $1)"
                        Expect.equal present (i < 2) "credentials stay only at the original origin before crossing"
                Expect.equal (fst requests.[1]) "https://hub.example/same" "relative redirect"
                Expect.equal request.Headers.Count 4 "caller headers unchanged"
            })

        testCaseAsync "opaque browser redirects stop before another request" <|
            withRedirects [ 0, "opaqueredirect", [] ] (fun requests -> async {
                let request = HttpRequest("https://hub.example/start", "GET")
                request.Headers <- ResizeArray [ "JOB-TOKEN", "secret" ]
                let! result = (FetchHttpClient() :> IHttpClient).SendAsync request |> Async.Catch
                match result with
                | Choice1Of2 _ -> failwith "expected opaque redirect failure"
                | Choice2Of2 error -> Expect.isTrue (error.Message.Contains "Cannot safely follow") "actionable error"
                Expect.equal requests.Count 1 "redirect destination never requested"
            })

        testCaseAsync "POST 302 becomes GET without request content headers" <|
            withRedirects [ 302, "basic", [ "location", "/next" ]; 200, "basic", [] ] (fun requests -> async {
                let request = HttpRequest("https://hub.example/start", "POST")
                request.Headers <- ResizeArray [ "PRIVATE-TOKEN", "secret"; "Content-Type", "application/json"; "Content-Language", "en" ]
                request.Body <- Some "{}"
                let! _ = (FetchHttpClient() :> IHttpClient).SendAsync request
                let _, init = requests.[1]
                Expect.equal (init?method : string) "GET" "redirect method"
                Expect.isFalse (emitJsExpr init "Object.hasOwn($0, 'body')") "body removed"
                Expect.isFalse (emitJsExpr init "Object.hasOwn($0.headers, 'Content-Type')") "content type removed"
                Expect.isFalse (emitJsExpr init "Object.hasOwn($0.headers, 'Content-Language')") "content language removed"
            })

        testCaseAsync "307 preserves method and body while removing credentials on another origin" <|
            withRedirects [ 307, "basic", [ "location", "https://storage.example/file" ]; 200, "basic", [] ] (fun requests -> async {
                let request = HttpRequest("https://hub.example/start", "PUT")
                request.Headers <- ResizeArray [ "PRIVATE-TOKEN", "secret"; "Content-Type", "text/plain" ]
                request.Body <- Some "payload"
                let! _ = (FetchHttpClient() :> IHttpClient).SendAsync request
                let _, init = requests.[1]
                Expect.equal (init?method : string) "PUT" "method preserved"
                Expect.equal (init?body : string) "payload" "body preserved"
                Expect.equal (init?headers?("Content-Type") : string) "text/plain" "content type preserved"
                Expect.isFalse (emitJsExpr init "Object.hasOwn($0.headers, 'PRIVATE-TOKEN')") "token removed"
            })

        testCaseAsync "unsafe redirect destinations are rejected before sending credentials" <| async {
            for location in [ "http://hub.example/insecure"; "https://user:password@storage.example/file"; "file:///tmp/export" ] do
                do! withRedirects [ 302, "basic", [ "location", location ] ] (fun requests -> async {
                    let request = HttpRequest("https://hub.example/start", "GET")
                    request.Headers <- ResizeArray [ "PRIVATE-TOKEN", "secret" ]
                    let! result = (FetchHttpClient() :> IHttpClient).SendAsync request |> Async.Catch
                    match result with
                    | Choice1Of2 _ -> failwith "expected unsafe redirect rejection"
                    | Choice2Of2 _ -> ()
                    Expect.equal requests.Count 1 "unsafe destination never requested"
                })
        }

        testCaseAsync "manual redirects are bounded" <|
            withRedirects (List.replicate 21 (302, "basic", [ "location", "/loop" ])) (fun requests -> async {
                let request = HttpRequest("https://hub.example/loop", "GET")
                request.Headers <- ResizeArray [ "PRIVATE-TOKEN", "secret" ]
                let! result = (FetchHttpClient() :> IHttpClient).SendAsync request |> Async.Catch
                match result with
                | Choice1Of2 _ -> failwith "expected redirect limit"
                | Choice2Of2 error -> Expect.isTrue (error.Message.Contains "redirect limit") "bounded error"
                Expect.equal requests.Count 21 "initial request and at most twenty follows"
            })
    ]
