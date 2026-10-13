# TypeScript

The npm package ships JavaScript ES modules and matching `.d.ts` declarations.
Use the **JavaScript / TypeScript** tabs throughout this guide for API calls;
TypeScript infers their types from the same imports. No separate `@types`
package is needed for DataHubClient.

## Setup

Install the package in your application:

```sh
npm install @nfdi4plants/datahub-client
npm install --save-dev typescript
```

### Node.js

Use a Node.js runtime with native `fetch`. Install Node's types and enable ES
modules in your application's `package.json`:

```sh
npm install --save-dev @types/node
npm pkg set type=module
```

For a standalone application with sources in `src/`, use this `tsconfig.json`:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "NodeNext",
    "moduleResolution": "NodeNext",
    "strict": true,
    "verbatimModuleSyntax": true,
    "noEmitOnError": true,
    "types": ["node"],
    "rootDir": "src",
    "outDir": "dist"
  },
  "include": ["src/**/*.ts"]
}
```

Save your program as `src/index.ts`, then compile and run it:

```sh
npx tsc
node dist/index.js
```

Keep the `.js` suffix on the package's deep imports, even inside `.ts` files.
TypeScript resolves the corresponding declarations; the emitted JavaScript
loads the `.js` module.

### Browser applications

Keep your framework's TypeScript configuration. For a standalone browser app
using a bundler such as Vite, a minimal `tsconfig.json` is:

```json
{
  "compilerOptions": {
    "target": "ES2022",
    "module": "ESNext",
    "moduleResolution": "Bundler",
    "lib": ["ES2022", "DOM"],
    "types": [],
    "strict": true,
    "verbatimModuleSyntax": true,
    "noEmit": true
  },
  "include": ["src/**/*.ts"]
}
```

Run `npx tsc` to check types and your bundler to build the application. These
settings follow TypeScript's distinction between
[Node.js and bundler module resolution](https://www.typescriptlang.org/docs/handbook/modules/reference.html).
DataHubClient uses the browser's `fetch`; cross-origin requests still require
the DataHub to allow your application's origin through CORS.

## Inferred types

Create a client using the same imports as JavaScript:

```typescript
--8<-- "docs/samples/typescript/sample.ts:setup"
```

Replace `https://hub.example` with your DataHub URL. Anonymous authentication
reads public resources; see [authentication](getting-started.md#authentication)
for private resources.

```typescript
--8<-- "docs/samples/typescript/sample.ts:inference"
```

`GetAsync` returns `Promise<Project>`, so `await` gives you a `Project` with typed
properties and editor completion. `Description` is optional (`string | undefined`).
You only need to import the `Project` type when you name it in your own code.
This is ordinary [TypeScript inference](https://www.typescriptlang.org/docs/handbook/2/everyday-types.html#type-annotations-on-variables).

Collection results are native arrays:

```typescript
--8<-- "docs/samples/typescript/sample.ts:collections"
```

Use array methods, indexing, and `.length` directly. `Projects.ListAsync` returns
one page; use `Projects.ListAllAsync` when you need every page.

## Explicit types for your own functions

Use `import type` to annotate parameters or return values with the library's
model types:

```typescript
--8<-- "docs/samples/typescript/sample.ts:typed-function"
```

Call it with an inferred result:

```typescript
--8<-- "docs/samples/typescript/sample.ts:typed-call"
```

Model types live under `Models/`, such as `Models/Project.js` and `Models/Issue.js`.
The package root exports `DataHubClient`. A
[type-only import](https://www.typescriptlang.org/docs/handbook/modules/reference.html#type-only-imports-and-exports)
is removed from emitted JavaScript. Use a regular import when you need the class
at runtime, for example to call `new Project(...)` or use `instanceof`.

## Browser downloads

The shared [RO-Crate guide](ro-crate.md) covers fetching metadata and saving it
in Node.js or the browser. For a browser helper written in TypeScript, annotate
its parameters and return type. Save this as `browser.ts`:

```typescript
--8<-- "docs/samples/ro-crate/browser.ts"
```

Then use it from your application's TypeScript entry point:

```typescript
--8<-- "docs/samples/ro-crate/browser-example.ts"
```

Replace the example DataHub URL and project ID with your own. The `./browser.js`
import resolves to `browser.ts` during type checking and to the emitted module
at runtime. Keep filesystem imports such as `node:fs/promises` in Node.js code;
browser saving uses `Blob` and an object URL.
