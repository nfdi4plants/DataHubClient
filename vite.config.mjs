import { readdirSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";
import { defineConfig } from "vite";
import dts from "vite-plugin-dts";
import ts from "typescript";

const inputDirectory = resolve("dist/ts");
const outputDirectory = resolve("dist/js");

// Each library module is an entry so existing deep imports keep their exports.
// Vite follows and includes the local Fable runtime dependencies as needed.
const entries = Object.fromEntries(
  readdirSync(inputDirectory, { recursive: true })
    .map(path => path.replaceAll("\\", "/"))
    .filter(path => path.endsWith(".ts") && !path.endsWith(".d.ts") && !path.startsWith("fable_modules/"))
    .map(path => [path.slice(0, -3), resolve(inputDirectory, path)]),
);

// Vite skips type-only imports. Make their reachable source files explicit
// declaration roots, so the plugin also emits dependencies such as Fable Event.
// Unused runtime modules stay outside this program and the JavaScript bundle.
const sourceConfig = ts.readConfigFile(resolve("tsconfig.json"), ts.sys.readFile);
if (sourceConfig.error) throw new Error(ts.flattenDiagnosticMessageText(sourceConfig.error.messageText, "\n"));
const parsedConfig = ts.parseJsonConfigFileContent(sourceConfig.config, ts.sys, resolve("."));
const declarationProgram = ts.createProgram(Object.values(entries), parsedConfig.options);
const declarationConfigPath = resolve("dist/tsconfig.declarations.json");
writeFileSync(declarationConfigPath, JSON.stringify({
  extends: "../tsconfig.json",
  files: declarationProgram.getSourceFiles()
    .filter(file => !declarationProgram.isSourceFileDefaultLibrary(file) && !declarationProgram.isSourceFileFromExternalLibrary(file))
    .map(file => file.fileName),
  include: [],
  exclude: [],
}, null, 2));

export default defineConfig({
  plugins: [
    dts({
      tsconfigPath: declarationConfigPath,
      entryRoot: inputDirectory,
      outDir: outputDirectory,
      // Retain declarations for runtime dependencies alongside public modules.
      include: ["dist/ts/**/*.ts"],
      exclude: ["node_modules/**"],
      copyDtsFiles: true,
      afterDiagnostic(diagnostics) {
        if (diagnostics.some(diagnostic => diagnostic.category === ts.DiagnosticCategory.Error)) {
          throw new Error("TypeScript declaration generation failed.");
        }
      },
    }),
  ],
  build: {
    outDir: outputDirectory,
    target: "es2022",
    lib: { entry: entries, formats: ["es"] },
    minify: false,
    rollupOptions: {
      treeshake: false,
      preserveEntrySignatures: "strict",
      output: {
        preserveModules: true,
        preserveModulesRoot: inputDirectory,
        entryFileNames: "[name].js",
        chunkFileNames: "[name].js",
      },
    },
  },
});
