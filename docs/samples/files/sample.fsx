// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client =
    DataHubClient("https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"))
// --8<-- [end:setup]

// Offline harness: validate each request and return a GitLab-shaped fixture.
let responses = System.Collections.Generic.Queue<string * string * int * string>([
    ("GET", "https://hub.example/api/v4/projects/42/repository/files/README.md?ref=main", 200, """{"file_name":"README.md","file_path":"README.md","size":3,"encoding":"base64","content":"QVJD","ref":"main","blob_id":"blob1","commit_id":"commit1"}""")
    ("POST", "https://hub.example/api/v4/projects/42/repository/files/notes.txt", 201, """{"file_path":"notes.txt","branch":"main"}""")
    ("GET", "https://hub.example/api/v4/projects/42/repository/files/notes.txt?ref=main", 200, """{"file_name":"notes.txt","file_path":"notes.txt","size":3,"encoding":"base64","content":"QVJD","ref":"main","blob_id":"blob1","commit_id":"commit1"}""")
    ("PUT", "https://hub.example/api/v4/projects/42/repository/files/notes.txt", 200, """{"file_path":"notes.txt","branch":"main"}""")
    ("GET", "https://hub.example/api/v4/projects/42/repository/files/notes.txt?ref=main", 200, """{"file_name":"notes.txt","file_path":"notes.txt","size":3,"encoding":"base64","content":"QVJD","ref":"main","blob_id":"blob1","commit_id":"commit1"}""")
    ("DELETE", "https://hub.example/api/v4/projects/42/repository/files/notes.txt", 204, """""")
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

// --8<-- [start:get]
let file =
    client.Files.GetAsync(42, "README.md", "main") |> Async.RunSynchronously
let text =
    if file.Encoding = "base64" then
        System.Convert.FromBase64String(file.Content) |> System.Text.Encoding.UTF8.GetString
    else file.Content
printfn "%s" text
// --8<-- [end:get]

// --8<-- [start:create]
let created =
    client.Files.CreateAsync(42, "notes.txt", "main", "ARC", "Add notes") |> Async.RunSynchronously
printfn "Created %s on %s" created.FilePath created.Ref
// --8<-- [end:create]

// --8<-- [start:update]
let updated =
    client.Files.UpdateAsync(42, "notes.txt", "main", "QVJD", "Update notes", encoding = "base64") |> Async.RunSynchronously
printfn "Updated %s at %s" updated.FilePath updated.CommitId
// --8<-- [end:update]

// --8<-- [start:delete]
client.Files.DeleteAsync(42, "notes.txt", "main", "Remove notes") |> Async.RunSynchronously
printfn "Deleted notes.txt"
// --8<-- [end:delete]

if responses.Count <> 0 then failwith "Expected every documented call to run."
