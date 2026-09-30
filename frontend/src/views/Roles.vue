<template>
  <div class="roles-container">
    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="card-header">
          <span class="header-title">🛡️ 角色與側邊欄權限管理</span>
          <el-button type="primary" :icon="Plus" @click="openRoleDialog()">
            新增角色
          </el-button>
        </div>
      </template>

      <el-table :data="roles" style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="70" align="center" />
        <el-table-column prop="name" label="角色名稱" width="140">
          <template #default="scope">
            <el-tag :type="scope.row.name === 'Admin' ? 'danger' : 'info'">
              {{ scope.row.name }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="description" label="描述說明" min-width="180" />
        <el-table-column label="可存取側邊欄位 (Allowed Menus)" min-width="260">
          <template #default="scope">
            <div class="menu-tags">
              <el-tag
                v-for="menuPath in scope.row.allowedMenus"
                :key="menuPath"
                size="small"
                type="success"
                effect="plain"
              >
                {{ getMenuLabel(menuPath) }}
              </el-tag>
            </div>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="180" align="center">
          <template #default="scope">
            <el-button size="small" type="primary" @click="openRoleDialog(scope.row)">
              設定權限
            </el-button>
            <el-button
              size="small"
              type="danger"
              :disabled="scope.row.name === 'Admin' || scope.row.name === 'User'"
              @click="deleteRole(scope.row)"
            >
              刪除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Role Add/Edit Dialog -->
    <el-dialog
      v-model="roleDialogVisible"
      :title="editingRoleId ? '設定角色側邊欄權限' : '新增角色'"
      width="520px"
    >
      <el-form :model="roleForm" label-width="90px">
        <el-form-item label="角色名稱" required>
          <el-input v-model="roleForm.name" placeholder="例如: Manager" :disabled="roleForm.name === 'Admin'" />
        </el-form-item>
        <el-form-item label="描述說明">
          <el-input v-model="roleForm.description" placeholder="簡短描述該角色的職責" />
        </el-form-item>
        <el-form-item label="欄位權限" required>
          <div class="permission-checkboxes">
            <el-checkbox-group v-model="roleForm.allowedMenus">
              <div v-for="menu in allMenus" :key="menu.path" class="checkbox-item">
                <el-checkbox :label="menu.path">
                  <strong>{{ menu.label }}</strong>
                  <span class="menu-path">({{ menu.path }})</span>
                </el-checkbox>
              </div>
            </el-checkbox-group>
          </div>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="roleDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveRole">儲存權限</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import api from '@/api';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus } from '@element-plus/icons-vue';

interface Role {
  id: number;
  name: string;
  description?: string;
  allowedMenus: string[];
}

const allMenus = [
  { path: '/', label: '系統儀表板' },
  { path: '/orders', label: '團購場次管理' },
  { path: '/stores', label: '店家與菜單管理' },
  { path: '/users', label: '使用者管理' },
  { path: '/roles', label: '角色權限管理' },
  { path: '/logs', label: '操作紀錄' },
];

const roles = ref<Role[]>([]);
const loading = ref(false);

const roleDialogVisible = ref(false);
const editingRoleId = ref<number | null>(null);

const roleForm = reactive({
  name: '',
  description: '',
  allowedMenus: [] as string[],
});

const fetchRoles = async () => {
  loading.value = true;
  try {
    const res = await api.get('/roles');
    roles.value = res.data;
  } catch {
    ElMessage.error('載入角色失敗');
  } finally {
    loading.value = false;
  }
};

const openRoleDialog = (role?: Role) => {
  if (role) {
    editingRoleId.value = role.id;
    roleForm.name = role.name;
    roleForm.description = role.description || '';
    roleForm.allowedMenus = [...role.allowedMenus];
  } else {
    editingRoleId.value = null;
    roleForm.name = '';
    roleForm.description = '';
    roleForm.allowedMenus = ['/', '/orders'];
  }
  roleDialogVisible.value = true;
};

const saveRole = async () => {
  if (!roleForm.name.trim()) {
    ElMessage.warning('請輸入角色名稱');
    return;
  }

  try {
    const payload = {
      name: roleForm.name,
      description: roleForm.description,
      allowedMenus: roleForm.allowedMenus,
    };

    if (editingRoleId.value) {
      await api.put(`/roles/${editingRoleId.value}`, payload);
      ElMessage.success('角色權限更新成功');
    } else {
      await api.post('/roles', payload);
      ElMessage.success('新增角色成功');
    }
    roleDialogVisible.value = false;
    fetchRoles();
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '儲存失敗');
  }
};

const deleteRole = (role: Role) => {
  ElMessageBox.confirm(`確定要刪除角色 [ ${role.name} ] 嗎？`, '警告', {
    type: 'warning',
  }).then(async () => {
    try {
      await api.delete(`/roles/${role.id}`);
      ElMessage.success('已刪除角色');
      fetchRoles();
    } catch (err: any) {
      ElMessage.error(err.response?.data?.message || '刪除失敗');
    }
  });
};

const getMenuLabel = (path: string) => {
  const found = allMenus.find((m) => m.path === path);
  return found ? found.label : path;
};

onMounted(() => {
  fetchRoles();
});
</script>

<style scoped>
.roles-container {
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

.menu-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 6px;
}

.permission-checkboxes {
  border: 1px solid #e5e7eb;
  padding: 12px 16px;
  border-radius: 6px;
  background-color: #f9fafb;
  width: 100%;
}

.checkbox-item {
  margin-bottom: 6px;
}

.menu-path {
  font-size: 12px;
  color: #9ca3af;
  margin-left: 6px;
}
</style>
