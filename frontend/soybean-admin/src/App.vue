<script setup lang="ts">
import { computed } from "vue";
import { RouterLink, RouterView, useRoute } from "vue-router";
import { NButton, NConfigProvider, NLayout, NLayoutContent, NLayoutHeader, NSpace, NText } from "naive-ui";
import { useAuthStore } from "@/stores/auth";

const route = useRoute();
const authStore = useAuthStore();
const title = computed(() => String(route.meta.title ?? "WeCMS Next"));

async function handleLogout(): Promise<void> {
  await authStore.logout();
  window.location.assign("/login");
}
</script>

<template>
  <NConfigProvider>
    <NLayout class="min-h-screen bg-gray-50 text-gray-900">
      <NLayoutHeader class="border-b border-gray-200 bg-white px-6 py-4">
        <NSpace align="center" justify="space-between">
          <RouterLink class="text-lg font-semibold text-gray-950 no-underline" to="/dashboard">
            WeCMS Next
          </RouterLink>
          <NSpace align="center">
            <NText depth="3">{{ title }}</NText>
            <NButton v-if="authStore.isAuthenticated" size="small" secondary @click="handleLogout">
              退出
            </NButton>
          </NSpace>
        </NSpace>
      </NLayoutHeader>
      <NLayoutContent class="px-6 py-6">
        <RouterView />
      </NLayoutContent>
    </NLayout>
  </NConfigProvider>
</template>
