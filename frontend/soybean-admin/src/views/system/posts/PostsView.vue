<script setup lang="ts">
import { computed, h, reactive, ref, onMounted } from "vue";
import { NButton, NCard, NDataTable, NForm, NFormItem, NInput, NInputNumber, NModal, NSelect, NSpace, useMessage } from "naive-ui";
import type { FormInst, FormRules } from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { createPostApi, deletePostApi, disablePostApi, enablePostApi, getPostApi, getPostsApi, updatePostApi } from "@/api/system/posts";
import type { PostSummaryDto } from "@/api/types/generated";
import { apiErrorMessage } from "@/utils/api-error";

const message = useMessage();
const rows = ref<PostSummaryDto[]>([]);
const loading = ref(false);
const submitting = ref(false);
const page = ref(1);
const pageSize = ref(20);
const total = ref(0);
const formVisible = ref(false);
const formRef = ref<FormInst | null>(null);
const form = reactive({ id: undefined as number | undefined, code: "", name: "", sortOrder: 0, status: "enabled" });
const formRules: FormRules = {
  code: [{ required: true, message: "请输入编码", trigger: ["input", "blur"] }],
  name: [{ required: true, message: "请输入名称", trigger: ["input", "blur"] }],
  status: [{ required: true, message: "请选择状态", trigger: ["change", "blur"] }]
};

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

async function loadPosts(): Promise<void> {
  loading.value = true;
  try {
    const result = await getPostsApi({ page: page.value, pageSize: pageSize.value });
    rows.value = result.data.records;
    total.value = result.data.total;
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    loading.value = false;
  }
}

function openCreate(): void {
  Object.assign(form, { id: undefined, code: "", name: "", sortOrder: 0, status: "enabled" });
  formVisible.value = true;
}

async function openEdit(row: PostSummaryDto): Promise<void> {
  try {
    Object.assign(form, (await getPostApi(row.id)).data);
    formVisible.value = true;
  } catch (error) {
    message.error(apiErrorMessage(error));
  }
}

async function submitForm(): Promise<void> {
  try {
    await formRef.value?.validate();
  } catch {
    return;
  }

  submitting.value = true;
  try {
    if (form.id) {
      await updatePostApi(form.id, { name: form.name, sortOrder: form.sortOrder, status: form.status });
    } else {
      await createPostApi({ code: form.code, name: form.name, sortOrder: form.sortOrder, status: form.status });
    }
    message.success("岗位已保存。");
    formVisible.value = false;
    await loadPosts();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function confirmDelete(row: PostSummaryDto): Promise<void> {
  if (!window.confirm(`确认删除岗位 ${row.name}？`)) {
    return;
  }

  submitting.value = true;
  try {
    await deletePostApi(row.id);
    message.success("岗位已删除。");
    await loadPosts();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}

async function changeStatus(row: PostSummaryDto): Promise<void> {
  if (!window.confirm(`确认${row.status === "enabled" ? "禁用" : "启用"}岗位 ${row.name}？`)) {
    return;
  }

  submitting.value = true;
  try {
    if (row.status === "enabled") {
      await disablePostApi(row.id);
    } else {
      await enablePostApi(row.id);
    }
    message.success("岗位状态已更新。");
    await loadPosts();
  } catch (error) {
    message.error(apiErrorMessage(error));
  } finally {
    submitting.value = false;
  }
}
</script>

<template>
  <main>
    <NCard title="岗位管理">
      <NSpace class="mb-4">
        <PermissionButton type="primary" :permissions="['sys:post:create']" @click="openCreate">新建岗位</PermissionButton>
      </NSpace>
      <NDataTable
        :columns="columns"
        :data="rows"
        :loading="loading"
        :pagination="{ page, pageSize, itemCount: total, onUpdatePage: (nextPage: number) => { page = nextPage; loadPosts(); } }"
      >
        <template #empty>暂无岗位</template>
      </NDataTable>
    </NCard>
    <NModal v-model:show="formVisible" preset="card" title="岗位表单" class="max-w-lg">
      <NForm ref="formRef" :model="form" :rules="formRules">
        <NFormItem v-if="!form.id" path="code" label="编码"><NInput v-model:value="form.code" /></NFormItem>
        <NFormItem path="name" label="名称"><NInput v-model:value="form.name" /></NFormItem>
        <NFormItem label="排序"><NInputNumber v-model:value="form.sortOrder" /></NFormItem>
        <NFormItem path="status" label="状态"><NSelect v-model:value="form.status" :options="[{ label: '启用', value: 'enabled' }, { label: '禁用', value: 'disabled' }]" /></NFormItem>
        <NSpace justify="end">
          <NButton :disabled="submitting" @click="formVisible = false">取消</NButton>
          <NButton type="primary" :loading="submitting" @click="submitForm">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>
  </main>
</template>
