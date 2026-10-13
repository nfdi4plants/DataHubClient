// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client =
    DataHubClient("https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"))
// --8<-- [end:setup]

// Offline harness: validate each request and return a GitLab-shaped fixture.
let responses = System.Collections.Generic.Queue<string * string * int * string>([
    ("GET", "https://hub.example/api/v4/user", 200, """{"id":7,"username":"carl","name":"Carl Correns","state":"active","web_url":"https://hub.example/carl","email":"carl@example.org"}""")
    ("GET", "https://hub.example/api/v4/users/7", 200, """{"id":7,"username":"carl","name":"Carl Correns","state":"active","web_url":"https://hub.example/carl"}""")
])
client.Http <-
    { new IHttpClient with
        member _.SendAsync request = async {
            if responses.Count = 0 then failwith "Unexpected extra request."
            let method, url, status, body = responses.Dequeue()
            if (request.Method, request.Url) <> (method, url) then
                failwithf "Unexpected request: %s %s" request.Method request.Url
            return HttpResponse(status, body, ResizeArray())
        } }

// --8<-- [start:get-current]
let currentUser =
    client.Users.GetCurrentAsync() |> Async.RunSynchronously
printfn "%s (%A)" currentUser.Username currentUser.Email
// --8<-- [end:get-current]

// --8<-- [start:get]
let user =
    client.Users.GetAsync(7) |> Async.RunSynchronously
printfn "%s: %s" user.Username user.WebUrl
// --8<-- [end:get]

if responses.Count <> 0 then failwith "Expected every documented call to run."
