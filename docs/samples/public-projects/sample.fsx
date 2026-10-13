// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let url = "https://git.nfdi4plants.org"
let publicClient = DataHubClient(url, Authentication.Anonymous())
// --8<-- [end:setup]

// Offline CI harness: two deliberately short pages, then an empty final page.
// A short page must not stop ListAllAsync early.
let requests = ResizeArray<HttpRequest>()
let projectJson id =
    $"""[{{"id":{id},"name":"ARC {id}","path":"arc-{id}","path_with_namespace":"examples/arc-{id}","visibility":"public","web_url":"https://git.nfdi4plants.org/examples/arc-{id}"}}]"""

publicClient.Http <-
    { new IHttpClient with
        member _.SendAsync request = async {
            requests.Add request
            let body =
                match requests.Count with
                | 1 -> projectJson 1
                | 2 -> projectJson 2
                | 3 -> "[]"
                | _ -> failwith "Unexpected extra pagination request."
            return HttpResponse(200, body, [])
        } }

// --8<-- [start:list]

let projects =
    publicClient.Projects.ListAllAsync(visibility = "public")
    |> Async.RunSynchronously

printfn $"Found {projects.Length} public projects"
// --8<-- [end:list]

if (projects |> Array.map (fun project -> project.Id)) <> [| 1; 2 |] then
    failwith "Expected both project pages in ascending ID order."
if requests.Count <> 3 then failwith "Expected an empty final page."
for request in requests do
    if request.Method <> "GET" then failwith "Expected GET."
    for query in [ "pagination=keyset"; "order_by=id"; "sort=asc"; "per_page=100"; "visibility=public" ] do
        if not (request.Url.Contains query) then failwith $"Missing query: {query}"
    if request.Headers |> List.exists (fun (name, _) -> name = "PRIVATE-TOKEN" || name = "Authorization" || name = "JOB-TOKEN" || name = "") then
        failwith "Anonymous requests must omit authentication headers."
if requests.[0].Url.Contains "id_after" then failwith "The first page must not have a cursor."
if not (requests.[1].Url.Contains "id_after=1") then failwith "Expected the first cursor."
if not (requests.[2].Url.Contains "id_after=2") then failwith "Expected the second cursor."
