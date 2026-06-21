import type { RouteRecordRaw } from "vue-router";

declare module "vue-router" {
  interface RouteMeta {
    title: string;
    requiresAuth?: boolean;
    permissions?: string[];
    hideInMenu?: boolean;
    skipSessionRestore?: boolean;
  }
}

const LoginView = () => import("@/views/LoginView.vue");
const TwoFactorLoginView = () => import("@/views/auth/TwoFactorLoginView.vue");
const DashboardView = () => import("@/views/DashboardView.vue");
const AccountProfileView = () => import("@/views/account/AccountProfileView.vue");
const AccountSecurityView = () => import("@/views/account/AccountSecurityView.vue");
const UsersView = () => import("@/views/system/users/UsersView.vue");
const RolesView = () => import("@/views/system/roles/RolesView.vue");
const PermissionsView = () => import("@/views/system/permissions/PermissionsView.vue");
const MenusView = () => import("@/views/system/menus/MenusView.vue");
const DepartmentsView = () => import("@/views/system/depts/DepartmentsView.vue");
const PositionsView = () => import("@/views/system/positions/PositionsView.vue");
const DictsView = () => import("@/views/system/dicts/DictsView.vue");
const SettingsView = () => import("@/views/system/settings/SettingsView.vue");
const I18nMessagesView = () => import("@/views/system/i18n/I18nMessagesView.vue");
const LoginLogsView = () => import("@/views/system/logs/LoginLogsView.vue");
const AuditLogsView = () => import("@/views/system/logs/AuditLogsView.vue");
const SecurityCenterView = () => import("@/views/system/security/SecurityCenterView.vue");
const SecurityEventsView = () => import("@/views/system/logs/SecurityEventsView.vue");
const FilesView = () => import("@/views/system/files/FilesView.vue");

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
    path: "/auth/two-factor",
    component: TwoFactorLoginView,
    meta: {
      title: "二次验证",
      hideInMenu: true,
      skipSessionRestore: true
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
    path: "/account/profile",
    component: AccountProfileView,
    meta: {
      title: "个人中心",
      requiresAuth: true,
      permissions: []
    }
  },
  {
    path: "/account/security",
    component: AccountSecurityView,
    meta: {
      title: "账户安全",
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
  },
  {
    path: "/system/positions",
    component: PositionsView,
    meta: {
      title: "岗位管理",
      requiresAuth: true,
      permissions: ["sys:position:list"]
    }
  },
  {
    path: "/system/dicts",
    component: DictsView,
    meta: {
      title: "字典管理",
      requiresAuth: true,
      permissions: ["sys:dict:type:list"]
    }
  },
  {
    path: "/system/settings",
    component: SettingsView,
    meta: {
      title: "系统设置",
      requiresAuth: true,
      permissions: ["sys:setting:list"]
    }
  },
  {
    path: "/system/i18n",
    component: I18nMessagesView,
    meta: {
      title: "多语言文案",
      requiresAuth: true,
      permissions: ["sys:i18n:list"]
    }
  },
  {
    path: "/system/logs/login",
    component: LoginLogsView,
    meta: {
      title: "登录日志",
      requiresAuth: true,
      permissions: ["sys:login-log:list"]
    }
  },
  {
    path: "/system/logs/audit",
    component: AuditLogsView,
    meta: {
      title: "操作审计日志",
      requiresAuth: true,
      permissions: ["sys:audit-log:list"]
    }
  },
  {
    path: "/system/security",
    component: SecurityCenterView,
    meta: {
      title: "安全中心",
      requiresAuth: true,
      permissions: ["sys:security:page"]
    }
  },
  {
    path: "/system/logs/security",
    component: SecurityEventsView,
    meta: {
      title: "安全事件",
      requiresAuth: true,
      permissions: ["sys:security-event:list"]
    }
  },
  {
    path: "/system/files",
    component: FilesView,
    meta: {
      title: "文件管理",
      requiresAuth: true,
      permissions: ["sys:file:list"]
    }
  }
];
