import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const productionExamplePath = resolve(root, ".env.production.example");
const requestSourcePath = resolve(root, "src", "api", "request.ts");

test("production API base example uses HTTPS and not localhost", async () => {
  const env = await readFile(productionExamplePath, "utf8");
  const value = readEnvValue(env, "VITE_API_BASE_URL");

  assert.ok(value, "VITE_API_BASE_URL example must be present");
  assert.match(value, /^https:\/\//);
  assert.doesNotMatch(value, /localhost|127\.0\.0\.1|\[::1\]/i);
  assert.doesNotMatch(value, /^http:\/\//i);
});

test("empty production API base is only documented as same-origin mode", async () => {
  const requestSource = await readFile(requestSourcePath, "utf8");

  assert.match(requestSource, /const apiBaseUrl = import\.meta\.env\.VITE_API_BASE_URL \?\? "";/);
  assert.match(requestSource, /same-origin/i);
});

test("request client maps production error status codes to generic messages", async () => {
  const source = await readFile(requestSourcePath, "utf8");

  assert.match(source, /response\.status === 403/);
  assert.match(source, /无权限访问。/);
  assert.match(source, /response\.status === 429/);
  assert.match(source, /请求过于频繁，请稍后再试。/);
  assert.match(source, /response\.status >= 500/);
  assert.match(source, /系统异常，请稍后再试。/);
  assert.match(source, /handleTerminalUnauthorized\(response, options\)/);
});

function readEnvValue(source, key) {
  for (const line of source.split(/\r?\n/)) {
    const trimmed = line.trim();
    if (!trimmed || trimmed.startsWith("#")) {
      continue;
    }

    const [name, ...valueParts] = trimmed.split("=");
    if (name === key) {
      return valueParts.join("=").trim();
    }
  }

  return "";
}
