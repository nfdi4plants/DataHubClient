module DataHubClient.Python.Tests.HttpxHttpClientTests

open Fable.Core.PyInterop
open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Tests.TestHelpers

// Keep the real AsyncClient so HTTPX handles redirects and builds requests.
// MockTransport replaces only network I/O with a sequence of canned responses.
// Each case restores the original constructor, including when assertions fail.
do
    emitPyStatement () """
import httpx as _dhc_httpx

_dhc_async_client = _dhc_httpx.AsyncClient
_dhc_requests = []
_dhc_responses = []

def _dhc_handle_request(request):
    _dhc_requests.append(request)
    if not _dhc_responses:
        raise AssertionError("Unexpected HTTP request: " + str(request.url))
    status, body, headers = _dhc_responses.pop(0)
    return _dhc_httpx.Response(status, text=body, headers=list(headers))

def _dhc_install_httpx(responses):
    _dhc_requests.clear()
    _dhc_responses[:] = list(responses)
    _dhc_httpx.AsyncClient = lambda **kwargs: _dhc_async_client(
        transport=_dhc_httpx.MockTransport(_dhc_handle_request), **kwargs)
"""

/// <summary>Runs a case with real HTTPX behavior and canned network responses.</summary>
/// <param name="responses">The status, body, and headers returned for successive requests.</param>
/// <param name="work">The test to run before restoring the original client constructor.</param>
let private withMockHttpx (responses: (int * string * (string * string) list) list) (work: Async<unit>) =
    async {
        emitPyStatement responses "_dhc_install_httpx($0)"
        try
            return! work
        finally
            emitPyStatement () "_dhc_httpx.AsyncClient = _dhc_async_client"
    }

/// <summary>The number of requests handled by the mock transport.</summary>
let private requestCount () : int = emitPyExpr () "len(_dhc_requests)"

/// <summary>The HTTP method recorded for a request.</summary>
/// <param name="index">The zero-based request index.</param>
let private recMethod (index: int) : string = emitPyExpr index "_dhc_requests[$0].method"

/// <summary>The URL recorded for a request.</summary>
/// <param name="index">The zero-based request index.</param>
let private recUrl (index: int) : string = emitPyExpr index "str(_dhc_requests[$0].url)"

/// <summary>The UTF-8 body recorded for the first request.</summary>
let private recContent () : string = emitPyExpr () "_dhc_requests[0].content.decode('utf-8')"

/// <summary>The value of a header recorded for the first request.</summary>
/// <param name="name">The case-insensitive HTTP header name.</param>
let private recHeader (name: string) : string = emitPyExpr name "_dhc_requests[0].headers.get($0)"

/// <summary>The value of a header recorded for a request.</summary>
/// <param name="index">The zero-based request index.</param>
/// <param name="name">The case-insensitive HTTP header name.</param>
let private recHeaderAt (index: int) (name: string) : string =
    emitPyExpr (index, name) "_dhc_requests[$0].headers.get($1)"

/// <summary>Whether a recorded request contains a header.</summary>
/// <param name="index">The zero-based request index.</param>
/// <param name="name">The case-insensitive HTTP header name.</param>
let private recHasHeader (index: int) (name: string) : bool =
    emitPyExpr (index, name) "$1 in _dhc_requests[$0].headers"

/// <summary>Request mapping and redirect regression tests using the real Python HTTPX client.</summary>
let tests =
    testList "HttpxHttpClient" [

        testCaseAsync "maps method, url and request headers onto the httpx call" <|
            withMockHttpx [ 200, "ok", [] ] (async {
                let transport = HttpxHttpClient() :> IHttpClient

                let request = HttpRequest("https://hub.example/api/v4/projects", "GET")
                request.Headers <- ResizeArray([ "PRIVATE-TOKEN", "secret-token" ])

                let! _ = transport.SendAsync request

                Expect.equal (recUrl 0) "https://hub.example/api/v4/projects" "request url"
                Expect.equal (recMethod 0) "GET" "request method"
                Expect.equal (recHeader "PRIVATE-TOKEN") "secret-token" "auth header survives"
            })

        testCaseAsync "sends the request body through httpx" <|
            withMockHttpx [ 201, "{}", [] ] (async {
                let transport = HttpxHttpClient() :> IHttpClient

                let request = HttpRequest("https://hub.example/api/v4/projects/42/issues", "POST")
                request.Headers <- ResizeArray([ "Content-Type", "application/json" ])
                request.Body <- Some """{"title":"bug"}"""

                let! _ = transport.SendAsync request

                Expect.equal (recContent ()) """{"title":"bug"}""" "request body survives"
                Expect.equal (recHeader "Content-Type") "application/json" "content type survives"
            })

        testCaseAsync "maps the httpx response status, body and headers to HttpResponse" <|
            withMockHttpx [ 201, "created-body", [ "x-request-id", "req-7" ] ] (async {
                let transport = HttpxHttpClient() :> IHttpClient

                let! response = transport.SendAsync(HttpRequest("https://hub.example/api/v4/projects", "GET"))

                Expect.equal response.StatusCode 201 "status code"
                Expect.equal response.Body "created-body" "response body"
                Expect.isTrue
                    (response.Headers |> Seq.contains ("x-request-id", "req-7"))
                    "response header survives"
            })

        testCaseAsync "DownloadROCrateAsync follows a 302 to object storage and returns the JSON body" <|
            withMockHttpx [
                302, "redirecting", [ "location", "https://storage.example/exports/arc-ro-crate-metadata.json" ]
                200, """{"@context":"https://w3id.org/ro/crate/1.1/context","@graph":[]}""", [ "content-type", "application/ld+json" ]
            ] (async {
                let client = DataHubClient("https://hub.example/", Authentication.Anonymous())

                let! body = client.Packages.DownloadROCrateAsync(1040) |> awaitApi

                Expect.equal body """{"@context":"https://w3id.org/ro/crate/1.1/context","@graph":[]}""" "final JSON-LD body"
                Expect.equal (requestCount ()) 2 "both the package request and redirect are sent"
                Expect.equal (recUrl 0)
                    "https://hub.example/api/v4/projects/1040/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json"
                    "RO-Crate package coordinates"
                Expect.equal (recUrl 1) "https://storage.example/exports/arc-ro-crate-metadata.json" "redirect destination"
                Expect.equal (recMethod 0) "GET" "initial method"
                Expect.equal (recMethod 1) "GET" "redirect method"
            })

        testCaseAsync "DownloadROCrateAsync follows relative redirects and maps the final error" <|
            withMockHttpx [
                302, "redirecting", [ "location", "/exports/missing.json" ]
                404, "missing export", []
            ] (async {
                let client = DataHubClient("https://hub.example/", Authentication.Anonymous())

                try
                    let! _ = client.Packages.DownloadROCrateAsync(1040) |> awaitApi
                    failwith "expected NotFoundError"
                with
                | :? NotFoundError as error ->
                    Expect.equal error.StatusCode 404 "final response status"
                    Expect.equal error.Body "missing export" "final response body"

                Expect.equal (requestCount ()) 2 "the relative redirect is followed"
                Expect.equal (recUrl 1) "https://hub.example/exports/missing.json" "relative location resolves against the request URL"
            })

        testCaseAsync "same-origin redirects retain credentials and ordinary headers" <|
            withMockHttpx [
                302, "redirecting", [ "location", "/exports/metadata.json" ]
                200, "{}", []
            ] (async {
                let transport = HttpxHttpClient() :> IHttpClient
                let headers = [
                    "PRIVATE-TOKEN", "test-pat"
                    "JOB-TOKEN", "test-job"
                    "Authorization", "Bearer test-oauth"
                    "X-Request-Id", "trace-123"
                ]
                let request = HttpRequest("https://hub.example/packages/metadata.json", "GET")
                request.Headers <- ResizeArray headers

                let! response = transport.SendAsync request

                Expect.equal response.StatusCode 200 "redirect reaches the final response"
                Expect.equal (requestCount ()) 2 "same-origin redirect is followed"
                Expect.equal (recUrl 1) "https://hub.example/exports/metadata.json" "redirect destination"
                for name, value in headers do
                    Expect.equal (recHeaderAt 0 name) value (name + " reaches the original host")
                    Expect.equal (recHeaderAt 1 name) value (name + " survives a same-origin redirect")
            })

        testCaseAsync "cross-origin redirects strip credentials without restoring them on return" <|
            withMockHttpx [
                302, "redirecting", [ "location", "https://storage.example/metadata.json" ]
                302, "redirecting back", [ "location", "https://hub.example/exports/metadata.json" ]
                200, "{}", []
            ] (async {
                let transport = HttpxHttpClient() :> IHttpClient
                let credentials = [
                    "PRIVATE-TOKEN", "test-pat"
                    "JOB-TOKEN", "test-job"
                    "Authorization", "Bearer test-oauth"
                ]
                let request = HttpRequest("https://hub.example/packages/metadata.json", "GET")
                request.Headers <- ResizeArray(credentials @ [ "X-Request-Id", "trace-123" ])

                let! response = transport.SendAsync request

                Expect.equal response.StatusCode 200 "redirect chain reaches the final response"
                Expect.equal (requestCount ()) 3 "both redirects are followed"
                Expect.equal (recUrl 1) "https://storage.example/metadata.json" "cross-origin destination"
                Expect.equal (recUrl 2) "https://hub.example/exports/metadata.json" "return to the original origin"
                for name, value in credentials do
                    Expect.equal (recHeaderAt 0 name) value (name + " reaches the original host")
                    Expect.isFalse (recHasHeader 1 name) (name + " is stripped before crossing origins")
                    Expect.isFalse (recHasHeader 2 name) (name + " is not restored later in the chain")
                for index in [ 0; 1; 2 ] do
                    Expect.equal (recHeaderAt index "X-Request-Id") "trace-123" "ordinary headers survive the redirect chain"
            })
    ]
