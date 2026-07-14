namespace DataHubClient.Json

open DataHubClient
open Thoth.Json.Core

/// JSON encoder and decoder for <see cref="T:DataHubClient.ValidationResult"/>.
module ValidationResult =

    /// Decodes a <see cref="T:DataHubClient.ValidationResult"/> from its arc-validate JSON representation.
    let decoder : Decoder<ValidationResult> =
        Decode.object (fun get ->
            ValidationResult(
                get.Required.Field "HasFailures" Decode.bool,
                get.Required.Field "Total" Decode.int,
                get.Required.Field "Passed" Decode.int,
                get.Required.Field "Failed" Decode.int,
                get.Required.Field "Errored" Decode.int))

    /// Encodes a <see cref="T:DataHubClient.ValidationResult"/> to its arc-validate JSON representation.
    let encoder (result: ValidationResult) : IEncodable =
        Encode.object [
            "HasFailures", Encode.bool result.HasFailures
            "Total", Encode.int result.Total
            "Passed", Encode.int result.Passed
            "Failed", Encode.int result.Failed
            "Errored", Encode.int result.Errored ]

/// JSON encoder and decoder for <see cref="T:DataHubClient.ValidationPackageSummary"/>.
module ValidationPackageSummary =

    /// Decodes a <see cref="T:DataHubClient.ValidationPackageSummary"/> from its arc-validate JSON representation.
    let decoder : Decoder<ValidationPackageSummary> =
        Decode.object (fun get ->
            ValidationPackageSummary(
                get.Required.Field "Name" Decode.string,
                get.Required.Field "Version" Decode.string,
                ?summary = get.Optional.Field "Summary" Decode.string,
                ?description = get.Optional.Field "Description" Decode.string,
                ?cqcHookEndpoint = get.Optional.Field "CQCHookEndpoint" Decode.string))

    /// Encodes a <see cref="T:DataHubClient.ValidationPackageSummary"/> to its arc-validate JSON representation.
    let encoder (package: ValidationPackageSummary) : IEncodable =
        Encode.object [
            "Name", Encode.string package.Name
            "Version", Encode.string package.Version
            "Summary", ThothExtensions.encodeOption Encode.string package.Summary
            "Description", ThothExtensions.encodeOption Encode.string package.Description
            "CQCHookEndpoint", ThothExtensions.encodeOption Encode.string package.CQCHookEndpoint ]

/// JSON encoder and decoder for <see cref="T:DataHubClient.ValidationSummary"/>.
module ValidationSummary =

    /// Decodes a <see cref="T:DataHubClient.ValidationSummary"/> from its arc-validate JSON representation.
    let decoder : Decoder<ValidationSummary> =
        Decode.object (fun get ->
            ValidationSummary(
                get.Required.Field "Critical" ValidationResult.decoder,
                get.Required.Field "NonCritical" ValidationResult.decoder,
                get.Required.Field "ValidationPackage" ValidationPackageSummary.decoder))

    /// Encodes a <see cref="T:DataHubClient.ValidationSummary"/> to its arc-validate JSON representation.
    let encoder (summary: ValidationSummary) : IEncodable =
        Encode.object [
            "Critical", ValidationResult.encoder summary.Critical
            "NonCritical", ValidationResult.encoder summary.NonCritical
            "ValidationPackage", ValidationPackageSummary.encoder summary.ValidationPackage ]
