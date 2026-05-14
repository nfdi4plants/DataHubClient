namespace DataHubClient

open Fable.Core

[<AttachMembers>]
type Authentication private (header: string, value: string) =
    member _.Header = header
    member _.Value = value

    static member PersonalAccessToken(token: string) =
        Authentication("PRIVATE-TOKEN", token)

    static member OAuthToken(token: string) =
        Authentication("Authorization", "Bearer " + token)

    static member JobToken(token: string) =
        Authentication("JOB-TOKEN", token)
