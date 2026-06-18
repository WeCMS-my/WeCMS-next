<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { NAlert, NButton, NForm, NFormItem, NInput, NRadioButton, NRadioGroup } from "naive-ui";
import { useAuthStore } from "@/stores/auth";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const loading = ref(false);
const errorMessage = ref("");
const mode = ref<"totp" | "recovery">("totp");
const form = reactive({
  code: "",
  recoveryCode: ""
});

const expiresAtText = computed(() => {
  const expiresAt = authStore.twoFactorChallenge?.expiresAt;
  return expiresAt ? new Date(expiresAt).toLocaleString() : "";
});

onMounted(() => {
  if (!authStore.twoFactorChallenge) {
    router.replace("/login");
  }
});

async function handleSubmit(): Promise<void> {
  errorMessage.value = "";
  const redirect = typeof route.query.redirect === "string" ? route.query.redirect : "/dashboard";
  loading.value = true;
  try {
    if (mode.value === "totp") {
      if (!form.code.trim()) {
        errorMessage.value = "请输入验证码。";
        return;
      }
      await authStore.verifyTwoFactor(form.code.trim());
    } else {
      if (!form.recoveryCode.trim()) {
        errorMessage.value = "请输入恢复码。";
        return;
      }
      await authStore.verifyTwoFactorRecoveryCode(form.recoveryCode.trim());
    }

    await router.replace(redirect);
  } catch (error) {
    const apiError = error as { msg?: string; message?: string };
    errorMessage.value = apiError.msg || apiError.message || "二次验证失败，请重试。";
  } finally {
    loading.value = false;
  }
}

async function backToLogin(): Promise<void> {
  authStore.clearTwoFactorChallenge();
  await router.replace("/login");
}
</script>

<template>
  <main class="mx-auto max-w-md rounded border border-gray-200 bg-white p-6">
    <h1 class="mb-2 text-xl font-semibold">二次验证</h1>
    <p class="mb-4 text-sm text-gray-500">
      输入认证器验证码，或使用一次性恢复码完成登录。
    </p>
    <NAlert v-if="expiresAtText" class="mb-4" type="info">
      验证请求有效期至 {{ expiresAtText }}
    </NAlert>
    <NAlert v-if="errorMessage" class="mb-4" type="error">
      {{ errorMessage }}
    </NAlert>
    <NForm @submit.prevent="handleSubmit">
      <NFormItem label="验证方式">
        <NRadioGroup v-model:value="mode">
          <NRadioButton value="totp">
            验证码
          </NRadioButton>
          <NRadioButton value="recovery">
            恢复码
          </NRadioButton>
        </NRadioGroup>
      </NFormItem>
      <NFormItem v-if="mode === 'totp'" label="验证码">
        <NInput v-model:value="form.code" autocomplete="one-time-code" maxlength="16" placeholder="请输入 6 位验证码" />
      </NFormItem>
      <NFormItem v-else label="恢复码">
        <NInput v-model:value="form.recoveryCode" autocomplete="one-time-code" placeholder="请输入恢复码" />
      </NFormItem>
      <div class="flex gap-3">
        <NButton :loading="loading" attr-type="submit" class="flex-1" type="primary">
          验证
        </NButton>
        <NButton class="flex-1" @click="backToLogin">
          返回登录
        </NButton>
      </div>
    </NForm>
  </main>
</template>
