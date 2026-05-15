namespace DataHubClient

open Thoth.Json.Core

/// <summary>
/// Shared Thoth.Json encoder helpers used by the model classes. Internal: not
/// part of the public, transpiled API surface.
/// </summary>
module internal ThothExtensions =

    /// <summary>Builds a JSON object, dropping any field whose value is <c>None</c>.</summary>
    /// <param name="fields">Field name/optional-value pairs; <c>None</c> entries are omitted.</param>
    /// <returns>An encodable JSON object containing only the present fields.</returns>
    /// <remarks>Handy for request bodies where GitLab expects omitted (not null) fields.</remarks>
    let objectSkipNull (fields: (string * IEncodable option) list) : IEncodable =
        fields
        |> List.choose (fun (key, value) -> value |> Option.map (fun v -> key, v))
        |> Encode.object

    /// <summary>Encodes an optional value, emitting JSON <c>null</c> when <c>None</c>.</summary>
    /// <param name="encoder">The encoder applied to the value when present.</param>
    /// <param name="value">The optional value to encode.</param>
    /// <returns>The encoded value, or an encoded JSON <c>null</c>.</returns>
    let encodeOption (encoder: 'T -> IEncodable) (value: 'T option) : IEncodable =
        match value with
        | Some v -> encoder v
        | None -> Encode.nil
