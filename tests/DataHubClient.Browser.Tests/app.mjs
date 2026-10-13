import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { Project } from "@nfdi4plants/datahub-client/Models/Project.js";
import { DataHubError, NotFoundError, UnauthorizedError } from "@nfdi4plants/datahub-client/Models/Errors.js";
import { downloadROCrate } from "./download-ro-crate.mjs";

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

// This module runs inside an actual browser, with native fetch and no Node shims.
globalThis.compatibility = {
  async exercise(baseUrl) {
    assert(typeof process === "undefined" && typeof Buffer === "undefined", "Unexpected Node polyfills.");
    const client = new DataHubClient(baseUrl, Authentication.PersonalAccessToken("browser-test-token"));
    const pending = client.Projects.GetAsync(42);
    assert(pending instanceof Promise, "Expected a native Promise.");
    const project = await pending;
    assert(project instanceof Project && project.Name === "Browser ARC", "Project decoding/class identity failed.");
    assert(project.Description === undefined, "Optional fields must decode to undefined.");
    project.Name = "Renamed ARC";
    assert(project.Name === "Renamed ARC", "Model setter failed.");

    const anonymous = new DataHubClient(baseUrl, Authentication.Anonymous());
    const projects = await anonymous.Projects.ListAllAsync(undefined, true, "public");
    assert(projects.length === 1 && projects[0].Id === 42, "Anonymous pagination failed.");
    assert(Array.isArray(projects) && projects.map(item => item.Id).join() === "42", "Public collections must be ordinary browser arrays.");
    projects.push(project);
    assert(projects.length === 2 && projects.pop() === project, "Public collections must support array mutation.");
    const file = await client.Files.GetAsync(42, "notes/été.txt", "main");
    const text = new TextDecoder().decode(Uint8Array.from(atob(file.Content), character => character.charCodeAt(0)));
    assert(file.Encoding === "base64" && text === "Pflanzen – étude", "Browser file decoding failed.");
    const uploaded = await client.Packages.UploadGenericFileAsync(42, "browser-fixture", "0.0.1", "hello.txt", text);
    assert(uploaded === "Created", "Package upload failed.");
    const { metadata } = await import("./download-only.mjs");
    assert(JSON.parse(metadata)["@graph"][0].name === text, "Metadata-only documentation snippet failed.");
    const typedExample = await import("./download-only-typescript.mjs");
    assert(typedExample.metadata === metadata, "TypeScript metadata-only documentation snippet failed.");

    for (const [status, ErrorClass] of [[404, NotFoundError], [401, UnauthorizedError]]) {
      let caught;
      try { await client.Projects.GetAsync(status); } catch (error) { caught = error; }
      assert(caught instanceof ErrorClass && caught instanceof DataHubError, `Incorrect HTTP ${status} error class.`);
      assert(caught.StatusCode === status && caught.Body.includes("fixture error"), "Error details were lost.");
    }
    return { projectId: project.Id, fileText: text, projects: projects.length };
  },

  async checkCorsDenial(baseUrl) {
    const client = new DataHubClient(`${baseUrl}/denied`, Authentication.Anonymous());
    try { await client.Projects.GetAsync(42); } catch (error) {
      assert(error instanceof TypeError, "CORS failures should be browser fetch errors.");
      return true;
    }
    throw new Error("The browser allowed a response without CORS permission.");
  },

  prepareDownload(baseUrl) {
    const client = new DataHubClient(baseUrl, Authentication.Anonymous());
    document.querySelector("#download").onclick = () => {
      downloadROCrate(client, 42).catch(error => { globalThis.downloadError = String(error); });
    };
  },

  async prepareTypeScriptDownload() {
    await import("./browser-example.js");
  },
};
