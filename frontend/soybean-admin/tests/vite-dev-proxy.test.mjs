import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import process from "node:process";
import { fileURLToPath } from "node:url";
import test from "node:test";
import { loadConfigFromFile } from "vite";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const viteConfigPath = resolve(root, "vite.config.ts");
const envDevelopmentPath = resolve(root, ".env.development");
const repoRoot = resolve(root, "..", "..");
const readmePath = resolve(repoRoot, "README.md");
const launchSettingsPath = resolve(repoRoot, "backend", "src", "WeCms.Api", "Properties", "launchSettings.json");

test("development server proxies API and health requests to the backend", async () => {
  process.chdir(root);

  const loaded = await loadConfigFromFile(
    { command: "serve", mode: "development" },
    viteConfigPath
  );

  assert.ok(loaded, "vite config should load");
  const proxy = loaded.config.server?.proxy;
  assert.ok(proxy, "dev server proxy should be configured");
  assert.deepEqual(proxy["/api"], {
    target: "http://localhost:5261",
    changeOrigin: true
  });
  assert.deepEqual(proxy["/health"], {
    target: "http://localhost:5261",
    changeOrigin: true
  });
});

test("development API base URL stays same-origin so Vite proxy handles requests", async () => {
  const envText = await readFile(envDevelopmentPath, "utf8");

  assert.match(envText, /^VITE_API_BASE_URL=$/m);
  assert.doesNotMatch(envText, /localhost:5261/);
});

test("README API examples and backend launch profile agree on the development port", async () => {
  const [readmeText, launchSettingsText] = await Promise.all([
    readFile(readmePath, "utf8"),
    readFile(launchSettingsPath, "utf8")
  ]);

  const launchSettings = JSON.parse(launchSettingsText);
  assert.equal(launchSettings.profiles.http.applicationUrl, "http://localhost:5261");
  assert.match(readmeText, /curl http:\/\/localhost:5261\/health\/live/);
  assert.match(readmeText, /curl http:\/\/localhost:5261\/health\/ready/);
  assert.match(readmeText, /curl http:\/\/localhost:5261\/api\/v1\/system\/ping/);
  assert.match(readmeText, /curl http:\/\/localhost:5261\/api\/v1\/system\/version/);
  assert.match(readmeText, /curl http:\/\/localhost:5261\/api\/v1\/system\/db-check/);
});
