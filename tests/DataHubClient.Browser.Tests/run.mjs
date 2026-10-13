import assert from "node:assert/strict";
import { spawnSync } from "node:child_process";
import { createServer } from "node:http";
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { readFile } from "node:fs/promises";
import { dirname, extname, isAbsolute, join, relative, resolve, sep } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { chromium, firefox } from "playwright";
import ts from "typescript";
import { build, createServer as createViteServer, preview } from "vite";

const fixtureDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(fixtureDirectory, "../..");
if (!process.argv[2]) throw new Error("Usage: node tests/DataHubClient.Browser.Tests/run.mjs <package.tgz>");
const tarball = resolve(process.argv[2]);
assert(existsSync(tarball), `Missing packed npm artifact: ${tarball}`);
const outputRoot = join(repositoryRoot, "artifacts/browser");
mkdirSync(outputRoot, { recursive: true });
const consumerDirectory = mkdtempSync(join(outputRoot, "consumer-"));
copyFileSync(join(fixtureDirectory, "app.mjs"), join(consumerDirectory, "app.mjs"));
copyFileSync(join(repositoryRoot, "docs/samples/ro-crate/browser.mjs"), join(consumerDirectory, "download-ro-crate.mjs"));
writeFileSync(join(consumerDirectory, "package.json"), JSON.stringify({
  name: "datahub-client-browser-consumer", private: true, type: "module",
  dependencies: { "@nfdi4plants/datahub-client": pathToFileURL(tarball).href },
}, null, 2));
// The native ESM case resolves the package through a browser import map.
// Vite's development and production cases resolve these same npm imports.
writeFileSync(join(consumerDirectory, "index.html"), `<!doctype html>
<html lang="en"><head><meta charset="UTF-8"><title>DataHubClient browser checks</title>
<link rel="icon" href="data:,">
<script type="importmap">${JSON.stringify({ imports: {
  "@nfdi4plants/datahub-client": "/node_modules/@nfdi4plants/datahub-client/DataHubClient.js",
  "@nfdi4plants/datahub-client/": "/node_modules/@nfdi4plants/datahub-client/",
} })}</script></head><body><button id="download">Download RO-Crate</button>
<script type="module" src="/app.mjs"></script></body></html>`);

const installArgs = ["install", "--offline", "--ignore-scripts", "--no-audit", "--no-fund", "--package-lock=false"];
const command = process.platform === "win32" ? process.env.ComSpec || "cmd.exe" : "npm";
const args = process.platform === "win32" ? ["/d", "/s", "/c", `npm ${installArgs.join(" ")}`] : installArgs;
const installed = spawnSync(command, args, {
  cwd: consumerDirectory, stdio: "inherit",
  env: { ...process.env, npm_config_cache: join(outputRoot, "npm-cache") },
});
if (installed.error) throw installed.error;
assert.equal(installed.status, 0, "Offline installation of the tarball failed.");
const packageMetadata = JSON.parse(readFileSync(join(consumerDirectory, "node_modules/@nfdi4plants/datahub-client/package.json"), "utf8"));

// RunDocsSamplesTypeScript checks these sources in strict mode. Here we emit
// the same examples as JavaScript so native browser modules can execute them.
function writeTypeScriptExample(filename, source) {
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { target: ts.ScriptTarget.ES2022, module: ts.ModuleKind.ESNext },
  });
  writeFileSync(join(consumerDirectory, filename), outputText);
}

const fileText = "Pflanzen – étude";
const crate = JSON.stringify({ "@context": "https://w3id.org/ro/crate/1.1/context", "@graph": [{ "@id": "./", "@type": "Dataset", name: fileText }] });
const project = { id: 42, name: "Browser ARC", path: "browser", path_with_namespace: "lab/browser", visibility: "public", web_url: "https://example.invalid/lab/browser", description: null };
const cratePath = "/api/v4/projects/42/packages/generic/isa_arc_json/0.0.1/arc-ro-crate-metadata.json";
const uploadPath = "/api/v4/projects/42/packages/generic/browser-fixture/0.0.1/hello.txt";
const allowedOrigins = new Set();
const requests = [];
const fixtureErrors = [];

// A separate origin exercises real fetch, preflight, headers and UTF-8 bodies.
// No page.route interception or fetch replacement bypasses browser CORS checks.
const api = createServer(async (request, response) => {
  try {
    const chunks = [];
    for await (const chunk of request) chunks.push(chunk);
    const body = Buffer.concat(chunks).toString("utf8");
    const url = new URL(request.url, "http://localhost");
    requests.push({ method: request.method, path: url.pathname, headers: request.headers, body });
    assert(allowedOrigins.has(request.headers.origin), "Unexpected fixture request origin.");
    if (!url.pathname.startsWith("/denied/")) {
      response.setHeader("Access-Control-Allow-Origin", request.headers.origin);
      response.setHeader("Access-Control-Allow-Methods", "GET, PUT, OPTIONS");
      response.setHeader("Access-Control-Allow-Headers", "PRIVATE-TOKEN, X-DataHubClient-Version, Content-Type");
      response.setHeader("Vary", "Origin");
    }
    if (request.method === "OPTIONS") { response.writeHead(204).end(); return; }
    assert.equal(request.headers["x-datahubclient-version"], packageMetadata.version);
    const anonymous = url.pathname === "/api/v4/projects" || url.pathname === cratePath;
    assert.equal(request.headers["private-token"], anonymous ? undefined : "browser-test-token");
    assert.equal(request.headers.authorization, undefined);
    assert.equal(request.headers["job-token"], undefined);

    let value;
    if (request.method === "GET" && url.pathname === "/api/v4/projects/42") value = project;
    else if (request.method === "GET" && url.pathname === "/api/v4/projects") {
      assert.equal(url.searchParams.get("pagination"), "keyset");
      assert.equal(url.searchParams.get("visibility"), "public");
      const after = url.searchParams.get("id_after");
      assert(after === null || after === "42");
      value = after ? [] : [project];
    } else if (request.method === "GET" && /^\/api\/v4\/projects\/(401|404)$/.test(url.pathname)) {
      response.statusCode = Number(url.pathname.split("/").at(-1));
      value = { message: "fixture error" };
    } else if (request.method === "GET" && decodeURIComponent(url.pathname) === "/api/v4/projects/42/repository/files/notes/été.txt") {
      assert.equal(url.searchParams.get("ref"), "main");
      value = { file_name: "été.txt", file_path: "notes/été.txt", encoding: "base64", content: Buffer.from(fileText).toString("base64"), size: Buffer.byteLength(fileText), ref: "main", blob_id: "abc123", commit_id: "def456" };
    } else if (request.method === "PUT" && url.pathname === uploadPath) {
      assert.equal(body, fileText);
      assert.equal(request.headers["content-type"], "application/octet-stream");
      response.writeHead(201, { "Content-Type": "text/plain; charset=utf-8" }).end("Created"); return;
    } else if (request.method === "GET" && url.pathname === cratePath) {
      response.writeHead(200, { "Content-Type": "application/ld+json; charset=utf-8" }).end(crate); return;
    } else throw new Error(`Unexpected request: ${request.method} ${request.url}`);
    response.setHeader("Content-Type", "application/json");
    response.end(JSON.stringify(value));
  } catch (error) {
    fixtureErrors.push(error);
    response.writeHead(500).end(String(error));
  }
});

function listen(server) {
  return new Promise((resolveListen, reject) => {
    server.once("error", reject);
    server.listen(0, "127.0.0.1", () => resolveListen(`http://127.0.0.1:${server.address().port}`));
  });
}
function close(server) {
  server.closeAllConnections();
  return new Promise((resolveClose, reject) => server.close(error => error ? reject(error) : resolveClose()));
}
function staticServer(root) {
  return createServer(async (request, response) => {
    try {
      const pathname = decodeURIComponent(new URL(request.url, "http://localhost").pathname);
      const path = resolve(root, `.${pathname === "/" ? "/index.html" : pathname}`);
      const withinRoot = relative(root, path);
      if (isAbsolute(withinRoot) || withinRoot === ".." || withinRoot.startsWith(`..${sep}`)) {
        response.writeHead(403).end(); return;
      }
      const data = await readFile(path);
      const type = { ".html": "text/html", ".js": "text/javascript", ".mjs": "text/javascript", ".json": "application/json" }[extname(path)] || "application/octet-stream";
      response.writeHead(200, { "Content-Type": `${type}; charset=utf-8` }).end(data);
    } catch { response.writeHead(404).end(); }
  });
}

const cleanups = [];
const results = [];
const originalNodeEnv = process.env.NODE_ENV;
try {
  const apiOrigin = await listen(api);
  cleanups.push(() => close(api));
  // Execute the exact metadata-only documentation snippet in every browser mode.
  // Only the server URL changes so requests stay on the local fixture.
  const exampleSource = readFileSync(join(repositoryRoot, "docs/samples/ro-crate/sample.mjs"), "utf8");
  const exampleRegion = name => exampleSource.split(`// --8<-- [start:${name}]`)[1].split(`// --8<-- [end:${name}]`)[0];
  writeFileSync(join(consumerDirectory, "download-only.mjs"),
    `${exampleRegion("setup")}\n${exampleRegion("download")}\nexport { metadata };\n`
      .replace("https://git.nfdi4plants.org", apiOrigin));
  const typeScriptSource = readFileSync(join(repositoryRoot, "docs/samples/ro-crate/sample.ts"), "utf8");
  const typeScriptRegion = name => typeScriptSource.split(`// --8<-- [start:${name}]`)[1].split(`// --8<-- [end:${name}]`)[0];
  writeTypeScriptExample("download-only-typescript.mjs",
    `${typeScriptRegion("setup")}\n${typeScriptRegion("download")}\nexport { metadata };\n`
      .replace("https://git.nfdi4plants.org", apiOrigin));
  writeTypeScriptExample("browser.js", readFileSync(join(repositoryRoot, "docs/samples/ro-crate/browser.ts"), "utf8"));
  writeTypeScriptExample("browser-example.js",
    readFileSync(join(repositoryRoot, "docs/samples/ro-crate/browser-example.ts"), "utf8")
      .replace("https://git.nfdi4plants.org", apiOrigin));
  const native = staticServer(consumerDirectory);
  const nativeOrigin = await listen(native);
  cleanups.push(() => close(native));
  const viteOptions = { configFile: false, root: consumerDirectory, logLevel: "warn", cacheDir: join(consumerDirectory, ".vite") };
  // Vite's dev server sets NODE_ENV; compile production before starting it so
  // production conditions and optimizations cannot inherit development mode.
  process.env.NODE_ENV = "production";
  await build({ ...viteOptions, build: { outDir: "bundled", target: "es2022", rollupOptions: {
    onwarn(warning, handler) {
      if (warning.code === "UNRESOLVED_IMPORT" || /externalized for browser compatibility/i.test(warning.message)) throw new Error(warning.message);
      handler(warning);
    },
  } } });
  const production = await preview({ ...viteOptions, build: { outDir: "bundled" }, preview: { host: "127.0.0.1", port: 0 } });
  cleanups.push(() => close(production.httpServer));
  const productionOrigin = `http://127.0.0.1:${production.httpServer.address().port}`;
  process.env.NODE_ENV = "development";
  const development = await createViteServer({ ...viteOptions, server: { host: "127.0.0.1", port: 0 } });
  await development.listen();
  cleanups.push(() => development.close());
  const developmentOrigin = `http://127.0.0.1:${development.httpServer.address().port}`;

  for (const origin of [nativeOrigin, developmentOrigin, productionOrigin]) allowedOrigins.add(origin);
  for (const [browserName, browserType] of [["chromium", chromium], ["firefox", firefox]]) {
    const browser = await browserType.launch({ headless: true });
    try {
      for (const [mode, origin] of [["native-esm", nativeOrigin], ["vite-development", developmentOrigin], ["vite-production", productionOrigin]]) {
        const context = await browser.newContext({ acceptDownloads: true });
        try {
          const page = await context.newPage();
          const pageErrors = [];
          page.on("pageerror", error => pageErrors.push(String(error)));
          page.setDefaultTimeout(15_000);
          const firstRequest = requests.length;
          await page.goto(origin);
          await page.waitForFunction(() => Boolean(globalThis.compatibility));
          const values = await page.evaluate(baseUrl => globalThis.compatibility.exercise(baseUrl), apiOrigin);
          assert.deepEqual(values, { projectId: 42, fileText, projects: 1 });
          await page.evaluate(baseUrl => globalThis.compatibility.prepareDownload(baseUrl), apiOrigin);
          const downloadPromise = page.waitForEvent("download");
          await page.locator("#download").click();
          let download;
          try { download = await downloadPromise; } catch (error) {
            const downloadError = await page.evaluate(() => globalThis.downloadError);
            if (downloadError) throw new Error(`Browser download failed: ${downloadError}`, { cause: error });
            throw error;
          }
          assert.equal(download.suggestedFilename(), "arc-ro-crate-metadata.json");
          const downloadPath = join(consumerDirectory, `${browserName}-${mode}.json`);
          await download.saveAs(downloadPath);
          assert.deepEqual(await readFile(downloadPath), Buffer.from(crate, "utf8"));
          await page.evaluate(() => globalThis.compatibility.prepareTypeScriptDownload());
          const typeScriptDownloadPromise = page.waitForEvent("download");
          await page.getByRole("button", { name: "Download RO-Crate", exact: true }).last().click();
          const typeScriptDownload = await typeScriptDownloadPromise;
          assert.equal(typeScriptDownload.suggestedFilename(), "arc-ro-crate-metadata.json");
          const typeScriptDownloadPath = join(consumerDirectory, `${browserName}-${mode}-typescript.json`);
          await typeScriptDownload.saveAs(typeScriptDownloadPath);
          assert.deepEqual(await readFile(typeScriptDownloadPath), Buffer.from(crate, "utf8"));
          assert.equal(await page.evaluate(baseUrl => globalThis.compatibility.checkCorsDenial(baseUrl), apiOrigin), true);
          assert.deepEqual(pageErrors, [], "Unexpected uncaught browser errors.");
          assert.deepEqual(fixtureErrors, [], "Unexpected fixture server errors.");
          const observed = requests.slice(firstRequest);
          assert.equal(observed.filter(request => request.method === "GET" && request.path === "/api/v4/projects").length, 2, "Pagination must fetch the terminal empty page.");
          const preflight = observed.find(request => request.method === "OPTIONS" && request.headers["access-control-request-headers"]?.includes("private-token"));
          assert(preflight, "Expected a real authenticated CORS preflight.");
          assert(observed.some(request => request.method === "OPTIONS" && request.path.startsWith("/denied/")), "Expected denied CORS preflight.");
          assert(!observed.some(request => request.method !== "OPTIONS" && request.path.startsWith("/denied/")), "Browser sent a request despite denied CORS.");
          results.push({ browser: browserName, browserVersion: browser.version(), mode, passed: true, requests: observed.length });
          console.log(`PASS ${browserName} ${mode}: imports, fetch, CORS, models, errors, upload, and JS/TS UTF-8 downloads`);
        } finally { await context.close(); }
      }
    } finally { await browser.close(); }
  }
  writeFileSync(join(consumerDirectory, "results.json"), JSON.stringify({ packageVersion: packageMetadata.version, tarball, results }, null, 2));
  console.log(`Passed ${results.length} actual-browser cases for ${packageMetadata.name}@${packageMetadata.version}. Results: ${consumerDirectory}`);
} finally {
  if (originalNodeEnv === undefined) delete process.env.NODE_ENV;
  else process.env.NODE_ENV = originalNodeEnv;
  for (const cleanup of cleanups.reverse()) await cleanup();
}
