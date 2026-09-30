<template>
  <div class="dashboard-container">
    <el-row :gutter="20" class="stat-cards">
      <el-col :span="8">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon bg-blue"><Shop /></div>
          <div class="stat-info">
            <div class="stat-value">{{ stores.length }}</div>
            <div class="stat-label">合作便當店家</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon bg-green"><ShoppingCart /></div>
          <div class="stat-info">
            <div class="stat-value">{{ sessions.length }}</div>
            <div class="stat-label">累計團購場次</div>
          </div>
        </el-card>
      </el-col>
      <el-col :span="8">
        <el-card shadow="hover" class="stat-card">
          <div class="stat-icon bg-orange"><Food /></div>
          <div class="stat-info">
            <div class="stat-value">{{ activeSessions.length }}</div>
            <div class="stat-label">進行中點餐場次</div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- Active Sessions Quick List -->
    <el-card class="section-card" shadow="never">
      <template #header>
        <div class="card-header">
          <span class="header-title">🔥 進行中的團購場次</span>
          <el-button type="primary" @click="$router.push('/orders')">檢視全部場次</el-button>
        </div>
      </template>

      <el-table :data="activeSessions" style="width: 100%" v-loading="loading">
        <el-table-column prop="title" label="團購主題" min-width="180" />
        <el-table-column prop="storeName" label="店家名稱" width="160" />
        <el-table-column prop="createdByName" label="發起人" width="120" />
        <el-table-column prop="totalItems" label="已訂數量" width="100" align="center" />
        <el-table-column label="目前金額" width="120" align="right">
          <template #default="scope">
            <span class="price-text">NT$ {{ scope.row.totalAmount }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="120" align="center">
          <template #default="scope">
            <el-button type="success" size="small" @click="$router.push(`/orders/${scope.row.id}`)">
              前往點餐
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import api from '@/api';
import { Shop, ShoppingCart, Food } from '@element-plus/icons-vue';

interface Store {
  id: number;
  name: string;
}

interface OrderSession {
  id: number;
  title: string;
  storeName: string;
  createdByName: string;
  status: string;
  totalItems: number;
  totalAmount: number;
}

const stores = ref<Store[]>([]);
const sessions = ref<OrderSession[]>([]);
const loading = ref(true);

const activeSessions = computed(() =>
  sessions.value.filter((s) => s.status === 'Open')
);

const fetchData = async () => {
  loading.value = true;
  try {
    const [storesRes, sessionsRes] = await Promise.all([
      api.get('/stores'),
      api.get('/orders/sessions'),
    ]);
    stores.value = storesRes.data;
    sessions.value = sessionsRes.data;
  } catch (err) {
    console.error(err);
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchData();
});
</script>

<style scoped>
.dashboard-container {
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.stat-card {
  border-radius: 8px;
}

.stat-card :deep(.el-card__body) {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 20px;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 28px;
  color: #fff;
}

.bg-blue {
  background-color: #3b82f6;
}

.bg-green {
  background-color: #10b981;
}

.bg-orange {
  background-color: #f59e0b;
}

.stat-info {
  display: flex;
  flex-direction: column;
}

.stat-value {
  font-size: 24px;
  font-weight: 700;
  color: #1f2937;
}

.stat-label {
  font-size: 13px;
  color: #6b7280;
}

.section-card {
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
  color: #111827;
}

.price-text {
  font-weight: 600;
  color: #ef4444;
}
</style>
