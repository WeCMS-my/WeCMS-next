import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { getMenuTreeApi } from "@/api/menu";
import { usePermissionStore } from "@/stores/permission";
import type { AuthMenuDto, MenuTreeDto } from "@/api/types/generated";

export interface NavigationItem {
  id: string;
  title: string;
  path: string;
}

export const useMenuStore = defineStore("menu", () => {
  const menus = ref<AuthMenuDto[]>([]);
  const menuTree = ref<MenuTreeDto[]>([]);
  const menuTreeLoaded = ref(false);

  const navigationItems = computed<NavigationItem[]>(() => {
    const treeItems = flattenTreeNavigation(menuTree.value);
    if (treeItems.length > 0) {
      return treeItems;
    }

    return menus.value
      .filter((menu) => menu.type !== "button" && Boolean(menu.path))
      .map((menu) => ({
        id: String(menu.id),
        title: menu.title,
        path: menu.path
      }));
  });

  function setMenus(nextMenus: AuthMenuDto[]): void {
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
