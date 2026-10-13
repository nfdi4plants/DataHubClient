module DataHubClient.Tests.AuthenticationTests

open Fable.Pyxpecto
open DataHubClient

let tests =
    testList "Authentication" [
        testCase "Anonymous has no authentication header" <| fun () ->
            let auth = Authentication.Anonymous()
            Expect.equal auth.Header "" "no header name"
            Expect.equal auth.Value "" "no header value"

        testCase "PersonalAccessToken sets PRIVATE-TOKEN header" <| fun () ->
            let auth = Authentication.PersonalAccessToken("abc123")
            Expect.equal auth.Header "PRIVATE-TOKEN" "header name"
            Expect.equal auth.Value "abc123" "header value"

        testCase "OAuthToken sets Authorization Bearer" <| fun () ->
            let auth = Authentication.OAuthToken("xyz")
            Expect.equal auth.Header "Authorization" "header name"
            Expect.equal auth.Value "Bearer xyz" "header value"

        testCase "JobToken sets JOB-TOKEN header" <| fun () ->
            let auth = Authentication.JobToken("ci-token")
            Expect.equal auth.Header "JOB-TOKEN" "header name"
            Expect.equal auth.Value "ci-token" "header value"
    ]
