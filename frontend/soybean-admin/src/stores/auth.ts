import { defineStore } from "pinia";
import { computed, ref } from "vue";
import {
  authMeApi,
  loginApi,
  logoutApi,
  refreshApi,
  verifyTwoFactorApi,
  verifyTwoFactorRecoveryCodeApi
} from "@/api/auth";
import type { AuthUserDto, LoginRequest, LoginResponse, MenuTreeDto } from "@/api/types/generated";
import { useMenuStore } from "@/stores/menu";
import { usePermissionStore } from "@/stores/permission";
import { clearTokenSet, readTokenSet, saveTokenSet, type TokenSet } from "@/utils/token";

interface TwoFactorChallengeState {
  challengeId: string;
  expiresAt: string;
}

export const useAuthStore = defineStore("auth", () => {
  const tokenSet = ref<TokenSet | null>(readTokenSet());
  const user = ref<AuthUserDto | null>(null);
  const roles = ref<string[]>([]);
  const twoFactorChallenge = ref<TwoFactorChallengeState | null>(null);
  const initialized = ref(false);
  let restorePromise: Promise<void> | null = null;

  const isAuthenticated = computed(() => Boolean(tokenSet.value?.accessToken));

  async function login(request: LoginRequest): Promise<"authenticated" | "two-factor"> {
    const result = await loginApi(request);
    if (result.data.requiresTwoFactor) {
      setTwoFactorChallenge(result.data);
      clearAuthenticatedState();
      initialized.value = true;
      return "two-factor";
    }

    applyLoginResponse(result.data);
    return "authenticated";
  }

  async function verifyTwoFactor(code: string): Promise<void> {
    const challenge = requireTwoFactorChallenge();
    const result = await verifyTwoFactorApi({
      challengeId: challenge.challengeId,
      code
    });
    applyLoginResponse(result.data);
    clearTwoFactorChallenge();
  }

  async function verifyTwoFactorRecoveryCode(recoveryCode: string): Promise<void> {
    const challenge = requireTwoFactorChallenge();
    const result = await verifyTwoFactorRecoveryCodeApi({
      challengeId: challenge.challengeId,
      recoveryCode
    });
    applyLoginResponse(result.data);
    clearTwoFactorChallenge();
  }

  async function restoreSession(): Promise<void> {
    if (initialized.value) {
      return;
    }

    if (!restorePromise) {
      restorePromise = tokenSet.value?.accessToken
        ? authMeApi().then((result) => {
            applyAuthState(result.data.user, result.data.roles, result.data.permissions, result.data.menus);
          })
        : refreshApi().then((result) => {
            const response = requireAuthenticatedResponse(result.data);
            setTokenSet({
              accessToken: response.accessToken,
              expiresAt: response.expiresAt
            });
            applyAuthState(response.user, response.roles, response.permissions, response.menus);
          });

      restorePromise = restorePromise
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
    try {
      await logoutApi();
    } finally {
      clearSession();
    }
  }

  function applyLoginResponse(response: LoginResponse): void {
    const authenticated = requireAuthenticatedResponse(response);
    setTokenSet({
      accessToken: authenticated.accessToken,
      expiresAt: authenticated.expiresAt
    });
    applyAuthState(authenticated.user, authenticated.roles, authenticated.permissions, authenticated.menus);
    initialized.value = true;
  }

  function requireAuthenticatedResponse(response: LoginResponse): LoginResponse & { user: AuthUserDto } {
    if (response.requiresTwoFactor || !response.user) {
      throw new Error("Two-factor verification is required.");
    }

    return response as LoginResponse & { user: AuthUserDto };
  }

  function setTwoFactorChallenge(response: LoginResponse): void {
    if (!response.twoFactorChallengeId || !response.twoFactorChallengeExpiresAt) {
      throw new Error("Two-factor challenge is missing.");
    }

    twoFactorChallenge.value = {
      challengeId: response.twoFactorChallengeId,
      expiresAt: response.twoFactorChallengeExpiresAt
    };
  }

  function requireTwoFactorChallenge(): TwoFactorChallengeState {
    if (!twoFactorChallenge.value) {
      throw new Error("Two-factor challenge has expired. Please sign in again.");
    }

    return twoFactorChallenge.value;
  }

  function clearTwoFactorChallenge(): void {
    twoFactorChallenge.value = null;
  }

  function applyAuthState(
    nextUser: AuthUserDto,
    nextRoles: string[],
    nextPermissions: string[],
    nextMenus: MenuTreeDto[]
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
    clearTwoFactorChallenge();
    clearAuthenticatedState();
  }

  function clearAuthenticatedState(): void {
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
    twoFactorChallenge,
    initialized,
    isAuthenticated,
    login,
    verifyTwoFactor,
    verifyTwoFactorRecoveryCode,
    clearTwoFactorChallenge,
    restoreSession,
    logout,
    setTokenSet,
    clearSession
  };
});
