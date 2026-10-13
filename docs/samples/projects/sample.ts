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
    "https://hub.example/api/v4/projects?search=plant%20arc&simple=true",
    200,
    [
      {
        "id": 42,
        "name": "My ARC",
        "path": "my-arc",
        "path_with_namespace": "lab/my-arc",
        "visibility": "public",
        "web_url": "https://hub.example/lab/my-arc",
        "description": "An annotated research context",
        "default_branch": "main"
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects?order_by=id&pagination=keyset&per_page=100&sort=asc&visibility=public",
    200,
    [
      {
        "id": 42,
        "name": "My ARC",
        "path": "my-arc",
        "path_with_namespace": "lab/my-arc",
        "visibility": "public",
        "web_url": "https://hub.example/lab/my-arc",
        "description": "An annotated research context",
        "default_branch": "main"
      }
    ]
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects?id_after=42&order_by=id&pagination=keyset&per_page=100&sort=asc&visibility=public",
    200,
    []
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects/42",
    200,
    {
      "id": 42,
      "name": "My ARC",
      "path": "my-arc",
      "path_with_namespace": "lab/my-arc",
      "visibility": "public",
      "web_url": "https://hub.example/lab/my-arc",
      "description": "An annotated research context",
      "default_branch": "main"
    }
  ],
  [
    "GET",
    "https://hub.example/api/v4/projects?search=plant%20arc",
    200,
    [
      {
        "id": 42,
        "name": "My ARC",
        "path": "my-arc",
        "path_with_namespace": "lab/my-arc",
        "visibility": "public",
        "web_url": "https://hub.example/lab/my-arc",
        "description": "An annotated research context",
        "default_branch": "main"
      }
    ]
  ]
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (url, init) => {
  const expected = responses.shift();
  if (!expected || init?.method !== expected[0] || url !== expected[1])
    throw new Error(`Unexpected request: ${init?.method} ${url}`);
  return new Response(expected[3] === null ? null : JSON.stringify(expected[3]), { status: expected[2] });
};

// --8<-- [start:list]
const projects = await client.Projects.ListAsync("plant arc", true);
for (const project of projects) console.log(project.Id, project.PathWithNamespace);
// --8<-- [end:list]

// --8<-- [start:list-all]
const allProjects = await client.Projects.ListAllAsync(undefined, undefined, "public");
console.log(`Found ${allProjects.length} public projects`);
// --8<-- [end:list-all]

// --8<-- [start:get]
const project = await client.Projects.GetAsync(42);
console.log(project.Name, project.WebUrl);
// --8<-- [end:get]

// --8<-- [start:search]
const matches = await client.Projects.SearchAsync("plant arc");
for (const project of matches) console.log(project.PathWithNamespace);
// --8<-- [end:search]

globalThis.fetch = originalFetch;
if (responses.length !== 0) throw new Error("Expected every documented call to run.");
