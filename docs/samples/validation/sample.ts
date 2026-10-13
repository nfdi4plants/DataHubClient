// --8<-- [start:setup]
import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { ValidationPackageRef } from "@nfdi4plants/datahub-client/Models/ValidationPackageRef.js";

const client = new DataHubClient("https://git.nfdi4plants.org", Authentication.PersonalAccessToken("your-datahub-pat"));
const projectId = 42;
// --8<-- [end:setup]

// Offline verification harness, omitted from the documentation snippets.
import assert from "node:assert/strict";
const responses: [method: string, body: string][] = [
  [
    "GET",
    "[{\"name\":\"main\",\"type\":\"tree\"},{\"name\":\"README.md\",\"type\":\"blob\"}]"
  ],
  [
    "GET",
    "[{\"name\":\"invenio@3.1.0\",\"type\":\"tree\"}]"
  ],
  [
    "GET",
    "[{\"id\":\"abc123def456\",\"short_id\":\"abc123de\",\"title\":\"Update validation results\",\"message\":\"Update validation results\",\"author_name\":\"ARC Researcher\",\"author_email\":\"researcher@example.org\",\"created_at\":\"2026-10-13T10:00:00Z\"}]"
  ],
  [
    "GET",
    "{\"Critical\":{\"HasFailures\":false,\"Total\":10,\"Passed\":10,\"Failed\":0,\"Errored\":0},\"NonCritical\":{\"HasFailures\":true,\"Total\":4,\"Passed\":3,\"Failed\":1,\"Errored\":0},\"ValidationPackage\":{\"Name\":\"invenio\",\"Version\":\"3.1.0\",\"Summary\":\"Checks ARC publication requirements.\"}}"
  ],
  [
    "GET",
    "<testsuites><testsuite name=\"invenio\" tests=\"10\" failures=\"0\"/></testsuites>"
  ],
  [
    "GET",
    "<svg xmlns=\"http://www.w3.org/2000/svg\"><text>passed</text></svg>"
  ],
  [
    "GET",
    "[{\"name\":\"invenio@3.1.0\",\"type\":\"tree\"}]"
  ],
  [
    "GET",
    "{\"Critical\":{\"HasFailures\":false,\"Total\":10,\"Passed\":10,\"Failed\":0,\"Errored\":0},\"NonCritical\":{\"HasFailures\":true,\"Total\":4,\"Passed\":3,\"Failed\":1,\"Errored\":0},\"ValidationPackage\":{\"Name\":\"invenio\",\"Version\":\"3.1.0\",\"Summary\":\"Checks ARC publication requirements.\"}}"
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

// --8<-- [start:branches]
const branches = await client.Validation.ListValidatedBranchesAsync(projectId);
for (const branch of branches) console.log(branch);
// --8<-- [end:branches]

// --8<-- [start:packages]
const packages = await client.Validation.ListPackagesAsync(projectId, "main");
for (const pkg of packages) console.log(pkg.Path);
// --8<-- [end:packages]

// --8<-- [start:history]
const history = await client.Validation.ListHistoryAsync(projectId, "main");
for (const commit of history) console.log(`${commit.Id}: ${commit.Title}`);
// --8<-- [end:history]

// --8<-- [start:summary]
const packageRef = ValidationPackageRef.Create("invenio", "3.1.0", "main");
const summary = await client.Validation.GetSummaryAsync(projectId, packageRef);
console.log(`Critical: ${summary.Critical.Passed}/${summary.Critical.Total} passed`);
// --8<-- [end:summary]

// --8<-- [start:report]
const report = await client.Validation.GetReportAsync(projectId, packageRef);
console.log(report);
// --8<-- [end:report]

// --8<-- [start:badge]
const badge = await client.Validation.GetBadgeAsync(projectId, packageRef);
console.log(badge);
// --8<-- [end:badge]

// --8<-- [start:all-summaries]
const summaries = await client.Validation.GetAllSummariesAsync(projectId, "main");
for (const item of summaries)
  console.log(`${item.ValidationPackage.Name}: ${item.Critical.HasFailures}`);
// --8<-- [end:all-summaries]

globalThis.fetch = originalFetch;
assert.equal(responses.length, 0, "Expected every sample request to run.");
assert.deepEqual(branches, ["main"]);
assert.equal(summary.Critical.Passed, 10);
assert.equal(summaries.length, 1);
assert.match(report, /<testsuites>/);
assert.match(badge, /<svg/);
