namespace DataHubClient

open Fable.Core

/// <summary>
/// Aggregated pass/fail counts for one severity class of validation cases,
/// as written by arc-validate into <c>validation_summary.json</c>.
/// </summary>
/// <param name="hasFailures">Whether any case in this class failed or errored.</param>
/// <param name="total">The total number of validation cases executed.</param>
/// <param name="passed">The number of cases that passed.</param>
/// <param name="failed">The number of cases that failed.</param>
/// <param name="errored">The number of cases that errored.</param>
[<AttachMembers>]
type ValidationResult
    (
        hasFailures: bool,
        total: int,
        passed: int,
        failed: int,
        errored: int
    ) =
    let mutable _hasFailures = hasFailures
    let mutable _total = total
    let mutable _passed = passed
    let mutable _failed = failed
    let mutable _errored = errored

    /// Whether any case in this class failed or errored.
    member _.HasFailures with get () = _hasFailures and set value = _hasFailures <- value
    /// The total number of validation cases executed.
    member _.Total with get () = _total and set value = _total <- value
    /// The number of cases that passed.
    member _.Passed with get () = _passed and set value = _passed <- value
    /// The number of cases that failed.
    member _.Failed with get () = _failed and set value = _failed <- value
    /// The number of cases that errored.
    member _.Errored with get () = _errored and set value = _errored <- value

/// <summary>
/// The validation package a summary was produced by, as embedded in
/// <c>validation_summary.json</c>.
/// </summary>
/// <param name="name">The validation package name.</param>
/// <param name="version">The validation package version.</param>
/// <param name="summary">A one-line summary of the package, if present.</param>
/// <param name="description">A longer package description, if present.</param>
/// <param name="cqcHookEndpoint">The CQC hook endpoint triggered by this package, if any.</param>
[<AttachMembers>]
type ValidationPackageSummary
    (
        name: string,
        version: string,
        ?summary: string,
        ?description: string,
        ?cqcHookEndpoint: string
    ) =
    let mutable _name = name
    let mutable _version = version
    let mutable _summary : string option = summary
    let mutable _description : string option = description
    let mutable _cqcHookEndpoint : string option = cqcHookEndpoint

    /// The validation package name.
    member _.Name with get () = _name and set value = _name <- value
    /// The validation package version.
    member _.Version with get () = _version and set value = _version <- value
    /// A one-line summary of the package, or <c>None</c> if not present.
    member _.Summary with get () = _summary and set value = _summary <- value
    /// A longer package description, or <c>None</c> if not present.
    member _.Description with get () = _description and set value = _description <- value
    /// The CQC hook endpoint triggered by this package, or <c>None</c> if not present.
    member _.CQCHookEndpoint with get () = _cqcHookEndpoint and set value = _cqcHookEndpoint <- value

/// <summary>
/// The contents of a <c>validation_summary.json</c> produced by an ARC
/// validation package run, mirroring arc-validate's <c>ValidationSummary</c>.
/// See the
/// <see href="https://nfdi4plants.github.io/nfdi4plants.knowledgebase/arc-validation/authoring-validation-packages/#validation-output">validation output specification</see>.
/// </summary>
/// <param name="critical">Counts for critical validation cases.</param>
/// <param name="nonCritical">Counts for non-critical validation cases.</param>
/// <param name="validationPackage">The package that produced the summary.</param>
[<AttachMembers>]
type ValidationSummary
    (
        critical: ValidationResult,
        nonCritical: ValidationResult,
        validationPackage: ValidationPackageSummary
    ) =
    let mutable _critical = critical
    let mutable _nonCritical = nonCritical
    let mutable _validationPackage = validationPackage

    /// Counts for critical validation cases.
    member _.Critical with get () = _critical and set value = _critical <- value
    /// Counts for non-critical validation cases.
    member _.NonCritical with get () = _nonCritical and set value = _nonCritical <- value
    /// The package that produced the summary.
    member _.ValidationPackage with get () = _validationPackage and set value = _validationPackage <- value
