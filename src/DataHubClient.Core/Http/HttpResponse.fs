namespace DataHubClient

open Fable.Core

[<AttachMembers>]
type HttpResponse(statusCode: int, body: string, headers: (string * string) list) =
    member _.StatusCode = statusCode
    member _.Body = body
    member _.Headers = headers
