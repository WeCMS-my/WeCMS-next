import { readFile } from "node:fs/promises";
import { test } from "node:test";
import assert from "node:assert/strict";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const authStoreSource = await readFile(resolve(root, "src", "stores", "auth.ts"), "utf8");
const requestSource = await readFile(resolve(root, "src", "api", "request.ts"), "utf8");

test("auth store stores permissionVersion and can refresh auth state on mismatch", () => {
  assert.match(authStoreSource, /permissionVersion\s*=\s*ref<number \| null>/);
  assert.match(authStoreSource, /applyAuthState\([^)]*permissionVersion/s);
  assert.match(authStoreSource, /refreshPermissionState/);
});

test("request client checks permission version response header", () => {
  assert.match(requestSource, /X-Permission-Version/);
  assert.match(requestSource, /handlePermissionVersionHeader/);
});
