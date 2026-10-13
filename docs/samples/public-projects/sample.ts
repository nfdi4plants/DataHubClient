// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const url = "https://git.nfdi4plants.org";
const publicClient = new DataHubClient(url, Authentication.Anonymous());
// --8<-- [end:setup]

// Offline CI harness: replace fetch before the first request.
const originalFetch = globalThis.fetch;
const requests: { url: URL; init: RequestInit | undefined }[] = [];
globalThis.fetch = async (requestUrl, init) => {
  requests.push({ url: new URL(String(requestUrl)), init });
  const id = requests.length;
  if (id > 3) throw new Error("Unexpected extra pagination request.");
  const projects = id < 3 ? [{
    id, name: `ARC ${id}`, path: `arc-${id}`,
    path_with_namespace: `examples/arc-${id}`, visibility: "public",
    web_url: `${url}/examples/arc-${id}`,
  }] : [];
  return new Response(JSON.stringify(projects), { status: 200 });
};

// --8<-- [start:list]

const projects = await publicClient.Projects.ListAllAsync(undefined, undefined, "public");
console.log(`Found ${projects.length} public projects`);
// --8<-- [end:list]

globalThis.fetch = originalFetch;

import assert from "node:assert/strict";
assert.deepEqual(projects.map(project => project.Id), [1, 2]);
assert.equal(requests.length, 3);
for (const { url: requestUrl, init } of requests) {
  assert.equal(init?.method, "GET");
  for (const [name, value] of Object.entries({
    pagination: "keyset", order_by: "id", sort: "asc", per_page: "100", visibility: "public",
  })) assert.equal(requestUrl.searchParams.get(name), value);
  const headers = new Headers(init?.headers);
  for (const name of ["PRIVATE-TOKEN", "Authorization", "JOB-TOKEN"])
    assert.equal(headers.has(name), false);
}
assert.equal(requests[0].url.searchParams.has("id_after"), false);
assert.equal(requests[1].url.searchParams.get("id_after"), "1");
assert.equal(requests[2].url.searchParams.get("id_after"), "2");
