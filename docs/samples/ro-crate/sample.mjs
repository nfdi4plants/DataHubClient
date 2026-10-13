// --8<-- [start:setup]
import { writeFile } from "node:fs/promises";
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const client = new DataHubClient("https://git.nfdi4plants.org", Authentication.Anonymous());
const projectId = 42;
// --8<-- [end:setup]

// Offline harness: validate the real export coordinates without network access.
import assert from "node:assert/strict";
import { mkdtemp, readFile, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";

const fixture = '{"@context":"https://w3id.org/ro/crate/1.1/context","@graph":[{"@id":"./","@type":"Dataset","name":"Pflanzen – étude"}]}';
const expectedUrl = "https://git.nfdi4plants.org/api/v4/projects/42/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json";
const originalFetch = globalThis.fetch;
let requestCount = 0;
globalThis.fetch = async (url, init) => {
  requestCount += 1;
  assert.equal(url, expectedUrl);
  assert.equal(init.method, "GET");
  const headers = new Headers(init.headers);
  for (const name of ["PRIVATE-TOKEN", "Authorization", "JOB-TOKEN"])
    assert.equal(headers.has(name), false);
  return new Response(fixture, { status: 200 });
};
const originalDirectory = process.cwd();
const temporaryDirectory = await mkdtemp(join(tmpdir(), "datahubclient-ro-crate-"));
process.chdir(temporaryDirectory);

// --8<-- [start:download]

const metadata = await client.Packages.DownloadROCrateAsync(projectId);
await writeFile("arc-ro-crate-metadata.json", metadata, "utf8");
console.log("Saved arc-ro-crate-metadata.json");
// --8<-- [end:download]

try {
  assert.equal(requestCount, 1);
  assert.deepEqual(await readFile("arc-ro-crate-metadata.json"), Buffer.from(fixture, "utf8"));
} finally {
  process.chdir(originalDirectory);
  globalThis.fetch = originalFetch;
  await rm(temporaryDirectory, { recursive: true });
}
