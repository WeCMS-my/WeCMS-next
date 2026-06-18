<script setup lang="ts">
import { computed, h, onBeforeUnmount, onMounted, reactive, ref, watch } from "vue";
import { NButton, NCard, NDataTable, NDescriptions, NDescriptionsItem, NInput, NModal, NSelect, NSpace, NText, useMessage } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { deleteFileApi, downloadFileApi, getFileApi, getFilesApi, previewFileApi, uploadFileApi } from "@/api/system/files";
import type { FileSummaryDto } from "@/api/types/generated";
import { hasPermission } from "@/utils/permission";

type UploadPolicy = "image" | "document";

const uploadPolicies: Record<UploadPolicy, { label: string; maxSize: number; extensions: Set<string>; mimeTypes: Set<string>; accept: string; allowPreview: boolean }> = {
  image: {
    label: "图片",
    maxSize: 10 * 1024 * 1024,
    extensions: new Set(["jpg", "jpeg", "png", "webp"]),
    mimeTypes: new Set(["image/jpeg", "image/png", "image/webp"]),
    accept: ".jpg,.jpeg,.png,.webp",
    allowPreview: true
  },
  document: {
    label: "文档",
    maxSize: 10 * 1024 * 1024,
    extensions: new Set(["pdf", "txt"]),
    mimeTypes: new Set(["application/pdf", "text/plain"]),
    accept: ".pdf,.txt",
    allowPreview: false
  }
};

const message = useMessage();
const rows = ref<FileSummaryDto[]>([]);
const detail = ref<FileSummaryDto | null>(null);
const loading = ref(false);
const uploading = ref(false);
const detailVisible = ref(false);
const previewVisible = ref(false);
const previewUrl = ref("");
const previewMimeType = ref("");
const previewText = ref("");
const selectedFile = ref<File | null>(null);
const selectedSha256 = ref("");
const selectedPolicy = ref<UploadPolicy>("document");
const canUpload = computed(() => hasPermission("sys:file:upload"));
const currentPolicy = computed(() => uploadPolicies[selectedPolicy.value]);
const query = reactive({ page: 1, pageSize: 20, keyword: "", mimeType: "", status: "" });
const pagination = reactive({ page: 1, pageSize: 20, itemCount: 0 });
const mimeOptions = [
  { label: "图片 JPEG", value: "image/jpeg" },
  { label: "图片 PNG", value: "image/png" },
  { label: "图片 WebP", value: "image/webp" },
  { label: "PDF", value: "application/pdf" },
  { label: "文本", value: "text/plain" }
];
const statusOptions = [
  { label: "active", value: "active" },
  { label: "deleted", value: "deleted" }
];
const policyOptions = [
  { label: "文档", value: "document" },
  { label: "图片", value: "image" }
];
const columns = computed(() => [
  { title: "文件名", key: "originalName" },
  { title: "类型", key: "mimeType" },
  { title: "大小", key: "sizeBytes", render: (row: FileSummaryDto) => formatBytes(row.sizeBytes) },
  { title: "状态", key: "status" },
  { title: "创建时间", key: "createdAt" },
  { title: "操作", key: "actions", render: (row: FileSummaryDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:file:detail"], onClick: () => void openDetail(row.id) }, { default: () => "详情" }),
    isPreviewable(row) ? h(PermissionButton, { secondary: true, permissions: ["sys:file:download"], onClick: () => void preview(row) }, { default: () => "预览" }) : null,
    h(PermissionButton, { secondary: true, permissions: ["sys:file:download"], onClick: () => void download(row) }, { default: () => "下载" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:file:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" })
  ] }) }
]);

onMounted(load);
onBeforeUnmount(clearPreview);
watch(selectedPolicy, () => {
  selectedFile.value = null;
  selectedSha256.value = "";
});

async function load(): Promise<void> {
  loading.value = true;
  try {
    const result = await getFilesApi(query);
    rows.value = result.data.records;
    Object.assign(pagination, { page: result.data.page, pageSize: result.data.pageSize, itemCount: result.data.total });
  } finally {
    loading.value = false;
  }
}

async function search(): Promise<void> {
  query.page = 1;
  pagination.page = 1;
  await load();
}

async function handleFileChange(event: Event): Promise<void> {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0] ?? null;
  selectedFile.value = null;
  selectedSha256.value = "";
  if (!file) {
    return;
  }
  const validation = validateFile(file);
  if (validation) {
    input.value = "";
    message.error(validation);
    return;
  }
  selectedFile.value = file;
  selectedSha256.value = await sha256(file);
}

async function upload(): Promise<void> {
  if (!selectedFile.value || !selectedSha256.value) {
    message.error("请选择可上传文件。");
    return;
  }
  uploading.value = true;
  try {
    await uploadFileApi({ file: selectedFile.value, sha256: selectedSha256.value, policy: selectedPolicy.value });
    message.success("文件已上传。");
    selectedFile.value = null;
    selectedSha256.value = "";
    await load();
  } finally {
    uploading.value = false;
  }
}

async function openDetail(id: number): Promise<void> {
  detail.value = (await getFileApi(id)).data;
  detailVisible.value = true;
}

async function preview(row: FileSummaryDto): Promise<void> {
  clearPreview();
  const blob = await previewFileApi(row.id);
  previewMimeType.value = row.mimeType;
  if (row.mimeType === "text/plain") {
    previewText.value = await blob.text();
  } else {
    previewUrl.value = URL.createObjectURL(blob);
  }
  previewVisible.value = true;
}

async function download(row: FileSummaryDto): Promise<void> {
  const blob = await downloadFileApi(row.id);
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = row.originalName;
  link.click();
  URL.revokeObjectURL(url);
}

async function confirmDelete(row: FileSummaryDto): Promise<void> {
  if (!window.confirm(`确认删除文件 ${row.originalName}？`)) {
    return;
  }
  await deleteFileApi(row.id);
  message.success("文件已删除。");
  await load();
}

function validateFile(file: File): string | null {
  const policy = currentPolicy.value;
  const extension = file.name.split(".").pop()?.toLowerCase() ?? "";
  if (!policy.extensions.has(extension)) {
    return `当前策略仅允许上传 ${Array.from(policy.extensions).join("/")} 文件。`;
  }
  if (!policy.mimeTypes.has(file.type)) {
    return "文件 MIME 类型不允许。";
  }
  if (file.size <= 0 || file.size > policy.maxSize) {
    return `文件大小必须大于 0 且不超过 ${formatBytes(policy.maxSize)}。`;
  }
  return null;
}

function isPreviewable(row: FileSummaryDto): boolean {
  return row.mimeType.startsWith("image/");
}

async function sha256(file: File): Promise<string> {
  const buffer = await file.arrayBuffer();
  const hash = await crypto.subtle.digest("SHA-256", buffer);
  return Array.from(new Uint8Array(hash)).map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

function clearPreview(): void {
  if (previewUrl.value) {
    URL.revokeObjectURL(previewUrl.value);
  }
  previewUrl.value = "";
  previewText.value = "";
  previewMimeType.value = "";
}

function formatBytes(value: number): string {
  if (value < 1024) return `${value} B`;
  if (value < 1024 * 1024) return `${(value / 1024).toFixed(1)} KB`;
  return `${(value / 1024 / 1024).toFixed(1)} MB`;
}
</script>

<template>
  <main>
    <NCard title="文件管理">
      <NSpace class="mb-4" align="center">
        <NInput v-model:value="query.keyword" clearable placeholder="关键词" />
        <NSelect v-model:value="query.mimeType" clearable placeholder="MIME 类型" :options="mimeOptions" class="w-48" />
        <NSelect v-model:value="query.status" clearable placeholder="状态" :options="statusOptions" class="w-36" />
        <NButton type="primary" @click="search">查询</NButton>
      </NSpace>
      <div v-if="canUpload" class="mb-4">
        <NSelect v-model:value="selectedPolicy" :options="policyOptions" class="mr-3 inline-block w-32" />
        <PermissionButton type="primary" :loading="uploading" :permissions="['sys:file:upload']" @click="upload">
          上传
        </PermissionButton>
        <input class="ml-3" type="file" :accept="currentPolicy.accept" @change="handleFileChange">
        <NText v-if="selectedFile" depth="3">{{ selectedFile.name }} / {{ formatBytes(selectedFile.size) }} / {{ selectedSha256 }}</NText>
      </div>
      <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="pagination" remote @update:page="(page: number) => { query.page = page; pagination.page = page; void load(); }" />
    </NCard>
    <NModal v-model:show="detailVisible" preset="card" title="文件详情" class="max-w-2xl">
      <NDescriptions v-if="detail" label-placement="left" bordered :column="1">
        <NDescriptionsItem label="文件名">{{ detail.originalName }}</NDescriptionsItem>
        <NDescriptionsItem label="扩展名">{{ detail.fileExt }}</NDescriptionsItem>
        <NDescriptionsItem label="MIME">{{ detail.mimeType }}</NDescriptionsItem>
        <NDescriptionsItem label="大小">{{ formatBytes(detail.sizeBytes) }}</NDescriptionsItem>
        <NDescriptionsItem label="SHA-256">{{ detail.sha256 }}</NDescriptionsItem>
        <NDescriptionsItem label="状态">{{ detail.status }}</NDescriptionsItem>
        <NDescriptionsItem label="创建人">{{ detail.createdBy }}</NDescriptionsItem>
        <NDescriptionsItem label="创建时间">{{ detail.createdAt }}</NDescriptionsItem>
      </NDescriptions>
    </NModal>
    <NModal v-model:show="previewVisible" preset="card" title="文件预览" class="max-w-4xl" @after-leave="clearPreview">
      <img v-if="previewUrl && previewMimeType.startsWith('image/')" class="max-h-[70vh] max-w-full" :src="previewUrl" alt="文件预览">
      <iframe v-else-if="previewUrl && previewMimeType === 'application/pdf'" class="h-[70vh] w-full border-0" :src="previewUrl" title="PDF 预览" />
      <pre v-else-if="previewText" class="max-h-[70vh] overflow-auto whitespace-pre-wrap rounded bg-gray-100 p-4">{{ previewText }}</pre>
      <NText v-else depth="3">当前文件类型不支持内嵌预览，请下载查看。</NText>
    </NModal>
  </main>
</template>
