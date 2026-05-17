namespace DataHubClient

open System
open System.Net.Http
open System.Net.Http.Headers

/// <summary>
/// .NET implementation of <see cref="T:DataHubClient.IHttpClient"/> backed by
/// <see cref="T:System.Net.Http.HttpClient"/>.
/// </summary>
/// <param name="httpClient">The <see cref="T:System.Net.Http.HttpClient"/> instance used to send requests.</param>
type DotNetHttpClient(httpClient: HttpClient) =

    static let sharedHttpClient = lazy (new HttpClient())

    /// <summary>
    /// Creates a transport using a shared <see cref="T:System.Net.Http.HttpClient"/> instance.
    /// </summary>
    new() = DotNetHttpClient(sharedHttpClient.Value)

    /// The underlying <see cref="T:System.Net.Http.HttpClient"/> used to send requests.
    member _.HttpClient = httpClient

    interface IHttpClient with
        /// <summary>Sends a request through <see cref="T:System.Net.Http.HttpClient"/>.</summary>
        /// <param name="request">The transport-agnostic request to send.</param>
        /// <returns>An async computation producing the transport-agnostic response.</returns>
        member _.SendAsync(request: HttpRequest) : Async<HttpResponse> =
            async {
                use message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url)

                match request.Body with
                | Some body ->
                    let content = new StringContent(body)
                    message.Content <- content
                | None -> ()

                for name, value in request.Headers do
                    if String.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase) then
                        match message.Content with
                        | null -> ()
                        | content -> content.Headers.ContentType <- MediaTypeHeaderValue.Parse(value)
                    else
                        let addedToRequest = message.Headers.TryAddWithoutValidation(name, value)

                        if not addedToRequest then
                            match message.Content with
                            | null -> ()
                            | content -> content.Headers.TryAddWithoutValidation(name, value) |> ignore

                let! response = httpClient.SendAsync(message) |> Async.AwaitTask
                let! body = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                let responseHeaders =
                    [
                        for header in response.Headers do
                            for value in header.Value do
                                header.Key, value

                        if not (isNull response.Content) then
                            for header in response.Content.Headers do
                                for value in header.Value do
                                    header.Key, value
                    ]

                return HttpResponse(int response.StatusCode, body, responseHeaders)
            }
