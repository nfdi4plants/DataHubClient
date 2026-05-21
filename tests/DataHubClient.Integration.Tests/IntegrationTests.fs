module DataHubClient.Integration.Tests.IntegrationTests

open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Integration.Tests

/// Builds a client pointed at the configured live DataHub. The transport is the
/// per-target default — DotNetHttpClient / FetchHttpClient / HttpxHttpClient —
/// so these cases exercise the real HTTP stack, not the mock.
let private makeClient () =
    DataHubClient(LiveConfig.url, Authentication.PersonalAccessToken LiveConfig.token)

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

    testCaseAsync "Projects.ListAsync returns a project array" <| async {
        let client = makeClient ()
        let! projects = client.Projects.ListAsync()
        Expect.isTrue (projects.Length >= 0) "a project array is returned"
        for project in projects do
            Expect.isTrue (project.Id > 0) "each project has an id"
            Expect.notEqual project.Name "" "each project has a name"
    }
]

/// Project-scoped read calls — additionally need DATAHUB_TEST_PROJECT.
let private projectCases = [

    testCaseAsync "Projects.GetAsync returns the configured project" <| async {
        let client = makeClient ()
        let! project = client.Projects.GetAsync LiveConfig.projectId
        Expect.equal project.Id LiveConfig.projectId "the requested project id round-trips"
        Expect.notEqual project.Name "" "the project has a name"
    }

    testCaseAsync "Repository.ListBranchesAsync returns branches" <| async {
        let client = makeClient ()
        let! branches = client.Repository.ListBranchesAsync LiveConfig.projectId
        Expect.isTrue (branches.Length >= 0) "a branch array is returned"
        for branch in branches do
            Expect.notEqual branch.Name "" "each branch has a name"
    }

    testCaseAsync "Repository.ListCommitsAsync returns commits" <| async {
        let client = makeClient ()
        let! commits = client.Repository.ListCommitsAsync LiveConfig.projectId
        Expect.isTrue (commits.Length >= 0) "a commit array is returned"
        for commit in commits do
            Expect.notEqual commit.Id "" "each commit has an id"
    }

    testCaseAsync "Issues.ListAsync returns issues" <| async {
        let client = makeClient ()
        let! issues = client.Issues.ListAsync LiveConfig.projectId
        Expect.isTrue (issues.Length >= 0) "an issue array is returned"
        for issue in issues do
            Expect.equal issue.ProjectId LiveConfig.projectId "each issue belongs to the project"
    }

    testCaseAsync "MergeRequests.ListAsync returns merge requests" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.ListAsync LiveConfig.projectId
        Expect.isTrue (mergeRequests.Length >= 0) "a merge request array is returned"
        for mr in mergeRequests do
            Expect.equal mr.ProjectId LiveConfig.projectId "each merge request belongs to the project"
    }

    // The detail calls below chain off a list, so they work against any
    // project: when the list is empty there is nothing to detail and the case
    // passes without an assertion — the corresponding List case covers the
    // empty path.

    testCaseAsync "Repository.GetBranchAsync round-trips a listed branch" <| async {
        let client = makeClient ()
        let! branches = client.Repository.ListBranchesAsync LiveConfig.projectId
        if branches.Length > 0 then
            let name = branches.[0].Name
            let! branch = client.Repository.GetBranchAsync(LiveConfig.projectId, name)
            Expect.equal branch.Name name "the requested branch round-trips"
    }

    testCaseAsync "Repository.GetCommitAsync round-trips a listed commit" <| async {
        let client = makeClient ()
        let! commits = client.Repository.ListCommitsAsync LiveConfig.projectId
        if commits.Length > 0 then
            let sha = commits.[0].Id
            let! commit = client.Repository.GetCommitAsync(LiveConfig.projectId, sha)
            Expect.equal commit.Id sha "the requested commit round-trips"
    }

    testCaseAsync "Issues.GetAsync round-trips a listed issue" <| async {
        let client = makeClient ()
        let! issues = client.Issues.ListAsync LiveConfig.projectId
        if issues.Length > 0 then
            let iid = issues.[0].Iid
            let! issue = client.Issues.GetAsync(LiveConfig.projectId, iid)
            Expect.equal issue.Iid iid "the requested issue round-trips"
            Expect.equal issue.ProjectId LiveConfig.projectId "the issue belongs to the project"
    }

    testCaseAsync "Issues.NotesAsync returns notes for a listed issue" <| async {
        let client = makeClient ()
        let! issues = client.Issues.ListAsync LiveConfig.projectId
        if issues.Length > 0 then
            let! notes = client.Issues.NotesAsync(LiveConfig.projectId, issues.[0].Iid)
            Expect.isTrue (notes.Length >= 0) "a note array is returned"
    }

    testCaseAsync "MergeRequests.GetAsync round-trips a listed merge request" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.ListAsync LiveConfig.projectId
        if mergeRequests.Length > 0 then
            let iid = mergeRequests.[0].Iid
            let! mr = client.MergeRequests.GetAsync(LiveConfig.projectId, iid)
            Expect.equal mr.Iid iid "the requested merge request round-trips"
            Expect.equal mr.ProjectId LiveConfig.projectId "the merge request belongs to the project"
    }

    testCaseAsync "MergeRequests.NotesAsync returns notes for a listed merge request" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.ListAsync LiveConfig.projectId
        if mergeRequests.Length > 0 then
            let! notes = client.MergeRequests.NotesAsync(LiveConfig.projectId, mergeRequests.[0].Iid)
            Expect.isTrue (notes.Length >= 0) "a note array is returned"
    }

    testCaseAsync "Packages.ListAsync returns packages" <| async {
        let client = makeClient ()
        let! packages = client.Packages.ListAsync LiveConfig.projectId
        Expect.isTrue (packages.Length >= 0) "a package array is returned"
        for package in packages do
            Expect.notEqual package.Name "" "each package has a name"
    }
]

/// Content assertions for the `dataplant-dev` fixture — the public
/// `integration_tests/test_1` project on the DataPLANT dev instance. Unlike the
/// generic cases above, these hardcode known values, so they are only valid
/// when the suite is pointed at that exact fixture. They are gated below by
/// matching on `DATAHUB_TEST_URL` (see `fixtureCases`), on the assumption that
/// an operator pointing at the DataPLANT dev instance also points
/// `DATAHUB_TEST_PROJECT` at the canonical `integration_tests/test_1` project.
let private dataplantDevCases = [

    testCaseAsync "Fixture: project metadata matches dataplant-dev" <| async {
        let client = makeClient ()
        let! project = client.Projects.GetAsync LiveConfig.projectId
        Expect.equal project.Name "Test_1" "project name"
        Expect.equal project.Path "test_1" "project path"
        Expect.equal project.PathWithNamespace "integration_tests/test_1" "project namespace path"
        Expect.equal project.Visibility "public" "project visibility"
        Expect.equal project.DefaultBranch (Some "main") "default branch"
    }

    testCaseAsync "Fixture: main branch is default and protected" <| async {
        let client = makeClient ()
        let! branch = client.Repository.GetBranchAsync(LiveConfig.projectId, "main")
        Expect.equal branch.Name "main" "branch name"
        Expect.isTrue branch.Default "main is the default branch"
        Expect.isTrue branch.Protected "main is protected"
    }

    testCaseAsync "Fixture: issue 1 is Test_Issue_1" <| async {
        let client = makeClient ()
        let! issue = client.Issues.GetAsync(LiveConfig.projectId, 1)
        Expect.equal issue.Iid 1 "issue iid"
        Expect.equal issue.Title "Test_Issue_1" "issue title"
        Expect.equal issue.State "opened" "issue state"
        Expect.equal
            issue.Description
            (Some "This is an issue for DataHubClient integration tests!")
            "issue description"
        Expect.isTrue
            (Array.contains "integration_tests" issue.Labels)
            "issue carries the integration_tests label"
    }

    testCaseAsync "Fixture: README.md has the expected content" <| async {
        let client = makeClient ()
        let! file = client.Files.GetAsync(LiveConfig.projectId, "README.md", "main")
        Expect.equal file.FilePath "README.md" "file path"
        Expect.equal file.Ref "main" "file ref"
        // Content is compared trimmed — a committed file may or may not carry a
        // trailing newline, which is not what this fixture is asserting.
        Expect.equal (decodeBase64(file.Content).Trim()) "Hello DataHubClient!" "README content"
    }
]

/// The fixture cases selected by the configured `DATAHUB_TEST_URL`. A URL that
/// is not a known fixture host yields no cases — the generic suite still runs.
let private fixtureCases =
    match LiveConfig.url with
    | "https://gitdev.nfdi4plants.org" -> dataplantDevCases
    | _ -> []

/// The live integration suite. It is read-only: every case lists or gets
/// resources, none create or mutate. The case set narrows by what is
/// configured — it collapses to an empty (skipped, not failed) list when no
/// DataHub credentials are present, and gains the fixture content assertions
/// only when `DATAHUB_TEST_URL` matches a known fixture host. See LiveConfig.
let tests =
    let cases, label =
        if not LiveConfig.isConfigured then
            [], "Integration (skipped: set DATAHUB_TEST_URL and DATAHUB_TEST_TOKEN)"
        elif not LiveConfig.hasProject then
            instanceCases, "Integration (instance-only: set DATAHUB_TEST_PROJECT for project cases)"
        elif List.isEmpty fixtureCases then
            instanceCases @ projectCases, "Integration"
        else
            instanceCases @ projectCases @ fixtureCases, "Integration (fixture: DataPLANT dev instance)"

    testList label cases
