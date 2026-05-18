namespace DataHubClient

open System
open Fable.Core
open Thoth.Json.Core

#if FABLE_COMPILER_PYTHON
module private JsonRuntime =
    let encode (value: IEncodable) = Thoth.Json.Python.Encode.toString 0 value
    let decode decoder value = Thoth.Json.Python.Decode.fromString decoder value
#endif
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
module private JsonRuntime =
    let encode (value: IEncodable) = Thoth.Json.JavaScript.Encode.toString 0 value
    let decode decoder value = Thoth.Json.JavaScript.Decode.fromString decoder value
#endif
#if !FABLE_COMPILER
module private JsonRuntime =
    let encode (value: IEncodable) = Thoth.Json.Newtonsoft.Encode.toString 0 value
    let decode decoder value = Thoth.Json.Newtonsoft.Decode.fromString decoder value
#endif

/// <summary>
/// Internal helpers shared by resource API classes. They centralize URL
/// construction, JSON parsing, authentication headers, and error mapping.
/// </summary>
module internal ResourceHelpers =

    let private isSuccess (response: HttpResponse) =
        response.StatusCode >= 200 && response.StatusCode < 300

    let private trimTrailingSlash (value: string) =
        value.TrimEnd([| '/' |])

    let private encodeSegment (value: string) =
        Uri.EscapeDataString(value)

    let private queryString (query: (string * string option) list) =
        query
        |> List.choose (fun (key, value) -> value |> Option.map (fun v -> key, v))
        |> List.sortBy fst
        |> List.map (fun (key, value) -> encodeSegment key + "=" + encodeSegment value)
        |> function
            | [] -> ""
            | pairs -> "?" + String.concat "&" pairs

    let url (baseUrl: string) (segments: string list) (query: (string * string option) list) =
        let path =
            segments
            |> List.map encodeSegment
            |> String.concat "/"

        trimTrailingSlash baseUrl + "/api/v4/" + path + queryString query

    let request
        (baseUrl: string)
        (auth: Authentication)
        (method: string)
        (segments: string list)
        (query: (string * string option) list)
        (body: string option)
        (contentType: string option)
        =
        let req = HttpRequest(url baseUrl segments query, method)
        req.Headers <- [ auth.Header, auth.Value ]
        req.Body <- body

        match contentType, body with
        | Some value, Some _ -> req.Headers <- req.Headers @ [ "Content-Type", value ]
        | _ -> ()

        req

    let jsonRequest baseUrl auth method segments query body =
        request baseUrl auth method segments query (Some(JsonRuntime.encode body)) (Some "application/json")

    let emptyRequest baseUrl auth method segments query =
        request baseUrl auth method segments query None None

    let textRequest baseUrl auth method segments query body contentType =
        request baseUrl auth method segments query (Some body) (Some contentType)

    let ensureSuccess (response: HttpResponse) =
        if not (isSuccess response) then
            raise (DataHubError.FromResponse response)

    let decode (decoder: Decoder<'T>) (response: HttpResponse) =
        ensureSuccess response

        match JsonRuntime.decode decoder response.Body with
        | Ok value -> value
        | Error err -> raise (DataHubError(response.StatusCode, err, response.Body))

    let decodeArray (decoder: Decoder<'T>) (response: HttpResponse) =
        decode (Decode.array decoder) response

    let responseBody (response: HttpResponse) =
        ensureSuccess response
        response.Body
