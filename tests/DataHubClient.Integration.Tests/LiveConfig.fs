module DataHubClient.Integration.Tests.LiveConfig

#if FABLE_COMPILER
open Fable.Core
#endif

/// <summary>
/// Reads an environment variable, returning <c>""</c> when it is unset. The
/// lookup is target-specific — <c>System.Environment</c> on .NET,
/// <c>process.env</c> on JavaScript, and <c>os.environ</c> on Python — selected
/// by <c>#if</c>, exactly the way <c>TestJson.fs</c> picks a Thoth.Json runtime.
/// </summary>
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
[<Emit("(typeof process !== 'undefined' && process.env[$0]) || ''")>]
let private getEnv (name: string) : string = nativeOnly
#endif
#if FABLE_COMPILER_PYTHON
[<Emit("__import__('os').environ.get($0, '')")>]
let private getEnv (name: string) : string = nativeOnly
#endif
#if !FABLE_COMPILER
let private getEnv (name: string) : string =
    match System.Environment.GetEnvironmentVariable name with
    | null -> ""
    | value -> value
#endif

/// The DataHub root URL the live suite targets, from <c>DATAHUB_TEST_URL</c>.
let url = getEnv "DATAHUB_TEST_URL"

/// The personal access token the live suite authenticates with, from
/// <c>DATAHUB_TEST_TOKEN</c>.
let token = getEnv "DATAHUB_TEST_TOKEN"

let private projectRaw = getEnv "DATAHUB_TEST_PROJECT"

/// <summary>
/// True when both <c>DATAHUB_TEST_URL</c> and <c>DATAHUB_TEST_TOKEN</c> are set,
/// so the suite can reach a DataHub. When false every case is skipped — not
/// failed — so the suite no-ops on fork PRs and on local runs without
/// credentials.
/// </summary>
let isConfigured = url <> "" && token <> ""

/// The numeric project id from <c>DATAHUB_TEST_PROJECT</c>, or <c>-1</c> when it
/// is unset or not an integer.
let projectId =
    match System.Int32.TryParse projectRaw with
    | true, value -> value
    | _ -> -1

/// True when a usable project id is available, gating the project-scoped cases.
let hasProject = isConfigured && projectId > 0
