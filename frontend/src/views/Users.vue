<template>
  <div class="users-container">
    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="card-header">
          <span class="header-title">👥 使用者帳號管理</span>
          <el-button type="primary" :icon="Plus" @click="openUserDialog()">
            新增使用者
          </el-button>
        </div>
      </template>

      <el-table :data="users" style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="70" align="center" />
        <el-table-column prop="username" label="帳號" min-width="120" />
        <el-table-column prop="fullName" label="姓名" min-width="140" />
        <el-table-column label="分配角色" width="140" align="center">
          <template #default="scope">
            <el-tag :type="getRoleTagType(scope.row.role)">
              {{ scope.row.role }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="建立時間" width="180">
          <template #default="scope">
            {{ formatDate(scope.row.createdAt) }}
          </template>
        </el-table-column>
        <el-table-column label="操作" width="280" align="center">
          <template #default="scope">
            <el-button size="small" type="warning" plain @click="resetPassword(scope.row)">
              <el-icon><RefreshLeft /></el-icon>重設密碼
            </el-button>
            <el-button size="small" type="primary" @click="openUserDialog(scope.row)">
              編輯
            </el-button>
            <el-button
              size="small"
              type="danger"
              :disabled="scope.row.username === 'admin'"
              @click="deleteUser(scope.row)"
            >
              刪除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Add/Edit User Dialog -->
    <el-dialog
      v-model="userDialogVisible"
      :title="editingUserId ? '編輯使用者' : '新增使用者'"
      width="480px"
    >
      <el-form :model="userForm" label-width="90px">
        <el-form-item label="帳號" required v-if="!editingUserId">
          <el-input v-model="userForm.username" placeholder="例如: user3" />
        </el-form-item>
        <el-form-item label="密碼" v-if="!editingUserId">
          <el-input
            v-model="userForm.password"
            placeholder="預設為 admin123 (可留空)"
            show-password
          />
        </el-form-item>
        <el-form-item label="真實姓名" required>
          <el-input v-model="userForm.fullName" placeholder="例如: 王小明" />
        </el-form-item>
        <el-form-item label="角色權限" required>
          <el-select v-model="userForm.roleId" placeholder="請選擇角色" style="width: 100%">
            <el-option
              v-for="r in roles"
              :key="r.id"
              :label="`${r.name} (${r.description || ''})`"
              :value="r.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="userDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveUser">儲存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import api from '@/api';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus, RefreshLeft } from '@element-plus/icons-vue';

interface Role {
  id: number;
  name: string;
  description?: string;
}

interface User {
  id: number;
  username: string;
  fullName: string;
  role: string;
  roleId?: number;
  createdAt: string;
}

const users = ref<User[]>([]);
const roles = ref<Role[]>([]);
const loading = ref(false);

const userDialogVisible = ref(false);
const editingUserId = ref<number | null>(null);

const userForm = reactive({
  username: '',
  password: '',
  fullName: '',
  roleId: null as number | null,
});

const fetchUsersAndRoles = async () => {
  loading.value = true;
  try {
    const [uRes, rRes] = await Promise.all([api.get('/users'), api.get('/roles')]);
    users.value = uRes.data;
    roles.value = rRes.data;
  } catch {
    ElMessage.error('載入使用者與角色失敗');
  } finally {
    loading.value = false;
  }
};

const openUserDialog = (user?: User) => {
  if (user) {
    editingUserId.value = user.id;
    userForm.username = user.username;
    userForm.fullName = user.fullName;
    userForm.roleId = user.roleId || (roles.value.find((r) => r.name === user.role)?.id ?? null);
  } else {
    editingUserId.value = null;
    userForm.username = '';
    userForm.password = '';
    userForm.fullName = '';
    userForm.roleId = roles.value.length > 0 ? roles.value[0].id : null;
  }
  userDialogVisible.value = true;
};

const saveUser = async () => {
  if (!userForm.fullName.trim()) {
    ElMessage.warning('請填寫姓名');
    return;
  }

  const selectedRole = roles.value.find((r) => r.id === userForm.roleId);
  const roleName = selectedRole ? selectedRole.name : 'User';

  try {
    if (editingUserId.value) {
      await api.put(`/users/${editingUserId.value}`, {
        fullName: userForm.fullName,
        role: roleName,
        roleId: userForm.roleId,
      });
      ElMessage.success('修改使用者成功');
    } else {
      if (!userForm.username.trim()) {
        ElMessage.warning('請填寫帳號');
        return;
      }
      await api.post('/users', {
        username: userForm.username,
        password: userForm.password || 'admin123',
        fullName: userForm.fullName,
        role: roleName,
        roleId: userForm.roleId,
      });
      ElMessage.success('新增使用者成功');
    }
    userDialogVisible.value = false;
    fetchUsersAndRoles();
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '儲存失敗');
  }
};

const resetPassword = (user: User) => {
  ElMessageBox.confirm(
    `確定要將使用者 [ ${user.fullName} (${user.username}) ] 的密碼重置為預設密碼 admin123 嗎？`,
    '恢復預設密碼',
    {
      confirmButtonText: '確定重置',
      cancelButtonText: '取消',
      type: 'warning',
    }
  ).then(async () => {
    try {
      await api.post(`/users/${user.id}/reset-password`);
      ElMessage.success(`帳號 [ ${user.username} ] 密碼已恢復為預設密碼 admin123`);
    } catch (err: any) {
      ElMessage.error(err.response?.data?.message || '重置密碼失敗');
    }
  });
};

const deleteUser = (user: User) => {
  ElMessageBox.confirm(`確定要刪除使用者 [ ${user.fullName} ] 嗎？`, '警告', {
    type: 'warning',
  }).then(async () => {
    try {
      await api.delete(`/users/${user.id}`);
      ElMessage.success('已刪除使用者');
      fetchUsersAndRoles();
    } catch (err: any) {
      ElMessage.error(err.response?.data?.message || '刪除失敗');
    }
  });
};

const getRoleTagType = (role: string) => {
  if (role === 'Admin') return 'danger';
  if (role === 'Manager') return 'warning';
  return 'info';
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-TW', { hour12: false });
};

onMounted(() => {
  fetchUsersAndRoles();
});
</script>

<style scoped>
.users-container {
  display: flex;
  flex-direction: column;
}

.table-card {
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-title {
  font-size: 16px;
  font-weight: 600;
}
</style>
