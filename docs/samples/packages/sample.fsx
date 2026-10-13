// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client = DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"))
let projectId = 42
// --8<-- [end:setup]

// Offline verification harness, omitted from the documentation snippets.
let responses =
    System.Collections.Generic.Queue<string * string>([|
        "GET", """[{"id":9,"name":"arc-export","version":"1.0.0","package_type":"generic","status":"default","created_at":"2026-10-13T10:00:00Z"}]"""
        "GET", """{"id":9,"name":"arc-export","version":"1.0.0","package_type":"generic","status":"default","created_at":"2026-10-13T10:00:00Z"}"""
        "PUT", """{"message":"201 Created"}"""
        "GET", """{"name":"My ARC"}"""
    |])

client.Http <-
    { new IHttpClient with
        member _.SendAsync request = async {
            if responses.Count = 0 then failwith "Unexpected extra HTTP request."
            let expectedMethod, body = responses.Dequeue()
            if request.Method <> expectedMethod then failwith $"Expected {expectedMethod}, got {request.Method}."
            return HttpResponse(200, body, ResizeArray())
        } }

// --8<-- [start:list]
let packages =
    client.Packages.ListAsync(projectId, packageType = "generic", packageName = "arc-export")
    |> Async.RunSynchronously
for package in packages do printfn $"{package.Name}@{package.Version}"
// --8<-- [end:list]

// --8<-- [start:get]
let package = client.Packages.GetAsync(projectId, 9) |> Async.RunSynchronously
printfn "%s" package.Version
// --8<-- [end:get]

// --8<-- [start:upload]
let uploadResponse =
    client.Packages.UploadGenericFileAsync(projectId, "arc-export", "1.0.0", "manifest.json", """{"name":"My ARC"}""")
    |> Async.RunSynchronously
printfn "%s" uploadResponse
// --8<-- [end:upload]

// --8<-- [start:download]
let content =
    client.Packages.DownloadGenericFileAsync(projectId, "arc-export", "1.0.0", "manifest.json")
    |> Async.RunSynchronously
printfn "%s" content
// --8<-- [end:download]

if responses.Count <> 0 then failwith "Expected every sample request to run."
if package.Version <> "1.0.0" || content <> """{"name":"My ARC"}""" then
    failwith "Unexpected package metadata or file content."
