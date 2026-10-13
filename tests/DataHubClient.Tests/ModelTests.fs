module DataHubClient.Tests.ModelTests

open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Json
open DataHubClient.Tests.TestJson

let private sampleUser = User(7, "carl", "Carl Correns", "active", "https://hub/carl", "https://hub/avatar.png")

let private sampleCommit =
    Commit(
        "abc123def456", "abc123de", "Add ARC metadata", "Add ARC metadata\n\nlong body",
        "Carl Correns", "carl@hub", "2026-05-01T10:00:00Z", "https://hub/commit/abc123de")

let tests =
    testList "Models" [

        testCase "User round-trips through JSON" <| fun () ->
            let r = roundTrip User.encoder User.decoder sampleUser
            Expect.equal r.Id 7 "id"
            Expect.equal r.Username "carl" "username"
            Expect.equal r.Name "Carl Correns" "name"
            Expect.equal r.State "active" "state"
            Expect.equal r.WebUrl "https://hub/carl" "web url"
            Expect.equal r.AvatarUrl (Some "https://hub/avatar.png") "avatar url"

        testCase "User omitted avatar decodes to None" <| fun () ->
            let r = roundTrip User.encoder User.decoder (User(1, "u", "U", "active", "https://hub/u"))
            Expect.equal r.AvatarUrl None "avatar url"

        testCase "Project round-trips through JSON" <| fun () ->
            let p =
                Project(
                    42, "My ARC", "my-arc", "lab/my-arc", "private", "https://hub/lab/my-arc",
                    "An annotated research context", "main")
            let r = roundTrip Project.encoder Project.decoder p
            Expect.equal r.Id 42 "id"
            Expect.equal r.PathWithNamespace "lab/my-arc" "path with namespace"
            Expect.equal r.Visibility "private" "visibility"
            Expect.equal r.Description (Some "An annotated research context") "description"
            Expect.equal r.DefaultBranch (Some "main") "default branch"

        testCase "Commit round-trips through JSON" <| fun () ->
            let r = roundTrip Commit.encoder Commit.decoder sampleCommit
            Expect.equal r.Id "abc123def456" "id"
            Expect.equal r.ShortId "abc123de" "short id"
            Expect.equal r.AuthorEmail "carl@hub" "author email"
            Expect.equal r.WebUrl (Some "https://hub/commit/abc123de") "web url"

        testCase "Branch round-trips through JSON with nested commit" <| fun () ->
            let r = roundTrip Branch.encoder Branch.decoder (Branch("main", true, true, false, sampleCommit))
            Expect.equal r.Name "main" "name"
            Expect.isTrue r.Default "default"
            Expect.isTrue r.Protected "protected"
            Expect.isFalse r.Merged "merged"
            Expect.equal r.Commit.ShortId "abc123de" "nested commit short id"

        testCase "RepoFile round-trips through JSON" <| fun () ->
            let f = RepoFile("isa.investigation.xlsx", "isa.investigation.xlsx", 1024, "base64", "QVJD", "main", "blob1", "commit1")
            let r = roundTrip RepoFile.encoder RepoFile.decoder f
            Expect.equal r.FilePath "isa.investigation.xlsx" "file path"
            Expect.equal r.Size 1024 "size"
            Expect.equal r.Encoding "base64" "encoding"
            Expect.equal r.Content "QVJD" "content"
            Expect.equal r.Ref "main" "ref"

        testCase "Note round-trips through JSON with nested author" <| fun () ->
            let r = roundTrip Note.encoder Note.decoder (Note(5, "Looks good", sampleUser, false, "2026-05-02T09:00:00Z", "2026-05-02T09:00:00Z"))
            Expect.equal r.Id 5 "id"
            Expect.equal r.Body "Looks good" "body"
            Expect.isFalse r.System "system"
            Expect.equal r.Author.Username "carl" "nested author"

        testCase "Issue round-trips through JSON with resizable collections" <| fun () ->
            let issue =
                Issue(
                    100, 3, 42, "Missing assay metadata", "opened", sampleUser,
                    ResizeArray([ sampleUser ]), ResizeArray([ "bug"; "metadata" ]), "https://hub/issue/3",
                    "2026-05-01T00:00:00Z", "2026-05-03T00:00:00Z", "please fix")
            let r = roundTrip Issue.encoder Issue.decoder issue
            Expect.equal r.Iid 3 "iid"
            Expect.equal r.ProjectId 42 "project id"
            Expect.equal r.State "opened" "state"
            Expect.equal (r.Labels |> Seq.toList) [ "bug"; "metadata" ] "labels"
            Expect.equal r.Assignees.Count 1 "assignee count"
            Expect.equal r.Assignees.[0].Username "carl" "assignee"
            Expect.equal r.Description (Some "please fix") "description"

        testCase "Issue collections can be resized and replaced before JSON encoding" <| fun () ->
            let issue =
                Issue(
                    100, 3, 42, "Missing assay metadata", "opened", sampleUser,
                    ResizeArray(), ResizeArray(), "https://hub/issue/3",
                    "2026-05-01T00:00:00Z", "2026-05-03T00:00:00Z")
            issue.Assignees.Add(sampleUser)
            issue.Labels.Add("obsolete")
            issue.Labels <- ResizeArray([ "bug" ])
            issue.Labels.Add("metadata")

            let decoded = roundTrip Issue.encoder Issue.decoder issue
            Expect.equal decoded.Assignees.Count 1 "added assignee is encoded"
            Expect.equal (decoded.Labels |> Seq.toList) [ "bug"; "metadata" ] "replacement and appended labels are encoded"

            decoded.Assignees.Clear()
            decoded.Labels.RemoveAt(0)
            decoded.Labels.Add("reviewed")
            let updated = roundTrip Issue.encoder Issue.decoder decoded
            Expect.equal updated.Assignees.Count 0 "decoded assignees can be cleared"
            Expect.equal (updated.Labels |> Seq.toList) [ "metadata"; "reviewed" ] "decoded labels can be removed and appended"

        testCase "HTTP request and response headers support resizable collections" <| fun () ->
            let request = HttpRequest("https://hub.example/api/v4/projects", "GET")
            request.Headers.Add(("X-Initial", "one"))
            request.Headers <- ResizeArray([ "X-Request-Id", "req-7" ])
            request.Headers.Add(("Accept", "application/json"))
            Expect.equal request.Headers.Count 2 "headers can be replaced and appended"
            Expect.equal request.Headers.[0] ("X-Request-Id", "req-7") "replacement header survives"

            let response = HttpResponse(200, "[]", ResizeArray([ "X-Request-Id", "req-7" ]))
            response.Headers.Add(("X-Next-Page", "2"))
            response.Headers.RemoveAt(0)
            Expect.equal (response.Headers |> Seq.toList) [ "X-Next-Page", "2" ] "response header collection can be resized"

        testCase "MergeRequest round-trips through JSON" <| fun () ->
            let mr =
                MergeRequest(
                    200, 8, 42, "Add assay", "opened", "feature/assay", "main", sampleUser,
                    "https://hub/mr/8", "2026-05-01T00:00:00Z", "2026-05-04T00:00:00Z",
                    "adds an assay", "can_be_merged")
            let r = roundTrip MergeRequest.encoder MergeRequest.decoder mr
            Expect.equal r.Iid 8 "iid"
            Expect.equal r.SourceBranch "feature/assay" "source branch"
            Expect.equal r.TargetBranch "main" "target branch"
            Expect.equal r.MergeStatus (Some "can_be_merged") "merge status"

        testCase "Package round-trips through JSON" <| fun () ->
            let r = roundTrip Package.encoder Package.decoder (Package(9, "arc-bundle", "1.2.0", "generic", "default", "2026-05-01T00:00:00Z"))
            Expect.equal r.Name "arc-bundle" "name"
            Expect.equal r.Version "1.2.0" "version"
            Expect.equal r.PackageType "generic" "package type"
            Expect.equal r.WebUrl None "web url"

        testCase "ValidationSummary round-trips through JSON with nested results" <| fun () ->
            let summary =
                ValidationSummary(
                    ValidationResult(false, 10, 10, 0, 0),
                    ValidationResult(true, 4, 2, 1, 1),
                    ValidationPackageSummary("invenio", "3.1.0", summary = "Validates invenio publishability"))
            let r = roundTrip ValidationSummary.encoder ValidationSummary.decoder summary
            Expect.isFalse r.Critical.HasFailures "critical has failures"
            Expect.equal r.Critical.Total 10 "critical total"
            Expect.equal r.NonCritical.Errored 1 "non-critical errored"
            Expect.equal r.ValidationPackage.Name "invenio" "package name"
            Expect.equal r.ValidationPackage.Summary (Some "Validates invenio publishability") "package summary"
            Expect.equal r.ValidationPackage.CQCHookEndpoint None "hook endpoint"

        testCase "ValidationPackageRef computes cqc folder path" <| fun () ->
            let package = ValidationPackageRef.Create("invenio", "3.1.0")
            Expect.equal package.Branch "main" "default branch"
            Expect.equal package.Path "main/invenio@3.1.0" "path"
            Expect.equal (ValidationPackageRef("pride", "1.0.2", "dev")).Path "dev/pride@1.0.2" "explicit branch path"
    ]
