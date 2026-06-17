import { defineStore } from "pinia";
import { ref } from "vue";
import type { AuthMenuDto } from "@/api/types/generated";

export const useMenuStore = defineStore("menu", () => {
  const menus = ref<AuthMenuDto[]>([]);

  function setMenus(nextMenus: AuthMenuDto[]): void {
    menus.value = nextMenus;
  }

  function clearMenus(): void {
    menus.value = [];
  }

  return {
    menus,
    setMenus,
    clearMenus
  };
});
