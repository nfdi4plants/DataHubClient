module DataHubClient.Python.Tests.TranspileShapeTests

open Fable.Core.PyInterop
open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Tests.Mock.MockHttpClient

// Transpiled-shape smoke tests. These prove [<AttachMembers>] did its job: the
// F# members landed as real instance members on the Python output, so a Python
// consumer reaches them as ordinary attributes and methods. They run only on the
// transpiled target — there is nothing to assert on .NET.

/// `callable(getattr(target, name))` — whether a member is a callable method.
let private isCallable (target: obj) (name: string) : bool =
    emitPyExpr (target, name) "callable(getattr($0, $1, None))"

/// `type(value).__name__` — the runtime type name as Python sees it.
let private pyTypeName (value: obj) : string =
    emitPyExpr value "type($0).__name__"

let private makeClient () =
    let client = DataHubClient("https://hub.example/", Authentication.PersonalAccessToken "token")
    client.Http <- MockHttpClient()
    client

let tests =
    testList "TranspiledShape" [

        testCase "facade exposes resource APIs as objects" <| fun () ->
            let client = makeClient ()
            Expect.equal (pyTypeName client.Projects) "ProjectsApi" "Projects resolves to a ProjectsApi"
            Expect.equal (pyTypeName client.Issues) "IssuesApi" "Issues resolves to an IssuesApi"
            Expect.equal (pyTypeName client.Packages) "PackagesApi" "Packages resolves to a PackagesApi"

        testCase "resource API methods attach as callable members" <| fun () ->
            let client = makeClient ()
            Expect.isTrue (isCallable client.Projects "List") "Projects.List is callable"
            Expect.isTrue (isCallable client.Issues "Create") "Issues.Create is callable"
            Expect.isTrue (isCallable client.Files "Get") "Files.Get is callable"

        testCase "model scalar members attach as readable properties" <| fun () ->
            let project = Project(42, "My ARC", "my-arc", "lab/my-arc", "private", "https://hub/lab/my-arc")
            Expect.equal (pyTypeName project.Name) "str" "Name is a str property"
            Expect.equal project.Name "My ARC" "Name carries its value"
            Expect.equal project.Id 42 "Id carries its value"

        testCase "HttpxHttpClient attaches the IHttpClient contract method" <| fun () ->
            let transport = HttpxHttpClient()
            Expect.isTrue (isCallable transport "SendAsync") "SendAsync is callable"
    ]
