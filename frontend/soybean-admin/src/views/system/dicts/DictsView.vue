<script setup lang="ts">
import { computed, h, reactive, ref, onMounted } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NInputNumber, NModal, NSelect, NSpace, NSwitch, useMessage } from "naive-ui";
import type { FormInst, FormRules } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import {
  createDictTypeApi,
  createDictValueApi,
  deleteDictTypeApi,
  deleteDictValueApi,
  getDictTypesApi,
  getDictValuesApi,
  updateDictTypeApi,
  updateDictValueApi
} from "@/api/system/dicts";
import type { DictTypeSummaryDto, DictValueDto } from "@/api/types/generated";
import { apiErrorMessage } from "@/utils/api-error";

const message = useMessage();
const types = ref<DictTypeSummaryDto[]>([]);
const values = ref<DictValueDto[]>([]);
const activeType = ref<DictTypeSummaryDto | null>(null);
const typeLoading = ref(false);
const valueLoading = ref(false);
const submitting = ref(false);
const typePage = ref(1);
const typePageSize = ref(20);
const typeTotal = ref(0);
const typeVisible = ref(false);
const valueVisible = ref(false);
const typeFormRef = ref<FormInst | null>(null);
const valueFormRef = ref<FormInst | null>(null);
const typeForm = reactive({ id: undefined as number | undefined, code: "", name: "", description: "", sortOrder: 0, status: "enabled", isSystem: false });
const valueForm = reactive({ id: undefined as number | undefined, label: "", value: "", description: "", sortOrder: 0, isDefault: false, status: "enabled" });
const typeRules: FormRules = {
  code: [{ required: true, message: "请输入编码", trigger: ["input", "blur"] }],
  name: [{ required: true, message: "请输入名称", trigger: ["input", "blur"] }],
  status: [{ required: true, message: "请选择状态", trigger: ["change", "blur"] }]
};
const valueRules: FormRules = {
  label: [{ required: true, message: "请输入标签", trigger: ["input", "blur"] }],
  value: [{ required: true, message: "请输入值", trigger: ["input", "blur"] }],
  status: [{ required: true, message: "请选择状态", trigger: ["change", "blur"] }]
};

const typeColumns = computed(() => [
  { title: "编码", key: "code" },
  { title: "名称", key: "name" },
  { title: "系统", key: "isSystem", render: (row: DictTypeSummaryDto) => row.isSystem ? "是" : "否" },
  { title: "状态", key: "status" },
  { title: "操作", key: "actions", render: (row: DictTypeSummaryDto) => h(NSpace, null, { default: () => [
    h(NButton, { secondary: true, onClick: () => void selectType(row) }, { default: () => "值" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:dict:type:update"], onClick: () => openTypeEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, disabled: row.isSystem, permissions: ["sys:dict:type:delete"], onClick: () => void confirmDeleteType(row) }, { default: () => "删除" })
  ] }) }
]);
const valueColumns = computed(() => [
  { title: "标签", key: "label" },
  { title: "值", key: "value" },
  { title: "默认", key: "isDefault", render: (row: DictValueDto) => row.isDefault ? "是" : "否" },
  { title: "状态", key: "status" },
  { title: "操作", key: "actions", render: (row: DictValueDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:dict:value:update"], onClick: () => openValueEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:dict:value:delete"], onClick: () => void confirmDeleteValue(row) }, { default: () => "删除" })
  ] }) }
]);

onMounted(loadTypes);

async function loadTypes(): Promise<void> {
  typeLoading.value = true;
  try {
    const result = await getDictTypesApi({ page: typePage.value, pageSize: typePageSize.value });
    types.value = result.data.records;
    typeTotal.value = result.data.total;
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    typeLoading.value = false;
  }
}

async function selectType(row: DictTypeSummaryDto): Promise<void> {
  activeType.value = row;
  valueLoading.value = true;
  try {
    values.value = (await getDictValuesApi(row.code)).data;
  } catch (error) {
    values.value = [];
    message.error(apiErrorMessage(error));
  } finally {
    valueLoading.value = false;
  }
}

function openTypeCreate(): void {
  Object.assign(typeForm, { id: undefined, code: "", name: "", description: "", sortOrder: 0, status: "enabled", isSystem: false });
  typeVisible.value = true;
}

function openTypeEdit(row: DictTypeSummaryDto): void {
  Object.assign(typeForm, row);
  typeVisible.value = true;
}

async function submitType(): Promise<void> {
  try {
    await typeFormRef.value?.validate();
  } catch {
    return;
  }

  submitting.value = true;
  try {
    if (typeForm.id) {
      await updateDictTypeApi(typeForm.id, { name: typeForm.name, description: typeForm.description || null, sortOrder: typeForm.sortOrder, status: typeForm.status });
    } else {
      await createDictTypeApi({ code: typeForm.code, name: typeForm.name, description: typeForm.description || null, sortOrder: typeForm.sortOrder, status: typeForm.status });
    }
    message.success("字典类型已保存。");
    typeVisible.value = false;
    await loadTypes();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function confirmDeleteType(row: DictTypeSummaryDto): Promise<void> {
  if (!window.confirm(`确认删除字典类型 ${row.name}？`)) {
    return;
  }

  submitting.value = true;
  try {
    await deleteDictTypeApi(row.id);
    message.success("字典类型已删除。");
    if (activeType.value?.id === row.id) {
      activeType.value = null;
      values.value = [];
    }
    await loadTypes();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

function openValueCreate(): void {
  Object.assign(valueForm, { id: undefined, label: "", value: "", description: "", sortOrder: 0, isDefault: false, status: "enabled" });
  valueVisible.value = true;
}

function openValueEdit(row: DictValueDto): void {
  Object.assign(valueForm, row);
  valueVisible.value = true;
}

async function submitValue(): Promise<void> {
  if (!activeType.value) {
    message.error("请先选择字典类型。");
    return;
  }

  try {
    await valueFormRef.value?.validate();
  } catch {
    return;
  }

  const request = { label: valueForm.label, value: valueForm.value, description: valueForm.description || null, sortOrder: valueForm.sortOrder, isDefault: valueForm.isDefault, status: valueForm.status };
  submitting.value = true;
  try {
    if (valueForm.id) {
      await updateDictValueApi(valueForm.id, request);
    } else {
      await createDictValueApi(activeType.value.code, request);
    }
    message.success("字典值已保存。");
    valueVisible.value = false;
    await selectType(activeType.value);
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function confirmDeleteValue(row: DictValueDto): Promise<void> {
  if (!window.confirm(`确认删除字典值 ${row.label}？`)) {
    return;
  }

  submitting.value = true;
  try {
    await deleteDictValueApi(row.id);
    message.success("字典值已删除。");
    if (activeType.value) {
      await selectType(activeType.value);
    }
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <main class="grid gap-4 lg:grid-cols-[1fr_1fr]">
    <NCard title="字典类型">
      <NSpace class="mb-4">
        <PermissionButton type="primary" :permissions="['sys:dict:type:create']" @click="openTypeCreate">新建类型</PermissionButton>
      </NSpace>
      <NDataTable
        :columns="typeColumns"
        :data="types"
        :loading="typeLoading"
        :pagination="{ page: typePage, pageSize: typePageSize, itemCount: typeTotal, onUpdatePage: (nextPage: number) => { typePage = nextPage; loadTypes(); } }"
      >
        <template #empty>暂无字典类型</template>
      </NDataTable>
    </NCard>
    <NCard :title="activeType ? `字典值：${activeType.name}` : '字典值'">
      <NSpace class="mb-4">
        <PermissionButton type="primary" :disabled="!activeType" :permissions="['sys:dict:value:create']" @click="openValueCreate">新建值</PermissionButton>
      </NSpace>
      <NDataTable :columns="valueColumns" :data="values" :loading="valueLoading">
        <template #empty>{{ activeType ? "暂无字典值" : "请先选择字典类型" }}</template>
      </NDataTable>
    </NCard>
    <NModal v-model:show="typeVisible" preset="card" title="字典类型表单" class="max-w-lg">
      <NForm ref="typeFormRef" :model="typeForm" :rules="typeRules">
        <NFormItem v-if="!typeForm.id" path="code" label="编码"><NInput v-model:value="typeForm.code" /></NFormItem>
        <NFormItem path="name" label="名称"><NInput v-model:value="typeForm.name" /></NFormItem>
        <NFormItem label="描述"><NInput v-model:value="typeForm.description" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="typeForm.sortOrder" /></NFormItem>
        <NFormItem path="status" label="状态"><NSelect v-model:value="typeForm.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end">
          <NButton :disabled="submitting" @click="typeVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitting" @click="submitType">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>
    <NModal v-model:show="valueVisible" preset="card" title="字典值表单" class="max-w-lg">
      <NForm ref="valueFormRef" :model="valueForm" :rules="valueRules">
        <NFormItem path="label" label="标签"><NInput v-model:value="valueForm.label" /></NFormItem>
        <NFormItem path="value" label="值"><NInput v-model:value="valueForm.value" /></NFormItem>
        <NFormItem label="描述"><NInput v-model:value="valueForm.description" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="valueForm.sortOrder" /></NFormItem>
        <NFormItem label="默认"><NSwitch v-model:value="valueForm.isDefault" /></NFormItem>
        <NFormItem path="status" label="状态"><NSelect v-model:value="valueForm.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end">
          <NButton :disabled="submitting" @click="valueVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitting" @click="submitValue">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </main>
</template>
