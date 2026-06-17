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
    target: "http://localhost:5080",
    changeOrigin: true
  });
  assert.deepEqual(proxy["/health"], {
    target: "http://localhost:5080",
    changeOrigin: true
  });
});

test("development API base URL stays same-origin so Vite proxy handles requests", async () => {
  const envText = await readFile(envDevelopmentPath, "utf8");

  assert.match(envText, /^VITE_API_BASE_URL=$/m);
  assert.doesNotMatch(envText, /localhost:5080/);
});
