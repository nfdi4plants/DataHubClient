namespace DataHubClient

open Fable.Core

[<AttachMembers>]
type HttpRequest(url: string, method: string) =
    member val Url = url with get, set
    member val Method = method with get, set
    member val Headers : (string * string) list = [] with get, set
    member val Body : string option = None with get, set
