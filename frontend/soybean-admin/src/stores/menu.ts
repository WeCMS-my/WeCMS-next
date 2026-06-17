import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { getMenuTreeApi } from "@/api/menu";
import { usePermissionStore } from "@/stores/permission";
import type { MenuTreeDto } from "@/api/types/generated";

export interface NavigationItem {
  id: string;
  title: string;
  path: string;
}

export const useMenuStore = defineStore("menu", () => {
  const menus = ref<MenuTreeDto[]>([]);
  const menuTree = ref<MenuTreeDto[]>([]);
  const menuTreeLoaded = ref(false);
  const effectiveMenuTree = computed<MenuTreeDto[]>(() => (
    menuTreeLoaded.value ? menuTree.value : menus.value
  ));

  const navigationItems = computed<NavigationItem[]>(() => {
    return flattenTreeNavigation(effectiveMenuTree.value);
  });

  function setMenus(nextMenus: MenuTreeDto[]): void {
    menus.value = nextMenus;
  }

  async function loadMenuTreeIfAllowed(): Promise<void> {
    const permissionStore = usePermissionStore();
    if (menuTreeLoaded.value || !permissionStore.hasPermission("sys:menu:tree")) {
      return;
    }

    const result = await getMenuTreeApi();
    menuTree.value = result.data;
    menuTreeLoaded.value = true;
  }

  function clearMenus(): void {
    menus.value = [];
    menuTree.value = [];
    menuTreeLoaded.value = false;
  }

  return {
    menus,
    menuTree,
    menuTreeLoaded,
    effectiveMenuTree,
    navigationItems,
    setMenus,
    loadMenuTreeIfAllowed,
    clearMenus
  };
});

function flattenTreeNavigation(menus: MenuTreeDto[]): NavigationItem[] {
  return menus.flatMap((menu) => {
    const current = menu.hidden || menu.type === "button" || menu.status.toLowerCase() !== "enabled"
      ? []
      : [{
        id: menu.code,
        title: menu.title,
        path: menu.path
      }];

    return [...current, ...flattenTreeNavigation(menu.children ?? [])];
  });
}
