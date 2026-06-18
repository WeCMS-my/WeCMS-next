<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NModal, NSelect, NSpace, NTag, useMessage } from "naive-ui";
import type { FormInst, FormRules } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import {
  createI18nMessageApi,
  deleteI18nMessageApi,
  getI18nMessageApi,
  getI18nMessagesApi,
  updateI18nMessageApi
} from "@/api/system/i18n";
import type { I18nMessageSummaryDto } from "@/api/types/generated";
import { apiErrorMessage } from "@/utils/api-error";

const message = useMessage();
const rows = ref<I18nMessageSummaryDto[]>([]);
const loading = ref(false);
const submitting = ref(false);
const formVisible = ref(false);
const formRef = ref<FormInst | null>(null);
const query = reactive({ locale: "", module: "", keyword: "", status: "", page: 1, pageSize: 20 });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });
const form = reactive({
  id: undefined as number | undefined,
  locale: "zh-CN",
  module: "system",
  messageKey: "",
  messageValue: "",
  remark: "",
  status: "enabled"
});

const localeOptions = [
  { label: "zh-CN", value: "zh-CN" },
  { label: "en-US", value: "en-US" },
  { label: "ms-MY", value: "ms-MY" }
];
const statusOptions = [
  { label: "全部状态", value: "" },
  { label: "启用", value: "enabled" },
  { label: "禁用", value: "disabled" }
];
const formStatusOptions = statusOptions.filter(option => option.value);
const messageKeyPattern = /^[a-z0-9][a-z0-9._-]*$/;
const rules: FormRules = {
  locale: [{ required: true, message: "请选择语言", trigger: ["change", "blur"] }],
  module: [{ required: true, message: "请输入模块", trigger: ["input", "blur"] }],
  messageKey: [
    { required: true, message: "请输入文案 Key", trigger: ["input", "blur"] },
    {
      trigger: ["input", "blur"],
      validator: (_rule, value: string) => messageKeyPattern.test(value) || new Error("仅允许小写字母、数字、点、下划线和连字符。")
    }
  ],
  messageValue: [{ required: true, message: "请输入文案内容", trigger: ["input", "blur"] }],
  status: [{ required: true, message: "请选择状态", trigger: ["change", "blur"] }]
};

const columns = computed(() => [
  { title: "语言", key: "locale", width: 100 },
  { title: "模块", key: "module", width: 120 },
  { title: "Key", key: "messageKey", minWidth: 220 },
  { title: "内容", key: "messageValue", ellipsis: { tooltip: true } },
  { title: "状态", key: "status", width: 100, render: (row: I18nMessageSummaryDto) => h(NTag, { type: row.status === "enabled" ? "success" : "default" }, { default: () => row.status === "enabled" ? "启用" : "禁用" }) },
  { title: "更新时间", key: "updatedAt", width: 190 },
  { title: "操作", key: "actions", width: 170, render: (row: I18nMessageSummaryDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:i18n:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:i18n:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" })
  ] }) }
]);

onMounted(loadMessages);

async function loadMessages(): Promise<void> {
  loading.value = true;
  try {
    const result = await getI18nMessagesApi(query);
    rows.value = result.data.records;
    pagination.page = result.data.page;
    pagination.pageSize = result.data.pageSize;
    pagination.itemCount = result.data.total;
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    loading.value = false;
  }
}

async function search(): Promise<void> {
  query.page = 1;
  pagination.page = 1;
  await loadMessages();
}

function openCreate(): void {
  Object.assign(form, {
    id: undefined,
    locale: "zh-CN",
    module: "system",
    messageKey: "",
    messageValue: "",
    remark: "",
    status: "enabled"
  });
  formVisible.value = true;
}

async function openEdit(row: I18nMessageSummaryDto): Promise<void> {
  const detail = (await getI18nMessageApi(row.id)).data;
  Object.assign(form, {
    id: detail.id,
    locale: detail.locale,
    module: detail.module,
    messageKey: detail.messageKey,
    messageValue: detail.messageValue,
    remark: detail.remark ?? "",
    status: detail.status
  });
  formVisible.value = true;
}

async function submit(): Promise<void> {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }

  submitting.value = true;
  try {
    if (form.id) {
      await updateI18nMessageApi(form.id, {
        module: form.module,
        messageValue: form.messageValue,
        remark: form.remark || null,
        status: form.status
      });
    } else {
      await createI18nMessageApi({
        locale: form.locale,
        module: form.module,
        messageKey: form.messageKey,
        messageValue: form.messageValue,
        remark: form.remark || null,
        status: form.status
      });
    }
    message.success("多语言文案已保存。");
    formVisible.value = false;
    await loadMessages();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function confirmDelete(row: I18nMessageSummaryDto): Promise<void> {
  if (!window.confirm(`确认删除文案 ${row.messageKey}？`)) {
    return;
  }

  submitting.value = true;
  try {
    await deleteI18nMessageApi(row.id);
    message.success("多语言文案已删除。");
    await loadMessages();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <main>
    <NCard title="多语言文案">
      <NSpace class="mb-4" align="center">
        <NSelect v-model:value="query.locale" clearable placeholder="语言" :options="localeOptions" class="w-32" />
        <NInput v-model:value="query.module" clearable placeholder="模块" />
        <NInput v-model:value="query.keyword" clearable placeholder="Key 或内容" />
        <NSelect v-model:value="query.status" placeholder="状态" :options="statusOptions" class="w-32" />
        <NButton type="primary" @click="search">查询</NButton>
        <PermissionButton type="primary" :permissions="['sys:i18n:create']" @click="openCreate">新建文案</PermissionButton>
      </NSpace>
      <NDataTable
        :columns="columns"
        :data="rows"
        :loading="loading"
        :pagination="pagination"
        remote
        @update:page="(page: number) => { query.page = page; pagination.page = page; void loadMessages(); }"
      >
        <template #empty>暂无多语言文案</template>
      </NDataTable>
    </NCard>

    <NModal v-model:show="formVisible" preset="card" title="多语言文案表单" class="max-w-2xl">
      <NForm ref="formRef" :model="form" :rules="rules">
        <NFormItem path="locale" label="语言">
          <NSelect v-model:value="form.locale" :disabled="Boolean(form.id)" :options="localeOptions" />
        </NFormItem>
        <NFormItem path="module" label="模块"><NInput v-model:value="form.module" /></NFormItem>
        <NFormItem path="messageKey" label="文案 Key"><NInput v-model:value="form.messageKey" :readonly="Boolean(form.id)" /></NFormItem>
        <NFormItem path="messageValue" label="文案内容"><NInput v-model:value="form.messageValue" type="textarea" /></NFormItem>
        <NFormItem label="备注"><NInput v-model:value="form.remark" type="textarea" /></NFormItem>
        <NFormItem path="status" label="状态"><NSelect v-model:value="form.status" :options="formStatusOptions" /></NFormItem>
        <NSpace justify="end">
          <NButton :disabled="submitting" @click="formVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitting" @click="submit">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </main>
</template>

