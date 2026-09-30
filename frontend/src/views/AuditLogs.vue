<template>
  <div class="audit-logs-container">
    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="card-header">
          <span class="header-title">📜 系統操作紀錄 (Audit Logs)</span>
          <div class="header-filter">
            <el-input
              v-model="searchKeyword"
              placeholder="搜尋操作者 / 動作 / 詳細說明"
              clearable
              style="width: 280px"
              @keyup.enter="fetchLogs"
              @clear="fetchLogs"
            >
              <template #append>
                <el-button :icon="Search" @click="fetchLogs" />
              </template>
            </el-input>
          </div>
        </div>
      </template>

      <el-table :data="logs" style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="70" align="center" />
        <el-table-column prop="userName" label="操作者" width="140">
          <template #default="scope">
            <span style="font-weight: 600; color: #1f2937">{{ scope.row.userName }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="action" label="動作名稱" width="160">
          <template #default="scope">
            <el-tag :type="getActionTagType(scope.row.action)">
              {{ scope.row.action }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="details" label="詳細說明" min-width="240" />
        <el-table-column prop="ipAddress" label="IP 位址" width="130" align="center" />
        <el-table-column label="操作時間" width="180">
          <template #default="scope">
            {{ formatDate(scope.row.createdAt) }}
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import api from '@/api';
import { ElMessage } from 'element-plus';
import { Search } from '@element-plus/icons-vue';

interface AuditLog {
  id: number;
  userId?: number;
  userName: string;
  action: string;
  details?: string;
  ipAddress?: string;
  createdAt: string;
}

const logs = ref<AuditLog[]>([]);
const loading = ref(false);
const searchKeyword = ref('');

const fetchLogs = async () => {
  loading.value = true;
  try {
    const res = await api.get('/auditlogs', {
      params: { search: searchKeyword.value },
    });
    logs.value = res.data;
  } catch {
    ElMessage.error('載入操作紀錄失敗');
  } finally {
    loading.value = false;
  }
};

const getActionTagType = (action: string) => {
  if (action.includes('刪除')) return 'danger';
  if (action.includes('新增') || action.includes('登入')) return 'success';
  if (action.includes('修改') || action.includes('重設')) return 'warning';
  return 'info';
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleString('zh-TW', { hour12: false });
};

onMounted(() => {
  fetchLogs();
});
</script>

<style scoped>
.audit-logs-container {
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
