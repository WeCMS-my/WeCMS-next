import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { authMeApi, loginApi, logoutApi } from "@/api/auth";
import type { AuthMenuDto, AuthUserDto, LoginRequest, LoginResponse } from "@/api/types/generated";
import { useMenuStore } from "@/stores/menu";
import { usePermissionStore } from "@/stores/permission";
import { clearTokenSet, readTokenSet, saveTokenSet, type TokenSet } from "@/utils/token";

export const useAuthStore = defineStore("auth", () => {
  const tokenSet = ref<TokenSet | null>(readTokenSet());
  const user = ref<AuthUserDto | null>(null);
  const roles = ref<string[]>([]);
  const initialized = ref(false);
  let restorePromise: Promise<void> | null = null;

  const isAuthenticated = computed(() => Boolean(tokenSet.value?.accessToken));

  async function login(request: LoginRequest): Promise<void> {
    const result = await loginApi(request);
    applyLoginResponse(result.data);
  }

  async function restoreSession(): Promise<void> {
    if (initialized.value) {
      return;
    }

    if (!tokenSet.value?.accessToken) {
      initialized.value = true;
      return;
    }

    if (!restorePromise) {
      restorePromise = authMeApi()
        .then((result) => {
          applyAuthState(result.data.user, result.data.roles, result.data.permissions, result.data.menus);
        })
        .catch(() => {
          clearSession();
        })
        .finally(() => {
          initialized.value = true;
          restorePromise = null;
        });
    }

    await restorePromise;
  }

  async function logout(): Promise<void> {
    const refreshToken = tokenSet.value?.refreshToken;
    try {
      if (refreshToken) {
        await logoutApi({ refreshToken });
      }
    } finally {
      clearSession();
    }
  }

  function applyLoginResponse(response: LoginResponse): void {
    setTokenSet({
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: response.expiresAt
    });
    applyAuthState(response.user, response.roles, response.permissions, response.menus);
    initialized.value = true;
  }

  function applyAuthState(
    nextUser: AuthUserDto,
    nextRoles: string[],
    nextPermissions: string[],
    nextMenus: AuthMenuDto[]
  ): void {
    const permissionStore = usePermissionStore();
    const menuStore = useMenuStore();

    user.value = nextUser;
    roles.value = nextRoles;
    permissionStore.setPermissions(nextPermissions);
    menuStore.setMenus(nextMenus);
  }

  function setTokenSet(nextTokenSet: TokenSet): void {
    tokenSet.value = nextTokenSet;
    saveTokenSet(nextTokenSet);
  }

  function clearSession(): void {
    const permissionStore = usePermissionStore();
    const menuStore = useMenuStore();

    tokenSet.value = null;
    user.value = null;
    roles.value = [];
    permissionStore.setPermissions([]);
    menuStore.clearMenus();
    clearTokenSet();
  }

  return {
    tokenSet,
    user,
    roles,
    initialized,
    isAuthenticated,
    login,
    restoreSession,
    logout,
    setTokenSet,
    clearSession
  };
});
