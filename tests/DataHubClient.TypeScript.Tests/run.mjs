import { spawnSync } from "node:child_process";
import { copyFileSync, existsSync, mkdirSync, mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";
import { build } from "vite";

const fixtureDirectory = dirname(fileURLToPath(import.meta.url));
const repositoryRoot = resolve(fixtureDirectory, "../..");
const tarballArgument = process.argv[2];
if (!tarballArgument) throw new Error("Usage: node tests/DataHubClient.TypeScript.Tests/run.mjs <package.tgz>");
const tarball = resolve(tarballArgument);
if (!existsSync(tarball)) throw new Error(`Packed npm artifact not found: ${tarball}`);
const compiler = join(repositoryRoot, "node_modules/typescript/bin/tsc");
if (!existsSync(compiler)) throw new Error("TypeScript is missing. Install the root build dependencies with npm ci.");

const outputRoot = join(repositoryRoot, "dist/typescript-tests");
mkdirSync(outputRoot, { recursive: true });
const consumerDirectory = mkdtempSync(join(outputRoot, "consumer-"));
for (const filename of ["consumer.ts", "runtime.ts", "tsconfig.json", "tsconfig.bundler.json", "tsconfig.declarations.json"]) {
  copyFileSync(join(fixtureDirectory, filename), join(consumerDirectory, filename));
}
writeFileSync(join(consumerDirectory, "package.json"), JSON.stringify({
  name: "datahub-client-typescript-consumer-tests",
  private: true,
  type: "module",
  dependencies: { "@nfdi4plants/datahub-client": pathToFileURL(tarball).href },
}, null, 2));

function run(command, args, env = process.env) {
  const result = spawnSync(command, args, { cwd: consumerDirectory, env, stdio: "inherit" });
  if (result.error) throw result.error;
  if (result.status !== 0) throw new Error(`${command} failed with exit code ${result.status}. Consumer: ${consumerDirectory}`);
}

// The tarball path is in package.json so Windows cmd receives only fixed text.
// Disable scripts and registry access: this check needs only the local artifact.
const installArgs = ["install", "--offline", "--ignore-scripts", "--no-audit", "--no-fund", "--package-lock=false"];
const installEnvironment = { ...process.env, npm_config_cache: join(outputRoot, "npm-cache") };
if (process.platform === "win32") {
  run(process.env.ComSpec || "cmd.exe", ["/d", "/s", "/c", `npm ${installArgs.join(" ")}`], installEnvironment);
} else {
  run("npm", installArgs, installEnvironment);
}

const packageDirectory = join(consumerDirectory, "node_modules/@nfdi4plants/datahub-client");
const metadata = JSON.parse(readFileSync(join(packageDirectory, "package.json"), "utf8"));
if (metadata.type !== "module" || !metadata.main?.endsWith(".js") || !metadata.types?.endsWith(".d.ts")) {
  throw new Error("The packed package must declare JavaScript ESM and its TypeScript declaration entry point.");
}
if (!existsSync(join(packageDirectory, metadata.main)) || !existsSync(join(packageDirectory, metadata.types))) {
  throw new Error("The packed package is missing its JavaScript or declaration entry point.");
}

run(process.execPath, [compiler, "--project", "tsconfig.json"]);
run(process.execPath, [compiler, "--project", "tsconfig.bundler.json"]);
run(process.execPath, [compiler, "--project", "tsconfig.declarations.json"]);
run(process.execPath, [join(consumerDirectory, "compiled/runtime.js")]);
console.log("All packed declarations and consumer NodeNext/Bundler checks passed.");

// Build the installed package as a browser consumer would, keeping the same
// offline fixture so class identity, decoding, and error behavior are exercised.
const browserBuild = await build({
  configFile: false,
  root: consumerDirectory,
  build: {
    target: "es2022",
    minify: false,
    outDir: join(consumerDirectory, "browser"),
    lib: {
      entry: join(consumerDirectory, "runtime.ts"),
      formats: ["es"],
      fileName: () => "consumer.mjs",
    },
    rollupOptions: {
      external: [],
      output: { inlineDynamicImports: true },
      onwarn(warning, defaultHandler) {
        if (warning.code === "UNRESOLVED_IMPORT" || /externalized for browser compatibility/i.test(warning.message)) {
          throw new Error(`Browser consumer dependency resolution failed: ${warning.message}`);
        }
        defaultHandler(warning);
      },
    },
  },
});
for (const result of Array.isArray(browserBuild) ? browserBuild : [browserBuild]) {
  for (const output of result.output) {
    if (output.type === "chunk" && (output.imports.length !== 0 || output.dynamicImports.length !== 0)) {
      throw new Error("The browser consumer bundle must include every dependency.");
    }
  }
}
run(process.execPath, [join(consumerDirectory, "browser/consumer.mjs")]);
console.log("Browser-bundle build and offline runtime checks passed (executed in Node, not a browser).");
