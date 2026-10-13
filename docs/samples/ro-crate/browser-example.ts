import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { downloadROCrate } from "./browser.js";

const client = new DataHubClient("https://git.nfdi4plants.org", Authentication.Anonymous());
const button = document.createElement("button");
button.textContent = "Download RO-Crate";
button.addEventListener("click", () => {
  downloadROCrate(client, 42).catch(console.error);
});
document.body.append(button);
