module DataHubClient.DotNet.Tests.DataHubClientTests

open System.Net
open System.Net.Http
open Fable.Pyxpecto
open DataHubClient

let private baseUrl = "https://hub.example/"
let private auth = Authentication.PersonalAccessToken("token")

/// A single GitLab-shaped project payload matching DataHubClient.Project's decoder.
let private projectJson =
    """{"id":42,"name":"My ARC","path":"my-arc","path_with_namespace":"lab/my-arc","visibility":"private","web_url":"https://hub.example/lab/my-arc","default_branch":"main"}"""

let tests =
    testList "DataHubClient" [

        testCase "two-argument constructor wires resource APIs with the default .NET transport" <| fun () ->
            let client = DataHubClient(baseUrl, auth)
            Expect.isTrue (not (isNull (box client.Projects))) "Projects API is wired"
            Expect.isTrue (not (isNull (box client.Packages))) "Packages API is wired"

        testCaseAsync "routes resource calls through a caller-supplied transport" <| async {
            let handler = new FakeHttpMessageHandler(HttpStatusCode.OK, projectJson, [])
            use httpClient = new HttpClient(handler)
            let client = DataHubClient(baseUrl, auth)
            client.Http <- DotNetHttpClient(httpClient)

            let! project = client.Projects.GetAsync(42)

            Expect.equal project.Id 42 "decoded project id"
            Expect.equal project.PathWithNamespace "lab/my-arc" "decoded project path"
            Expect.equal handler.RequestMethod "GET" "request method"
            Expect.equal handler.RequestUrl "https://hub.example/api/v4/projects/42" "request url"
            Expect.isTrue
                (handler.RequestHeaders |> Header.contains "PRIVATE-TOKEN" "token")
                "auth header reaches the transport"
        }
    ]
