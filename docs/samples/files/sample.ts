// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const client = new DataHubClient(
  "https://hub.example", Authentication.PersonalAccessToken("your-datahub-pat"));
// --8<-- [end:setup]

// Offline harness: validate requests through the shipped fetch transport.
const responses: Array<[method: string, url: string, status: number, body: unknown]> = [
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/files/README.md?ref=main",
    200,
    {
      "file_name": "README.md",
      "file_path": "README.md",
      "size": 3,
      "encoding": "base64",
      "content": "QVJD",
      "ref": "main",
      "blob_id": "blob1",
      "commit_id": "commit1"
    }
  ],
  [
    "POST",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt",
    201,
    {
      "file_path": "notes.txt",
      "branch": "main"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt?ref=main",
    200,
    {
      "file_name": "notes.txt",
      "file_path": "notes.txt",
      "size": 3,
      "encoding": "base64",
      "content": "QVJD",
      "ref": "main",
      "blob_id": "blob1",
      "commit_id": "commit1"
    }
  ],
  [
    "PUT",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt",
    200,
    {
      "file_path": "notes.txt",
      "branch": "main"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt?ref=main",
    200,
    {
      "file_name": "notes.txt",
      "file_path": "notes.txt",
      "size": 3,
      "encoding": "base64",
      "content": "QVJD",
      "ref": "main",
      "blob_id": "blob1",
      "commit_id": "commit1"
    }
  ],
  [
    "DELETE",
    "https://hub.example/api/v4/projects/42/repository/files/notes.txt",
    204,
    null
  ]
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (url, init) => {
  const expected = responses.shift();
  if (!expected || init?.method !== expected[0] || url !== expected[1])
    throw new Error(`Unexpected request: ${init?.method} ${url}`);
  return new Response(expected[3] === null ? null : JSON.stringify(expected[3]), { status: expected[2] });
};

// --8<-- [start:get]
const file = await client.Files.GetAsync(42, "README.md", "main");
const text = file.Encoding === "base64"
  ? Buffer.from(file.Content, "base64").toString("utf8") : file.Content;
console.log(text);
// --8<-- [end:get]

// --8<-- [start:create]
const created = await client.Files.CreateAsync(42, "notes.txt", "main", "ARC", "Add notes");
console.log(`Created ${created.FilePath} on ${created.Ref}`);
// --8<-- [end:create]

// --8<-- [start:update]
const updated = await client.Files.UpdateAsync(42, "notes.txt", "main", "QVJD", "Update notes", "base64");
console.log(`Updated ${updated.FilePath} at ${updated.CommitId}`);
// --8<-- [end:update]

// --8<-- [start:delete]
await client.Files.DeleteAsync(42, "notes.txt", "main", "Remove notes");
console.log("Deleted notes.txt");
// --8<-- [end:delete]

globalThis.fetch = originalFetch;
if (responses.length !== 0) throw new Error("Expected every documented call to run.");
