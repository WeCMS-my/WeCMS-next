<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NInputNumber, NModal, NSelect, NSpace, useMessage } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { createPostApi, deletePostApi, disablePostApi, enablePostApi, getPostApi, getPostsApi, updatePostApi } from "@/api/system/posts";
import type { PostSummaryDto } from "@/api/types/generated";

const message = useMessage();
const rows = ref<PostSummaryDto[]>([]);
const formVisible = ref(false);
const form = reactive({ id: undefined as number | undefined, code: "", name: "", sortOrder: 0, status: "enabled" });
const columns = computed(() => [
  { title: "编码", key: "code" },
  { title: "名称", key: "name" },
  { title: "排序", key: "sortOrder" },
  { title: "状态", key: "status" },
  { title: "操作", key: "actions", render: (row: PostSummaryDto) => h(NSpace, null, { default: () => [
    h(PermissionButton, { secondary: true, permissions: ["sys:post:update"], onClick: () => void openEdit(row) }, { default: () => "编辑" }),
    h(PermissionButton, { secondary: true, permissions: ["sys:post:delete"], onClick: () => void confirmDelete(row) }, { default: () => "删除" }),
    h(PermissionButton, { secondary: true, permissions: [row.status === "enabled" ? "sys:post:disable" : "sys:post:enable"], onClick: () => void changeStatus(row) }, { default: () => row.status === "enabled" ? "禁用" : "启用" })
  ] }) }
]);

onMounted(loadPosts);
async function loadPosts(): Promise<void> { rows.value = (await getPostsApi()).data.records; }
function openCreate(): void { Object.assign(form, { id: undefined, code: "", name: "", sortOrder: 0, status: "enabled" }); formVisible.value = true; }
async function openEdit(row: PostSummaryDto): Promise<void> { Object.assign(form, await (await getPostApi(row.id)).data); formVisible.value = true; }
async function submitForm(): Promise<void> {
  if (!form.name.trim() || (!form.id && !form.code.trim())) { message.error("请填写必填字段。"); return; }
  if (form.id) await updatePostApi(form.id, { name: form.name, sortOrder: form.sortOrder, status: form.status });
  else await createPostApi({ code: form.code, name: form.name, sortOrder: form.sortOrder, status: form.status });
  message.success("岗位已保存。"); formVisible.value = false; await loadPosts();
}
async function confirmDelete(row: PostSummaryDto): Promise<void> { if (!window.confirm(`确认删除岗位 ${row.name}？`)) return; await deletePostApi(row.id); message.success("岗位已删除。"); await loadPosts(); }
async function changeStatus(row: PostSummaryDto): Promise<void> { if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}岗位 ${row.name}？`)) return; if (row.status === "enabled") await disablePostApi(row.id); else await enablePostApi(row.id); message.success("岗位状态已更新。"); await loadPosts(); }
</script>

<template>
  <main>
    <NCard title="岗位管理">
      <NSpace class="mb-4"><PermissionButton type="primary" :permissions="['sys:post:create']" @click="openCreate">新建岗位</PermissionButton></NSpace>
      <NDataTable :columns="columns" :data="rows" />
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="岗位表单" class="max-w-lg">
      <NForm>
        <NFormItem v-if="!form.id" label="编码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem label="名称"><NInput v-model:value="form.name" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="form.sortOrder" /></NFormItem>
        <NFormItem label="状态"><NSelect v-model:value="form.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end"><NButton @click="formVisible = false">取消</NButton><NButton type="primary" @click="submitForm">保存</NButton></NSpace>
      </NForm>
    </NModal>
  </main>
</template>
