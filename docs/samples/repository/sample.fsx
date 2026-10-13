// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client =
    DataHubClient("https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"))
// --8<-- [end:setup]

// Offline harness: validate each request and return a GitLab-shaped fixture.
let responses = System.Collections.Generic.Queue<string * string * int * string>([
    ("GET", "https://hub.example/api/v4/projects/42/repository/branches?search=main", 200, """[{"name":"main","default":true,"protected":true,"merged":false,"commit":{"id":"abc123def456","short_id":"abc123de","title":"Add ARC metadata","message":"Add ARC metadata","author_name":"Carl Correns","author_email":"carl@example.org","created_at":"2026-05-01T00:00:00Z"}}]""")
    ("GET", "https://hub.example/api/v4/projects/42/repository/branches/main", 200, """{"name":"main","default":true,"protected":true,"merged":false,"commit":{"id":"abc123def456","short_id":"abc123de","title":"Add ARC metadata","message":"Add ARC metadata","author_name":"Carl Correns","author_email":"carl@example.org","created_at":"2026-05-01T00:00:00Z"}}""")
    ("GET", "https://hub.example/api/v4/projects/42/repository/commits?path=README.md&ref_name=main", 200, """[{"id":"abc123def456","short_id":"abc123de","title":"Add ARC metadata","message":"Add ARC metadata","author_name":"Carl Correns","author_email":"carl@example.org","created_at":"2026-05-01T00:00:00Z"}]""")
    ("GET", "https://hub.example/api/v4/projects/42/repository/commits/abc123def456", 200, """{"id":"abc123def456","short_id":"abc123de","title":"Add ARC metadata","message":"Add ARC metadata","author_name":"Carl Correns","author_email":"carl@example.org","created_at":"2026-05-01T00:00:00Z"}""")
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

// --8<-- [start:list-branches]
let branches =
    client.Repository.ListBranchesAsync(42, search = "main") |> Async.RunSynchronously
for branch in branches do printfn "%s: %s" branch.Name branch.Commit.ShortId
// --8<-- [end:list-branches]

// --8<-- [start:get-branch]
let branch =
    client.Repository.GetBranchAsync(42, "main") |> Async.RunSynchronously
printfn "%s points to %s" branch.Name branch.Commit.Id
// --8<-- [end:get-branch]

// --8<-- [start:list-commits]
let commits =
    client.Repository.ListCommitsAsync(42, refName = "main", path = "README.md") |> Async.RunSynchronously
for commit in commits do printfn "%s: %s" commit.ShortId commit.Title
// --8<-- [end:list-commits]

// --8<-- [start:get-commit]
let commit =
    client.Repository.GetCommitAsync(42, "abc123def456") |> Async.RunSynchronously
printfn "%s by %s" commit.Title commit.AuthorName
// --8<-- [end:get-commit]

if responses.Count <> 0 then failwith "Expected every documented call to run."
