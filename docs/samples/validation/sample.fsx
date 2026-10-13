// --8<-- [start:setup]
#r "nuget: DataHubClient"

open DataHubClient

let client = DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"))
let projectId = 42
// --8<-- [end:setup]

// Offline verification harness, omitted from the documentation snippets.
let responses =
    System.Collections.Generic.Queue<string * string>([|
        "GET", """[{"name":"main","type":"tree"},{"name":"README.md","type":"blob"}]"""
        "GET", """[{"name":"invenio@3.1.0","type":"tree"}]"""
        "GET", """[{"id":"abc123def456","short_id":"abc123de","title":"Update validation results","message":"Update validation results","author_name":"ARC Researcher","author_email":"researcher@example.org","created_at":"2026-10-13T10:00:00Z"}]"""
        "GET", """{"Critical":{"HasFailures":false,"Total":10,"Passed":10,"Failed":0,"Errored":0},"NonCritical":{"HasFailures":true,"Total":4,"Passed":3,"Failed":1,"Errored":0},"ValidationPackage":{"Name":"invenio","Version":"3.1.0","Summary":"Checks ARC publication requirements."}}"""
        "GET", """<testsuites><testsuite name="invenio" tests="10" failures="0"/></testsuites>"""
        "GET", """<svg xmlns="http://www.w3.org/2000/svg"><text>passed</text></svg>"""
        "GET", """[{"name":"invenio@3.1.0","type":"tree"}]"""
        "GET", """{"Critical":{"HasFailures":false,"Total":10,"Passed":10,"Failed":0,"Errored":0},"NonCritical":{"HasFailures":true,"Total":4,"Passed":3,"Failed":1,"Errored":0},"ValidationPackage":{"Name":"invenio","Version":"3.1.0","Summary":"Checks ARC publication requirements."}}"""
    |])

client.Http <-
    { new IHttpClient with
        member _.SendAsync request = async {
            if responses.Count = 0 then failwith "Unexpected extra HTTP request."
            let expectedMethod, body = responses.Dequeue()
            if request.Method <> expectedMethod then failwith $"Expected {expectedMethod}, got {request.Method}."
            return HttpResponse(200, body, ResizeArray())
        } }

// --8<-- [start:branches]
let branches = client.Validation.ListValidatedBranchesAsync(projectId) |> Async.RunSynchronously
for branch in branches do printfn "%s" branch
// --8<-- [end:branches]

// --8<-- [start:packages]
let packages = client.Validation.ListPackagesAsync(projectId, branch = "main") |> Async.RunSynchronously
for package in packages do printfn "%s" package.Path
// --8<-- [end:packages]

// --8<-- [start:history]
let history = client.Validation.ListHistoryAsync(projectId, branch = "main") |> Async.RunSynchronously
for commit in history do printfn $"{commit.Id}: {commit.Title}"
// --8<-- [end:history]

// --8<-- [start:summary]
let packageRef = ValidationPackageRef.Create("invenio", "3.1.0", branch = "main")
let summary = client.Validation.GetSummaryAsync(projectId, packageRef) |> Async.RunSynchronously
printfn $"Critical: {summary.Critical.Passed}/{summary.Critical.Total} passed"
// --8<-- [end:summary]

// --8<-- [start:report]
let report = client.Validation.GetReportAsync(projectId, packageRef) |> Async.RunSynchronously
printfn "%s" report
// --8<-- [end:report]

// --8<-- [start:badge]
let badge = client.Validation.GetBadgeAsync(projectId, packageRef) |> Async.RunSynchronously
printfn "%s" badge
// --8<-- [end:badge]

// --8<-- [start:all-summaries]
let summaries = client.Validation.GetAllSummariesAsync(projectId, branch = "main") |> Async.RunSynchronously
for item in summaries do
    printfn $"{item.ValidationPackage.Name}: {item.Critical.HasFailures}"
// --8<-- [end:all-summaries]

if responses.Count <> 0 then failwith "Expected every sample request to run."
if (branches |> Seq.toList) <> [ "main" ] || summary.Critical.Passed <> 10 || summaries.Count <> 1 then
    failwith "Unexpected validation discovery or summary."
if not (report.Contains "<testsuites>") || not (badge.Contains "<svg") then
    failwith "Expected raw XML and SVG responses."
