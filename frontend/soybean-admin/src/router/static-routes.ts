import type { RouteRecordRaw } from "vue-router";

declare module "vue-router" {
  interface RouteMeta {
    title: string;
    requiresAuth?: boolean;
    permissions?: string[];
    hideInMenu?: boolean;
  }
}

const LoginView = () => import("@/views/LoginView.vue");
const DashboardView = () => import("@/views/DashboardView.vue");
const UsersView = () => import("@/views/system/users/UsersView.vue");
const RolesView = () => import("@/views/system/roles/RolesView.vue");
const PermissionsView = () => import("@/views/system/permissions/PermissionsView.vue");
const MenusView = () => import("@/views/system/menus/MenusView.vue");
const DepartmentsView = () => import("@/views/system/depts/DepartmentsView.vue");

export const staticRoutes: RouteRecordRaw[] = [
  {
    path: "/",
    redirect: "/dashboard"
  },
  {
    path: "/login",
    component: LoginView,
    meta: {
      title: "登录",
      hideInMenu: true
    }
  },
  {
    path: "/dashboard",
    component: DashboardView,
    meta: {
      title: "工作台",
      requiresAuth: true,
      permissions: []
    }
  },
  {
    path: "/system/users",
    component: UsersView,
    meta: {
      title: "用户管理",
      requiresAuth: true,
      permissions: ["sys:user:list"]
    }
  },
  {
    path: "/system/roles",
    component: RolesView,
    meta: {
      title: "角色管理",
      requiresAuth: true,
      permissions: ["sys:role:list"]
    }
  },
  {
    path: "/system/permissions",
    component: PermissionsView,
    meta: {
      title: "权限管理",
      requiresAuth: true,
      permissions: ["sys:permission:list"]
    }
  },
  {
    path: "/system/menus",
    component: MenusView,
    meta: {
      title: "菜单管理",
      requiresAuth: true,
      permissions: ["sys:menu:list"]
    }
  },
  {
    path: "/system/depts",
    component: DepartmentsView,
    meta: {
      title: "部门管理",
      requiresAuth: true,
      permissions: ["sys:dept:list"]
    }
  }
];
