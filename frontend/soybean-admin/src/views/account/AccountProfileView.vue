<script setup lang="ts">
import { onMounted, reactive, ref } from "vue";
import { useRouter } from "vue-router";
import {
  NAlert,
  NAvatar,
  NButton,
  NCard,
  NForm,
  NFormItem,
  NInput,
  NSpace,
  NTag
} from "naive-ui";
import {
  changeAccountPasswordApi,
  getAccountProfileApi,
  uploadAccountAvatarApi,
  updateAccountProfileApi
} from "@/api/account-profile";
import type { AccountProfileResponse } from "@/api/types/generated";
import { useAuthStore } from "@/stores/auth";

const maxAvatarSizeBytes = 512 * 1024;
const allowedAvatarTypes = new Set(["image/jpeg", "image/png", "image/webp"]);
const allowedAvatarExtensions = new Set([".jpg", ".jpeg", ".png", ".webp"]);

const router = useRouter();
const authStore = useAuthStore();
const loading = ref(false);
const actionLoading = ref(false);
const avatarLoading = ref(false);
const apiErrorMessage = ref("");
const successMessage = ref("");
const profile = ref<AccountProfileResponse | null>(null);
const selectedAvatarName = ref("");
const profileForm = reactive({
  displayName: "",
  email: "",
  phone: ""
});
const passwordForm = reactive({
  oldPassword: "",
  newPassword: "",
  confirmPassword: ""
});

onMounted(() => {
  void loadProfile();
});

async function loadProfile(): Promise<void> {
  loading.value = true;
  apiErrorMessage.value = "";
  try {
    const result = await getAccountProfileApi();
    profile.value = result.data;
    profileForm.displayName = result.data.displayName;
    profileForm.email = result.data.email ?? "";
    profileForm.phone = result.data.phone ?? "";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "无法加载个人资料。");
  } finally {
    loading.value = false;
  }
}

async function saveProfile(): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  if (!profileForm.displayName.trim()) {
    apiErrorMessage.value = "显示名称不能为空。";
    return;
  }

  actionLoading.value = true;
  try {
    const result = await updateAccountProfileApi({
      displayName: profileForm.displayName.trim(),
      email: profileForm.email.trim() || null,
      phone: profileForm.phone.trim() || null
    });
    profile.value = result.data;
    successMessage.value = "个人资料已更新。";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "个人资料更新失败。");
  } finally {
    actionLoading.value = false;
  }
}

async function changePassword(): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  if (passwordForm.newPassword !== passwordForm.confirmPassword) {
    apiErrorMessage.value = "两次输入的新密码不一致。";
    return;
  }

  if (!isStrongPassword(passwordForm.newPassword)) {
    apiErrorMessage.value = "新密码至少 8 位，并包含大小写字母、数字和符号。";
    return;
  }

  actionLoading.value = true;
  try {
    await changeAccountPasswordApi({
      oldPassword: passwordForm.oldPassword,
      newPassword: passwordForm.newPassword
    });
    passwordForm.oldPassword = "";
    passwordForm.newPassword = "";
    passwordForm.confirmPassword = "";
    await authStore.logout();
    await router.push({ path: "/login", query: { redirect: "/account/profile" } });
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "密码修改失败。");
  } finally {
    actionLoading.value = false;
  }
}

async function uploadAvatar(event: Event): Promise<void> {
  apiErrorMessage.value = "";
  successMessage.value = "";
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) {
    return;
  }

  selectedAvatarName.value = file.name;
  input.value = "";
  if (!isAllowedAvatar(file)) {
    apiErrorMessage.value = "头像仅支持 JPG、PNG、WebP，且大小不超过 512KB。";
    return;
  }

  avatarLoading.value = true;
  try {
    const sha256 = await computeSha256(file);
    const result = await uploadAccountAvatarApi({ file, sha256 });
    profile.value = profile.value ? { ...profile.value, avatarUrl: `${result.data.avatarUrl}?t=${Date.now()}` } : null;
    successMessage.value = "头像已更新。";
  } catch (error) {
    apiErrorMessage.value = messageOf(error, "头像上传失败。");
  } finally {
    avatarLoading.value = false;
  }
}

function isAllowedAvatar(file: File): boolean {
  const extension = file.name.slice(file.name.lastIndexOf(".")).toLowerCase();
  return file.size > 0
    && file.size <= maxAvatarSizeBytes
    && allowedAvatarTypes.has(file.type)
    && allowedAvatarExtensions.has(extension);
}

function isStrongPassword(value: string): boolean {
  return value.length >= 8
    && /[A-Z]/.test(value)
    && /[a-z]/.test(value)
    && /\d/.test(value)
    && /[^A-Za-z0-9]/.test(value);
}

async function computeSha256(file: File): Promise<string> {
  const buffer = await file.arrayBuffer();
  const hash = await crypto.subtle.digest("SHA-256", buffer);
  return Array.from(new Uint8Array(hash))
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");
}

function messageOf(error: unknown, fallback: string): string {
  const apiError = error as { msg?: string; message?: string };
  return apiError.msg || apiError.message || fallback;
}
</script>

<template>
  <main class="space-y-4">
    <header class="flex flex-wrap items-center justify-between gap-3">
      <div>
        <h1 class="text-xl font-semibold">个人中心</h1>
        <p class="text-sm text-gray-500">{{ profile?.username ?? "" }}</p>
      </div>
      <NTag v-if="profile" type="info">ID {{ profile.id }}</NTag>
    </header>

    <NAlert v-if="apiErrorMessage" type="error">
      {{ apiErrorMessage }}
    </NAlert>
    <NAlert v-if="successMessage" type="success">
      {{ successMessage }}
    </NAlert>

    <NCard title="个人资料">
      <NSpace vertical>
        <div class="flex flex-wrap items-center gap-4">
          <NAvatar :size="72" :src="profile?.avatarUrl || undefined">
            {{ profile?.displayName?.slice(0, 1) ?? "U" }}
          </NAvatar>
          <div class="space-y-2">
            <input id="account-avatar-file" accept="image/jpeg,image/png,image/webp" class="hidden" type="file" @change="uploadAvatar">
            <label for="account-avatar-file">
              <NButton :loading="avatarLoading" tag="span">上传头像</NButton>
            </label>
            <p class="text-xs text-gray-500">{{ selectedAvatarName || "JPG / PNG / WebP，最大 512KB" }}</p>
          </div>
        </div>

        <NForm :disabled="loading" @submit.prevent="saveProfile">
          <NFormItem label="显示名称">
            <NInput v-model:value="profileForm.displayName" maxlength="120" />
          </NFormItem>
          <NFormItem label="邮箱">
            <NInput v-model:value="profileForm.email" maxlength="160" />
          </NFormItem>
          <NFormItem label="手机号">
            <NInput v-model:value="profileForm.phone" maxlength="40" />
          </NFormItem>
          <NButton :loading="actionLoading" attr-type="submit" type="primary">
            保存资料
          </NButton>
        </NForm>
      </NSpace>
    </NCard>

    <NCard title="修改密码">
      <NForm @submit.prevent="changePassword">
        <NFormItem label="当前密码">
          <NInput v-model:value="passwordForm.oldPassword" autocomplete="current-password" show-password-on="mousedown" type="password" />
        </NFormItem>
        <NFormItem label="新密码">
          <NInput v-model:value="passwordForm.newPassword" autocomplete="new-password" show-password-on="mousedown" type="password" />
        </NFormItem>
        <NFormItem label="确认新密码">
          <NInput v-model:value="passwordForm.confirmPassword" autocomplete="new-password" show-password-on="mousedown" type="password" />
        </NFormItem>
        <NButton :loading="actionLoading" attr-type="submit" type="primary">
          修改密码
        </NButton>
      </NForm>
    </NCard>
  </main>
</template>
