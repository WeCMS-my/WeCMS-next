<script setup lang="ts">
import { reactive, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { NAlert, NButton, NForm, NFormItem, NInput } from "naive-ui";
import { useAuthStore } from "@/stores/auth";

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();
const loading = ref(false);
const errorMessage = ref("");
const form = reactive({
  username: "",
  password: ""
});

async function handleSubmit(): Promise<void> {
  errorMessage.value = "";
  if (!form.username.trim() || !form.password.trim()) {
    errorMessage.value = "请输入用户名和密码。";
    return;
  }

  loading.value = true;
  try {
    await authStore.login({
      username: form.username.trim(),
      password: form.password
    });
    const redirect = typeof route.query.redirect === "string" ? route.query.redirect : "/dashboard";
    await router.replace(redirect);
  } catch (error) {
    const apiError = error as { msg?: string };
    errorMessage.value = apiError.msg || "登录失败，请检查账号或密码。";
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <main class="mx-auto max-w-md rounded border border-gray-200 bg-white p-6">
    <h1 class="mb-4 text-xl font-semibold">登录</h1>
    <NAlert v-if="errorMessage" class="mb-4" type="error">
      {{ errorMessage }}
    </NAlert>
    <NForm @submit.prevent="handleSubmit">
      <NFormItem label="用户名">
        <NInput v-model:value="form.username" autocomplete="username" placeholder="请输入用户名" />
      </NFormItem>
      <NFormItem label="密码">
        <NInput
          v-model:value="form.password"
          autocomplete="current-password"
          placeholder="请输入密码"
          show-password-on="mousedown"
          type="password"
        />
      </NFormItem>
      <NButton :loading="loading" attr-type="submit" block type="primary">
        登录
      </NButton>
    </NForm>
  </main>
</template>
