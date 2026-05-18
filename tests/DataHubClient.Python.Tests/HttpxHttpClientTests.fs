module DataHubClient.Python.Tests.HttpxHttpClientTests

open Fable.Core.PyInterop
open Fable.Pyxpecto
open DataHubClient

// HttpxHttpClient calls the real `httpx.AsyncClient`; like FetchHttpClient on the
// JS side it cannot enter the shared Pyxpecto suite. This target-only suite swaps
// in a fake `httpx.AsyncClient` — mirroring the fake global `fetch` in
// DataHubClient.JavaScript.Tests — records the outgoing request, and asserts the
// request/response mapping.

// A fake httpx response and AsyncClient, defined once at module load. `request`
// is a native coroutine (so the transport's `await` succeeds) that records its
// arguments onto `_dhc_rec` and answers from the canned `_dhc_canned` response.
do
    emitPyStatement () """
import types as _dhc_types

_dhc_rec = _dhc_types.SimpleNamespace()
_dhc_canned = _dhc_types.SimpleNamespace(status=200, body="", headers=[])

class _DhcFakeResponse:
    def __init__(self):
        self.status_code = _dhc_canned.status
        self.text = _dhc_canned.body
        self.headers = _dhc_types.SimpleNamespace(items=lambda: list(_dhc_canned.headers))

class _DhcFakeAsyncClient:
    async def request(self, method, url, headers=None, content=None):
        _dhc_rec.method = method
        _dhc_rec.url = url
        _dhc_rec.headers = headers
        _dhc_rec.content = content
        return _DhcFakeResponse()

    async def aclose(self):
        _dhc_rec.closed = True
"""

// `installFakeHttpx` arms the canned response and points `httpx.AsyncClient` at
// the fake. Its emitted body must be a single line: Fable indents only the first
// line of a multi-line `emitPyStatement`, which works at module scope (the fake
// class block above) but corrupts statements emitted inside a function.

/// Installs the fake `httpx.AsyncClient` and arms it with a canned response.
let private installFakeHttpx (status: int) (body: string) (headers: (string * string) list) : unit =
    emitPyStatement
        (status, body, headers)
        "_dhc_canned.status = $0; _dhc_canned.body = $1; _dhc_canned.headers = list($2); __import__('httpx').AsyncClient = _DhcFakeAsyncClient"

/// The HTTP method recorded by the most recent fake request.
let private recMethod () : string = emitPyExpr () "_dhc_rec.method"

/// The URL recorded by the most recent fake request.
let private recUrl () : string = emitPyExpr () "_dhc_rec.url"

/// The body recorded by the most recent fake request.
let private recContent () : string = emitPyExpr () "_dhc_rec.content"

/// The value of a header recorded by the most recent fake request.
let private recHeader (name: string) : string = emitPyExpr name "dict(_dhc_rec.headers).get($0)"

let tests =
    testList "HttpxHttpClient" [

        testCaseAsync "maps method, url and request headers onto the httpx call" <| async {
            installFakeHttpx 200 "ok" []
            let transport = HttpxHttpClient() :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects", "GET")
            request.Headers <- [ "PRIVATE-TOKEN", "secret-token" ]

            let! _ = transport.SendAsync request

            Expect.equal (recUrl ()) "https://hub.example/api/v4/projects" "request url"
            Expect.equal (recMethod ()) "GET" "request method"
            Expect.equal (recHeader "PRIVATE-TOKEN") "secret-token" "auth header survives"
        }

        testCaseAsync "sends the request body through httpx" <| async {
            installFakeHttpx 201 "{}" []
            let transport = HttpxHttpClient() :> IHttpClient

            let request = HttpRequest("https://hub.example/api/v4/projects/42/issues", "POST")
            request.Headers <- [ "Content-Type", "application/json" ]
            request.Body <- Some """{"title":"bug"}"""

            let! _ = transport.SendAsync request

            Expect.equal (recContent ()) """{"title":"bug"}""" "request body survives"
            Expect.equal (recHeader "Content-Type") "application/json" "content type survives"
        }

        testCaseAsync "maps the httpx response status, body and headers to HttpResponse" <| async {
            installFakeHttpx 201 "created-body" [ "x-request-id", "req-7" ]
            let transport = HttpxHttpClient() :> IHttpClient

            let! response = transport.SendAsync(HttpRequest("https://hub.example/api/v4/projects", "GET"))

            Expect.equal response.StatusCode 201 "status code"
            Expect.equal response.Body "created-body" "response body"
            Expect.isTrue
                (response.Headers |> List.contains ("x-request-id", "req-7"))
                "response header survives"
        }
    ]
