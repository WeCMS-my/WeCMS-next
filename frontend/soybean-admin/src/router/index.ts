import { createRouter, createWebHistory } from "vue-router";
import { installRouterGuards } from "./guards";
import { staticRoutes } from "./static-routes";

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    ...staticRoutes,
    {
      path: "/403",
      component: () => import("@/views/ForbiddenView.vue"),
      meta: {
        title: "无权限",
        hideInMenu: true
      }
    },
    {
      path: "/:pathMatch(.*)*",
      component: () => import("@/views/NotFoundView.vue"),
      meta: {
        title: "未找到",
        hideInMenu: true
      }
    }
  ]
});

installRouterGuards(router);
