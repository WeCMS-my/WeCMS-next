<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NInputNumber, NModal, NSelect, NSpace, useMessage } from "naive-ui";
import type { FormInst, FormRules } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import {
  createDepartmentApi,
  deleteDepartmentApi,
  disableDepartmentApi,
  enableDepartmentApi,
  getDepartmentApi,
  getDepartmentTreeApi,
  updateDepartmentApi
} from "@/api/system/depts";
import type { DepartmentTreeDto } from "@/api/types/generated";
import { apiErrorMessage } from "@/utils/api-error";

const message = useMessage();
const departments = ref<DepartmentTreeDto[]>([]);
const loading = ref(false);
const submitting = ref(false);
const formVisible = ref(false);
const formRef = ref<FormInst | null>(null);
const form = reactive({ id: undefined as number | undefined, parentId: undefined as number | undefined, code: "", name: "", sortOrder: 0, status: "enabled" });
const formRules: FormRules = {
  code: [{ required: true, message: "请输入编码", trigger: ["input", "blur"] }],
  name: [{ required: true, message: "请输入名称", trigger: ["input", "blur"] }],
  status: [{ required: true, message: "请选择状态", trigger: ["change", "blur"] }]
};
const departmentOptions = computed(() => flattenDepartments(departments.value).filter((item) => item.value !== form.id && !isDescendant(form.id, item.value, departments.value)));
const columns = computed(() => [
  { title: "名称", key: "name" },
  { title: "编码", key: "code" },
  { title: "排序", key: "sortOrder" },
  { title: "状态", key: "status" },
  { title: "操作", key: "actions", render: (row: DepartmentTreeDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:dept:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:dept:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" }),
    h(PermissionButton, { secondary: true, permissions: [row.status === "enabled" ? "sys:dept:disable" : "sys:dept:enable"], onClick: () => void changeStatus(row) }, { default: () => row.status === "enabled" ? "禁用" : "启用" })
  ] }) }
]);

onMounted(loadDepartments);

async function loadDepartments(): Promise<void> {
  loading.value = true;
  try {
    departments.value = (await getDepartmentTreeApi()).data;
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    loading.value = false;
  }
}

function openCreate(): void {
  Object.assign(form, { id: undefined, parentId: undefined, code: "", name: "", sortOrder: 0, status: "enabled" });
  formVisible.value = true;
}

async function openEdit(row: DepartmentTreeDto): Promise<void> {
  const detail = (await getDepartmentApi(row.id)).data;
  Object.assign(form, { ...detail, parentId: detail.parentId ?? undefined });
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
      await updateDepartmentApi(form.id, { parentId: form.parentId ?? null, name: form.name, sortOrder: form.sortOrder, status: form.status });
    } else {
      await createDepartmentApi({ parentId: form.parentId ?? null, code: form.code, name: form.name, sortOrder: form.sortOrder, status: form.status });
    }
    message.success("部门已保存。");
    formVisible.value = false;
    await loadDepartments();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function confirmDelete(row: DepartmentTreeDto): Promise<void> {
  if (!window.confirm(`确认删除部门 ${row.name}？`)) return;
  submitting.value = true;
  try {
    await deleteDepartmentApi(row.id);
    message.success("部门已删除。");
    await loadDepartments();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function changeStatus(row: DepartmentTreeDto): Promise<void> {
  if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}部门 ${row.name}？`)) return;
  submitting.value = true;
  try {
    if (row.status === "enabled") await disableDepartmentApi(row.id); else await enableDepartmentApi(row.id);
    message.success("部门状态已更新。");
    await loadDepartments();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

function flattenDepartments(items: DepartmentTreeDto[]): Array<{ label: string; value: number }> {
  return items.flatMap((item) => [{ label: item.name, value: item.id }, ...flattenDepartments(item.children ?? [])]);
}

function isDescendant(rootId: number | undefined, candidateId: number, items: DepartmentTreeDto[]): boolean {
  if (!rootId) return false;
  const root = findDepartment(rootId, items);
  return Boolean(root && flattenDepartments(root.children ?? []).some((item) => item.value === candidateId));
}

function findDepartment(id: number, items: DepartmentTreeDto[]): DepartmentTreeDto | undefined {
  for (const item of items) {
    if (item.id === id) return item;
    const found = findDepartment(id, item.children ?? []);
    if (found) return found;
  }
  return undefined;
}
</script>

<template>
  <main>
    <NCard title="部门管理">
      <NSpace class="mb-4"><PermissionButton type="primary" :permissions="['sys:dept:create']" @click="openCreate">新建部门</PermissionButton></NSpace>
      <NDataTable :columns="columns" :data="departments" :children-key="'children'" :loading="loading">
        <template #empty>暂无部门</template>
      </NDataTable>
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="部门表单" class="max-w-lg">
      <NForm ref="formRef" :model="form" :rules="formRules">
        <NFormItem label="父级"><NSelect v-model:value="form.parentId" clearable :options="departmentOptions" /></NFormItem>
        <NFormItem v-if="!form.id" path="code" label="编码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem path="name" label="名称"><NInput v-model:value="form.name" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="form.sortOrder" /></NFormItem>
        <NFormItem path="status" label="状态"><NSelect v-model:value="form.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end"><NButton :disabled="submitting" @click="formVisible = false">取消</NButton><NButton type="primary" :loading="submitting" @click="submitForm">保存</NButton></NSpace>
      </NForm>
    </NModal>
  </main>
</template>
