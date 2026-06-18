<script setup lang="ts">
import { onMounted, reactive, ref } from "vue";
import {
  NAlert,
  NButton,
  NCard,
  NForm,
  NFormItem,
  NInput,
  NList,
  NListItem,
  NSpace,
  NTag
} from "naive-ui";
import { getAccountSecurityApi } from "@/api/account-profile";
import {
  beginAccountTwoFactorSetupApi,
  confirmAccountTwoFactorSetupApi,
  disableAccountTwoFactorApi,
  getAccountTwoFactorStatusApi,
  regenerateAccountTwoFactorRecoveryCodesApi
} from "@/api/account-two-factor";
import type {
  AccountSecurityResponse,
  AccountTwoFactorSetupResponse,
  AccountTwoFactorStatusResponse
} from "@/api/types/generated";

const loading = ref(false);
const actionLoading = ref(false);
const apiErrorMessage = ref("");
const successMessage = ref("");
const status = ref<AccountTwoFactorStatusResponse | null>(null);
const accountSecurity = ref<AccountSecurityResponse | null>(null);
const setupResult = ref<AccountTwoFactorSetupResponse | null>(null);
const recoveryCodes = ref<string[]>([]);
const confirmForm = reactive({
  code: ""
});
const sensitiveForm = reactive({
  currentPassword: "",
  code: ""
});

onMounted(() => {
  void loadStatus();
});

async function loadStatus(): Promise<void> {
  apiErrorMessage.value = "";
  loading.value = true;
  try {
    const [twoFactorResult, securityResult] = await Promise.all([
      getAccountTwoFactorStatusApi(),
      getAccountSecurityApi()
    ]);
    status.value = twoFactorResult.data;
    accountSecurity.value = securityResult.data;
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "无法加载 2FA 状态。");
  } finally {
    loading.value = false;
  }
}

async function beginSetup(): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  actionLoading.value = true;
  try {
    const result = await beginAccountTwoFactorSetupApi();
    setupResult.value = result.data;
    recoveryCodes.value = result.data.recoveryCodes;
    successMessage.value = "请扫描或手动录入密钥，然后输入验证码完成绑定。";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "无法开始 2FA 绑定。");
  } finally {
    actionLoading.value = false;
  }
}

async function confirmSetup(): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  if (!confirmForm.code.trim()) {
    apiErrorMessage.value = "请输入验证码。";
    return;
  }

  actionLoading.value = true;
  try {
    const result = await confirmAccountTwoFactorSetupApi({ code: confirmForm.code.trim() });
    status.value = result.data;
    setupResult.value = null;
    confirmForm.code = "";
    successMessage.value = "2FA 已启用。请妥善保存刚才显示的恢复码。";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "2FA 验证码无效。");
  } finally {
    actionLoading.value = false;
  }
}

async function disableTwoFactor(): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  actionLoading.value = true;
  try {
    await disableAccountTwoFactorApi(normalizedSensitiveRequest());
    clearSensitiveForm();
    recoveryCodes.value = [];
    setupResult.value = null;
    await loadStatus();
    successMessage.value = "2FA 已禁用。";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "禁用 2FA 失败。");
  } finally {
    actionLoading.value = false;
  }
}

async function regenerateRecoveryCodes(): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  actionLoading.value = true;
  try {
    const result = await regenerateAccountTwoFactorRecoveryCodesApi(normalizedSensitiveRequest());
    clearSensitiveForm();
    recoveryCodes.value = result.data.recoveryCodes;
    await loadStatus();
    successMessage.value = "恢复码已重新生成，旧恢复码已失效。";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "重新生成恢复码失败。");
  } finally {
    actionLoading.value = false;
  }
}

function normalizedSensitiveRequest(): { currentPassword?: string | null; code?: string | null } {
  return {
    currentPassword: sensitiveForm.currentPassword.trim() || null,
    code: sensitiveForm.code.trim() || null
  };
}

function clearSensitiveForm(): void {
  sensitiveForm.currentPassword = "";
  sensitiveForm.code = "";
}

function messageOf(error: unknown, fallback: string): string {
  const apiError = error as { msg?: string; message?: string };
  return apiError.msg || apiError.message || fallback;
}
</script>

<template>
  <main class="space-y-4">
    <header>
      <h1 class="text-xl font-semibold">账户安全</h1>
    </header>

    <NAlert v-if="apiErrorMessage" type="error">
      {{ apiErrorMessage }}
    </NAlert>
    <NAlert v-if="successMessage" type="success">
      {{ successMessage }}
    </NAlert>

    <NCard title="账号状态">
      <div class="grid gap-3 md:grid-cols-3">
        <div>
          <p class="text-xs text-gray-500">强制改密</p>
          <NTag :type="accountSecurity?.mustChangePassword ? 'warning' : 'success'">
            {{ accountSecurity?.mustChangePassword ? "需要" : "不需要" }}
          </NTag>
        </div>
        <div>
          <p class="text-xs text-gray-500">上次登录 IP</p>
          <p class="text-sm">{{ accountSecurity?.lastLoginIp ?? "-" }}</p>
        </div>
        <div>
          <p class="text-xs text-gray-500">上次登录时间</p>
          <p class="text-sm">{{ accountSecurity?.lastLoginAt ?? "-" }}</p>
        </div>
      </div>
    </NCard>

    <NCard title="双因素认证">
      <NSpace vertical>
        <div class="flex flex-wrap items-center gap-3">
          <NTag :type="status?.enabled ? 'success' : 'warning'">
            {{ status?.enabled ? "已启用" : "未启用" }}
          </NTag>
          <span class="text-sm text-gray-500">剩余恢复码：{{ status?.recoveryCodesRemaining ?? 0 }}</span>
          <span v-if="status?.resetRequired" class="text-sm text-red-500">管理员已要求重新绑定</span>
        </div>
        <NButton :disabled="loading" :loading="actionLoading" type="primary" @click="beginSetup">
          {{ status?.enabled ? "重新绑定" : "绑定 2FA" }}
        </NButton>
      </NSpace>
    </NCard>

    <NCard v-if="setupResult" title="绑定确认">
      <NSpace vertical>
        <NAlert type="warning">
          密钥和恢复码只在本次流程显示。确认后请保存恢复码。
        </NAlert>
        <div class="rounded border border-gray-200 p-3">
          <p class="text-sm font-medium">手动密钥</p>
          <p class="break-all font-mono text-sm">{{ setupResult.secret }}</p>
          <p class="mt-3 text-sm font-medium">OTP Auth URI</p>
          <p class="break-all font-mono text-xs text-gray-600">{{ setupResult.otpAuthUri }}</p>
        </div>
        <NForm @submit.prevent="confirmSetup">
          <NFormItem label="验证码">
            <NInput v-model:value="confirmForm.code" autocomplete="one-time-code" maxlength="16" placeholder="请输入认证器验证码" />
          </NFormItem>
          <NButton :loading="actionLoading" attr-type="submit" type="primary">
            确认启用
          </NButton>
        </NForm>
      </NSpace>
    </NCard>

    <NCard v-if="recoveryCodes.length > 0" title="恢复码">
      <NSpace vertical>
        <NAlert type="info">
          每个恢复码只能使用一次。重新生成后旧恢复码立即失效。
        </NAlert>
        <NList bordered>
          <NListItem v-for="code in recoveryCodes" :key="code">
            <span class="font-mono">{{ code }}</span>
          </NListItem>
        </NList>
      </NSpace>
    </NCard>

    <NCard v-if="status?.enabled" title="敏感操作">
      <NSpace vertical>
        <NForm>
          <NFormItem label="当前密码">
            <NInput
              v-model:value="sensitiveForm.currentPassword"
              autocomplete="current-password"
              placeholder="当前密码或验证码二选一"
              show-password-on="mousedown"
              type="password"
            />
          </NFormItem>
          <NFormItem label="验证码">
            <NInput v-model:value="sensitiveForm.code" autocomplete="one-time-code" maxlength="16" placeholder="当前验证码" />
          </NFormItem>
        </NForm>
        <div class="flex flex-wrap gap-3">
          <NButton :loading="actionLoading" @click="regenerateRecoveryCodes">
            重新生成恢复码
          </NButton>
          <NButton :loading="actionLoading" type="error" @click="disableTwoFactor">
            禁用 2FA
          </NButton>
        </div>
      </NSpace>
    </NCard>
  </main>
</template>
