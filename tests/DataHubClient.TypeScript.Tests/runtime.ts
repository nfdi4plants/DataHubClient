import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { HttpRequest } from "@nfdi4plants/datahub-client/Http/HttpRequest.js";
import { HttpResponse } from "@nfdi4plants/datahub-client/Http/HttpResponse.js";
import { Project } from "@nfdi4plants/datahub-client/Models/Project.js";
import { User } from "@nfdi4plants/datahub-client/Models/User.js";
import { Issue } from "@nfdi4plants/datahub-client/Models/Issue.js";
import { DataHubError, NotFoundError } from "@nfdi4plants/datahub-client/Models/Errors.js";

function assert(condition: unknown, message: string): asserts condition {
  if (!condition) throw new Error(message);
}

const originalFetch = globalThis.fetch;
let requestCount = 0;
const projectJson = {
  id: 42,
  name: "Example ARC",
  path: "example",
  path_with_namespace: "lab/example",
  visibility: "public",
  web_url: "https://datahub.invalid/lab/example",
  description: null,
  default_branch: "main",
};
const userJson = {
  id: 1,
  username: "researcher",
  name: "ARC Researcher",
  state: "active",
  web_url: "https://datahub.invalid/researcher",
};
globalThis.fetch = async (input, init) => {
  requestCount += 1;
  const url = new URL(String(input));
  assert(url.origin === "https://datahub.invalid", "Expected the offline fixture origin.");
  assert(init?.method === "GET", "Expected a GET request.");
  const headers = new Headers(init.headers);
  assert(headers.get("PRIVATE-TOKEN") === "test-token", "Authentication header was lost.");
  assert(headers.has("X-DataHubClient-Version"), "Version header was lost.");

  if (url.pathname === "/api/v4/projects/42") {
    return Response.json(projectJson);
  }
  if (url.pathname === "/api/v4/projects") {
    return Response.json([projectJson]);
  }
  if (url.pathname === "/api/v4/projects/42/issues/3") {
    return Response.json({
      id: 7, iid: 3, project_id: 42, title: "Review ARC", state: "opened",
      author: userJson, assignees: [userJson], labels: ["review"],
      web_url: "https://datahub.invalid/issues/3", created_at: "2026-10-13", updated_at: "2026-10-13",
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

  const projects: Project[] = await client.Projects.ListAsync();
  assert(Array.isArray(projects), "List results must be ordinary JavaScript arrays.");
  assert(projects.map(item => item.Name).join() === "Example ARC", "Array map must expose decoded models.");
  projects.push(project);
  assert(projects.length === 2 && projects.pop() === project, "List results must support ordinary array mutation.");

  const decodedIssue = await client.Issues.GetAsync(42, 3);
  assert(Array.isArray(decodedIssue.Assignees) && Array.isArray(decodedIssue.Labels), "Decoded model collections must be arrays.");
  assert(decodedIssue.Assignees[0] instanceof User, "Collection items must retain model identity.");
  assert(decodedIssue.Assignees.map(user => user.Name).join() === "ARC Researcher", "Nested array map failed.");
  const author = decodedIssue.Assignees[0];
  const issue = new Issue(7, 3, 42, "Review ARC", "opened", author, [author], ["review"], "https://datahub.invalid/issues/3", "2026-10-13", "2026-10-13");
  issue.Assignees = [author];
  issue.Labels = ["metadata"];
  issue.Assignees.push(author);
  issue.Labels.push("review");
  assert(issue.Assignees.length === 2 && issue.Labels.join() === "metadata,review", "Model collection arguments and setters must accept arrays.");

  const request = new HttpRequest("https://datahub.invalid/api/v4/projects", "GET");
  assert(Array.isArray(request.Headers), "Request headers must default to an ordinary array.");
  request.Headers = [["Accept", "application/json"]];
  request.Headers.push(["X-Custom", "value"]);
  assert(request.Headers.map(([name]) => name).join() === "Accept,X-Custom", "Request headers must accept and mutate tuple arrays.");
  const response = new HttpResponse(200, "[]", [["Content-Type", "application/json"]]);
  assert(Array.isArray(response.Headers) && response.Headers.map(([name]) => name).join() === "Content-Type", "Response header arguments must accept tuple arrays.");

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
  assert(requestCount === 4, "Expected all four offline fetch requests to execute.");
  console.log("Packed TypeScript consumer runtime checks passed.");
} finally {
  globalThis.fetch = originalFetch;
}
