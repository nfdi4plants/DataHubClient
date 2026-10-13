// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";

const client = new DataHubClient(
  "https://hub.example", Authentication.Anonymous());
// --8<-- [end:setup]

// Offline harness: exercise the packaged client without contacting a DataHub.
const fixture = {
  id: 42,
  name: "My ARC",
  path: "my-arc",
  path_with_namespace: "lab/my-arc",
  visibility: "public",
  web_url: "https://hub.example/lab/my-arc"
};
const responses: Array<[url: string, body: unknown]> = [
  ["https://hub.example/api/v4/projects/42", fixture],
  ["https://hub.example/api/v4/projects", [fixture]]
];
const originalFetch = globalThis.fetch;
globalThis.fetch = async (url, init) => {
  const expected = responses.shift();
  if (!expected || init?.method !== "GET" || url !== expected[0])
    throw new Error(`Unexpected request: ${init?.method} ${url}`);
  return new Response(JSON.stringify(expected[1]), { status: 200 });
};

try {
  // --8<-- [start:inference]
  const project = await client.Projects.GetAsync(42); // Project
  const name = project.Name;                        // string
  const description = project.Description ?? "No description";
  console.log(name, description);
  // --8<-- [end:inference]

  // --8<-- [start:collections]
  const projects = await client.Projects.ListAsync(); // Project[] (one page)
  const names = projects.map(project => project.Name); // string[]
  console.log(projects.length, names);
  // --8<-- [end:collections]

  // --8<-- [start:typed-call]
  console.log(projectLabel(project));
  // --8<-- [end:typed-call]

  if (name !== "My ARC" || description !== "No description" ||
      names.join() !== "My ARC" || projectLabel(project) !== "lab/my-arc: My ARC")
    throw new Error("Unexpected typed example result.");
  if (responses.length !== 0)
    throw new Error("Expected every documented call to run.");
} finally {
  globalThis.fetch = originalFetch;
}

// --8<-- [start:typed-function]
import type { Project } from "@nfdi4plants/datahub-client/Models/Project.js";

function projectLabel(project: Project): string {
  return `${project.PathWithNamespace}: ${project.Name}`;
}
// --8<-- [end:typed-function]
