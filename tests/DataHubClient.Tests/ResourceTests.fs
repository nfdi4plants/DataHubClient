module DataHubClient.Tests.ResourceTests

open Fable.Pyxpecto
open Thoth.Json.Core
open DataHubClient
open DataHubClient.Tests.TestJson
open DataHubClient.Tests.TestHelpers
open DataHubClient.Tests.Mock.MockHttpClient
open DataHubClient.Tests.Mock

let private baseUrl = "https://hub.example/"
let private apiUrl path = "https://hub.example/api/v4/" + path
let private auth = Authentication.PersonalAccessToken("token")

let private makeClient () =
    let mock = MockHttpClient()
    let client = DataHubClient(baseUrl, auth)
    client.Http <- mock
    client, mock

let private expectAuthHeader (request: HttpRequest) =
    Expect.equal request.Headers.[0] ("PRIVATE-TOKEN", "token") "auth header"
    Expect.isTrue
        (request.Headers |> List.contains (DataHubClientVersion.HeaderName, DataHubClientVersion.Value))
        "client version header"

let private expectJsonHeader (request: HttpRequest) =
    Expect.isTrue (request.Headers |> List.contains ("Content-Type", "application/json")) "content type"

let private requestBody (request: HttpRequest) =
    match request.Body with
    | Some body -> body
    | None -> failwith "expected request body"

let private decodeObjectFields json =
    decodeString
        (Decode.object (fun get ->
            get.Optional.Field "title" Decode.string,
            get.Optional.Field "description" Decode.string,
            get.Optional.Field "state_event" Decode.string,
            get.Optional.Field "branch" Decode.string,
            get.Optional.Field "content" Decode.string,
            get.Optional.Field "commit_message" Decode.string,
            get.Optional.Field "encoding" Decode.string,
            get.Optional.Field "source_branch" Decode.string,
            get.Optional.Field "target_branch" Decode.string,
            get.Optional.Field "body" Decode.string))
        json

let tests =
    testList "Resources" [

        testCaseAsync "Projects.ListAsync builds sorted query and decodes response" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects?search=arc&simple=true", 200, SampleData.projects)

            let! projects = client.Projects.ListAsync(search = "arc", simple = true) |> awaitApi

            Expect.equal projects.Length 1 "project count"
            Expect.equal projects.[0].PathWithNamespace "lab/my-arc" "decoded project"
            let request = mock.LastRequest
            Expect.equal request.Method "GET" "method"
            Expect.equal request.Url (apiUrl "projects?search=arc&simple=true") "url"
            expectAuthHeader request
        }

        testCaseAsync "Projects.GetAsync maps 404 to NotFoundError" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/404", 404, """{"message":"404 Project Not Found"}""")

            try
                let! _ = client.Projects.GetAsync 404 |> awaitApi
                failwith "expected NotFoundError"
            with
            | :? NotFoundError as err ->
                Expect.equal err.StatusCode 404 "status"
                Expect.equal err.Body """{"message":"404 Project Not Found"}""" "body"
        }

        testCaseAsync "Repository APIs build branch and commit requests" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/repository/branches?search=main", 200, SampleData.branches)
            mock.Add("GET", apiUrl "projects/42/repository/commits?path=isa.investigation.xlsx&ref_name=main", 200, SampleData.commits)

            let! branches = client.Repository.ListBranchesAsync(42, search = "main") |> awaitApi
            let! commits = client.Repository.ListCommitsAsync(42, refName = "main", path = "isa.investigation.xlsx") |> awaitApi

            Expect.equal branches.[0].Name "main" "branch name"
            Expect.equal branches.[0].Commit.ShortId "abc123de" "branch commit"
            Expect.equal commits.[0].AuthorEmail "carl@hub.example" "commit author"
            Expect.equal mock.Requests.[0].Url (apiUrl "projects/42/repository/branches?search=main") "branch url"
            Expect.equal mock.Requests.[1].Url (apiUrl "projects/42/repository/commits?path=isa.investigation.xlsx&ref_name=main") "commit url"
        }

        testCaseAsync "Files.GetAsync encodes file path and query ref" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/repository/files/assays%2Fa%2Fisa.assay.xlsx?ref=main", 200, SampleData.repoFile)

            let! file = client.Files.GetAsync(42, "assays/a/isa.assay.xlsx", "main") |> awaitApi

            Expect.equal file.FilePath "assays/a/isa.assay.xlsx" "file path"
            Expect.equal mock.LastRequest.Url (apiUrl "projects/42/repository/files/assays%2Fa%2Fisa.assay.xlsx?ref=main") "url"
            expectAuthHeader mock.LastRequest
        }

        testCaseAsync "Files.CreateAsync sends JSON body" <| async {
            let client, mock = makeClient ()
            mock.Add("POST", apiUrl "projects/42/repository/files/assays%2Fa%2Fisa.assay.xlsx", 201, SampleData.repoFile)

            let! file =
                client.Files.CreateAsync(
                    42,
                    "assays/a/isa.assay.xlsx",
                    "main",
                    "QVJD",
                    "Add assay file",
                    encoding = "base64")
                |> awaitApi

            let request = mock.LastRequest
            let _, _, _, branch, content, commitMessage, encoding, _, _, _ = decodeObjectFields (requestBody request)
            Expect.equal file.Encoding "base64" "decoded file"
            Expect.equal request.Method "POST" "method"
            expectJsonHeader request
            Expect.equal branch (Some "main") "branch"
            Expect.equal content (Some "QVJD") "content"
            Expect.equal commitMessage (Some "Add assay file") "commit message"
            Expect.equal encoding (Some "base64") "encoding"
        }

        testCaseAsync "Issues APIs send bodies and decode notes" <| async {
            let client, mock = makeClient ()
            mock.Add("POST", apiUrl "projects/42/issues", 201, SampleData.issue)
            mock.Add("GET", apiUrl "projects/42/issues/3/notes", 200, SampleData.notes)
            mock.Add("PUT", apiUrl "projects/42/issues/3", 200, SampleData.closedIssue)

            let! issue = client.Issues.CreateAsync(42, "Missing assay metadata", description = "please fix") |> awaitApi
            let createBody = decodeObjectFields (requestBody mock.LastRequest)
            let createTitle, createDescription, _, _, _, _, _, _, _, _ = createBody

            let! notes = client.Issues.NotesAsync(42, 3) |> awaitApi
            let! closed = client.Issues.CloseAsync(42, 3) |> awaitApi
            let _, _, stateEvent, _, _, _, _, _, _, _ = decodeObjectFields (requestBody mock.LastRequest)

            Expect.equal issue.Iid 3 "issue iid"
            Expect.equal createTitle (Some "Missing assay metadata") "title"
            Expect.equal createDescription (Some "please fix") "description"
            Expect.equal notes.[0].Body "Looks good" "note body"
            Expect.equal closed.State "closed" "closed state"
            Expect.equal stateEvent (Some "close") "state event"
        }

        testCaseAsync "MergeRequests.CreateAsync sends source and target branches" <| async {
            let client, mock = makeClient ()
            mock.Add("POST", apiUrl "projects/42/merge_requests", 201, SampleData.mergeRequest)

            let! mr = client.MergeRequests.CreateAsync(42, "feature/assay", "main", "Add assay", description = "adds an assay") |> awaitApi
            let _, description, _, _, _, _, _, sourceBranch, targetBranch, _ = decodeObjectFields (requestBody mock.LastRequest)

            Expect.equal mr.Iid 8 "merge request iid"
            Expect.equal sourceBranch (Some "feature/assay") "source branch"
            Expect.equal targetBranch (Some "main") "target branch"
            Expect.equal description (Some "adds an assay") "description"
        }

        testCaseAsync "MergeRequests.NotesAsync decodes notes" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/merge_requests/8/notes", 200, SampleData.notes)

            let! notes = client.MergeRequests.NotesAsync(42, 8) |> awaitApi

            Expect.equal notes.Length 1 "note count"
            Expect.equal notes.[0].Author.Username "carl" "note author"
            Expect.equal mock.LastRequest.Url (apiUrl "projects/42/merge_requests/8/notes") "url"
        }

        testCaseAsync "Validation.ListValidatedBranchesAsync lists cqc tree folders" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/repository/tree?per_page=100&ref=cqc", 200, SampleData.validationBranchTree)

            let! branches = client.Validation.ListValidatedBranchesAsync(42) |> awaitApi

            Expect.equal branches [| "main"; "dev" |] "folders only, blobs skipped"
            Expect.equal mock.LastRequest.Url (apiUrl "projects/42/repository/tree?per_page=100&ref=cqc") "url"
            expectAuthHeader mock.LastRequest
        }

        testCaseAsync "Validation.ListPackagesAsync parses package folders and skips others" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/repository/tree?path=main&per_page=100&ref=cqc", 200, SampleData.validationPackageTree)

            let! packages = client.Validation.ListPackagesAsync(42) |> awaitApi

            Expect.equal packages.Length 2 "unversioned folders and blobs skipped"
            Expect.equal packages.[0].Name "invenio" "name"
            Expect.equal packages.[0].Version "3.1.0" "version"
            Expect.equal packages.[0].Branch "main" "branch"
            Expect.equal packages.[0].Path "main/invenio@3.1.0" "path"
            Expect.equal packages.[1].Name "pride" "second name"
            Expect.equal mock.LastRequest.Url (apiUrl "projects/42/repository/tree?path=main&per_page=100&ref=cqc") "url"
        }

        testCaseAsync "Validation.GetSummaryAsync fetches raw file and decodes summary" <| async {
            let client, mock = makeClient ()
            mock.Add(
                "GET",
                apiUrl "projects/42/repository/files/main%2Finvenio%403.1.0%2Fvalidation_summary.json/raw?ref=cqc",
                200,
                SampleData.validationSummary)

            let package = ValidationPackageRef.Create("invenio", "3.1.0")
            let! summary = client.Validation.GetSummaryAsync(42, package) |> awaitApi

            Expect.equal summary.Critical.Total 10 "critical total"
            Expect.isFalse summary.Critical.HasFailures "critical has failures"
            Expect.equal summary.NonCritical.Failed 1 "non-critical failed"
            Expect.equal summary.ValidationPackage.Name "invenio" "package name"
            Expect.equal summary.ValidationPackage.CQCHookEndpoint (Some "https://hub.example/hooks/invenio") "hook endpoint"
            expectAuthHeader mock.LastRequest
        }

        testCaseAsync "Validation.GetReportAsync and GetBadgeAsync return raw text" <| async {
            let client, mock = makeClient ()
            mock.Add(
                "GET",
                apiUrl "projects/42/repository/files/main%2Finvenio%403.1.0%2Fvalidation_report.xml/raw?ref=cqc",
                200,
                SampleData.validationReport)
            mock.Add(
                "GET",
                apiUrl "projects/42/repository/files/main%2Finvenio%403.1.0%2Fbadge.svg/raw?ref=abc123def456",
                200,
                SampleData.validationBadge)

            let package = ValidationPackageRef.Create("invenio", "3.1.0")
            let! report = client.Validation.GetReportAsync(42, package) |> awaitApi
            let! badge = client.Validation.GetBadgeAsync(42, package, ref = "abc123def456") |> awaitApi

            Expect.equal report SampleData.validationReport "report body"
            Expect.equal badge SampleData.validationBadge "badge body at historic ref"
        }

        testCaseAsync "Validation.GetAllSummariesAsync discovers packages then fetches every summary" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/repository/tree?path=main&per_page=100&ref=cqc", 200, SampleData.validationPackageTree)
            mock.Add(
                "GET",
                apiUrl "projects/42/repository/files/main%2Finvenio%403.1.0%2Fvalidation_summary.json/raw?ref=cqc",
                200,
                SampleData.validationSummary)
            mock.Add(
                "GET",
                apiUrl "projects/42/repository/files/main%2Fpride%401.0.2%2Fvalidation_summary.json/raw?ref=cqc",
                200,
                SampleData.prideValidationSummary)

            let! summaries = client.Validation.GetAllSummariesAsync(42) |> awaitApi

            Expect.equal summaries.Length 2 "summary count"
            let names = summaries |> Array.map (fun s -> s.ValidationPackage.Name) |> Array.sort
            Expect.equal names [| "invenio"; "pride" |] "summary package names"
            Expect.equal mock.Requests.Length 3 "one tree listing plus one fetch per package"
        }

        testCaseAsync "Validation.ListHistoryAsync lists cqc commits scoped to a branch folder" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/repository/commits?path=main&ref_name=cqc", 200, SampleData.commits)

            let! commits = client.Validation.ListHistoryAsync(42, branch = "main") |> awaitApi

            Expect.equal commits.[0].Id "abc123def456" "commit id"
            Expect.equal mock.LastRequest.Url (apiUrl "projects/42/repository/commits?path=main&ref_name=cqc") "url"
        }

        testCaseAsync "Packages APIs list and transfer generic files" <| async {
            let client, mock = makeClient ()
            mock.Add("GET", apiUrl "projects/42/packages?package_name=arc-bundle&package_type=generic", 200, SampleData.packages)
            mock.Add("PUT", apiUrl "projects/42/packages/generic/arc-bundle/1.2.0/arc.zip", 201, """{"message":"201 Created"}""")
            mock.Add("GET", apiUrl "projects/42/packages/generic/arc-bundle/1.2.0/arc.zip", 200, "zip-content")

            let! packages = client.Packages.ListAsync(42, packageType = "generic", packageName = "arc-bundle") |> awaitApi
            let! uploadResponse = client.Packages.UploadGenericFileAsync(42, "arc-bundle", "1.2.0", "arc.zip", "zip-content") |> awaitApi
            let uploadRequest = mock.Requests.[1]
            let! downloadResponse = client.Packages.DownloadGenericFileAsync(42, "arc-bundle", "1.2.0", "arc.zip") |> awaitApi

            Expect.equal packages.[0].PackageType "generic" "package type"
            Expect.equal uploadRequest.Method "PUT" "upload method"
            Expect.equal uploadRequest.Body (Some "zip-content") "upload body"
            Expect.isTrue (uploadRequest.Headers |> List.contains ("Content-Type", "application/octet-stream")) "upload content type"
            Expect.equal uploadResponse """{"message":"201 Created"}""" "upload response"
            Expect.equal downloadResponse "zip-content" "download response"
        }
    ]
