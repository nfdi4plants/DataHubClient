module TestTasks

open BlackFox.Fake
open Fake.Core
open Fake.DotNet

open ProjectInfo
open BasicTasks


let runTests = BuildTask.create "RunTests" [clean; buildSolution] {
    testProjects
    |> Seq.iter (fun testProject ->
        let result =
            DotNet.exec
                (fun opts ->
                    { opts with
                        CustomParams = Some "-tl"
                    })
                "run"
                $"--project {testProject} --configuration {configuration} --no-build"
        if not result.OK then
            failwithf "Tests failed for %s" testProject
    )
}

/// Transpiles the JavaScript/TypeScript Pyxpecto suite with Fable and runs it on
/// node. Fable — not `dotnet` — drives this target end to end: the suite, the
/// JS shim, and DataHubClient.Core.Javascript.fsproj are all transpiled together.
let runTestsJavaScript = BuildTask.create "RunTestsJavaScript" [clean] {
    let outDir = "dist/js-tests"

    let transpile =
        DotNet.exec id "fable" $"{javaScriptTestProject} --lang javascript -o {outDir} --noCache"
    if not transpile.OK then
        failwith "Fable transpilation of the JavaScript test suite failed"

    // node executes the ESM output only when the directory is marked as a module.
    System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "package.json"), "{ \"type\": \"module\" }")

    let run =
        CreateProcess.fromRawCommand "node" [ System.IO.Path.Combine(outDir, "Program.js") ]
        |> Proc.run
    if run.ExitCode <> 0 then
        failwith "JavaScript test suite failed"
}
