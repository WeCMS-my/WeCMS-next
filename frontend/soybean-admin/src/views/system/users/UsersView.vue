<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from "vue";
import {
  NButton,
  NCard,
  NDataTable,
  NForm,
  NFormItem,
  NInput,
  NModal,
  NSelect,
  NSpace,
  NTag,
  useMessage
} from "naive-ui";
import PermissionButton from "@/components/PermissionButton.vue";
import { getDepartmentTreeApi } from "@/api/system/depts";
import { getPostsApi } from "@/api/system/posts";
import { getRolesApi } from "@/api/system/roles";
import {
  assignUserPostsApi,
  assignUserRolesApi,
  createUserApi,
  deleteUserApi,
  disableUserApi,
  enableUserApi,
  getUserApi,
  getUsersApi,
  resetUserPasswordApi,
  updateUserApi
} from "@/api/system/users";
import type { DepartmentTreeDto, PostSummaryDto, RoleSummaryDto, UserDetailDto, UserSummaryDto } from "@/api/types/generated";

interface UserFormState {
  id?: number;
  username: string;
  displayName: string;
  password: string;
  email: string;
  phone: string;
  deptId?: number;
  roleIds: number[];
  postIds: number[];
}

const message = useMessage();
const loading = ref(false);
const rows = ref<UserSummaryDto[]>([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(20);
const roles = ref<RoleSummaryDto[]>([]);
const posts = ref<PostSummaryDto[]>([]);
const departments = ref<DepartmentTreeDto[]>([]);
const formVisible = ref(false);
const assignRolesVisible = ref(false);
const assignPostsVisible = ref(false);
const activeUser = ref<UserDetailDto | null>(null);
const filters = reactive({
  keyword: "",
  status: "",
  deptId: undefined as number | undefined
});
const form = reactive<UserFormState>(createEmptyForm());
const assignedRoleIds = ref<number[]>([]);
const assignedPostIds = ref<number[]>([]);

const roleOptions = computed(() => roles.value.map((role) => ({
  label: role.name,
  value: role.id
})));
const postOptions = computed(() => posts.value.map((post) => ({
  label: post.name,
  value: post.id
})));
const departmentOptions = computed(() => flattenDepartments(departments.value));

const columns = computed(() => [
  { title: "ID", key: "id", width: 70 },
  { title: "用户名", key: "username" },
  { title: "显示名", key: "displayName" },
  { title: "邮箱", key: "email" },
  { title: "手机", key: "phone" },
  {
    title: "部门",
    key: "deptId",
    render: (row: UserSummaryDto) => findDepartmentName(row.deptId)
  },
  {
    title: "状态",
    key: "status",
    render: (row: UserSummaryDto) => h(
      NTag,
      { type: row.status === "enabled" ? "success" : "warning" },
      { default: () => row.status === "enabled" ? "启用" : "禁用" }
    )
  },
  {
    title: "超级管理员",
    key: "isSuperAdmin",
    render: (row: UserSummaryDto) => row.isSuperAdmin ? "是" : "否"
  },
  { title: "最近登录", key: "lastLoginAt" },
  { title: "创建时间", key: "createdAt" },
  {
    title: "操作",
    key: "actions",
    width: 360,
    render: (row: UserSummaryDto) => h(
      NSpace,
      null,
      {
        default: () => [
          h(PermissionButton, {
            secondary: true,
            permissions: ["sys:user:update"],
            onClick: () => void openEdit(row)
          }, { default: () => "编辑" }),
          h(PermissionButton, {
            secondary: true,
            permissions: ["sys:user:delete"],
            onClick: () => void confirmDelete(row)
          }, { default: () => "删除" }),
          h(PermissionButton, {
            secondary: true,
            permissions: [row.status === "enabled" ? "sys:user:disable" : "sys:user:enable"],
            onClick: () => void changeStatus(row)
          }, { default: () => row.status === "enabled" ? "禁用" : "启用" }),
          h(PermissionButton, {
            secondary: true,
            permissions: ["sys:user:reset-password"],
            onClick: () => void resetPassword(row)
          }, { default: () => "重置密码" }),
          h(PermissionButton, {
            secondary: true,
            permissions: ["sys:user:assign-role"],
            onClick: () => void openAssignRoles(row)
          }, { default: () => "分配角色" }),
          h(PermissionButton, {
            secondary: true,
            permissions: ["sys:user:assign-post"],
            onClick: () => void openAssignPosts(row)
          }, { default: () => "分配岗位" })
        ]
      }
    )
  }
]);

onMounted(async () => {
  await Promise.all([loadUsers(), loadLookups()]);
});

async function loadUsers(): Promise<void> {
  loading.value = true;
  try {
    const result = await getUsersApi({
      page: page.value,
      pageSize: pageSize.value,
      keyword: filters.keyword.trim() || undefined,
      status: filters.status || undefined,
      deptId: filters.deptId
    });
    rows.value = result.data.records;
    total.value = result.data.total;
  } finally {
    loading.value = false;
  }
}

async function loadLookups(): Promise<void> {
  const [roleResult, postResult, deptResult] = await Promise.all([
    getRolesApi(),
    getPostsApi(),
    getDepartmentTreeApi()
  ]);
  roles.value = roleResult.data.records;
  posts.value = postResult.data.records;
  departments.value = deptResult.data;
}

function openCreate(): void {
  Object.assign(form, createEmptyForm());
  formVisible.value = true;
}

async function openEdit(row: UserSummaryDto): Promise<void> {
  const result = await getUserApi(row.id);
  const detail = result.data;
  Object.assign(form, {
    id: detail.id,
    username: detail.username,
    displayName: detail.displayName,
    password: "",
    email: detail.email ?? "",
    phone: detail.phone ?? "",
    deptId: detail.deptId ?? undefined,
    roleIds: detail.roleIds,
    postIds: detail.postIds
  });
  formVisible.value = true;
}

async function submitForm(): Promise<void> {
  if (!form.displayName.trim() || (!form.id && (!form.username.trim() || !form.password.trim()))) {
    message.error("请填写必填字段。");
    return;
  }

  if (form.id) {
    await updateUserApi(form.id, {
      displayName: form.displayName.trim(),
      email: form.email || null,
      phone: form.phone || null,
      deptId: form.deptId ?? null
    });
  } else {
    await createUserApi({
      username: form.username.trim(),
      displayName: form.displayName.trim(),
      password: form.password,
      email: form.email || null,
      phone: form.phone || null,
      deptId: form.deptId ?? null,
      roleIds: form.roleIds,
      postIds: form.postIds
    });
  }

  message.success("用户已保存。");
  formVisible.value = false;
  await loadUsers();
}

async function confirmDelete(row: UserSummaryDto): Promise<void> {
  if (!window.confirm(`确认删除用户 ${row.username}？`)) {
    return;
  }
  await deleteUserApi(row.id);
  message.success("用户已删除。");
  await loadUsers();
}

async function changeStatus(row: UserSummaryDto): Promise<void> {
  const enabling = row.status !== "enabled";
  if (!window.confirm(`确认${enabling ? "启用" : "禁用"}用户 ${row.username}？`)) {
    return;
  }
  if (enabling) {
    await enableUserApi(row.id);
  } else {
    await disableUserApi(row.id);
  }
  message.success("用户状态已更新。");
  await loadUsers();
}

async function resetPassword(row: UserSummaryDto): Promise<void> {
  const password = window.prompt(`请输入 ${row.username} 的新密码`);
  if (!password) {
    return;
  }
  await resetUserPasswordApi(row.id, { password });
  message.success("密码已重置。");
}

async function openAssignRoles(row: UserSummaryDto): Promise<void> {
  const result = await getUserApi(row.id);
  activeUser.value = result.data;
  assignedRoleIds.value = result.data.roleIds;
  assignRolesVisible.value = true;
}

async function submitAssignRoles(): Promise<void> {
  if (!activeUser.value) {
    return;
  }
  await assignUserRolesApi(activeUser.value.id, { roleIds: assignedRoleIds.value });
  message.success("角色已分配。");
  assignRolesVisible.value = false;
}

async function openAssignPosts(row: UserSummaryDto): Promise<void> {
  const result = await getUserApi(row.id);
  activeUser.value = result.data;
  assignedPostIds.value = result.data.postIds;
  assignPostsVisible.value = true;
}

async function submitAssignPosts(): Promise<void> {
  if (!activeUser.value) {
    return;
  }
  await assignUserPostsApi(activeUser.value.id, { postIds: assignedPostIds.value });
  message.success("岗位已分配。");
  assignPostsVisible.value = false;
}

function createEmptyForm(): UserFormState {
  return {
    username: "",
    displayName: "",
    password: "",
    email: "",
    phone: "",
    deptId: undefined,
    roleIds: [],
    postIds: []
  };
}

function flattenDepartments(items: DepartmentTreeDto[]): Array<{ label: string; value: number }> {
  return items.flatMap((item) => [
    { label: item.name, value: item.id },
    ...flattenDepartments(item.children ?? [])
  ]);
}

function findDepartmentName(deptId?: number | null): string {
  if (!deptId) {
    return "-";
  }
  return departmentOptions.value.find((item) => item.value === deptId)?.label ?? String(deptId);
}
</script>

<template>
  <main class="space-y-4">
    <NCard title="用户管理">
      <NSpace class="mb-4" align="center">
        <NInput v-model:value="filters.keyword" clearable placeholder="搜索用户名/显示名" />
        <NSelect
          v-model:value="filters.status"
          clearable
          class="w-32"
          :options="[
            { label: '启用', value: 'enabled' },
            { label: '禁用', value: 'disabled' }
          ]"
          placeholder="状态"
        />
        <NSelect
          v-model:value="filters.deptId"
          clearable
          class="w-48"
          :options="departmentOptions"
          placeholder="部门"
        />
        <NButton secondary @click="loadUsers">查询</NButton>
        <PermissionButton type="primary" :permissions="['sys:user:create']" @click="openCreate">
          新建用户
        </PermissionButton>
      </NSpace>
      <NDataTable
        :columns="columns"
        :data="rows"
        :loading="loading"
        :pagination="{ page, pageSize, itemCount: total, onUpdatePage: (nextPage: number) => { page = nextPage; loadUsers(); } }"
      >
        <template #empty>暂无用户</template>
      </NDataTable>
    </NCard>

    <NModal v-model:show="formVisible" preset="card" title="用户表单" class="max-w-xl">
      <NForm>
        <NFormItem v-if="!form.id" label="用户名">
          <NInput v-model:value="form.username" />
        </NFormItem>
        <NFormItem label="显示名">
          <NInput v-model:value="form.displayName" />
        </NFormItem>
        <NFormItem v-if="!form.id" label="密码">
          <NInput v-model:value="form.password" type="password" />
        </NFormItem>
        <NFormItem label="邮箱">
          <NInput v-model:value="form.email" />
        </NFormItem>
        <NFormItem label="手机">
          <NInput v-model:value="form.phone" />
        </NFormItem>
        <NFormItem label="部门">
          <NSelect v-model:value="form.deptId" clearable :options="departmentOptions" />
        </NFormItem>
        <NFormItem v-if="!form.id" label="角色">
          <NSelect v-model:value="form.roleIds" multiple :options="roleOptions" />
        </NFormItem>
        <NFormItem v-if="!form.id" label="岗位">
          <NSelect v-model:value="form.postIds" multiple :options="postOptions" />
        </NFormItem>
        <NSpace justify="end">
          <NButton @click="formVisible = false">取消</NButton>
          <NButton type="primary" @click="submitForm">保存</NButton>
        </NSpace>
      </NForm>
    </NModal>

    <NModal v-model:show="assignRolesVisible" preset="card" title="分配角色" class="max-w-lg">
      <NSelect v-model:value="assignedRoleIds" multiple :options="roleOptions" />
      <NSpace class="mt-4" justify="end">
        <NButton @click="assignRolesVisible = false">取消</NButton>
        <NButton type="primary" @click="submitAssignRoles">保存</NButton>
      </NSpace>
    </NModal>

    <NModal v-model:show="assignPostsVisible" preset="card" title="分配岗位" class="max-w-lg">
      <NSelect v-model:value="assignedPostIds" multiple :options="postOptions" />
      <NSpace class="mt-4" justify="end">
        <NButton @click="assignPostsVisible = false">取消</NButton>
        <NButton type="primary" @click="submitAssignPosts">保存</NButton>
      </NSpace>
    </NModal>
  </main>
</template>
