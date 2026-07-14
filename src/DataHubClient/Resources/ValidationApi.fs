namespace DataHubClient

open Fable.Core
open DataHubClient.Json
open Thoth.Json.Core

/// <summary>
/// Client for ARC validation (CQC) results. Every ARC on a DataHub has a
/// <c>cqc</c> branch holding the output of validation package runs, one folder
/// per validated ARC branch, containing one <c>&lt;name&gt;@&lt;version&gt;</c>
/// folder per package with <c>badge.svg</c>, <c>validation_report.xml</c>, and
/// <c>validation_summary.json</c>. See the
/// <see href="https://nfdi4plants.github.io/nfdi4plants.knowledgebase/arc-validation/authoring-validation-packages/#validation-output">validation output specification</see>.
/// All methods default <c>ref</c> to the <c>cqc</c> branch head; pass a commit
/// SHA from <c>ListHistoryAsync</c> to read results from any point in the
/// branch history.
/// </summary>
/// <param name="baseUrl">The DataHub root URL, without the <c>/api/v4</c> suffix.</param>
/// <param name="auth">The authentication header applied to every request.</param>
/// <param name="http">The transport used to send requests.</param>
[<AttachMembers>]
type ValidationApi(baseUrl: string, auth: Authentication, http: IHttpClient) =

    let defaultRef = "cqc"
    let defaultBranch = "main"

    let treeEntryDecoder : Decoder<string * string> =
        Decode.object (fun get ->
            get.Required.Field "name" Decode.string,
            get.Required.Field "type" Decode.string)

    let listTree projectId path refName =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    [ "projects"; string projectId; "repository"; "tree" ]
                    [ "path", path; "per_page", Some "100"; "ref", Some refName ]

            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray treeEntryDecoder response
        }

    let getRawFileResponse projectId (filePath: string) refName =
        let req =
            ResourceHelpers.emptyRequest
                baseUrl
                auth
                "GET"
                [ "projects"; string projectId; "repository"; "files"; filePath; "raw" ]
                [ "ref", Some refName ]

        http.SendAsync req

    let parsePackageFolder branch (folderName: string) =
        match folderName.LastIndexOf '@' with
        | at when at > 0 && at < folderName.Length - 1 ->
            Some(ValidationPackageRef(folderName.Substring(0, at), folderName.Substring(at + 1), branch))
        | _ -> None

    let listPackages projectId branch refName =
        async {
            let! entries = listTree projectId (Some branch) refName
            return
                entries
                |> Array.choose (fun (name, entryType) ->
                    if entryType = "tree" then parsePackageFolder branch name else None)
        }

    let getSummary projectId (package: ValidationPackageRef) refName =
        async {
            let! response = getRawFileResponse projectId (package.Path + "/validation_summary.json") refName
            return ResourceHelpers.decode ValidationSummary.decoder response
        }

    /// <summary>Lists the ARC branches that have validation results — the top-level folders of the cqc branch (usually just <c>main</c>).</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="ref">The results branch or commit SHA to read from. Defaults to <c>cqc</c>.</param>
    member _.ListValidatedBranchesAsync(projectId: int, ?ref: string) =
        async {
            let! entries = listTree projectId None (defaultArg ref defaultRef)
            return
                entries
                |> Array.choose (fun (name, entryType) -> if entryType = "tree" then Some name else None)
        }
        |> ResourceHelpers.toPublic

    /// <summary>Lists the validation packages that produced results for an ARC branch, parsed from the <c>&lt;name&gt;@&lt;version&gt;</c> result folders. Folders without a version suffix are skipped.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="branch">The validated ARC branch. Defaults to <c>main</c>.</param>
    /// <param name="ref">The results branch or commit SHA to read from. Defaults to <c>cqc</c>.</param>
    member _.ListPackagesAsync(projectId: int, ?branch: string, ?ref: string) =
        listPackages projectId (defaultArg branch defaultBranch) (defaultArg ref defaultRef)
        |> ResourceHelpers.toPublic

    /// <summary>Lists the commits of the cqc branch, newest first. Each SHA can be passed as <c>ref</c> to the other methods to read historic results.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="branch">Optional validated ARC branch used to scope the history to that branch's result folder.</param>
    /// <param name="ref">The results branch (or SHA to start from). Defaults to <c>cqc</c>.</param>
    member _.ListHistoryAsync(projectId: int, ?branch: string, ?ref: string) =
        async {
            let req =
                ResourceHelpers.emptyRequest
                    baseUrl
                    auth
                    "GET"
                    [ "projects"; string projectId; "repository"; "commits" ]
                    [ "path", branch; "ref_name", Some(defaultArg ref defaultRef) ]

            let! response = http.SendAsync req
            return ResourceHelpers.decodeArray Commit.decoder response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Gets a package's <c>validation_summary.json</c>, decoded.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="package">The validation package whose results to read.</param>
    /// <param name="ref">The results branch or commit SHA to read from. Defaults to <c>cqc</c>.</param>
    member _.GetSummaryAsync(projectId: int, package: ValidationPackageRef, ?ref: string) =
        getSummary projectId package (defaultArg ref defaultRef)
        |> ResourceHelpers.toPublic

    /// <summary>Gets a package's <c>validation_report.xml</c> (JUnit XML) as raw text.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="package">The validation package whose results to read.</param>
    /// <param name="ref">The results branch or commit SHA to read from. Defaults to <c>cqc</c>.</param>
    member _.GetReportAsync(projectId: int, package: ValidationPackageRef, ?ref: string) =
        async {
            let! response =
                getRawFileResponse projectId (package.Path + "/validation_report.xml") (defaultArg ref defaultRef)

            return ResourceHelpers.responseBody response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Gets a package's <c>badge.svg</c> as raw SVG text.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="package">The validation package whose results to read.</param>
    /// <param name="ref">The results branch or commit SHA to read from. Defaults to <c>cqc</c>.</param>
    member _.GetBadgeAsync(projectId: int, package: ValidationPackageRef, ?ref: string) =
        async {
            let! response = getRawFileResponse projectId (package.Path + "/badge.svg") (defaultArg ref defaultRef)
            return ResourceHelpers.responseBody response
        }
        |> ResourceHelpers.toPublic

    /// <summary>Discovers the packages validated for an ARC branch and fetches every package's summary.</summary>
    /// <param name="projectId">The project identifier.</param>
    /// <param name="branch">The validated ARC branch. Defaults to <c>main</c>.</param>
    /// <param name="ref">The results branch or commit SHA to read from. Defaults to <c>cqc</c>.</param>
    member _.GetAllSummariesAsync(projectId: int, ?branch: string, ?ref: string) =
        async {
            let refName = defaultArg ref defaultRef
            let! packages = listPackages projectId (defaultArg branch defaultBranch) refName

            return!
                packages
                |> Array.map (fun package -> getSummary projectId package refName)
                |> Async.Parallel
        }
        |> ResourceHelpers.toPublic
