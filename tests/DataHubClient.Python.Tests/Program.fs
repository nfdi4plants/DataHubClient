module DataHubClient.Python.Tests.Program

open Fable.Core.PyInterop
open Fable.Pyxpecto

// The Python test entry point. It runs the shared Pyxpecto suite (transpiled
// from tests/DataHubClient.Tests) plus the two Python-only suites that cannot be
// written once: the HttpxHttpClient transport and the [<AttachMembers>]
// transpiled-shape checks.
let all =
    testList "All" [
        DataHubClient.Tests.AuthenticationTests.tests
        DataHubClient.Tests.ModelTests.tests
        DataHubClient.Tests.ResourceTests.tests
        HttpxHttpClientTests.tests
        TranspileShapeTests.tests
    ]

[<EntryPoint>]
let main _ = !!Pyxpecto.runTests [||] all
