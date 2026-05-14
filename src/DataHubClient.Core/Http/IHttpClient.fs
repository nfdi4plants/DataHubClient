namespace DataHubClient

type IHttpClient =
    abstract SendAsync : HttpRequest -> Async<HttpResponse>
