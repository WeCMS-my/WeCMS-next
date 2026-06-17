import type { Router } from "vue-router";
import { registerDynamicRoutes } from "./dynamic-routes";
import { useAuthStore } from "@/stores/auth";
import { useMenuStore } from "@/stores/menu";
import { usePermissionStore } from "@/stores/permission";

export function installRouterGuards(router: Router): void {
  router.beforeEach(async (to) => {
    const authStore = useAuthStore();
    const menuStore = useMenuStore();
    const permissionStore = usePermissionStore();

    await authStore.restoreSession();
    if (authStore.isAuthenticated) {
      await menuStore.loadMenuTreeIfAllowed();
      registerDynamicRoutes(router, menuStore.effectiveMenuTree, permissionStore.permissions);
    }

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
