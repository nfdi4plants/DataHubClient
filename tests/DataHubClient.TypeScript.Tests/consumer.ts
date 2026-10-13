import { DataHubClient } from "@nfdi4plants/datahub-client";
import { Authentication } from "@nfdi4plants/datahub-client/Http/Authentication.js";
import { FetchHttpClient } from "@nfdi4plants/datahub-client/Http/FetchHttpClient.js";
import type { IHttpClient } from "@nfdi4plants/datahub-client/Http/IHttpClient.js";
import { Project } from "@nfdi4plants/datahub-client/Models/Project.js";
import type { User } from "@nfdi4plants/datahub-client/Models/User.js";
import type { Branch } from "@nfdi4plants/datahub-client/Models/Branch.js";
import type { Commit } from "@nfdi4plants/datahub-client/Models/Commit.js";
import type { RepoFile } from "@nfdi4plants/datahub-client/Models/RepoFile.js";
import type { Issue } from "@nfdi4plants/datahub-client/Models/Issue.js";
import type { MergeRequest } from "@nfdi4plants/datahub-client/Models/MergeRequest.js";
import type { Note } from "@nfdi4plants/datahub-client/Models/Note.js";
import type { Package } from "@nfdi4plants/datahub-client/Models/Package.js";
import { ValidationPackageRef } from "@nfdi4plants/datahub-client/Models/ValidationPackageRef.js";
import type { ValidationSummary } from "@nfdi4plants/datahub-client/Models/ValidationSummary.js";
import { DataHubError, NotFoundError } from "@nfdi4plants/datahub-client/Models/Errors.js";

// This function is compiled, never called: checking the public surface must not
// contact a DataHub or perform the write operations whose signatures it covers.
export function checkConsumerTypes(): void {
  const auth: Authentication = Authentication.PersonalAccessToken("test-token");
  Authentication.Anonymous();
  Authentication.OAuthToken("test-token");
  Authentication.JobToken("test-token");
  const header: string = auth.Header;
  const client = new DataHubClient("https://datahub.invalid", auth);
  const transport: IHttpClient = client.Http;
  client.Http = transport;
  client.Http = new FetchHttpClient();

  // Explicit Promise types catch accidental exposure of Fable Async values.
  const users: Promise<User> = client.Users.GetCurrentAsync();
  // Fable describes fixed-size F# arrays as MutableArray<T>. Verify their
  // element types and iteration without assuming resizable JavaScript arrays.
  const projects: Promise<Iterable<Project>> = client.Projects.ListAsync();
  const searched: Promise<Iterable<Project>> = client.Projects.ListAsync("ARC", true);
  const allProjects: Promise<Iterable<Project>> = client.Projects.ListAllAsync(undefined, true, "public");
  client.Projects.ListAsync().then(values => {
    const first: Project = values[0];
    const count: number = values.length;
    const names: string[] = Array.from(values, value => value.Name);
    // @ts-expect-error Array element properties must not collapse to any.
    const wrongName: number = values[0].Name;
    void [first, count, names, wrongName];
  });
  const project: Promise<Project> = client.Projects.GetAsync(42);
  const branches: Promise<Iterable<Branch>> = client.Repository.ListBranchesAsync(42);
  const commit: Promise<Commit> = client.Repository.GetCommitAsync(42, "abc123");
  const file: Promise<RepoFile> = client.Files.GetAsync(42, "README.md", "main");
  const deleted: Promise<void> = client.Files.DeleteAsync(42, "old.txt", "main", "Remove old file");
  const issues: Promise<Iterable<Issue>> = client.Issues.ListAsync(42);
  const issue: Promise<Issue> = client.Issues.CreateAsync(42, "Metadata review");
  const changedIssue: Promise<Issue> = client.Issues.UpdateAsync(42, 3, undefined, "Updated description");
  const mergeRequest: Promise<MergeRequest> = client.MergeRequests.CreateAsync(42, "feature", "main", "Review ARC");
  const notes: Promise<Iterable<Note>> = client.MergeRequests.NotesAsync(42, 3);
  const packages: Promise<Iterable<Package>> = client.Packages.ListAsync(42, "generic");
  const crate: Promise<string> = client.Packages.DownloadROCrateAsync(42);
  const validationPackage = ValidationPackageRef.Create("invenio", "1.0.0");
  const summary: Promise<ValidationSummary> = client.Validation.GetSummaryAsync(42, validationPackage);
  const report: Promise<string> = client.Validation.GetReportAsync(42, validationPackage, "cqc");

  // Constructors, optional model fields, mutable properties, and static factories
  // must remain ordinary TypeScript APIs without Fable-specific casts.
  const model = new Project(42, "Example", "example", "lab/example", "public", "https://datahub.invalid/lab/example");
  const id: number = model.Id;
  const description: string | undefined = model.Description;
  model.Id = 43;
  model.Name = "Renamed ARC";
  model.Description = "Description";
  model.Description = undefined;
  model.DefaultBranch = "main";
  const path: string = validationPackage.Path;
  validationPackage.Branch = "feature";
  const error: DataHubError = new NotFoundError("missing");
  const status: number = error.StatusCode;
  const body: string = error.Body;

  // Negative checks fail when declarations disappear or degrade to `any`.
  // @ts-expect-error Project identifiers are numeric.
  client.Projects.GetAsync("42");
  // @ts-expect-error Optional flags retain their boolean type.
  client.Projects.ListAsync(undefined, "true");
  // @ts-expect-error Authentication factories require string tokens.
  Authentication.PersonalAccessToken(123);
  // @ts-expect-error Mutable fields retain their declared value type.
  model.Name = 123;
  // @ts-expect-error Optional model fields accept strings or undefined.
  model.Description = 123;
  // @ts-expect-error Computed properties remain read-only.
  validationPackage.Path = "main/invenio@1.0.0";
  // @ts-expect-error Authentication metadata remains read-only.
  auth.Header = "Authorization";
  // @ts-expect-error Promise result types must not collapse to any.
  const wrongResult: Promise<string> = client.Projects.GetAsync(42);
  // @ts-expect-error A resource getter returns its concrete API class.
  client.Projects.UnknownOperation();

  void [header, users, projects, searched, allProjects, project, branches, commit,
    file, deleted, issues, issue, changedIssue, mergeRequest, notes, packages,
    crate, summary, report, id, description, path, status, body, wrongResult];
}
