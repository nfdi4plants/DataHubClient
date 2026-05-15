module DataHubClient.Tests.TestJson

open Thoth.Json.Core

#if FABLE_COMPILER_PYTHON
open Thoth.Json.Python
#endif
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Thoth.Json.JavaScript
#endif
#if !FABLE_COMPILER
open Thoth.Json.Newtonsoft
#endif

/// Encode a value to its JSON string form and decode it straight back, using
/// whichever Thoth.Json runtime the host target provides. Fails the test if the
/// decode step does not succeed.
let roundTrip (encoder: 'T -> IEncodable) (decoder: Decoder<'T>) (value: 'T) : 'T =
    let json = Encode.toString 0 (encoder value)
    match Decode.fromString decoder json with
    | Ok decoded -> decoded
    | Error err -> failwith ("decode failed: " + err)
