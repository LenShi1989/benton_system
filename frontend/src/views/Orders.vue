<template>
  <div class="orders-container">
    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="card-header">
          <span class="header-title">團購便當場次列表</span>
          <el-button
            v-if="authStore.isAdmin"
            type="primary"
            :icon="Plus"
            @click="openSessionDialog"
          >
            發起便當團購
          </el-button>
        </div>
      </template>

      <el-table :data="sessions" style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="70" align="center" />
        <el-table-column prop="title" label="團購主題" min-width="180" />
        <el-table-column prop="storeName" label="店家名稱" width="160" />
        <el-table-column prop="createdByName" label="發起人" width="120" />
        <el-table-column label="狀態" width="100" align="center">
          <template #default="scope">
            <el-tag :type="getStatusType(scope.row.status)">
              {{ getStatusLabel(scope.row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="totalItems" label="總份數" width="90" align="center" />
        <el-table-column label="總金額" width="120" align="right">
          <template #default="scope">
            <span class="price-text">NT$ {{ scope.row.totalAmount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="220" align="center">
          <template #default="scope">
            <el-button
              size="small"
              type="success"
              @click="$router.push(`/orders/${scope.row.id}`)"
            >
              檢視/點餐
            </el-button>
            <el-dropdown
              v-if="authStore.isAdmin"
              trigger="click"
              style="margin-left: 8px"
              @command="(cmd: string) => updateStatus(scope.row.id, cmd)"
            >
              <el-button size="small" type="primary" plain>
                變更狀態 <el-icon><CaretBottom /></el-icon>
              </el-button>
              <template #dropdown>
                <el-dropdown-menu>
                  <el-dropdown-item command="Open">開放點餐 (Open)</el-dropdown-item>
                  <el-dropdown-item command="Closed">截止結單 (Closed)</el-dropdown-item>
                  <el-dropdown-item command="Canceled">取消團購 (Canceled)</el-dropdown-item>
                </el-dropdown-menu>
              </template>
            </el-dropdown>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Create Session Dialog -->
    <el-dialog v-model="sessionDialogVisible" title="發起便當團購" width="500px">
      <el-form :model="sessionForm" label-width="90px">
        <el-form-item label="團購主題" required>
          <el-input v-model="sessionForm.title" placeholder="例如: 9/29 午餐池上排骨便當" />
        </el-form-item>
        <el-form-item label="選擇店家" required>
          <el-select v-model="sessionForm.storeId" placeholder="請選擇便當店家" style="width: 100%">
            <el-option
              v-for="s in stores"
              :key="s.id"
              :label="s.name"
              :value="s.id"
            />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="sessionDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="createSession">發起團購</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import api from '@/api';
import { useAuthStore } from '@/stores/auth';
import { ElMessage } from 'element-plus';
import { Plus, CaretBottom } from '@element-plus/icons-vue';

interface Store {
  id: number;
  name: string;
}

interface OrderSession {
  id: number;
  title: string;
  storeId: number;
  storeName: string;
  status: string;
  createdByName: string;
  totalItems: number;
  totalAmount: number;
}

const authStore = useAuthStore();
const sessions = ref<OrderSession[]>([]);
const stores = ref<Store[]>([]);
const loading = ref(false);

const sessionDialogVisible = ref(false);
const sessionForm = reactive({
  title: '',
  storeId: null as number | null,
});

const fetchSessions = async () => {
  loading.value = true;
  try {
    const res = await api.get('/orders/sessions');
    sessions.value = res.data;
  } catch {
    ElMessage.error('載入團購場次失敗');
  } finally {
    loading.value = false;
  }
};

const openSessionDialog = async () => {
  try {
    const res = await api.get('/stores?onlyActive=true');
    stores.value = res.data;
    if (stores.value.length === 0) {
      ElMessage.warning('目前沒有可用的便當店家，請先新增店家');
      return;
    }
    sessionForm.title = `${new Date().getMonth() + 1}/${new Date().getDate()} 午餐點餐`;
    sessionForm.storeId = stores.value[0].id;
    sessionDialogVisible.value = true;
  } catch {
    ElMessage.error('取得店家失敗');
  }
};

const createSession = async () => {
  if (!sessionForm.title.trim() || !sessionForm.storeId) {
    ElMessage.warning('請填寫完整資訊');
    return;
  }

  try {
    await api.post('/orders/sessions', sessionForm);
    ElMessage.success('成功發起團購場次');
    sessionDialogVisible.value = false;
    fetchSessions();
  } catch {
    ElMessage.error('發起失敗');
  }
};

const updateStatus = async (id: number, status: string) => {
  try {
    await api.put(`/orders/sessions/${id}/status`, JSON.stringify(status), {
      headers: { 'Content-Type': 'application/json' },
    });
    ElMessage.success('更新場次狀態成功');
    fetchSessions();
  } catch {
    ElMessage.error('更新狀態失敗');
  }
};

const getStatusType = (status: string) => {
  if (status === 'Open') return 'success';
  if (status === 'Closed') return 'warning';
  return 'info';
};

const getStatusLabel = (status: string) => {
  if (status === 'Open') return '開放點餐';
  if (status === 'Closed') return '已結單';
  return '已取消';
};

onMounted(() => {
  fetchSessions();
});
</script>

<style scoped>
.orders-container {
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

.price-text {
  font-weight: 600;
  color: #ef4444;
}
</style>
