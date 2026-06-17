import type { Router } from "vue-router";
import { useAuthStore } from "@/stores/auth";
import { usePermissionStore } from "@/stores/permission";

export function installRouterGuards(router: Router): void {
  router.beforeEach((to) => {
    const authStore = useAuthStore();
    const permissionStore = usePermissionStore();

    if (!to.meta.requiresAuth) {
      return true;
    }

    if (!authStore.isAuthenticated) {
      return {
        path: "/login",
        query: {
          redirect: to.fullPath
        }
      };
    }

    const requiredPermissions = to.meta.permissions ?? [];
    if (requiredPermissions.length > 0 && !permissionStore.hasAllPermissions(requiredPermissions)) {
      return "/403";
    }

    return true;
  });
}
