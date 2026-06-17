<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NModal, NSelect, NSpace, NTag, useMessage } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { getMenuTreeApi } from "@/api/menu";
import { getPermissionTreeApi } from "@/api/system/permissions";
import {
  assignRoleMenusApi,
  assignRolePermissionsApi,
  createRoleApi,
  deleteRoleApi,
  disableRoleApi,
  enableRoleApi,
  getRoleApi,
  getRolesApi,
  updateRoleApi
} from "@/api/system/roles";
import type { MenuTreeDto, PermissionTreeDto, RoleDetailDto, RoleSummaryDto } from "@/api/types/generated";

const message = useMessage();
const rows = ref<RoleSummaryDto[]>([]);
const loading = ref(false);
const formVisible = ref(false);
const permissionVisible = ref(false);
const menuVisible = ref(false);
const activeRole = ref<RoleDetailDto | null>(null);
const permissionTree = ref<PermissionTreeDto[]>([]);
const menuTree = ref<MenuTreeDto[]>([]);
const assignedPermissionIds = ref<number[]>([]);
const assignedMenuIds = ref<number[]>([]);
const form = reactive({ id: undefined as number | undefined, code: "", name: "" });
const permissionOptions = computed(() => permissionTree.value.flatMap((group) => group.permissions.map((item) => ({
  label: `${group.module} / ${item.name}`,
  value: item.id
}))));
const menuOptions = computed(() => flattenMenus(menuTree.value));
const columns = computed(() => [
  { title: "ID", key: "id", width: 70 },
  { title: "角色编码", key: "code" },
  { title: "角色名称", key: "name" },
  {
    title: "状态",
    key: "status",
    render: (row: RoleSummaryDto) => h(NTag, { type: row.status === "enabled" ? "success" : "warning" }, {
      default: () => row.status === "enabled" ? "启用" : "禁用"
    })
  },
  { title: "内置", key: "isBuiltin", render: (row: RoleSummaryDto) => row.isBuiltin ? "是" : "否" },
  { title: "锁定", key: "isLocked", render: (row: RoleSummaryDto) => row.isLocked ? "是" : "否" },
  { title: "创建时间", key: "createdAt" },
  {
    title: "操作",
    key: "actions",
    width: 360,
    render: (row: RoleSummaryDto) => h(NSpace, null, { default: () => [
      h(PermissionButton, { secondary: true, disabled: row.isLocked, permissions: ["sys:role:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }),
      h(PermissionButton, { secondary: true, disabled: row.isLocked || row.isBuiltin, permissions: ["sys:role:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" }),
      h(PermissionButton, { secondary: true, disabled: row.isLocked, permissions: [row.status === "enabled" ? "sys:role:disable" : "sys:role:enable"], onClick: () => void changeStatus(row) }, { default: () => row.status === "enabled" ? "禁用" : "启用" }),
      h(PermissionButton, { secondary: true, disabled: row.isLocked, permissions: ["sys:role:assign-permission"], onClick: () => void openAssignPermissions(row) }, { default: () => "分配权限" }),
      h(PermissionButton, { secondary: true, disabled: row.isLocked, permissions: ["sys:role:assign-menu"], onClick: () => void openAssignMenus(row) }, { default: () => "分配菜单" })
    ] })
  }
]);

onMounted(async () => {
  await Promise.all([loadRoles(), loadLookups()]);
});

async function loadRoles(): Promise<void> {
  loading.value = true;
  try {
    rows.value = (await getRolesApi()).data.records;
  } finally {
    loading.value = false;
  }
}

async function loadLookups(): Promise<void> {
  const [permissionResult, menuResult] = await Promise.all([getPermissionTreeApi(), getMenuTreeApi()]);
  permissionTree.value = permissionResult.data;
  menuTree.value = menuResult.data;
}

function openCreate(): void {
  Object.assign(form, { id: undefined, code: "", name: "" });
  formVisible.value = true;
}

async function openEdit(row: RoleSummaryDto): Promise<void> {
  const detail = (await getRoleApi(row.id)).data;
  Object.assign(form, { id: detail.id, code: detail.code, name: detail.name });
  formVisible.value = true;
}

async function submitForm(): Promise<void> {
  if (!form.name.trim() || (!form.id && !form.code.trim())) {
    message.error("请填写必填字段。");
    return;
  }
  if (form.id) {
    await updateRoleApi(form.id, { name: form.name.trim() });
  } else {
    await createRoleApi({ code: form.code.trim(), name: form.name.trim(), permissionIds: [], menuIds: [] });
  }
  message.success("角色已保存。");
  formVisible.value = false;
  await loadRoles();
}

async function confirmDelete(row: RoleSummaryDto): Promise<void> {
  if (!window.confirm(`确认删除角色 ${row.name}？`)) {
    return;
  }
  await deleteRoleApi(row.id);
  message.success("角色已删除。");
  await loadRoles();
}

async function changeStatus(row: RoleSummaryDto): Promise<void> {
  if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}角色 ${row.name}？`)) {
    return;
  }
  if (row.status === "enabled") {
    await disableRoleApi(row.id);
  } else {
    await enableRoleApi(row.id);
  }
  message.success("角色状态已更新。");
  await loadRoles();
}

async function openAssignPermissions(row: RoleSummaryDto): Promise<void> {
  activeRole.value = (await getRoleApi(row.id)).data;
  assignedPermissionIds.value = activeRole.value.permissionIds;
  permissionVisible.value = true;
}

async function openAssignMenus(row: RoleSummaryDto): Promise<void> {
  activeRole.value = (await getRoleApi(row.id)).data;
  assignedMenuIds.value = activeRole.value.menuIds;
  menuVisible.value = true;
}

async function submitPermissions(): Promise<void> {
  if (!activeRole.value) {
    return;
  }
  await assignRolePermissionsApi(activeRole.value.id, { permissionIds: assignedPermissionIds.value });
  message.success("权限已保存。");
  permissionVisible.value = false;
}

async function submitMenus(): Promise<void> {
  if (!activeRole.value) {
    return;
  }
  await assignRoleMenusApi(activeRole.value.id, { menuIds: assignedMenuIds.value });
  message.success("菜单已保存。");
  menuVisible.value = false;
}

function flattenMenus(menus: MenuTreeDto[]): Array<{ label: string; value: number }> {
  return menus.flatMap((menu) => [{ label: menu.title, value: menu.id }, ...flattenMenus(menu.children ?? [])]);
}
</script>

<template>
  <main>
    <NCard title="角色管理">
      <NSpace class="mb-4">
        <PermissionButton type="primary" :permissions="['sys:role:create']" @click="openCreate">新建角色</PermissionButton>
      </NSpace>
      <NDataTable :columns="columns" :data="rows" :loading="loading" />
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="角色表单" class="max-w-lg">
      <NForm>
        <NFormItem v-if="!form.id" label="角色编码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem label="角色名称"><NInput v-model:value="form.name" /></NFormItem>
        <NSpace justify="end"><NButton @click="formVisible = false">取消</NButton><NButton type="primary" @click="submitForm">保存</NButton></NSpace>
      </NForm>
    </NModal>
    <NModal v-model:show="permissionVisible" preset="card" title="分配权限" class="max-w-xl">
      <NSelect v-model:value="assignedPermissionIds" multiple filterable :options="permissionOptions" />
      <NSpace class="mt-4" justify="end"><NButton @click="permissionVisible = false">取消</NButton><NButton type="primary" @click="submitPermissions">保存</NButton></NSpace>
    </NModal>
    <NModal v-model:show="menuVisible" preset="card" title="分配菜单" class="max-w-xl">
      <NSelect v-model:value="assignedMenuIds" multiple filterable :options="menuOptions" />
      <NSpace class="mt-4" justify="end"><NButton @click="menuVisible = false">取消</NButton><NButton type="primary" @click="submitMenus">保存</NButton></NSpace>
    </NModal>
  </main>
</template>
