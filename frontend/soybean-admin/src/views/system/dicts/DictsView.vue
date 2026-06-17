<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NInputNumber, NModal, NSelect, NSpace, NSwitch, useMessage } from "naive-ui";
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

const message = useMessage();
const types = ref<DictTypeSummaryDto[]>([]);
const values = ref<DictValueDto[]>([]);
const activeType = ref<DictTypeSummaryDto | null>(null);
const typeVisible = ref(false);
const valueVisible = ref(false);
const typeForm = reactive({ id: undefined as number | undefined, code: "", name: "", description: "", sortOrder: 0, status: "enabled", isSystem: false });
const valueForm = reactive({ id: undefined as number | undefined, label: "", value: "", description: "", sortOrder: 0, isDefault: false, status: "enabled" });
const typeColumns = computed(() => [
  { title: "编码", key: "code" },
  { title: "名称", key: "name" },
  { title: "系统", key: "isSystem", render: (row: DictTypeSummaryDto) => row.isSystem ? "是" : "否" },
  { title: "状态", key: "status" },
  { title: "操作", key: "actions", render: (row: DictTypeSummaryDto) => h(NSpace, null, { default: () => [
    h(NButton, { secondary: true, onClick: () => void selectType(row) }, { default: () => "值" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:dict-type:update"], onClick: () => openTypeEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, disabled: row.isSystem, permissions: ["sys:dict-type:delete"], onClick: () => void confirmDeleteType(row) }, { default: () => "删除" })
  ] }) }
]);
const valueColumns = computed(() => [
  { title: "标签", key: "label" },
  { title: "值", key: "value" },
  { title: "默认", key: "isDefault", render: (row: DictValueDto) => row.isDefault ? "是" : "否" },
  { title: "状态", key: "status" },
  { title: "操作", key: "actions", render: (row: DictValueDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:dict-value:update"], onClick: () => openValueEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:dict-value:delete"], onClick: () => void confirmDeleteValue(row) }, { default: () => "删除" })
  ] }) }
]);

onMounted(loadTypes);
async function loadTypes(): Promise<void> { types.value = (await getDictTypesApi()).data.records; }
async function selectType(row: DictTypeSummaryDto): Promise<void> { activeType.value = row; values.value = (await getDictValuesApi(row.code)).data; }
function openTypeCreate(): void { Object.assign(typeForm, { id: undefined, code: "", name: "", description: "", sortOrder: 0, status: "enabled", isSystem: false }); typeVisible.value = true; }
function openTypeEdit(row: DictTypeSummaryDto): void { Object.assign(typeForm, row); typeVisible.value = true; }
async function submitType(): Promise<void> {
  if (!typeForm.name.trim() || (!typeForm.id && !typeForm.code.trim())) { message.error("请填写必填字段。"); return; }
  if (typeForm.id) await updateDictTypeApi(typeForm.id, { name: typeForm.name, description: typeForm.description || null, sortOrder: typeForm.sortOrder, status: typeForm.status });
  else await createDictTypeApi({ code: typeForm.code, name: typeForm.name, description: typeForm.description || null, sortOrder: typeForm.sortOrder, status: typeForm.status });
  message.success("字典类型已保存。"); typeVisible.value = false; await loadTypes();
}
async function confirmDeleteType(row: DictTypeSummaryDto): Promise<void> { if (!window.confirm(`确认删除字典类型 ${row.name}？`)) return; await deleteDictTypeApi(row.id); message.success("字典类型已删除。"); await loadTypes(); }
function openValueCreate(): void { Object.assign(valueForm, { id: undefined, label: "", value: "", description: "", sortOrder: 0, isDefault: false, status: "enabled" }); valueVisible.value = true; }
function openValueEdit(row: DictValueDto): void { Object.assign(valueForm, row); valueVisible.value = true; }
async function submitValue(): Promise<void> {
  if (!activeType.value || !valueForm.label.trim() || !valueForm.value.trim()) { message.error("请先选择类型并填写必填字段。"); return; }
  const request = { label: valueForm.label, value: valueForm.value, description: valueForm.description || null, sortOrder: valueForm.sortOrder, isDefault: valueForm.isDefault, status: valueForm.status };
  if (valueForm.id) await updateDictValueApi(valueForm.id, request);
  else await createDictValueApi(activeType.value.code, request);
  message.success("字典值已保存。"); valueVisible.value = false; await selectType(activeType.value);
}
async function confirmDeleteValue(row: DictValueDto): Promise<void> { if (!window.confirm(`确认删除字典值 ${row.label}？`)) return; await deleteDictValueApi(row.id); message.success("字典值已删除。"); if (activeType.value) await selectType(activeType.value); }
</script>

<template>
  <main class="grid gap-4 lg:grid-cols-[1fr_1fr]">
    <NCard title="字典类型">
      <NSpace class="mb-4"><PermissionButton type="primary" :permissions="['sys:dict-type:create']" @click="openTypeCreate">新建类型</PermissionButton></NSpace>
      <NDataTable :columns="typeColumns" :data="types" />
    </NCard>
    <NCard :title="activeType ? `字典值：${activeType.name}` : '字典值'">
      <NSpace class="mb-4"><PermissionButton type="primary" :permissions="['sys:dict-value:create']" @click="openValueCreate">新建值</PermissionButton></NSpace>
      <NDataTable :columns="valueColumns" :data="values" />
    </NCard>
    <NModal v-model:show="typeVisible" preset="card" title="字典类型表单" class="max-w-lg">
      <NForm>
        <NFormItem v-if="!typeForm.id" label="编码"><NInput v-model:value="typeForm.code" /></NFormItem>
        <NFormItem label="名称"><NInput v-model:value="typeForm.name" /></NFormItem>
        <NFormItem label="描述"><NInput v-model:value="typeForm.description" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="typeForm.sortOrder" /></NFormItem>
        <NFormItem label="状态"><NSelect v-model:value="typeForm.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end"><NButton @click="typeVisible = false">取消</NButton><NButton type="primary" @click="submitType">保存</NButton></NSpace>
      </NForm>
    </NModal>
    <NModal v-model:show="valueVisible" preset="card" title="字典值表单" class="max-w-lg">
      <NForm>
        <NFormItem label="标签"><NInput v-model:value="valueForm.label" /></NFormItem>
        <NFormItem label="值"><NInput v-model:value="valueForm.value" /></NFormItem>
        <NFormItem label="描述"><NInput v-model:value="valueForm.description" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="valueForm.sortOrder" /></NFormItem>
        <NFormItem label="默认"><NSwitch v-model:value="valueForm.isDefault" /></NFormItem>
        <NFormItem label="状态"><NSelect v-model:value="valueForm.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end"><NButton @click="valueVisible = false">取消</NButton><NButton type="primary" @click="submitValue">保存</NButton></NSpace>
      </NForm>
    </NModal>
  </main>
</template>
