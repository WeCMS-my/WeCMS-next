<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NInputNumber, NModal, NSelect, NSpace, NSwitch, useMessage } from "naive-ui";
import type { FormInst, FormRules } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { createMenuApi, deleteMenuApi, disableMenuApi, enableMenuApi, getMenuApi, getMenuTreeApi, updateMenuApi } from "@/api/menu";
import type { MenuTreeDto } from "@/api/types/generated";
import { apiErrorMessage } from "@/utils/api-error";

const message = useMessage();
const menus = ref<MenuTreeDto[]>([]);
const loading = ref(false);
const submitting = ref(false);
const formVisible = ref(false);
const formRef = ref<FormInst | null>(null);
const form = reactive({
  id: undefined as number | undefined,
  parentId: undefined as number | undefined,
  type: "menu",
  code: "",
  path: "",
  component: "",
  title: "",
  i18nKey: "",
  icon: "",
  sort: 0,
  externalUrl: "",
  permissionCode: "",
  hidden: false,
  keepAlive: false,
  status: "enabled",
  isBuiltin: false
});
const formRules: FormRules = {
  type: [{ required: true, message: "请选择类型", trigger: ["change", "blur"] }],
  code: [{ required: true, message: "请输入编码", trigger: ["input", "blur"] }],
  title: [{ required: true, message: "请输入标题", trigger: ["input", "blur"] }],
  path: [{ required: true, message: "请输入路径", trigger: ["input", "blur"] }],
  sort: [{ required: true, type: "number", message: "请输入排序", trigger: ["input", "blur"] }],
  status: [{ required: true, message: "请选择状态", trigger: ["change", "blur"] }]
};
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
  loading.value = true;
  try {
    menus.value = (await getMenuTreeApi()).data;
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    loading.value = false;
  }
}

function openCreate(): void {
  Object.assign(form, {
    id: undefined,
    parentId: undefined,
    type: "menu",
    code: "",
    path: "",
    component: "",
    title: "",
    i18nKey: "",
    icon: "",
    sort: 0,
    externalUrl: "",
    permissionCode: "",
    hidden: false,
    keepAlive: false,
    status: "enabled",
    isBuiltin: false
  });
  formVisible.value = true;
}

async function openEdit(row: MenuTreeDto): Promise<void> {
  const detail = (await getMenuApi(row.id)).data;
  Object.assign(form, {
    ...detail,
    parentId: detail.parentId ?? undefined,
    component: detail.component ?? "",
    i18nKey: detail.i18nKey ?? "",
    icon: detail.icon ?? "",
    externalUrl: detail.externalUrl ?? "",
    permissionCode: detail.permissionCode ?? ""
  });
  formVisible.value = true;
}

async function submitForm(): Promise<void> {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }

  submitting.value = true;
  try {
    if (form.id) {
      await updateMenuApi(form.id, {
        parentId: form.parentId ?? null,
        type: form.type,
        path: form.path,
        component: form.component || null,
        title: form.title,
        i18nKey: form.i18nKey || null,
        icon: form.icon || null,
        sort: form.sort,
        hidden: form.hidden,
        keepAlive: form.keepAlive,
        externalUrl: form.externalUrl || null,
        permissionCode: form.permissionCode || null,
        status: form.status
      });
    } else {
      await createMenuApi({
        parentId: form.parentId ?? null,
        type: form.type,
        code: form.code,
        path: form.path,
        component: form.component || null,
        title: form.title,
        i18nKey: form.i18nKey || null,
        icon: form.icon || null,
        sort: form.sort,
        hidden: form.hidden,
        keepAlive: form.keepAlive,
        externalUrl: form.externalUrl || null,
        permissionCode: form.permissionCode || null,
        status: form.status
      });
    }
    message.success("菜单已保存。");
    formVisible.value = false;
    await loadMenus();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function confirmDelete(row: MenuTreeDto): Promise<void> {
  if (!window.confirm(`确认删除菜单 ${row.title}？`)) return;
  submitting.value = true;
  try {
    await deleteMenuApi(row.id);
    message.success("菜单已删除。");
    await loadMenus();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function changeStatus(row: MenuTreeDto): Promise<void> {
  if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}菜单 ${row.title}？`)) return;
  submitting.value = true;
  try {
    if (row.status === "enabled") await disableMenuApi(row.id); else await enableMenuApi(row.id);
    message.success("菜单状态已更新。");
    await loadMenus();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
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
      <NDataTable :columns="columns" :data="menus" :children-key="'children'" :loading="loading">
        <template #empty>暂无菜单</template>
      </NDataTable>
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="菜单表单" class="max-w-xl">
      <NForm ref="formRef" :model="form" :rules="formRules">
        <NFormItem label="父级"><NSelect v-model:value="form.parentId" clearable :options="menuOptions" /></NFormItem>
        <NFormItem path="type" label="类型"><NSelect v-model:value="form.type" :options="[{ label: '目录', value: 'catalog' }, { label: '菜单', value: 'menu' }, { label: '按钮', value: 'button' }]" /></NFormItem>
        <NFormItem v-if="!form.id" path="code" label="编码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem path="title" label="标题"><NInput v-model:value="form.title" /></NFormItem>
        <NFormItem path="path" label="路径"><NInput v-model:value="form.path" /></NFormItem>
        <NFormItem label="国际化 Key"><NInput v-model:value="form.i18nKey" /></NFormItem>
        <NFormItem label="图标"><NInput v-model:value="form.icon" /></NFormItem>
        <NFormItem label="外链地址"><NInput v-model:value="form.externalUrl" /></NFormItem>
        <NFormItem label="权限码"><NInput v-model:value="form.permissionCode" /></NFormItem>
        <NFormItem label="组件"><NInput v-model:value="form.component" /></NFormItem>
        <NFormItem path="sort" label="排序"><NInputNumber v-model:value="form.sort" /></NFormItem>
        <NFormItem path="status" label="状态"><NSelect v-model:value="form.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NFormItem label="隐藏"><NSwitch v-model:value="form.hidden" /></NFormItem>
        <NFormItem label="缓存"><NSwitch v-model:value="form.keepAlive" /></NFormItem>
        <NSpace justify="end"><NButton :disabled="submitting" @click="formVisible = false">取消</NButton><NButton type="primary" :loading="submitting" @click="submitForm">保存</NButton></NSpace>
      </NForm>
    </NModal>
  </main>
</template>
