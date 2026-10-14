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
let url = (getEnv "DATAHUB_TEST_URL").TrimEnd('/')

/// The personal access token the live suite authenticates with, from
/// <c>DATAHUB_TEST_TOKEN</c>.
let token = getEnv "DATAHUB_TEST_TOKEN"

let private projectRaw = getEnv "DATAHUB_TEST_PROJECT"

/// Whether the caller explicitly requests public, anonymous read-only tests.
let anonymous = getEnv "DATAHUB_TEST_ANONYMOUS" = "true"

/// Whether missing configuration must fail instead of skipping the live suite.
let required = getEnv "DATAHUB_TEST_REQUIRED" = "true"

/// The optional strict content fixture name. Empty selects generic read tests.
let fixture = getEnv "DATAHUB_TEST_FIXTURE"

let private roCrateProjectRaw = getEnv "DATAHUB_TEST_ROCRATE_PROJECT"

/// True when a URL and either credentials or explicit anonymous access are set.
let isConfigured = url <> "" && (token <> "" || anonymous)

/// The numeric project id from <c>DATAHUB_TEST_PROJECT</c>, or <c>-1</c> when it
/// is unset or not an integer.
let projectId =
    match System.Int32.TryParse projectRaw with
    | true, value -> value
    | _ -> -1

/// True when a usable project id is available, gating the project-scoped cases.
let hasProject = isConfigured && projectId > 0

/// The optional project whose generated RO-Crate must be downloaded successfully.
let roCrateProjectId =
    match System.Int32.TryParse roCrateProjectRaw with
    | true, value -> value
    | _ -> -1

/// True when the RO-Crate package download cases are explicitly enabled.
let hasRoCrateProject = isConfigured && roCrateProjectId > 0

/// Configuration failures become a real failing test, never an empty green run.
let configurationErrors = [
    if required && not isConfigured then
        "DATAHUB_TEST_REQUIRED=true needs DATAHUB_TEST_URL and DATAHUB_TEST_TOKEN (or DATAHUB_TEST_ANONYMOUS=true)."
    if required && projectId <= 0 then
        "DATAHUB_TEST_REQUIRED=true needs a positive DATAHUB_TEST_PROJECT."
    if projectRaw <> "" && projectId <= 0 then
        "DATAHUB_TEST_PROJECT must be a positive integer."
    if roCrateProjectRaw <> "" && roCrateProjectId <= 0 then
        "DATAHUB_TEST_ROCRATE_PROJECT must be a positive integer."
    if roCrateProjectRaw <> "" && not isConfigured then
        "RO-Crate coverage needs DATAHUB_TEST_URL and credentials or explicit anonymous access."
    if fixture <> "" && fixture <> "dataplant-dev" then
        "DATAHUB_TEST_FIXTURE must be empty or dataplant-dev."
    if fixture <> "" && not hasProject then
        "A strict fixture needs a configured DATAHUB_TEST_PROJECT."
    if fixture = "dataplant-dev" && url <> "https://gitdev.nfdi4plants.org" then
        "The dataplant-dev fixture requires https://gitdev.nfdi4plants.org."
]
