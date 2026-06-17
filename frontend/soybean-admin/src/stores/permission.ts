import { defineStore } from "pinia";
import { ref } from "vue";

export const usePermissionStore = defineStore("permission", () => {
  const permissions = ref<string[]>([]);

  function setPermissions(nextPermissions: string[]): void {
    permissions.value = [...new Set(nextPermissions)].sort();
  }

  function hasPermission(code: string): boolean {
    return permissions.value.includes(code);
  }

  function hasAnyPermission(codes: string[]): boolean {
    return codes.length === 0 || codes.some((code) => hasPermission(code));
  }

  function hasAllPermissions(codes: string[]): boolean {
    return codes.every((code) => hasPermission(code));
  }

  return {
    permissions,
    setPermissions,
    hasPermission,
    hasAnyPermission,
    hasAllPermissions
  };
});
