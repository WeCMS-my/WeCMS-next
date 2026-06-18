import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import test from "node:test";

const root = resolve(dirname(fileURLToPath(import.meta.url)), "..");
const source = (...segments) => readFile(resolve(root, "src", ...segments), "utf8");

test("2FA login challenge route is memory-backed and skips session restore", async () => {
  const staticRoutes = await source("router", "static-routes.ts");
  const guards = await source("router", "guards.ts");
  const authStore = await source("stores", "auth.ts");
  const twoFactorView = await source("views", "auth", "TwoFactorLoginView.vue");

  assert.match(staticRoutes, /path: "\/auth\/two-factor"/);
  assert.match(staticRoutes, /skipSessionRestore: true/);
  assert.match(guards, /!to\.meta\.skipSessionRestore/);
  assert.match(authStore, /twoFactorChallenge = ref/);
  assert.match(authStore, /verifyTwoFactorApi/);
  assert.doesNotMatch(authStore, /localStorage/);
  assert.match(twoFactorView, /verifyTwoFactorRecoveryCode/);
  assert.doesNotMatch(twoFactorView, /v-html/);
  assert.doesNotMatch(twoFactorView, /localStorage/);
});

test("account security page exposes 2FA setup and recovery code actions without browser storage", async () => {
  const staticRoutes = await source("router", "static-routes.ts");
  const accountApi = await source("api", "account-two-factor.ts");
  const accountView = await source("views", "account", "AccountSecurityView.vue");

  assert.match(staticRoutes, /path: "\/account\/security"/);
  assert.match(accountApi, /\/api\/v1\/account\/2fa\/status/);
  assert.match(accountApi, /\/api\/v1\/account\/2fa\/recovery-codes\/regenerate/);
  assert.match(accountView, /beginAccountTwoFactorSetupApi/);
  assert.match(accountView, /getAccountSecurityApi/);
  assert.match(accountView, /disableAccountTwoFactorApi/);
  assert.match(accountView, /recoveryCodes = ref<string\[\]>\(\[\]\)/);
  assert.doesNotMatch(accountView, /v-html/);
  assert.doesNotMatch(accountView, /localStorage/);
});

test("account profile page exposes profile password and avatar self-service flow", async () => {
  const staticRoutes = await source("router", "static-routes.ts");
  const dynamicRoutes = await source("router", "dynamic-routes.ts");
  const accountApi = await source("api", "account-profile.ts");
  const accountView = await source("views", "account", "AccountProfileView.vue");

  assert.match(staticRoutes, /path: "\/account\/profile"/);
  assert.match(dynamicRoutes, /"account\/profile\/index"/);
  assert.match(accountApi, /\/api\/v1\/account\/profile/);
  assert.match(accountApi, /\/api\/v1\/account\/password/);
  assert.match(accountApi, /\/api\/v1\/account\/avatar/);
  assert.match(accountView, /computeSha256/);
  assert.match(accountView, /allowedAvatarTypes/);
  assert.match(accountView, /maxAvatarSizeBytes = 512 \* 1024/);
  assert.match(accountView, /authStore\.logout\(\)/);
  assert.doesNotMatch(accountView, /v-html/);
  assert.doesNotMatch(accountView, /localStorage/);
});

test("user management exposes admin reset 2FA behind permission and confirmation", async () => {
  const usersApi = await source("api", "system", "users.ts");
  const usersView = await source("views", "system", "users", "UsersView.vue");

  assert.match(usersApi, /resetUserTwoFactorApi/);
  assert.match(usersApi, /\/api\/v1\/system\/users\/\$\{id\}\/reset-2fa/);
  assert.match(usersView, /sys:user:reset-2fa/);
  assert.match(usersView, /window\.confirm\(`确认重置用户/);
  assert.match(usersView, /window\.prompt\("请输入重置原因"\)/);
  assert.match(usersView, /resetUserTwoFactorApi\(row\.id, \{ reason: reason\.trim\(\) \}\)/);
});
