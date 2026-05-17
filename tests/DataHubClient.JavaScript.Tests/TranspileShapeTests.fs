module DataHubClient.JavaScript.Tests.TranspileShapeTests

open Fable.Core.JsInterop
open Fable.Pyxpecto
open DataHubClient
open DataHubClient.Tests.Mock.MockHttpClient

// Transpiled-shape smoke tests. These prove [<AttachMembers>] did its job: the
// F# members landed as real class/prototype members on the JavaScript output,
// so a JS/TS consumer reaches them as ordinary object properties and methods.
// They run only on the transpiled target — there is nothing to assert on .NET.

/// `typeof target[name]` — the runtime kind of a member as JavaScript sees it.
let private memberType (target: obj) (name: string) : string =
    emitJsExpr (target, name) "typeof $0[$1]"

let private makeClient () =
    DataHubClient("https://hub.example/", Authentication.PersonalAccessToken "token", MockHttpClient())

let tests =
    testList "TranspiledShape" [

        testCase "facade exposes resource APIs as object properties" <| fun () ->
            let client = makeClient ()
            Expect.equal (jsTypeof client.Projects) "object" "Projects resolves to an object"
            Expect.equal (jsTypeof client.Issues) "object" "Issues resolves to an object"
            Expect.equal (jsTypeof client.Packages) "object" "Packages resolves to an object"

        testCase "resource API methods attach as callable members" <| fun () ->
            let client = makeClient ()
            Expect.equal (memberType client.Projects "List") "function" "Projects.List is a function"
            Expect.equal (memberType client.Issues "Create") "function" "Issues.Create is a function"
            Expect.equal (memberType client.Files "Get") "function" "Files.Get is a function"

        testCase "model scalar members attach as readable properties" <| fun () ->
            let project = Project(42, "My ARC", "my-arc", "lab/my-arc", "private", "https://hub/lab/my-arc")
            Expect.equal (jsTypeof project.Name) "string" "Name is a string property"
            Expect.equal project.Name "My ARC" "Name carries its value"
            Expect.equal (jsTypeof project.Id) "number" "Id is a number property"

        testCase "FetchHttpClient attaches the IHttpClient contract method" <| fun () ->
            let transport = FetchHttpClient()
            Expect.equal (memberType transport "SendAsync") "function" "SendAsync is a function"
    ]
