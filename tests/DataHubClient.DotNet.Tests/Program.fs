module DataHubClient.DotNet.Tests.Program

open Fable.Pyxpecto

let all =
    testList "All" [
        DotNetHttpClientTests.tests
        DataHubClientTests.tests
    ]

[<EntryPoint>]
let main argv = Pyxpecto.runTests [||] all
