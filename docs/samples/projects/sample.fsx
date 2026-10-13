// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client =
    DataHubClient("https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"))
// --8<-- [end:setup]

// Offline harness: validate each request and return a GitLab-shaped fixture.
let responses = System.Collections.Generic.Queue<string * string * int * string>([
    ("GET", "https://hub.example/api/v4/projects?search=plant%20arc&simple=true", 200, """[{"id":42,"name":"My ARC","path":"my-arc","path_with_namespace":"lab/my-arc","visibility":"public","web_url":"https://hub.example/lab/my-arc","description":"An annotated research context","default_branch":"main"}]""")
    ("GET", "https://hub.example/api/v4/projects?order_by=id&pagination=keyset&per_page=100&sort=asc&visibility=public", 200, """[{"id":42,"name":"My ARC","path":"my-arc","path_with_namespace":"lab/my-arc","visibility":"public","web_url":"https://hub.example/lab/my-arc","description":"An annotated research context","default_branch":"main"}]""")
    ("GET", "https://hub.example/api/v4/projects?id_after=42&order_by=id&pagination=keyset&per_page=100&sort=asc&visibility=public", 200, """[]""")
    ("GET", "https://hub.example/api/v4/projects/42", 200, """{"id":42,"name":"My ARC","path":"my-arc","path_with_namespace":"lab/my-arc","visibility":"public","web_url":"https://hub.example/lab/my-arc","description":"An annotated research context","default_branch":"main"}""")
    ("GET", "https://hub.example/api/v4/projects?search=plant%20arc", 200, """[{"id":42,"name":"My ARC","path":"my-arc","path_with_namespace":"lab/my-arc","visibility":"public","web_url":"https://hub.example/lab/my-arc","description":"An annotated research context","default_branch":"main"}]""")
])
client.Http <-
    { new IHttpClient with
        member _.SendAsync request = async {
            if responses.Count = 0 then failwith "Unexpected extra request."
            let method, url, status, body = responses.Dequeue()
            if (request.Method, request.Url) <> (method, url) then
                failwithf "Unexpected request: %s %s" request.Method request.Url
            return HttpResponse(status, body, [])
        } }

// --8<-- [start:list]
let projects =
    client.Projects.ListAsync(search = "plant arc", simple = true) |> Async.RunSynchronously
for project in projects do printfn "%d: %s" project.Id project.PathWithNamespace
// --8<-- [end:list]

// --8<-- [start:list-all]
let allProjects =
    client.Projects.ListAllAsync(visibility = "public") |> Async.RunSynchronously
printfn "Found %d public projects" allProjects.Length
// --8<-- [end:list-all]

// --8<-- [start:get]
let project =
    client.Projects.GetAsync(42) |> Async.RunSynchronously
printfn "%s: %s" project.Name project.WebUrl
// --8<-- [end:get]

// --8<-- [start:search]
let matches =
    client.Projects.SearchAsync("plant arc") |> Async.RunSynchronously
for project in matches do printfn "%s" project.PathWithNamespace
// --8<-- [end:search]

if responses.Count <> 0 then failwith "Expected every documented call to run."
