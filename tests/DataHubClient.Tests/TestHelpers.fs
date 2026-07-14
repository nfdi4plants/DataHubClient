module DataHubClient.Tests.TestHelpers

#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
open Fable.Core
#endif

/// <summary>
/// Reverse bridge for the per-target public surface that <c>ResourceHelpers.toPublic</c>
/// produces — tests pipe every public API call through this so a single F# test
/// body keeps using <c>let!</c> after transpilation. On .NET the public methods
/// already return <c>Async&lt;T&gt;</c>, so the helper is identity; on JS we
/// await a <c>JS.Promise</c>, on Python a <c>Task</c>.
/// </summary>
#if FABLE_COMPILER_JAVASCRIPT || FABLE_COMPILER_TYPESCRIPT
let awaitApi (call: JS.Promise<'T>) : Async<'T> = Async.AwaitPromise call
#endif
#if FABLE_COMPILER_PYTHON
let awaitApi (call: System.Threading.Tasks.Task<'T>) : Async<'T> = Async.AwaitTask call
#endif
#if !FABLE_COMPILER
let awaitApi (call: Async<'T>) : Async<'T> = call
#endif
