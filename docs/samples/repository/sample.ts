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
    "https://hub.example/api/v4/projects/42/repository/branches?search=main",
    200,
    [
      {
        "name": "main",
        "default": true,
        "protected": true,
        "merged": false,
        "commit": {
          "id": "abc123def456",
          "short_id": "abc123de",
          "title": "Add ARC metadata",
          "message": "Add ARC metadata",
          "author_name": "Carl Correns",
          "author_email": "carl@example.org",
          "created_at": "2026-05-01T00:00:00Z"
        }
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/branches/main",
    200,
    {
      "name": "main",
      "default": true,
      "protected": true,
      "merged": false,
      "commit": {
        "id": "abc123def456",
        "short_id": "abc123de",
        "title": "Add ARC metadata",
        "message": "Add ARC metadata",
        "author_name": "Carl Correns",
        "author_email": "carl@example.org",
        "created_at": "2026-05-01T00:00:00Z"
      }
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/commits?path=README.md&ref_name=main",
    200,
    [
      {
        "id": "abc123def456",
        "short_id": "abc123de",
        "title": "Add ARC metadata",
        "message": "Add ARC metadata",
        "author_name": "Carl Correns",
        "author_email": "carl@example.org",
        "created_at": "2026-05-01T00:00:00Z"
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42/repository/commits/abc123def456",
    200,
    {
      "id": "abc123def456",
      "short_id": "abc123de",
      "title": "Add ARC metadata",
      "message": "Add ARC metadata",
      "author_name": "Carl Correns",
      "author_email": "carl@example.org",
      "created_at": "2026-05-01T00:00:00Z"
    }
  ]
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (url, init) => {
  const expected = responses.shift();
  if (!expected || init?.method !== expected[0] || url !== expected[1])
    throw new Error(`Unexpected request: ${init?.method} ${url}`);
  return new Response(expected[3] === null ? null : JSON.stringify(expected[3]), { status: expected[2] });
};

// --8<-- [start:list-branches]
const branches = await client.Repository.ListBranchesAsync(42, "main");
for (const branch of branches) console.log(branch.Name, branch.Commit.ShortId);
// --8<-- [end:list-branches]

// --8<-- [start:get-branch]
const branch = await client.Repository.GetBranchAsync(42, "main");
console.log(`${branch.Name} points to ${branch.Commit.Id}`);
// --8<-- [end:get-branch]

// --8<-- [start:list-commits]
const commits = await client.Repository.ListCommitsAsync(42, "main", "README.md");
for (const commit of commits) console.log(commit.ShortId, commit.Title);
// --8<-- [end:list-commits]

// --8<-- [start:get-commit]
const commit = await client.Repository.GetCommitAsync(42, "abc123def456");
console.log(`${commit.Title} by ${commit.AuthorName}`);
// --8<-- [end:get-commit]

globalThis.fetch = originalFetch;
if (responses.length !== 0) throw new Error("Expected every documented call to run.");
