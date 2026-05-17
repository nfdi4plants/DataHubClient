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

let tests =
    testList "FetchHttpClient" [

        testCaseAsync "maps method, url and request headers onto the fetch call" <| async {
            installFetch 200 "ok" []
            let transport = FetchHttpClient() :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects", "GET")
            request.Headers <- [ "PRIVATE-TOKEN", "secret-token" ]

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
            request.Headers <- [ "Content-Type", "application/json" ]
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
                (response.Headers |> List.contains ("x-request-id", "req-7"))
                "response header survives"
        }
    ]
