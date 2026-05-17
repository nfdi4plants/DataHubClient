module DataHubClient.JavaScript.Tests.Program

open Fable.Core.JsInterop
open Fable.Pyxpecto

// The JavaScript/TypeScript test entry point. It runs the shared Pyxpecto suite
// (transpiled from tests/DataHubClient.Tests) plus the two JS-only suites that
// cannot be written once: the FetchHttpClient transport and the [<AttachMembers>]
// transpiled-shape checks.
let all =
    testList "All" [
        DataHubClient.Tests.AuthenticationTests.tests
        DataHubClient.Tests.ModelTests.tests
        DataHubClient.Tests.ResourceTests.tests
        FetchHttpClientTests.tests
        TranspileShapeTests.tests
    ]

[<EntryPoint>]
let main _ = !!Pyxpecto.runTests [||] all
