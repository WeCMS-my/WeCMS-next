<script setup lang="ts">
import { computed } from "vue";
import { NButton } from "naive-ui";
import { hasAllPermissions, hasAnyPermission } from "@/utils/permission";

const props = withDefaults(
  defineProps<{
    permissions?: string[];
    mode?: "any" | "all";
    type?: "default" | "tertiary" | "primary" | "info" | "success" | "warning" | "error";
    secondary?: boolean;
  }>(),
  {
    permissions: () => [],
    mode: "all",
    type: "default",
    secondary: false
  }
);

const visible = computed(() => {
  if (props.permissions.length === 0) {
    return true;
  }

  return props.mode === "any"
    ? hasAnyPermission(props.permissions)
    : hasAllPermissions(props.permissions);
});
</script>

<template>
  <NButton v-if="visible" :secondary="secondary" :type="type">
    <slot />
  </NButton>
</template>
