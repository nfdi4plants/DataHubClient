import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { NotFoundError, UnauthorizedError } from "@nfdi4plants/datahub-client/Models/Errors.js";

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

// Execute the installed package with native fetch in both Node and browsers.
// The fixture permits every relevant CORS header, so a missing token cannot be
// attributed to a preflight denial. All tokens below are dummy fixture values.
export async function exerciseRedirects(baseUrl, runtime) {
  const results = [];
  const browser = runtime !== "node";
  const credentials = [
    ["anonymous", Authentication.Anonymous()],
    ["oauth", Authentication.OAuthToken("redirect-oauth")],
    ["private", Authentication.PersonalAccessToken("redirect-private")],
    ["job", Authentication.JobToken("redirect-job")],
  ];
  for (const [credential, authentication] of credentials) {
    for (const route of ["same", "cross", "return", "missing"]) {
      const id = `${runtime}-${credential}-${route}`;
      const client = new DataHubClient(`${baseUrl}/redirect/${id}/${route}`, authentication);
      const blocked = browser && (credential === "private" || credential === "job");
      let body;
      let error;
      try { body = await client.Packages.DownloadROCrateAsync(42); }
      catch (caught) { error = caught; }
      if (blocked) {
        assert(error && /redirect/i.test(String(error)), `${id}: opaque browser redirect must fail clearly.`);
        assert(!(error instanceof NotFoundError), `${id}: the browser must stop at the first redirect.`);
      } else if (route === "missing") {
        assert(error instanceof NotFoundError && error.StatusCode === 404, `${id}: final HTTP error was lost.`);
        assert(error.Body.includes("redirect destination missing"), `${id}: final error body was lost.`);
      } else {
        assert(!error, `${id}: ${String(error)}`);
        const final = JSON.parse(body);
        assert(final.route === route, `${id}: did not reach the intended destination.`);
        assert(final.method === "GET", `${id}: request method changed.`);
        const retained = route === "same";
        assert(final.headers["private-token"] === (retained && credential === "private" ? "redirect-private" : undefined), `${id}: PRIVATE-TOKEN retention/removal failed.`);
        assert(final.headers["job-token"] === (retained && credential === "job" ? "redirect-job" : undefined), `${id}: JOB-TOKEN retention/removal failed.`);
        assert(final.headers.authorization === (retained && credential === "oauth" ? "Bearer redirect-oauth" : undefined), `${id}: Authorization retention/removal failed.`);
      }
      results.push({ id, credential, route, blocked });
    }
  }
  if (!browser) {
    for (const route of ["put303", "put307", "loop"]) {
      const id = `${runtime}-private-${route}`;
      const client = new DataHubClient(`${baseUrl}/redirect/${id}/${route}`, Authentication.PersonalAccessToken("redirect-private"));
      if (route === "loop") {
        let error;
        try { await client.Packages.DownloadROCrateAsync(42); } catch (caught) { error = caught; }
        assert(error && /redirect/i.test(String(error)), `${id}: the redirect limit must terminate a loop.`);
      } else {
        const final = JSON.parse(await client.Packages.UploadGenericFileAsync(42, "redirect-fixture", "0.0.1", "test.txt", "redirect upload"));
        assert(final.headers["private-token"] === "redirect-private", `${id}: same-origin upload lost credentials.`);
        assert(final.method === (route === "put303" ? "GET" : "PUT"), `${id}: incorrect redirect method.`);
        assert(final.body === (route === "put303" ? "" : "redirect upload"), `${id}: incorrect redirect body.`);
        assert(final.headers["content-type"] === (route === "put303" ? undefined : "application/octet-stream"), `${id}: incorrect content header.`);
      }
      results.push({ id, credential: "private", route, blocked: false });
    }
  }
  return results;
}

// Successful authenticated downloads must return the actual crate, not an echo
// of the request. The fixture requires the bearer token before redirecting and
// rejects credentials at cross-origin storage destinations.
export async function exerciseAuthenticatedDownloads(baseUrl, runtime, expectedMetadata) {
  const downloads = [];
  for (const route of ["same", "cross", "return"]) {
    for (const api of ["ro-crate", "generic-file"]) {
      const id = `${runtime}-oauth-download-${route}-${api}`;
      const client = new DataHubClient(`${baseUrl}/download/${id}/${route}`, Authentication.OAuthToken("redirect-oauth"));
      const metadata = api === "ro-crate"
        ? await client.Packages.DownloadROCrateAsync(42)
        : await client.Packages.DownloadGenericFileAsync(42, "isa_arc_json", "0.0.1", "arc-ro-crate-metadata.json");
      assert(metadata === expectedMetadata, `${id}: downloaded metadata changed.`);
      const parsed = JSON.parse(metadata);
      assert(parsed["@context"] === "https://w3id.org/ro/crate/1.1/context", `${id}: missing RO-Crate context.`);
      assert(parsed["@graph"][0]["@id"] === "./", `${id}: missing root dataset.`);
      downloads.push({ id, credential: "oauth", route, blocked: false, api });
    }
  }

  // Prove the fixture really gates the download on authentication: neither a
  // missing token nor a wrong token may obtain a redirect or the crate.
  const denials = [];
  for (const [credential, authentication] of [
    ["anonymous", Authentication.Anonymous()],
    ["invalid-oauth", Authentication.OAuthToken("invalid-redirect-oauth")],
  ]) {
    const id = `${runtime}-${credential}-download-denied`;
    const client = new DataHubClient(`${baseUrl}/download/${id}/cross`, authentication);
    let error;
    try { await client.Packages.DownloadROCrateAsync(42); } catch (caught) { error = caught; }
    assert(error instanceof UnauthorizedError && error.StatusCode === 401, `${id}: unauthenticated download was not rejected.`);
    denials.push({ id, credential });
  }
  return { downloads, denials };
}
