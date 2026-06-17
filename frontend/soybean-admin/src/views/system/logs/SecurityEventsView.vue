<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NDescriptions, NDescriptionsItem, NInput, NModal, NSpace } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { getSecurityEventApi, getSecurityEventsApi } from "@/api/system/logs";
import type { SecurityEventSummaryDto } from "@/api/types/generated";

const rows = ref<SecurityEventSummaryDto[]>([]);
const detail = ref<SecurityEventSummaryDto | null>(null);
const loading = ref(false);
const detailVisible = ref(false);
const query = reactive({ page: 1, pageSize: 20, eventType: "", severity: "", user: "", ip: "" });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });
const columns = computed(() => [
  { title: "类型", key: "eventType" },
  { title: "严重级别", key: "severity" },
  { title: "用户", key: "username" },
  { title: "IP", key: "ip" },
  { title: "消息", key: "message" },
  { title: "时间", key: "createdAt" },
  { title: "操作", key: "actions", render: (row: SecurityEventSummaryDto) => h(PermissionButton, { secondary: true, permissions: ["sys:security-event:detail"], onClick: () => void openDetail(row.id) }, { default: () => "详情" }) }
]);

onMounted(load);
async function load(): Promise<void> {
  loading.value = true;
  try {
    const result = await getSecurityEventsApi(query);
    rows.value = result.data.records;
    Object.assign(pagination, { page: result.data.page, pageSize: result.data.pageSize, itemCount: result.data.total });
  } finally {
    loading.value = false;
  }
}
async function search(): Promise<void> { query.page = 1; pagination.page = 1; await load(); }
async function openDetail(id: number): Promise<void> { detail.value = (await getSecurityEventApi(id)).data; detailVisible.value = true; }
</script>

<template>
  <main>
    <NCard title="安全事件">
      <NSpace class="mb-4" align="center">
        <NInput v-model:value="query.eventType" clearable placeholder="事件类型" />
        <NInput v-model:value="query.severity" clearable placeholder="级别" />
        <NInput v-model:value="query.user" clearable placeholder="用户" />
        <NButton type="primary" @click="search">查询</NButton>
      </NSpace>
      <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="pagination" remote @update:page="(page: number) => { query.page = page; pagination.page = page; void load(); }" />
    </NCard>
    <NModal v-model:show="detailVisible" preset="card" title="安全事件详情" class="max-w-2xl">
      <NDescriptions v-if="detail" label-placement="left" bordered :column="1">
        <NDescriptionsItem label="类型">{{ detail.eventType }}</NDescriptionsItem>
        <NDescriptionsItem label="严重级别">{{ detail.severity }}</NDescriptionsItem>
        <NDescriptionsItem label="用户">{{ detail.username ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="用户 ID">{{ detail.userId ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="IP">{{ detail.ip ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="消息">{{ detail.message }}</NDescriptionsItem>
        <NDescriptionsItem label="时间">{{ detail.createdAt }}</NDescriptionsItem>
      </NDescriptions>
    </NModal>
  </main>
</template>
