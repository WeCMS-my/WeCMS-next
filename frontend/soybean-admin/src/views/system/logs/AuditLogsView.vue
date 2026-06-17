<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NDescriptions, NDescriptionsItem, NInput, NModal, NSpace } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { getAuditLogApi, getAuditLogsApi } from "@/api/system/logs";
import type { AuditLogSummaryDto } from "@/api/types/generated";

const rows = ref<AuditLogSummaryDto[]>([]);
const detail = ref<AuditLogSummaryDto | null>(null);
const loading = ref(false);
const detailVisible = ref(false);
const query = reactive({ page: 1, pageSize: 20, user: "", module: "", resource: "", action: "", result: "" });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });
const columns = computed(() => [
  { title: "用户", key: "username" },
  { title: "模块", key: "module" },
  { title: "资源", key: "resource" },
  { title: "动作", key: "action" },
  { title: "结果", key: "result" },
  { title: "时间", key: "createdAt" },
  { title: "操作", key: "actions", render: (row: AuditLogSummaryDto) => h(PermissionButton, { secondary: true, permissions: ["sys:audit-log:detail"], onClick: () => void openDetail(row.id) }, { default: () => "详情" }) }
]);

onMounted(load);
async function load(): Promise<void> {
  loading.value = true;
  try {
    const result = await getAuditLogsApi(query);
    rows.value = result.data.records;
    Object.assign(pagination, { page: result.data.page, pageSize: result.data.pageSize, itemCount: result.data.total });
  } finally {
    loading.value = false;
  }
}
async function search(): Promise<void> { query.page = 1; pagination.page = 1; await load(); }
async function openDetail(id: number): Promise<void> { detail.value = (await getAuditLogApi(id)).data; detailVisible.value = true; }
</script>

<template>
  <main>
    <NCard title="操作审计日志">
      <NSpace class="mb-4" align="center">
        <NInput v-model:value="query.user" clearable placeholder="用户" />
        <NInput v-model:value="query.module" clearable placeholder="模块" />
        <NInput v-model:value="query.resource" clearable placeholder="资源" />
        <NInput v-model:value="query.action" clearable placeholder="动作" />
        <NButton type="primary" @click="search">查询</NButton>
      </NSpace>
      <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="pagination" remote @update:page="(page: number) => { query.page = page; pagination.page = page; void load(); }" />
    </NCard>
    <NModal v-model:show="detailVisible" preset="card" title="操作审计详情" class="max-w-2xl">
      <NDescriptions v-if="detail" label-placement="left" bordered :column="1">
        <NDescriptionsItem label="用户">{{ detail.username ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="模块">{{ detail.module }}</NDescriptionsItem>
        <NDescriptionsItem label="资源">{{ detail.resource }}</NDescriptionsItem>
        <NDescriptionsItem label="动作">{{ detail.action }}</NDescriptionsItem>
        <NDescriptionsItem label="目标">{{ detail.targetId ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="结果">{{ detail.result }}</NDescriptionsItem>
        <NDescriptionsItem label="请求">{{ detail.requestMethod ?? "-" }} {{ detail.requestPath ?? "" }}</NDescriptionsItem>
        <NDescriptionsItem label="IP">{{ detail.ipAddress ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="TraceId">{{ detail.traceId ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="详情">{{ detail.detail ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="时间">{{ detail.createdAt }}</NDescriptionsItem>
      </NDescriptions>
    </NModal>
  </main>
</template>
