<script setup lang="ts">
import { computed } from "vue";
import { useAuthStore } from "@/stores/auth";
import { usePermissionStore } from "@/stores/permission";

const authStore = useAuthStore();
const permissionStore = usePermissionStore();
const roleText = computed(() => (authStore.roles.length > 0 ? authStore.roles.join(", ") : "未分配角色"));
</script>

<template>
  <main class="space-y-4">
    <section class="rounded border border-gray-200 bg-white p-6">
      <h1 class="text-xl font-semibold">工作台</h1>
      <dl class="mt-4 grid gap-4 md:grid-cols-3">
        <div>
          <dt class="text-sm text-gray-500">当前用户</dt>
          <dd class="mt-1 font-medium">{{ authStore.user?.displayName || authStore.user?.username }}</dd>
        </div>
        <div>
          <dt class="text-sm text-gray-500">当前角色</dt>
          <dd class="mt-1 font-medium">{{ roleText }}</dd>
        </div>
        <div>
          <dt class="text-sm text-gray-500">权限数量</dt>
          <dd class="mt-1 font-medium">{{ permissionStore.permissions.length }}</dd>
        </div>
      </dl>
    </section>
  </main>
</template>
