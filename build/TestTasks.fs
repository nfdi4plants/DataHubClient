module TestTasks

open BlackFox.Fake
open Fake.Core
open Fake.DotNet

open ProjectInfo
open BasicTasks

let private uvArgs args =
    [ "--cache-dir"; "/tmp/datahubclient-uv-cache" ] @ args


let runTestsDotNet =
    BuildTask.create "RunTestsDotNet" [ clean; buildSolution ] {
        testProjects
        |> Seq.iter (fun testProject ->
            let result =
                DotNet.exec
                    (fun opts -> { opts with CustomParams = Some "-tl" })
                    "run"
                    $"--project {testProject} --configuration {configuration} --no-build"

            if not result.OK then
                failwithf "Tests failed for %s" testProject)
    }

/// Transpiles the JavaScript/TypeScript Pyxpecto suite with Fable and runs it on
/// node. Fable — not `dotnet` — drives this target end to end: the suite, the
/// JS shim, and DataHubClient.Javascript.fsproj are all transpiled together.
let runTestsJavaScript =
    BuildTask.create "RunTestsJavaScript" [ generateVersionSource ] {
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

/// Transpiles the Python Pyxpecto suite with Fable and runs it on the uv-managed
/// interpreter. Fable — not `dotnet` — drives this target end to end: the suite,
/// the httpx shim, and DataHubClient.Python.fsproj are all transpiled
/// together. `uv run` resolves the dev environment from the root pyproject.toml.
let runTestsPython =
    BuildTask.create "RunTestsPython" [ generateVersionSource ] {
        let outDir = "dist/py-tests"

        let transpile =
            DotNet.exec id "fable" $"{pythonTestProject} --lang python -o {outDir} --noCache"

        if not transpile.OK then
            failwith "Fable transpilation of the Python test suite failed"

        let run =
            CreateProcess.fromRawCommand "uv" (uvArgs [ "run"; "python"; System.IO.Path.Combine(outDir, "program.py") ])
            |> Proc.run

        if run.ExitCode <> 0 then
            failwith "Python test suite failed"
    }

let runTestsAll =
    BuildTask.createEmpty "RunTestsAll" [ clean; buildSolution; runTestsJavaScript; runTestsPython; runTestsDotNet ]

/// Runs the shared live integration suite on .NET. The suite targets the
/// DataHub named by DATAHUB_TEST_URL / DATAHUB_TEST_TOKEN and skips every case
/// when they are unset, so this task is safe to run without credentials.
let runIntegrationTestsDotNet =
    BuildTask.create "RunIntegrationTestsDotNet" [ generateVersionSource ] {
        let result =
            DotNet.exec
                (fun opts -> { opts with CustomParams = Some "-tl" })
                "run"
                $"--project {integrationTestProject} --configuration {configuration}"

        if not result.OK then
            failwith "Integration tests failed (.NET)"
    }

/// Transpiles the live integration suite with Fable and runs it on node.
let runIntegrationTestsJavaScript =
    BuildTask.create "RunIntegrationTestsJavaScript" [ generateVersionSource ] {
        let outDir = "dist/js-integration-tests"

        let transpile =
            DotNet.exec id "fable" $"{integrationJavaScriptTestProject} --lang javascript -o {outDir} --noCache"

        if not transpile.OK then
            failwith "Fable transpilation of the JavaScript integration suite failed"

        // node executes the ESM output only when the directory is marked as a module.
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "package.json"), "{ \"type\": \"module\" }")

        let run =
            CreateProcess.fromRawCommand "node" [ System.IO.Path.Combine(outDir, "Program.js") ]
            |> Proc.run

        if run.ExitCode <> 0 then
            failwith "JavaScript integration suite failed"
    }

/// Transpiles the live integration suite with Fable and runs it on the
/// uv-managed interpreter.
let runIntegrationTestsPython =
    BuildTask.create "RunIntegrationTestsPython" [ generateVersionSource ] {
        let outDir = "dist/py-integration-tests"

        let transpile =
            DotNet.exec id "fable" $"{integrationPythonTestProject} --lang python -o {outDir} --noCache"

        if not transpile.OK then
            failwith "Fable transpilation of the Python integration suite failed"

        let run =
            CreateProcess.fromRawCommand "uv" (uvArgs [ "run"; "python"; System.IO.Path.Combine(outDir, "program.py") ])
            |> Proc.run

        if run.ExitCode <> 0 then
            failwith "Python integration suite failed"
    }

let runIntegrationTestsAll =
    BuildTask.createEmpty
        "RunIntegrationTestsAll"
        [ runIntegrationTestsJavaScript; runIntegrationTestsPython; runIntegrationTestsDotNet ]
