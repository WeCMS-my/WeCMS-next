<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NDescriptions, NDescriptionsItem, NInput, NModal, NSpace } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { getLoginLogApi, getLoginLogsApi } from "@/api/system/logs";
import type { LoginLogDetailDto, LoginLogSummaryDto } from "@/api/types/generated";

const rows = ref<LoginLogSummaryDto[]>([]);
const detail = ref<LoginLogDetailDto | null>(null);
const loading = ref(false);
const detailVisible = ref(false);
const query = reactive({ page: 1, pageSize: 20, username: "", ip: "", result: "" });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });
const columns = computed(() => [
  { title: "用户", key: "username" },
  { title: "IP", key: "ip" },
  { title: "结果", key: "result" },
  { title: "原因", key: "reason" },
  { title: "时间", key: "createdAt" },
  { title: "操作", key: "actions", render: (row: LoginLogSummaryDto) => h(PermissionButton, { secondary: true, permissions: ["sys:login-log:detail"], onClick: () => void openDetail(row.id) }, { default: () => "详情" }) }
]);

onMounted(load);
async function load(): Promise<void> {
  loading.value = true;
  try {
    const result = await getLoginLogsApi(query);
    rows.value = result.data.records;
    Object.assign(pagination, { page: result.data.page, pageSize: result.data.pageSize, itemCount: result.data.total });
  } finally {
    loading.value = false;
  }
}
async function search(): Promise<void> { query.page = 1; pagination.page = 1; await load(); }
async function openDetail(id: number): Promise<void> { detail.value = (await getLoginLogApi(id)).data; detailVisible.value = true; }
</script>

<template>
  <main>
    <NCard title="登录日志">
      <NSpace class="mb-4" align="center">
        <NInput v-model:value="query.username" clearable placeholder="用户名" />
        <NInput v-model:value="query.ip" clearable placeholder="IP" />
        <NInput v-model:value="query.result" clearable placeholder="结果" />
        <NButton type="primary" @click="search">查询</NButton>
      </NSpace>
      <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="pagination" remote @update:page="(page: number) => { query.page = page; pagination.page = page; void load(); }" />
    </NCard>
    <NModal v-model:show="detailVisible" preset="card" title="登录日志详情" class="max-w-2xl">
      <NDescriptions v-if="detail" label-placement="left" bordered :column="1">
        <NDescriptionsItem label="用户">{{ detail.username }}</NDescriptionsItem>
        <NDescriptionsItem label="用户 ID">{{ detail.userId ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="IP">{{ detail.ip }}</NDescriptionsItem>
        <NDescriptionsItem label="结果">{{ detail.result }}</NDescriptionsItem>
        <NDescriptionsItem label="原因">{{ detail.reason ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="User Agent">{{ detail.userAgent ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="时间">{{ detail.createdAt }}</NDescriptionsItem>
      </NDescriptions>
    </NModal>
  </main>
</template>
