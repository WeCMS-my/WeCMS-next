import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const requestSourcePath = resolve(root, "src", "api", "request.ts");

test("request client falls back to same-origin paths when VITE_API_BASE_URL is unset", async () => {
  const source = await readFile(requestSourcePath, "utf8");

  assert.match(source, /const apiBaseUrl = import\.meta\.env\.VITE_API_BASE_URL \?\? "";/);
  assert.doesNotMatch(source, /const apiBaseUrl = import\.meta\.env\.VITE_API_BASE_URL;/);
  assert.match(source, /fetch\(`\$\{apiBaseUrl\}\$\{path\}`,/);
  assert.match(source, /fetch\(`\$\{apiBaseUrl\}\/api\/v1\/auth\/refresh`,/);
});
