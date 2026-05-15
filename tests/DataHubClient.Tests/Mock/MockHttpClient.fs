module DataHubClient.Tests.Mock.MockHttpClient

open DataHubClient

type MockRoute(method: string, url: string, response: HttpResponse) =
    member _.Method = method
    member _.Url = url
    member _.Response = response

type MockHttpClient() =
    let mutable routes : MockRoute list = []
    let mutable requests : HttpRequest list = []

    member _.Add(method: string, url: string, statusCode: int, body: string) =
        routes <- MockRoute(method, url, HttpResponse(statusCode, body, [])) :: routes

    member _.Requests =
        requests |> List.rev |> List.toArray

    member _.LastRequest =
        requests |> List.head

    interface IHttpClient with
        member _.SendAsync(request: HttpRequest) =
            async {
                requests <- request :: requests

                match routes |> List.tryFind (fun route -> route.Method = request.Method && route.Url = request.Url) with
                | Some route -> return route.Response
                | None ->
                    let known =
                        routes
                        |> List.map (fun route -> route.Method + " " + route.Url)
                        |> String.concat "\n"

                    return failwith ("No mock route for " + request.Method + " " + request.Url + "\nKnown routes:\n" + known)
            }
