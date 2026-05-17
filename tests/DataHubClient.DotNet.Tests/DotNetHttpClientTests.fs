module DataHubClient.DotNet.Tests.DotNetHttpClientTests

open System.Net
open System.Net.Http
open Fable.Pyxpecto
open DataHubClient

let tests =
    testList "DotNetHttpClient" [

        testCaseAsync "maps method, url and request headers onto the outgoing request" <| async {
            let handler = new FakeHttpMessageHandler(HttpStatusCode.OK, "ok", [])
            use httpClient = new HttpClient(handler)
            let transport = DotNetHttpClient(httpClient) :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects", "GET")
            request.Headers <- [ "PRIVATE-TOKEN", "secret-token" ]

            let! _ = transport.SendAsync request

            Expect.equal handler.RequestMethod "GET" "request method"
            Expect.equal handler.RequestUrl "https://hub.example/api/v4/projects" "request url"
            Expect.isTrue
                (handler.RequestHeaders |> Header.contains "PRIVATE-TOKEN" "secret-token")
                "auth header survives"
            Expect.equal handler.RequestBody None "no request body"
        }

        testCaseAsync "sends the body and Content-Type as request content" <| async {
            let handler = new FakeHttpMessageHandler(HttpStatusCode.Created, "{}", [])
            use httpClient = new HttpClient(handler)
            let transport = DotNetHttpClient(httpClient) :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects/42/issues", "POST")
            request.Headers <- [ "PRIVATE-TOKEN", "secret-token"; "Content-Type", "application/json" ]
            request.Body <- Some """{"title":"bug"}"""

            let! _ = transport.SendAsync request

            Expect.equal handler.RequestBody (Some """{"title":"bug"}""") "request body survives"
            Expect.equal handler.RequestContentType (Some "application/json") "content type survives"
            Expect.isTrue
                (handler.RequestHeaders |> Header.contains "PRIVATE-TOKEN" "secret-token")
                "auth header survives alongside content"
        }

        testCaseAsync "maps response status, body and headers back to HttpResponse" <| async {
            let handler =
                new FakeHttpMessageHandler(HttpStatusCode.Created, "created-body", [ "X-Request-Id", "req-7" ])
            use httpClient = new HttpClient(handler)
            let transport = DotNetHttpClient(httpClient) :> IHttpClient

            let! response = transport.SendAsync(HttpRequest("https://hub.example/api/v4/projects", "GET"))

            Expect.equal response.StatusCode 201 "status code"
            Expect.equal response.Body "created-body" "response body"
            Expect.isTrue
                (response.Headers |> Header.contains "X-Request-Id" "req-7")
                "response header survives"
        }
    ]
