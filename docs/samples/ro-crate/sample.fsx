// --8<-- [start:setup]
#r "nuget: DataHubClient"

open System.IO
open System.Text
open DataHubClient

let client = DataHubClient("https://git.nfdi4plants.org", Authentication.Anonymous())
let projectId = 42
// --8<-- [end:setup]

// Offline harness: validate the real export coordinates without network access.
let fixture = """{"@context":"https://w3id.org/ro/crate/1.1/context","@graph":[{"@id":"./","@type":"Dataset","name":"Pflanzen – étude"}]}"""
let expectedUrl = "https://git.nfdi4plants.org/api/v4/projects/42/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json"
let mutable requestCount = 0
client.Http <-
    { new IHttpClient with
        member _.SendAsync request = async {
            requestCount <- requestCount + 1
            if request.Method <> "GET" || request.Url <> expectedUrl then
                failwithf "Unexpected request: %s %s" request.Method request.Url
            if request.Headers <> [ (DataHubClientVersion.HeaderName, DataHubClientVersion.Value) ] then
                failwith "Expected an anonymous request with only the client-version header."
            return HttpResponse(200, fixture, [])
        } }

let originalDirectory = Directory.GetCurrentDirectory()
let temporaryDirectory = Directory.CreateTempSubdirectory("datahubclient-ro-crate-")
Directory.SetCurrentDirectory temporaryDirectory.FullName

// --8<-- [start:download]

let metadata =
    client.Packages.DownloadROCrateAsync(projectId)
    |> Async.RunSynchronously

File.WriteAllText("arc-ro-crate-metadata.json", metadata, UTF8Encoding(false))
printfn "Saved arc-ro-crate-metadata.json"
// --8<-- [end:download]

try
    if requestCount <> 1 then failwith "Expected one export download."
    let expectedBytes = UTF8Encoding(false).GetBytes fixture
    if File.ReadAllBytes "arc-ro-crate-metadata.json" <> expectedBytes then
        failwith "Saved RO-Crate content differs from the downloaded UTF-8 text."
finally
    Directory.SetCurrentDirectory originalDirectory
    temporaryDirectory.Delete(true)
