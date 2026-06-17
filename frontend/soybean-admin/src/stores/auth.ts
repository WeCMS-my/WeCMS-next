import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { clearTokenSet, readTokenSet, saveTokenSet, type TokenSet } from "@/utils/token";

export const useAuthStore = defineStore("auth", () => {
  const tokenSet = ref<TokenSet | null>(readTokenSet());

  const isAuthenticated = computed(() => Boolean(tokenSet.value?.accessToken));

  function setTokenSet(nextTokenSet: TokenSet): void {
    tokenSet.value = nextTokenSet;
    saveTokenSet(nextTokenSet);
  }

  function clearSession(): void {
    tokenSet.value = null;
    clearTokenSet();
  }

  return {
    tokenSet,
    isAuthenticated,
    setTokenSet,
    clearSession
  };
});
