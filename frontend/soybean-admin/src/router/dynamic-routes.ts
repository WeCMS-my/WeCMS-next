import type { Component } from "vue";
import type { RouteRecordRaw, Router } from "vue-router";
import type { MenuTreeDto } from "@/api/types/generated";

const routeComponentMap: Record<string, () => Promise<Component>> = {
  "dashboard/index": () => import("@/views/DashboardView.vue"),
  "account/profile/index": () => import("@/views/account/AccountProfileView.vue"),
  "account/security/index": () => import("@/views/account/AccountSecurityView.vue"),
  "system/user/index": () => import("@/views/system/users/UsersView.vue"),
  "system/users/index": () => import("@/views/system/users/UsersView.vue"),
  "system/role/index": () => import("@/views/system/roles/RolesView.vue"),
  "system/roles/index": () => import("@/views/system/roles/RolesView.vue"),
  "system/permission/index": () => import("@/views/system/permissions/PermissionsView.vue"),
  "system/permissions/index": () => import("@/views/system/permissions/PermissionsView.vue"),
  "system/menu/index": () => import("@/views/system/menus/MenusView.vue"),
  "system/menus/index": () => import("@/views/system/menus/MenusView.vue"),
  "system/dept/index": () => import("@/views/system/depts/DepartmentsView.vue"),
  "system/depts/index": () => import("@/views/system/depts/DepartmentsView.vue"),
  "system/position/index": () => import("@/views/system/positions/PositionsView.vue"),
  "system/positions/index": () => import("@/views/system/positions/PositionsView.vue"),
  "system/dict/index": () => import("@/views/system/dicts/DictsView.vue"),
  "system/dicts/index": () => import("@/views/system/dicts/DictsView.vue"),
  "system/setting/index": () => import("@/views/system/settings/SettingsView.vue"),
  "system/settings/index": () => import("@/views/system/settings/SettingsView.vue"),
  "system/i18n/index": () => import("@/views/system/i18n/I18nMessagesView.vue"),
  "system/i18n-message/index": () => import("@/views/system/i18n/I18nMessagesView.vue"),
  "system/login-log/index": () => import("@/views/system/logs/LoginLogsView.vue"),
  "system/login-logs/index": () => import("@/views/system/logs/LoginLogsView.vue"),
  "system/audit-log/index": () => import("@/views/system/logs/AuditLogsView.vue"),
  "system/audit-logs/index": () => import("@/views/system/logs/AuditLogsView.vue"),
  "system/security/index": () => import("@/views/system/security/SecurityCenterView.vue"),
  "system/security-event/index": () => import("@/views/system/logs/SecurityEventsView.vue"),
  "system/security-events/index": () => import("@/views/system/logs/SecurityEventsView.vue"),
  "system/file/index": () => import("@/views/system/files/FilesView.vue"),
  "system/files/index": () => import("@/views/system/files/FilesView.vue")
};

const dynamicRouteNames = new Set<string>();

export function buildDynamicRoutes(menus: MenuTreeDto[], permissions: string[]): RouteRecordRaw[] {
  return menus.flatMap((menu) => buildDynamicRoute(menu, permissions));
}

export function registerDynamicRoutes(router: Router, menus: MenuTreeDto[], permissions: string[]): void {
  for (const routeName of dynamicRouteNames) {
    if (router.hasRoute(routeName)) {
      router.removeRoute(routeName);
    }
  }
  dynamicRouteNames.clear();

  for (const route of buildDynamicRoutes(menus, permissions)) {
    router.addRoute(route);
    dynamicRouteNames.add(String(route.name));
  }
}

function buildDynamicRoute(menu: MenuTreeDto, permissions: string[]): RouteRecordRaw[] {
  if (!isRoutableMenu(menu) || !isMenuAllowed(menu, permissions)) {
    return buildDynamicRoutes(menu.children ?? [], permissions);
  }

  const componentKey = menu.component ?? "";
  const component = routeComponentMap[componentKey];
  if (!component) {
    console.warn(`Skipping unknown menu component: ${componentKey}`);
    return buildDynamicRoutes(menu.children ?? [], permissions);
  }

  const route: RouteRecordRaw = {
    path: menu.path,
    name: menu.code,
    component,
    meta: {
      title: menu.title,
      requiresAuth: true,
      permissions: menu.permissionCode ? [menu.permissionCode] : [],
      hideInMenu: menu.hidden
    }
  };

  return [route, ...buildDynamicRoutes(menu.children ?? [], permissions)];
}

function isRoutableMenu(menu: MenuTreeDto): boolean {
  return menu.status.toLowerCase() === "enabled"
    && !menu.hidden
    && menu.type !== "button"
    && Boolean(menu.path);
}

function isMenuAllowed(menu: MenuTreeDto, permissions: string[]): boolean {
  return !menu.permissionCode || permissions.includes(menu.permissionCode);
}
