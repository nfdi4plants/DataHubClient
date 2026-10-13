import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { Project } from "@nfdi4plants/datahub-client/Models/Project.js";
import { DataHubError, NotFoundError } from "@nfdi4plants/datahub-client/Models/Errors.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const originalFetch = globalThis.fetch;
let requestCount = 0;
globalThis.fetch = async (input, init) => {
  requestCount += 1;
  const url = new URL(String(input));
  assert(url.origin === "https://datahub.invalid", "Expected the offline fixture origin.");
  assert(init?.method === "GET", "Expected a GET request.");
  const headers = new Headers(init.headers);
  assert(headers.get("PRIVATE-TOKEN") === "test-token", "Authentication header was lost.");
  assert(headers.has("X-DataHubClient-Version"), "Version header was lost.");

  if (url.pathname === "/api/v4/projects/42") {
    return Response.json({
      id: 42,
      name: "Example ARC",
      path: "example",
      path_with_namespace: "lab/example",
      visibility: "public",
      web_url: "https://datahub.invalid/lab/example",
      description: null,
      default_branch: "main",
    });
  }
  if (url.pathname === "/api/v4/projects/404") {
    return Response.json({ message: "404 Project Not Found" }, { status: 404 });
  }
  throw new Error(`Unexpected request: ${url}`);
};

try {
  const client = new DataHubClient("https://datahub.invalid", Authentication.PersonalAccessToken("test-token"));
  const pending: Promise<Project> = client.Projects.GetAsync(42);
  assert(pending instanceof Promise, "Resource methods must return native Promises.");
  const project = await pending;
  assert(project instanceof Project, "Decoded projects must use the exported model class.");
  assert(project.Id === 42 && project.Name === "Example ARC", "Project decoding failed.");
  assert(project.Description === undefined, "An absent optional field must read as undefined.");
  project.Name = "Renamed ARC";
  project.Description = "Updated description";
  assert(project.Name === "Renamed ARC" && project.Description === "Updated description", "Model setters failed.");
  project.Description = undefined;
  assert(project.Description === undefined, "Optional setters must accept undefined.");

  let rejected = false;
  try {
    await client.Projects.GetAsync(404);
  } catch (error: unknown) {
    rejected = true;
    assert(error instanceof NotFoundError, "Expected the exported NotFoundError class.");
    assert(error instanceof DataHubError, "Expected the exported base error class.");
    assert(error.StatusCode === 404 && error.Body.includes("Project Not Found"), "Error details were lost.");
  }
  assert(rejected, "A failed request must reject its Promise.");
  assert(requestCount === 2, "Expected both offline requests to execute.");
  console.log("Packed TypeScript consumer runtime checks passed.");
} finally {
  globalThis.fetch = originalFetch;
}
