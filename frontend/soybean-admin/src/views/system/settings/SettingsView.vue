<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NModal, NSpace, NTag, useMessage } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { getSettingApi, getSettingsApi, reloadSettingCacheApi, updateSettingApi, validateIpRulesApi } from "@/api/system/settings";
import type { SettingSummaryDto } from "@/api/types/generated";
import { apiErrorMessage } from "@/utils/api-error";

const message = useMessage();
const rows = ref<SettingSummaryDto[]>([]);
const loading = ref(false);
const submitting = ref(false);
const editVisible = ref(false);
const query = reactive({ keyword: "", groupCode: "", page: 1, pageSize: 20 });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });
const form = reactive({ key: "", name: "", value: "", isSensitive: false });

const columns = computed(() => [
  { title: "键", key: "key" },
  { title: "名称", key: "name" },
  { title: "分组", key: "groupCode" },
  { title: "类型", key: "valueType" },
  { title: "值", key: "value", render: (row: SettingSummaryDto) => renderValue(row) },
  { title: "敏感", key: "isSensitive", render: (row: SettingSummaryDto) => h(NTag, { type: row.isSensitive ? "warning" : "default" }, { default: () => row.isSensitive ? "是" : "否" }) },
  { title: "更新时间", key: "updatedAt" },
  { title: "操作", key: "actions", render: (row: SettingSummaryDto) => h(PermissionButton, { secondary: true, permissions: ["sys:setting:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }) }
]);

onMounted(loadSettings);

async function loadSettings(): Promise<void> {
  loading.value = true;
  try {
    const result = await getSettingsApi(query);
    rows.value = result.data.records;
    pagination.page = result.data.page;
    pagination.pageSize = result.data.pageSize;
    pagination.itemCount = result.data.total;
  } finally {
    loading.value = false;
  }
}

async function search(): Promise<void> {
  query.page = 1;
  pagination.page = 1;
  await loadSettings();
}

async function openEdit(row: SettingSummaryDto): Promise<void> {
  const detail = (await getSettingApi(row.key)).data;
  Object.assign(form, {
    key: detail.key,
    name: detail.name,
    value: detail.isSensitive ? "" : detail.value ?? "",
    isSensitive: detail.isSensitive
  });
  editVisible.value = true;
}

async function submit(): Promise<void> {
  if (form.isSensitive && !form.value.trim()) {
    message.error("敏感配置更新必须输入新值。");
    return;
  }
  submitting.value = true;
  try {
    if (isIpRuleSetting(form.key)) {
      await validateIpRulesApi({ rules: form.value });
    }

    await updateSettingApi(form.key, { value: form.value });
    message.success("系统设置已更新。");
    editVisible.value = false;
    await loadSettings();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function reloadCache(): Promise<void> {
  submitting.value = true;
  try {
    await reloadSettingCacheApi();
    message.success("设置缓存已刷新。");
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

function renderValue(row: SettingSummaryDto): string {
  if (row.isSensitive) {
    return "******";
  }
  return row.value ?? "";
}

function isIpRuleSetting(key: string): boolean {
  return key === "security.ipAllowRules" || key === "security.ipDenyRules";
}
</script>

<template>
  <main>
    <NCard title="系统设置">
      <NSpace class="mb-4" align="center">
        <NInput v-model:value="query.keyword" clearable placeholder="关键词" />
        <NInput v-model:value="query.groupCode" clearable placeholder="分组编码" />
        <NButton type="primary" @click="search">查询</NButton>
        <PermissionButton :loading="submitting" :permissions="['sys:setting:reload-cache']" @click="reloadCache">刷新缓存</PermissionButton>
      </NSpace>
      <NDataTable
        :columns="columns"
        :data="rows"
        :loading="loading"
        :pagination="pagination"
        remote
        @update:page="(page: number) => { query.page = page; pagination.page = page; void loadSettings(); }"
      />
    </NCard>
    <NModal v-model:show="editVisible" preset="card" title="编辑系统设置" class="max-w-lg">
      <NForm>
        <NFormItem label="配置键"><NInput :value="form.key" readonly /></NFormItem>
        <NFormItem label="名称"><NInput :value="form.name" readonly /></NFormItem>
        <NFormItem label="配置值">
          <NInput
            v-model:value="form.value"
            :type="form.isSensitive ? 'password' : 'text'"
            :placeholder="form.isSensitive ? '请输入新的敏感配置值' : '请输入配置值'"
          />
        </NFormItem>
        <NSpace justify="end">
          <NButton :disabled="submitting" @click="editVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitting" @click="submit">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </main>
</template>
