module DataHubClient.Tests.Program

open Fable.Pyxpecto

#if !FABLE_COMPILER_JAVASCRIPT && !FABLE_COMPILER_TYPESCRIPT
let (!!) (any: 'a) = any
#endif
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core.JsInterop
#endif

let all = testList "All" [
    AuthenticationTests.tests
    ModelTests.tests
    ResourceTests.tests
]

[<EntryPoint>]
let main argv = !!Pyxpecto.runTests [||] all
