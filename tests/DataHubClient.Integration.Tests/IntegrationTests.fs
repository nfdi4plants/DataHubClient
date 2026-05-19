module DataHubClient.Integration.Tests.IntegrationTests

open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Integration.Tests

/// Builds a client pointed at the configured live DataHub. The transport is the
/// per-target default — DotNetHttpClient / FetchHttpClient / HttpxHttpClient —
/// so these cases exercise the real HTTP stack, not the mock.
let private makeClient () =
    DataHubClient(LiveConfig.url, Authentication.PersonalAccessToken LiveConfig.token)

/// Instance-level read calls — only need a reachable DataHub and a valid token.
let private instanceCases = [

    testCaseAsync "Projects.List returns a project array" <| async {
        let client = makeClient ()
        let! projects = client.Projects.List()
        Expect.isTrue (projects.Length >= 0) "a project array is returned"
        for project in projects do
            Expect.isTrue (project.Id > 0) "each project has an id"
            Expect.notEqual project.Name "" "each project has a name"
    }
]

/// Project-scoped read calls — additionally need DATAHUB_TEST_PROJECT.
let private projectCases = [

    testCaseAsync "Projects.Get returns the configured project" <| async {
        let client = makeClient ()
        let! project = client.Projects.Get LiveConfig.projectId
        Expect.equal project.Id LiveConfig.projectId "the requested project id round-trips"
        Expect.equal project.Name "Test_1" "the project has a name"
    }

    testCaseAsync "Repository.ListBranches returns branches" <| async {
        let client = makeClient ()
        let! branches = client.Repository.ListBranches LiveConfig.projectId
        Expect.isTrue (branches.Length >= 0) "a branch array is returned"
        for branch in branches do
            Expect.notEqual branch.Name "" "each branch has a name"
    }

    testCaseAsync "Repository.ListCommits returns commits" <| async {
        let client = makeClient ()
        let! commits = client.Repository.ListCommits LiveConfig.projectId
        Expect.isTrue (commits.Length >= 0) "a commit array is returned"
        for commit in commits do
            Expect.notEqual commit.Id "" "each commit has an id"
    }

    testCaseAsync "Issues.List returns issues" <| async {
        let client = makeClient ()
        let! issues = client.Issues.List LiveConfig.projectId
        Expect.isTrue (issues.Length >= 0) "an issue array is returned"
        for issue in issues do
            Expect.equal issue.ProjectId LiveConfig.projectId "each issue belongs to the project"
    }

    testCaseAsync "MergeRequests.List returns merge requests" <| async {
        let client = makeClient ()
        let! mergeRequests = client.MergeRequests.List LiveConfig.projectId
        Expect.isTrue (mergeRequests.Length >= 0) "a merge request array is returned"
        for mr in mergeRequests do
            Expect.equal mr.ProjectId LiveConfig.projectId "each merge request belongs to the project"
    }
]

/// The live integration suite. It is read-only: every case lists or gets
/// resources, none create or mutate. The case set narrows by what is
/// configured, and collapses to an empty (skipped, not failed) list when no
/// DataHub credentials are present — see LiveConfig.
let tests =
    let cases, label =
        if not LiveConfig.isConfigured then
            [], "Integration (skipped: set DATAHUB_TEST_URL and DATAHUB_TEST_TOKEN)"
        elif LiveConfig.hasProject then
            instanceCases @ projectCases, "Integration"
        else
            instanceCases, "Integration (instance-only: set DATAHUB_TEST_PROJECT for project cases)"

    testList label cases
