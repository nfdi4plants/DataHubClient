// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const client = new DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"));
const projectId = 42;
// --8<-- [end:setup]

// Offline verification harness, omitted from the documentation snippets.
import assert from "node:assert/strict";
const responses = [
  [
    "GET",
    "[{\"id\":9,\"name\":\"arc-export\",\"version\":\"1.0.0\",\"package_type\":\"generic\",\"status\":\"default\",\"created_at\":\"2026-10-13T10:00:00Z\"}]"
  ],
  [
    "GET",
    "{\"id\":9,\"name\":\"arc-export\",\"version\":\"1.0.0\",\"package_type\":\"generic\",\"status\":\"default\",\"created_at\":\"2026-10-13T10:00:00Z\"}"
  ],
  [
    "PUT",
    "{\"message\":\"201 Created\"}"
  ],
  [
    "GET",
    "{\"name\":\"My ARC\"}"
  ]
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (_url, init) => {
  assert.ok(responses.length > 0, "Unexpected extra HTTP request.");
  const [expectedMethod, body] = responses.shift();
  assert.equal(init.method, expectedMethod);
  return new Response(body, { status: 200 });
};

// --8<-- [start:list]
const packages = await client.Packages.ListAsync(projectId, "generic", "arc-export");
for (const pkg of packages) console.log(`${pkg.Name}@${pkg.Version}`);
// --8<-- [end:list]

// --8<-- [start:get]
const pkg = await client.Packages.GetAsync(projectId, 9);
console.log(pkg.Version);
// --8<-- [end:get]

// --8<-- [start:upload]
const uploadResponse = await client.Packages.UploadGenericFileAsync(
  projectId, "arc-export", "1.0.0", "manifest.json", JSON.stringify({ name: "My ARC" })
);
console.log(uploadResponse);
// --8<-- [end:upload]

// --8<-- [start:download]
const content = await client.Packages.DownloadGenericFileAsync(
  projectId, "arc-export", "1.0.0", "manifest.json"
);
console.log(content);
// --8<-- [end:download]

globalThis.fetch = originalFetch;
assert.equal(responses.length, 0, "Expected every sample request to run.");
assert.equal(pkg.Version, "1.0.0");
assert.deepEqual(JSON.parse(content), { name: "My ARC" });
