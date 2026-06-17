<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NModal, NSpace, NTag, useMessage } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import {
  createPermissionApi,
  deletePermissionApi,
  disablePermissionApi,
  enablePermissionApi,
  getPermissionApi,
  getPermissionsApi,
  updatePermissionApi
} from "@/api/system/permissions";
import type { PermissionDetailDto, PermissionSummaryDto } from "@/api/types/generated";

const message = useMessage();
const rows = ref<PermissionSummaryDto[]>([]);
const loading = ref(false);
const formVisible = ref(false);
const form = reactive({
  id: undefined as number | undefined,
  code: "",
  name: "",
  module: "",
  description: ""
});

const columns = computed(() => [
  { title: "ID", key: "id", width: 70 },
  { title: "权限码", key: "code" },
  { title: "名称", key: "name" },
  { title: "模块", key: "module" },
  { title: "描述", key: "description" },
  {
    title: "状态",
    key: "status",
    render: (row: PermissionSummaryDto) => h(NTag, { type: row.status === "enabled" ? "success" : "warning" }, {
      default: () => row.status === "enabled" ? "启用" : "禁用"
    })
  },
  { title: "内置", key: "isBuiltin", render: (row: PermissionSummaryDto) => row.isBuiltin ? "是" : "否" },
  { title: "绑定角色", key: "isRoleBound", render: (row: PermissionSummaryDto) => row.isRoleBound ? "是" : "否" },
  {
    title: "操作",
    key: "actions",
    width: 260,
    render: (row: PermissionSummaryDto) => h(NSpace, null, { default: () => [
      h(PermissionButton, { secondary: true, permissions: ["sys:permission:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }),
      h(PermissionButton, { secondary: true, disabled: row.isBuiltin, permissions: ["sys:permission:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" }),
      h(PermissionButton, { secondary: true, disabled: row.isBuiltin, permissions: [row.status === "enabled" ? "sys:permission:disable" : "sys:permission:enable"], onClick: () => void changeStatus(row) }, { default: () => row.status === "enabled" ? "禁用" : "启用" })
    ] })
  }
]);

onMounted(loadPermissions);

async function loadPermissions(): Promise<void> {
  loading.value = true;
  try {
    rows.value = (await getPermissionsApi()).data;
  } finally {
    loading.value = false;
  }
}

function openCreate(): void {
  Object.assign(form, { id: undefined, code: "", name: "", module: "", description: "" });
  formVisible.value = true;
}

async function openEdit(row: PermissionSummaryDto): Promise<void> {
  const detail: PermissionDetailDto = (await getPermissionApi(row.id)).data;
  Object.assign(form, {
    id: detail.id,
    code: detail.code,
    name: detail.name,
    module: detail.module,
    description: detail.description ?? ""
  });
  formVisible.value = true;
}

async function submitForm(): Promise<void> {
  if (!form.name.trim() || !form.module.trim() || (!form.id && !form.code.trim())) {
    message.error("请填写必填字段。");
    return;
  }
  if (form.id) {
    await updatePermissionApi(form.id, {
      name: form.name.trim(),
      module: form.module.trim(),
      description: form.description || null
    });
  } else {
    await createPermissionApi({
      code: form.code.trim(),
      name: form.name.trim(),
      module: form.module.trim(),
      description: form.description || null
    });
  }
  message.success("权限已保存。");
  formVisible.value = false;
  await loadPermissions();
}

async function confirmDelete(row: PermissionSummaryDto): Promise<void> {
  const prompt = row.isRoleBound
    ? `权限 ${row.name} 已绑定角色，仍要删除？`
    : `确认删除权限 ${row.name}？`;
  if (!window.confirm(prompt)) {
    return;
  }
  await deletePermissionApi(row.id);
  message.success("权限已删除。");
  await loadPermissions();
}

async function changeStatus(row: PermissionSummaryDto): Promise<void> {
  if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}权限 ${row.name}？`)) {
    return;
  }
  if (row.status === "enabled") {
    await disablePermissionApi(row.id);
  } else {
    await enablePermissionApi(row.id);
  }
  message.success("权限状态已更新。");
  await loadPermissions();
}
</script>

<template>
  <main>
    <NCard title="权限管理">
      <NSpace class="mb-4">
        <PermissionButton type="primary" :permissions="['sys:permission:create']" @click="openCreate">
          新建权限
        </PermissionButton>
      </NSpace>
      <NDataTable :columns="columns" :data="rows" :loading="loading" />
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="权限表单" class="max-w-lg">
      <NForm>
        <NFormItem v-if="!form.id" label="权限码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem label="名称"><NInput v-model:value="form.name" /></NFormItem>
        <NFormItem label="模块"><NInput v-model:value="form.module" /></NFormItem>
        <NFormItem label="描述"><NInput v-model:value="form.description" type="textarea" /></NFormItem>
        <NSpace justify="end">
          <NButton @click="formVisible = false">取消</NButton>
          <NButton type="primary" @click="submitForm">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </main>
</template>
