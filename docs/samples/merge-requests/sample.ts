// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const client = new DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"));
const projectId = 42;
// --8<-- [end:setup]

// Offline verification harness, omitted from the documentation snippets.
import assert from "node:assert/strict";
const responses: [method: string, body: string][] = [
  [
    "GET",
    "[{\"id\":200,\"iid\":8,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"opened\",\"source_branch\":\"feature/assay\",\"target_branch\":\"main\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/merge_requests/8\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}]"
  ],
  [
    "GET",
    "{\"id\":200,\"iid\":8,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"opened\",\"source_branch\":\"feature/assay\",\"target_branch\":\"main\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/merge_requests/8\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}"
  ],
  [
    "POST",
    "{\"id\":200,\"iid\":8,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"opened\",\"source_branch\":\"feature/assay\",\"target_branch\":\"main\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/merge_requests/8\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}"
  ],
  [
    "PUT",
    "{\"id\":200,\"iid\":8,\"project_id\":42,\"title\":\"Complete assay metadata\",\"state\":\"opened\",\"source_branch\":\"feature/assay\",\"target_branch\":\"main\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/merge_requests/8\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Add protocol details.\"}"
  ],
  [
    "PUT",
    "{\"id\":200,\"iid\":8,\"project_id\":42,\"title\":\"Add assay metadata\",\"state\":\"closed\",\"source_branch\":\"feature/assay\",\"target_branch\":\"main\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"web_url\":\"https://git.nfdi4plants.org/examples/arc/-/merge_requests/8\",\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\",\"description\":\"Describe the assay.\"}"
  ],
  [
    "GET",
    "[{\"id\":5,\"body\":\"Metadata reviewed.\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"system\":false,\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\"}]"
  ],
  [
    "POST",
    "{\"id\":5,\"body\":\"Metadata reviewed.\",\"author\":{\"id\":7,\"username\":\"researcher\",\"name\":\"ARC Researcher\",\"state\":\"active\",\"web_url\":\"https://git.nfdi4plants.org/researcher\"},\"system\":false,\"created_at\":\"2026-10-13T10:00:00Z\",\"updated_at\":\"2026-10-13T10:00:00Z\"}"
  ]
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (_url, init) => {
  const response = responses.shift();
  assert.ok(response, "Unexpected extra HTTP request.");
  const [expectedMethod, body] = response;
  assert.equal(init?.method, expectedMethod);
  return new Response(body, { status: 200 });
};

// --8<-- [start:list]
const mergeRequests = await client.MergeRequests.ListAsync(projectId, "opened");
for (const item of mergeRequests) console.log(`${item.Iid}: ${item.Title}`);
// --8<-- [end:list]

// --8<-- [start:get]
const mergeRequest = await client.MergeRequests.GetAsync(projectId, 8);
console.log(mergeRequest.Title);
// --8<-- [end:get]

// --8<-- [start:create]
const created = await client.MergeRequests.CreateAsync(
  projectId, "feature/assay", "main", "Add assay metadata", "Describe the assay."
);
console.log(created.WebUrl);
// --8<-- [end:create]

// --8<-- [start:update]
const updated = await client.MergeRequests.UpdateAsync(
  projectId, 8, "Complete assay metadata", "Add protocol details."
);
console.log(updated.Title);
// --8<-- [end:update]

// --8<-- [start:close]
const closed = await client.MergeRequests.CloseAsync(projectId, 8);
console.log(closed.State);
// --8<-- [end:close]

// --8<-- [start:notes]
const notes = await client.MergeRequests.NotesAsync(projectId, 8);
for (const note of notes) console.log(note.Body);
// --8<-- [end:notes]

// --8<-- [start:create-note]
const note = await client.MergeRequests.CreateNoteAsync(projectId, 8, "Metadata reviewed.");
console.log(note.Body);
// --8<-- [end:create-note]

globalThis.fetch = originalFetch;
assert.equal(responses.length, 0, "Expected every sample request to run.");
assert.equal(closed.State, "closed");
assert.equal(updated.Title, "Complete assay metadata");
assert.equal(note.Body, "Metadata reviewed.");
