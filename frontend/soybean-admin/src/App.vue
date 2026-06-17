<script setup lang="ts">
import { computed } from "vue";
import { RouterLink, RouterView, useRoute } from "vue-router";
import { NButton, NConfigProvider, NLayout, NLayoutContent, NLayoutHeader, NSpace, NText } from "naive-ui";
import { useAuthStore } from "@/stores/auth";
import { useMenuStore } from "@/stores/menu";

const route = useRoute();
const authStore = useAuthStore();
const menuStore = useMenuStore();
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
        <div class="grid gap-6 lg:grid-cols-[220px_1fr]">
          <nav
            v-if="authStore.isAuthenticated"
            class="rounded border border-gray-200 bg-white p-3"
            aria-label="系统菜单"
          >
            <RouterLink
              class="block rounded px-3 py-2 text-sm text-gray-700 no-underline hover:bg-gray-100"
              to="/dashboard"
            >
              工作台
            </RouterLink>
            <RouterLink
              v-for="item in menuStore.navigationItems"
              :key="item.id"
              class="block rounded px-3 py-2 text-sm text-gray-700 no-underline hover:bg-gray-100"
              :to="item.path"
            >
              {{ item.title }}
            </RouterLink>
          </nav>
          <RouterView />
        </div>
      </NLayoutContent>
    </NLayout>
  </NConfigProvider>
</template>
