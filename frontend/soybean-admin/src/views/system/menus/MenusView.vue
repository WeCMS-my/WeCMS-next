<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NModal, NSelect, NSpace, NSwitch, useMessage } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { createMenuApi, deleteMenuApi, disableMenuApi, enableMenuApi, getMenuApi, getMenuTreeApi, updateMenuApi } from "@/api/menu";
import type { MenuTreeDto } from "@/api/types/generated";

const message = useMessage();
const menus = ref<MenuTreeDto[]>([]);
const formVisible = ref(false);
const form = reactive({ id: undefined as number | undefined, parentId: undefined as number | undefined, type: "menu", code: "", path: "", component: "", title: "", sort: 0, hidden: false, keepAlive: false, status: "enabled", isBuiltin: false });
const menuOptions = computed(() => flattenMenus(menus.value).filter((item) => item.value !== form.id && !isDescendant(form.id, item.value, menus.value)));
const columns = computed(() => [
  { title: "标题", key: "title" },
  { title: "类型", key: "type" },
  { title: "路径", key: "path" },
  { title: "组件", key: "component" },
  { title: "权限码", key: "permissionCode" },
  { title: "状态", key: "status" },
  { title: "内置", key: "isBuiltin", render: (row: MenuTreeDto) => row.isBuiltin ? "是" : "否" },
  { title: "操作", key: "actions", render: (row: MenuTreeDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:menu:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, disabled: row.isBuiltin, permissions: ["sys:menu:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" }),
    h(PermissionButton, { secondary: true, disabled: row.isBuiltin, permissions: [row.status === "enabled" ? "sys:menu:disable" : "sys:menu:enable"], onClick: () => void changeStatus(row) }, { default: () => row.status === "enabled" ? "禁用" : "启用" })
  ] }) }
]);

onMounted(loadMenus);

async function loadMenus(): Promise<void> {
  menus.value = (await getMenuTreeApi()).data;
}

function openCreate(): void {
  Object.assign(form, { id: undefined, parentId: undefined, type: "menu", code: "", path: "", component: "", title: "", sort: 0, hidden: false, keepAlive: false, status: "enabled", isBuiltin: false });
  formVisible.value = true;
}

async function openEdit(row: MenuTreeDto): Promise<void> {
  const detail = (await getMenuApi(row.id)).data;
  Object.assign(form, { ...detail, parentId: detail.parentId ?? undefined, component: detail.component ?? "" });
  formVisible.value = true;
}

async function submitForm(): Promise<void> {
  if (!form.title.trim() || (!form.id && !form.code.trim())) {
    message.error("请填写必填字段。");
    return;
  }
  if (form.id) {
    await updateMenuApi(form.id, { parentId: form.parentId ?? null, type: form.type, path: form.path, component: form.component || null, title: form.title, sort: form.sort, hidden: form.hidden, keepAlive: form.keepAlive, status: form.status });
  } else {
    await createMenuApi({ parentId: form.parentId ?? null, type: form.type, code: form.code, path: form.path, component: form.component || null, title: form.title, sort: form.sort, hidden: form.hidden, keepAlive: form.keepAlive, status: form.status });
  }
  message.success("菜单已保存。");
  formVisible.value = false;
  await loadMenus();
}

async function confirmDelete(row: MenuTreeDto): Promise<void> {
  if (!window.confirm(`确认删除菜单 ${row.title}？`)) return;
  await deleteMenuApi(row.id);
  message.success("菜单已删除。");
  await loadMenus();
}

async function changeStatus(row: MenuTreeDto): Promise<void> {
  if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}菜单 ${row.title}？`)) return;
  if (row.status === "enabled") await disableMenuApi(row.id); else await enableMenuApi(row.id);
  message.success("菜单状态已更新。");
  await loadMenus();
}

function flattenMenus(items: MenuTreeDto[]): Array<{ label: string; value: number }> {
  return items.flatMap((item) => [{ label: item.title, value: item.id }, ...flattenMenus(item.children ?? [])]);
}

function isDescendant(rootId: number | undefined, candidateId: number, items: MenuTreeDto[]): boolean {
  if (!rootId) return false;
  const root = findMenu(rootId, items);
  return Boolean(root && flattenMenus(root.children ?? []).some((item) => item.value === candidateId));
}

function findMenu(id: number, items: MenuTreeDto[]): MenuTreeDto | undefined {
  for (const item of items) {
    if (item.id === id) return item;
    const found = findMenu(id, item.children ?? []);
    if (found) return found;
  }
  return undefined;
}
</script>

<template>
  <main>
    <NCard title="菜单管理">
      <NSpace class="mb-4"><PermissionButton type="primary" :permissions="['sys:menu:create']" @click="openCreate">新建菜单</PermissionButton></NSpace>
      <NDataTable :columns="columns" :data="menus" :children-key="'children'" />
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="菜单表单" class="max-w-xl">
      <NForm>
        <NFormItem label="父级"><NSelect v-model:value="form.parentId" clearable :options="menuOptions" /></NFormItem>
        <NFormItem label="类型"><NSelect v-model:value="form.type" :options="[{ label: '目录', value: 'catalog' }, { label: '菜单', value: 'menu' }, { label: '按钮', value: 'button' }]" /></NFormItem>
        <NFormItem v-if="!form.id" label="编码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem label="标题"><NInput v-model:value="form.title" /></NFormItem>
        <NFormItem label="路径"><NInput v-model:value="form.path" /></NFormItem>
        <NFormItem label="组件"><NInput v-model:value="form.component" /></NFormItem>
        <NFormItem label="隐藏"><NSwitch v-model:value="form.hidden" /></NFormItem>
        <NFormItem label="缓存"><NSwitch v-model:value="form.keepAlive" /></NFormItem>
        <NSpace justify="end"><NButton @click="formVisible = false">取消</NButton><NButton type="primary" @click="submitForm">保存</NButton></NSpace>
      </NForm>
    </NModal>
  </main>
</template>
