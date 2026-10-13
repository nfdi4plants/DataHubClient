// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client = DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"))
let projectId = 42
// --8<-- [end:setup]

// Offline verification harness, omitted from the documentation snippets.
let responses =
    System.Collections.Generic.Queue<string * string>([|
        "GET", """[{"id":100,"iid":3,"project_id":42,"title":"Add assay metadata","state":"opened","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"assignees":[],"labels":[],"web_url":"https://git.nfdi4plants.org/examples/arc/-/issues/3","created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z","description":"Describe the assay."}]"""
        "GET", """{"id":100,"iid":3,"project_id":42,"title":"Add assay metadata","state":"opened","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"assignees":[],"labels":[],"web_url":"https://git.nfdi4plants.org/examples/arc/-/issues/3","created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z","description":"Describe the assay."}"""
        "POST", """{"id":100,"iid":3,"project_id":42,"title":"Add assay metadata","state":"opened","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"assignees":[],"labels":[],"web_url":"https://git.nfdi4plants.org/examples/arc/-/issues/3","created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z","description":"Describe the assay."}"""
        "PUT", """{"id":100,"iid":3,"project_id":42,"title":"Complete assay metadata","state":"opened","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"assignees":[],"labels":[],"web_url":"https://git.nfdi4plants.org/examples/arc/-/issues/3","created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z","description":"Add protocol details."}"""
        "PUT", """{"id":100,"iid":3,"project_id":42,"title":"Add assay metadata","state":"closed","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"assignees":[],"labels":[],"web_url":"https://git.nfdi4plants.org/examples/arc/-/issues/3","created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z","description":"Describe the assay."}"""
        "GET", """[{"id":5,"body":"Metadata reviewed.","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"system":false,"created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z"}]"""
        "POST", """{"id":5,"body":"Metadata reviewed.","author":{"id":7,"username":"researcher","name":"ARC Researcher","state":"active","web_url":"https://git.nfdi4plants.org/researcher"},"system":false,"created_at":"2026-10-13T10:00:00Z","updated_at":"2026-10-13T10:00:00Z"}"""
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
let issues = client.Issues.ListAsync(projectId, state = "opened") |> Async.RunSynchronously
for item in issues do printfn $"{item.Iid}: {item.Title}"
// --8<-- [end:list]

// --8<-- [start:get]
let issue = client.Issues.GetAsync(projectId, 3) |> Async.RunSynchronously
printfn "%s" issue.Title
// --8<-- [end:get]

// --8<-- [start:create]
let created =
    client.Issues.CreateAsync(projectId, "Add assay metadata", description = "Describe the assay.")
    |> Async.RunSynchronously
printfn "%s" created.WebUrl
// --8<-- [end:create]

// --8<-- [start:update]
let updated =
    client.Issues.UpdateAsync(projectId, 3, title = "Complete assay metadata", description = "Add protocol details.")
    |> Async.RunSynchronously
printfn "%s" updated.Title
// --8<-- [end:update]

// --8<-- [start:close]
let closed = client.Issues.CloseAsync(projectId, 3) |> Async.RunSynchronously
printfn "%s" closed.State
// --8<-- [end:close]

// --8<-- [start:notes]
let notes = client.Issues.NotesAsync(projectId, 3) |> Async.RunSynchronously
for note in notes do printfn "%s" note.Body
// --8<-- [end:notes]

// --8<-- [start:create-note]
let note = client.Issues.CreateNoteAsync(projectId, 3, "Metadata reviewed.") |> Async.RunSynchronously
printfn "%s" note.Body
// --8<-- [end:create-note]

if responses.Count <> 0 then failwith "Expected every sample request to run."
if closed.State <> "closed" || updated.Title <> "Complete assay metadata" || note.Body <> "Metadata reviewed." then
    failwith "Unexpected decoded issue response."
