<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import {
  NButton,
  NCard,
  NDataTable,
  NDescriptions,
  NDescriptionsItem,
  NGi,
  NGrid,
  NInput,
  NModal,
  NSelect,
  NSpace,
  NStatistic,
  NSwitch,
  NTag,
  useMessage,
  type DataTableColumns,
  type DataTableRowKey
} from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import {
  batchUnbanSecurityBansApi,
  getSecurityBanApi,
  getSecurityBansApi,
  getSecurityStatusApi,
  unbanSecurityBanApi
} from "@/api/system/security";
import type { SecurityBanDetailDto, SecurityBanSummaryDto, SecurityStatusDto } from "@/api/types/generated";

const message = useMessage();
const status = ref<SecurityStatusDto | null>(null);
const rows = ref<SecurityBanSummaryDto[]>([]);
const detail = ref<SecurityBanDetailDto | null>(null);
const checkedRowKeys = ref<DataTableRowKey[]>([]);
const loading = ref(false);
const saving = ref(false);
const detailVisible = ref(false);
const unbanVisible = ref(false);
const unbanMode = ref<"single" | "batch">("single");
const selectedBanId = ref<number | null>(null);
const unbanReason = ref("");
const query = reactive({ page: 1, pageSize: 20, banType: "", target: "", severity: "", source: "", activeOnly: true });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });

const banTypeOptions = [
  { label: "IP", value: "ip" },
  { label: "用户", value: "user" }
];
const severityOptions = [
  { label: "Warning", value: "warning" },
  { label: "Critical", value: "critical" }
];

const selectedIds = computed(() => checkedRowKeys.value.map(Number).filter((id) => Number.isFinite(id)));
const columns = computed<DataTableColumns<SecurityBanSummaryDto>>(() => [
  { type: "selection" },
  { title: "类型", key: "banType", render: (row) => typeLabel(row.banType) },
  { title: "目标", key: "target" },
  { title: "级别", key: "severity", render: (row) => h(NTag, { type: row.severity === "critical" ? "error" : "warning", bordered: false }, { default: () => row.severity }) },
  { title: "来源", key: "source" },
  { title: "原因", key: "reason" },
  { title: "过期时间", key: "expiresAt", render: (row) => row.expiresAt ?? "-" },
  { title: "状态", key: "status", render: (row) => row.revokedAt ? h(NTag, { bordered: false }, { default: () => "已解封" }) : h(NTag, { type: "success", bordered: false }, { default: () => "生效中" }) },
  { title: "创建时间", key: "createdAt" },
  {
    title: "操作",
    key: "actions",
    render: (row) => h(NSpace, null, {
      default: () => [
        h(PermissionButton, { secondary: true, permissions: ["sys:security:ban:detail"], onClick: () => void openDetail(row.id) }, { default: () => "详情" }),
        h(PermissionButton, { type: "warning", secondary: true, disabled: Boolean(row.revokedAt), permissions: ["sys:security:ban:unban"], onClick: () => openUnban(row.id) }, { default: () => "解封" })
      ]
    })
  }
]);

onMounted(() => {
  void loadAll();
});

async function loadAll(): Promise<void> {
  await Promise.all([loadStatus(), loadBans()]);
}

async function loadStatus(): Promise<void> {
  status.value = (await getSecurityStatusApi()).data;
}

async function loadBans(): Promise<void> {
  loading.value = true;
  try {
    const result = await getSecurityBansApi({
      page: query.page,
      pageSize: query.pageSize,
      banType: query.banType || undefined,
      target: query.target || undefined,
      severity: query.severity || undefined,
      source: query.source || undefined,
      activeOnly: query.activeOnly
    });
    rows.value = result.data.records;
    checkedRowKeys.value = [];
    Object.assign(pagination, { page: result.data.page, pageSize: result.data.pageSize, itemCount: result.data.total });
  } finally {
    loading.value = false;
  }
}

async function search(): Promise<void> {
  query.page = 1;
  pagination.page = 1;
  await loadBans();
}

async function openDetail(id: number): Promise<void> {
  detail.value = (await getSecurityBanApi(id)).data;
  detailVisible.value = true;
}

function openUnban(id: number): void {
  unbanMode.value = "single";
  selectedBanId.value = id;
  unbanReason.value = "";
  unbanVisible.value = true;
}

function openBatchUnban(): void {
  unbanMode.value = "batch";
  selectedBanId.value = null;
  unbanReason.value = "";
  unbanVisible.value = true;
}

async function submitUnban(): Promise<void> {
  const reason = unbanReason.value.trim();
  if (!reason) {
    message.warning("请填写解封原因");
    return;
  }

  saving.value = true;
  try {
    if (unbanMode.value === "single" && selectedBanId.value) {
      await unbanSecurityBanApi(selectedBanId.value, { reason });
    } else {
      await batchUnbanSecurityBansApi({ ids: selectedIds.value, reason });
    }
    message.success("解封已提交");
    unbanVisible.value = false;
    await loadAll();
  } finally {
    saving.value = false;
  }
}

function typeLabel(value: string): string {
  return value === "ip" ? "IP" : "用户";
}
</script>

<template>
  <main>
    <NGrid :cols="4" :x-gap="16" :y-gap="16" responsive="screen" class="mb-4">
      <NGi>
        <NCard>
          <NStatistic label="生效封禁" :value="status?.activeBans ?? 0" />
        </NCard>
      </NGi>
      <NGi>
        <NCard>
          <NStatistic label="IP 封禁" :value="status?.activeIpBans ?? 0" />
        </NCard>
      </NGi>
      <NGi>
        <NCard>
          <NStatistic label="用户封禁" :value="status?.activeUserBans ?? 0" />
        </NCard>
      </NGi>
      <NGi>
        <NCard>
          <NStatistic label="高危封禁" :value="status?.criticalActiveBans ?? 0" />
        </NCard>
      </NGi>
    </NGrid>

    <NCard title="安全封禁">
      <NSpace class="mb-4" align="center">
        <NSelect v-model:value="query.banType" clearable placeholder="类型" :options="banTypeOptions" class="w-32" />
        <NInput v-model:value="query.target" clearable placeholder="目标" class="w-48" />
        <NSelect v-model:value="query.severity" clearable placeholder="级别" :options="severityOptions" class="w-36" />
        <NInput v-model:value="query.source" clearable placeholder="来源" class="w-40" />
        <NSwitch v-model:value="query.activeOnly" />
        <NButton type="primary" @click="search">查询</NButton>
        <PermissionButton
          type="warning"
          secondary
          :disabled="selectedIds.length === 0"
          :permissions="['sys:security:ban:batch-unban']"
          @click="openBatchUnban"
        >
          批量解封
        </PermissionButton>
      </NSpace>
      <NDataTable
        v-model:checked-row-keys="checkedRowKeys"
        :columns="columns"
        :data="rows"
        :loading="loading"
        :pagination="pagination"
        :row-key="(row: SecurityBanSummaryDto) => row.id"
        remote
        @update:page="(page: number) => { query.page = page; pagination.page = page; void loadBans(); }"
      />
    </NCard>

    <NModal v-model:show="detailVisible" preset="card" title="封禁详情" class="max-w-2xl">
      <NDescriptions v-if="detail" label-placement="left" bordered :column="1">
        <NDescriptionsItem label="类型">{{ typeLabel(detail.banType) }}</NDescriptionsItem>
        <NDescriptionsItem label="目标">{{ detail.target }}</NDescriptionsItem>
        <NDescriptionsItem label="级别">{{ detail.severity }}</NDescriptionsItem>
        <NDescriptionsItem label="来源">{{ detail.source }}</NDescriptionsItem>
        <NDescriptionsItem label="原因">{{ detail.reason }}</NDescriptionsItem>
        <NDescriptionsItem label="过期时间">{{ detail.expiresAt ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="解封人">{{ detail.revokedBy ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="解封时间">{{ detail.revokedAt ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="解封原因">{{ detail.revokeReason ?? "-" }}</NDescriptionsItem>
        <NDescriptionsItem label="创建时间">{{ detail.createdAt }}</NDescriptionsItem>
        <NDescriptionsItem label="更新时间">{{ detail.updatedAt }}</NDescriptionsItem>
      </NDescriptions>
    </NModal>

    <NModal
      v-model:show="unbanVisible"
      preset="dialog"
      :title="unbanMode === 'single' ? '解封安全封禁' : '批量解封安全封禁'"
      positive-text="确认"
      negative-text="取消"
      :loading="saving"
      @positive-click="submitUnban"
    >
      <NSpace vertical>
        <span>{{ unbanMode === "single" ? `封禁 ID：${selectedBanId}` : `已选择 ${selectedIds.length} 条封禁` }}</span>
        <NInput v-model:value="unbanReason" type="textarea" placeholder="解封原因" />
      </NSpace>
    </NModal>
  </main>
</template>
