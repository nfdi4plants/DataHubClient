module DataHubClient.Integration.Tests.IntegrationTests

open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Integration.Tests
open DataHubClient.Tests.TestHelpers
open Thoth.Json.Core

#if FABLE_COMPILER_PYTHON
open Thoth.Json.Python
#endif
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Thoth.Json.JavaScript
#endif
#if !FABLE_COMPILER
open Thoth.Json.Newtonsoft
#endif

/// Builds a client pointed at the configured live DataHub. The transport is the
/// per-target default — DotNetHttpClient / FetchHttpClient / HttpxHttpClient —
/// so these cases exercise the real HTTP stack, not the mock.
let private makeClient () =
    let auth =
        if LiveConfig.anonymous then Authentication.Anonymous()
        else Authentication.PersonalAccessToken LiveConfig.token
    DataHubClient(LiveConfig.url, auth)

/// Decodes the base64 body GitLab returns for a repository file into UTF-8 text.
/// Fable's Python target has no working System.Text.Encoding, so the Python
/// build decodes via the stdlib base64 module — the #if pattern LiveConfig uses.
#if FABLE_COMPILER_PYTHON
open Fable.Core

[<Emit("__import__('base64').b64decode($0).decode('utf-8')")>]
let private decodeBase64 (value: string) : string = nativeOnly
#else
let private decodeBase64 (value: string) : string =
    System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String value)
#endif

/// Instance-level read calls — only need a reachable DataHub and a valid token.
let private instanceCases = [

    testCaseAsync "Projects.ListAsync returns a project collection" <| async {
        let client = makeClient ()
        let! projects = client.Projects.ListAsync() |> awaitApi
        Expect.isTrue (projects.Count >= 0) "a project collection is returned"
        for project in projects do
            Expect.isTrue (project.Id > 0) "each project has an id"
            Expect.notEqual project.Name "" "each project has a name"
    }
]

/// Project-scoped read calls — additionally need DATAHUB_TEST_PROJECT.
let private projectCases = [

    testCaseAsync "Projects.GetAsync returns the configured project" <| async {
        let client = makeClient ()
        let! project = client.Projects.GetAsync LiveConfig.projectId |> awaitApi
        Expect.equal project.Id LiveConfig.projectId "the requested project id round-trips"
        Expect.notEqual project.Name "" "the project has a name"
    }

    testCaseAsync "Repository.ListBranchesAsync returns branches" <| async {
        let client = makeClient ()
        let! branches = client.Repository.ListBranchesAsync LiveConfig.projectId |> awaitApi
        Expect.isTrue (branches.Count >= 0) "a branch collection is returned"
        for branch in branches do
            Expect.notEqual branch.Name "" "each branch has a name"
    }

    testCaseAsync "Repository.ListCommitsAsync returns commits" <| async {
        let client = makeClient ()
        let! commits = client.Repository.ListCommitsAsync LiveConfig.projectId |> awaitApi
        Expect.isTrue (commits.Count >= 0) "a commit collection is returned"
        for commit in commits do
            Expect.notEqual commit.Id "" "each commit has an id"
    }

    testCaseAsync "Issues.ListAsync returns issues" <| async {
        let client = makeClient ()
        let! issues = client.Issues.ListAsync LiveConfig.projectId |> awaitApi
        Expect.isTrue (issues.Count >= 0) "an issue collection is returned"
        for issue in issues do
            Expect.equal issue.ProjectId LiveConfig.projectId "each issue belongs to the project"
    }

    testCaseAsync "MergeRequests.ListAsync returns merge requests" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.ListAsync LiveConfig.projectId |> awaitApi
        Expect.isTrue (mergeRequests.Count >= 0) "a merge request collection is returned"
        for mr in mergeRequests do
            Expect.equal mr.ProjectId LiveConfig.projectId "each merge request belongs to the project"
    }

    // The detail calls below chain off a list, so they work against any
    // project: when the list is empty there is nothing to detail and the case
    // passes without an assertion — the corresponding List case covers the
    // empty path.

    testCaseAsync "Repository.GetBranchAsync round-trips a listed branch" <| async {
        let client = makeClient ()
        let! branches = client.Repository.ListBranchesAsync LiveConfig.projectId |> awaitApi
        if branches.Count > 0 then
            let name = branches.[0].Name
            let! branch = client.Repository.GetBranchAsync(LiveConfig.projectId, name) |> awaitApi
            Expect.equal branch.Name name "the requested branch round-trips"
    }

    testCaseAsync "Repository.GetCommitAsync round-trips a listed commit" <| async {
        let client = makeClient ()
        let! commits = client.Repository.ListCommitsAsync LiveConfig.projectId |> awaitApi
        if commits.Count > 0 then
            let sha = commits.[0].Id
            let! commit = client.Repository.GetCommitAsync(LiveConfig.projectId, sha) |> awaitApi
            Expect.equal commit.Id sha "the requested commit round-trips"
    }

    testCaseAsync "Issues.GetAsync round-trips a listed issue" <| async {
        let client = makeClient ()
        let! issues = client.Issues.ListAsync LiveConfig.projectId |> awaitApi
        if issues.Count > 0 then
            let iid = issues.[0].Iid
            let! issue = client.Issues.GetAsync(LiveConfig.projectId, iid) |> awaitApi
            Expect.equal issue.Iid iid "the requested issue round-trips"
            Expect.equal issue.ProjectId LiveConfig.projectId "the issue belongs to the project"
    }

    testCaseAsync "Issues.NotesAsync returns notes for a listed issue" <| async {
        let client = makeClient ()
        let! issues = client.Issues.ListAsync LiveConfig.projectId |> awaitApi
        if issues.Count > 0 then
            let! notes = client.Issues.NotesAsync(LiveConfig.projectId, issues.[0].Iid) |> awaitApi
            Expect.isTrue (notes.Count >= 0) "a note collection is returned"
    }

    testCaseAsync "MergeRequests.GetAsync round-trips a listed merge request" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.ListAsync LiveConfig.projectId |> awaitApi
        if mergeRequests.Count > 0 then
            let iid = mergeRequests.[0].Iid
            let! mr = client.MergeRequests.GetAsync(LiveConfig.projectId, iid) |> awaitApi
            Expect.equal mr.Iid iid "the requested merge request round-trips"
            Expect.equal mr.ProjectId LiveConfig.projectId "the merge request belongs to the project"
    }

    testCaseAsync "MergeRequests.NotesAsync returns notes for a listed merge request" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.ListAsync LiveConfig.projectId |> awaitApi
        if mergeRequests.Count > 0 then
            let! notes = client.MergeRequests.NotesAsync(LiveConfig.projectId, mergeRequests.[0].Iid) |> awaitApi
            Expect.isTrue (notes.Count >= 0) "a note collection is returned"
    }

    testCaseAsync "Packages.ListAsync returns packages" <| async {
        let client = makeClient ()
        let! packages = client.Packages.ListAsync LiveConfig.projectId |> awaitApi
        Expect.isTrue (packages.Count >= 0) "a package collection is returned"
        for package in packages do
            Expect.notEqual package.Name "" "each package has a name"
    }

    // The Validation cases below are gated on the project actually having a
    // cqc branch — a project without one (nothing was ever validated) passes
    // without an assertion, like the empty-list paths above.

    testCaseAsync "Validation discovery chains from branches to packages" <| async {
        let client = makeClient ()
        let! hasCqc =
            async {
                try
                    let! _ = client.Repository.GetBranchAsync(LiveConfig.projectId, "cqc") |> awaitApi
                    return true
                with :? NotFoundError ->
                    return false
            }

        if hasCqc then
            let! branches = client.Validation.ListValidatedBranchesAsync LiveConfig.projectId |> awaitApi
            for branch in branches do
                Expect.notEqual branch "" "each validated branch has a name"
                let! packages = client.Validation.ListPackagesAsync(LiveConfig.projectId, branch = branch) |> awaitApi
                for package in packages do
                    Expect.notEqual package.Name "" "each package has a name"
                    Expect.notEqual package.Version "" "each package has a version"
                    Expect.equal package.Branch branch "each package carries its branch"
    }

    testCaseAsync "Validation results round-trip for a discovered package" <| async {
        let client = makeClient ()
        let! hasCqc =
            async {
                try
                    let! _ = client.Repository.GetBranchAsync(LiveConfig.projectId, "cqc") |> awaitApi
                    return true
                with :? NotFoundError ->
                    return false
            }

        if hasCqc then
            let! branches = client.Validation.ListValidatedBranchesAsync LiveConfig.projectId |> awaitApi
            if branches.Count > 0 then
                let! packages = client.Validation.ListPackagesAsync(LiveConfig.projectId, branch = branches.[0]) |> awaitApi
                if packages.Count > 0 then
                    let package = packages.[0]

                    let! summary = client.Validation.GetSummaryAsync(LiveConfig.projectId, package) |> awaitApi
                    Expect.notEqual summary.ValidationPackage.Name "" "summary names its package"
                    Expect.isTrue (summary.Critical.Total >= 0) "critical counts decode"
                    Expect.isTrue (summary.NonCritical.Total >= 0) "non-critical counts decode"

                    let! report = client.Validation.GetReportAsync(LiveConfig.projectId, package) |> awaitApi
                    Expect.notEqual report "" "report is non-empty"

                    let! badge = client.Validation.GetBadgeAsync(LiveConfig.projectId, package) |> awaitApi
                    Expect.isTrue (badge.Contains "<svg") "badge is an svg"

                    // Any commit SHA from the history is a valid ref — the head
                    // SHA must reproduce the head result.
                    let! history = client.Validation.ListHistoryAsync LiveConfig.projectId |> awaitApi
                    Expect.isTrue (history.Count > 0) "cqc branch has commits"
                    let! historic =
                        client.Validation.GetSummaryAsync(LiveConfig.projectId, package, ref = history.[0].Id)
                        |> awaitApi
                    Expect.equal
                        historic.ValidationPackage.Name
                        summary.ValidationPackage.Name
                        "summary at the head SHA matches the branch-head summary"
    }
]

/// Requires the explicitly selected canonical fixture; a reset or wrong id fails.
let private requireCanonicalFixtureProject (client: DataHubClient) =
    async {
        let! project = client.Projects.GetAsync LiveConfig.projectId |> awaitApi
        Expect.equal project.PathWithNamespace "integration_tests/test_1" "configured project must be the canonical fixture"
    }

let private dataplantDevCases = [

    testCaseAsync "Fixture: project metadata matches dataplant-dev" <| async {
        let client = makeClient ()
        do! requireCanonicalFixtureProject client
        let! project = client.Projects.GetAsync LiveConfig.projectId |> awaitApi
        Expect.equal project.Name "Test_1" "project name"
        Expect.equal project.Path "test_1" "project path"
        Expect.equal project.Visibility "public" "project visibility"
        Expect.equal project.DefaultBranch (Some "main") "default branch"
    }

    testCaseAsync "Fixture: main branch is default and protected" <| async {
        let client = makeClient ()
        do! requireCanonicalFixtureProject client
        let! branch = client.Repository.GetBranchAsync(LiveConfig.projectId, "main") |> awaitApi
        Expect.equal branch.Name "main" "branch name"
        Expect.isTrue branch.Default "main is the default branch"
        Expect.isTrue branch.Protected "main is protected"
    }

    testCaseAsync "Fixture: issue 1 is Test_Issue_1" <| async {
        let client = makeClient ()
        do! requireCanonicalFixtureProject client
        let! issue = client.Issues.GetAsync(LiveConfig.projectId, 1) |> awaitApi
        Expect.equal issue.Iid 1 "issue iid"
        Expect.equal issue.Title "Test_Issue_1" "issue title"
        Expect.equal issue.State "opened" "issue state"
        Expect.equal
            issue.Description
            (Some "This is an issue for DataHubClient integration tests!")
            "issue description"
        Expect.isTrue
            (Seq.contains "integration_tests" issue.Labels)
            "issue carries the integration_tests label"
    }

    testCaseAsync "Fixture: README.md has the expected content" <| async {
        let client = makeClient ()
        do! requireCanonicalFixtureProject client
        let! file = client.Files.GetAsync(LiveConfig.projectId, "README.md", "main") |> awaitApi
        Expect.equal file.FilePath "README.md" "file path"
        Expect.equal file.Ref "main" "file ref"
        // A committed file may or may not carry a trailing newline.
        Expect.equal (decodeBase64(file.Content).Trim()) "Hello DataHubClient!" "README content"
    }
]

/// Strict content assertions are enabled explicitly, never inferred from a host.
let private fixtureCases =
    match LiveConfig.fixture with
    | "dataplant-dev" -> dataplantDevCases
    | _ -> []

/// Validates JSON-LD structure so a successful HTML login page cannot pass.
let private expectRoCrate (body: string) =
    let decoder =
        Decode.object (fun get ->
            get.Required.Field "@context" Decode.value |> ignore
            get.Required.Field "@graph" (Decode.list (Decode.field "@id" Decode.string)))
    match Decode.fromString decoder body with
    | Ok ids -> Expect.isTrue (not (List.isEmpty ids)) "RO-Crate contains graph entities"
    | Error error -> failwith ("Downloaded package is not RO-Crate JSON-LD: " + error)

/// Explicit package fixtures must exist: missing exports are failures, not skips.
let private roCrateCases = [
    testCaseAsync "Packages.DownloadROCrateAsync downloads JSON-LD" <| async {
        let client = makeClient ()
        let! body = client.Packages.DownloadROCrateAsync LiveConfig.roCrateProjectId |> awaitApi
        expectRoCrate body
    }
    testCaseAsync "Packages.DownloadGenericFileAsync downloads the RO-Crate package" <| async {
        let client = makeClient ()
        let! body =
            client.Packages.DownloadGenericFileAsync(
                LiveConfig.roCrateProjectId, "isa_arc_json", "0.0.1", "arc-ro-crate-metadata.json")
            |> awaitApi
        expectRoCrate body
    }
]

/// Read-only live tests. Unconfigured local runs skip; required CI runs fail.
let tests =
    let cases, label =
        if not (List.isEmpty LiveConfig.configurationErrors) then
            [ testCase "Live integration configuration is valid" (fun () ->
                failwith (String.concat "\n" LiveConfig.configurationErrors)) ], "Integration (invalid configuration)"
        elif not LiveConfig.isConfigured then
            printfn "Live integration tests skipped: set DATAHUB_TEST_URL and a token, or explicitly enable anonymous tests."
            [], "Integration (skipped: set URL and token, or explicitly enable anonymous tests)"
        else
            if not LiveConfig.hasRoCrateProject then
                printfn "RO-Crate download coverage disabled: set DATAHUB_TEST_ROCRATE_PROJECT."
            let projectTests = if LiveConfig.hasProject then projectCases else []
            let packageTests = if LiveConfig.hasRoCrateProject then roCrateCases else []
            instanceCases @ projectTests @ fixtureCases @ packageTests, "Integration"

    testList label cases
