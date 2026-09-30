<template>
  <div class="order-detail-container" v-loading="loading">
    <!-- Header Info Card -->
    <el-card shadow="never" class="info-card">
      <div class="header-banner">
        <div class="session-info">
          <h2>{{ session?.title }}</h2>
          <div class="meta-tags">
            <el-tag type="primary"><el-icon><Shop /></el-icon> {{ session?.storeName }}</el-tag>
            <el-tag :type="session?.status === 'Open' ? 'success' : 'warning'">
              {{ session?.status === 'Open' ? '開放點餐中' : '已截止結單' }}
            </el-tag>
            <span class="created-meta">發起者: {{ session?.createdByName }}</span>
          </div>
        </div>

        <div class="summary-box">
          <div class="stat-item">
            <span class="stat-num">{{ session?.totalItems }}</span>
            <span class="stat-lbl">總數量 (份)</span>
          </div>
          <div class="stat-divider"></div>
          <div class="stat-item">
            <span class="stat-num price">NT$ {{ session?.totalAmount }}</span>
            <span class="stat-lbl">總金額</span>
          </div>
        </div>
      </div>
    </el-card>

    <el-row :gutter="20" style="margin-top: 20px">
      <!-- Pick Bento Menu Column -->
      <el-col :span="10">
        <el-card shadow="never" class="menu-card">
          <template #header>
            <div class="card-header">
              <span>🍱 選擇便當 (點擊下單)</span>
            </div>
          </template>

          <div v-if="session?.status !== 'Open'" class="closed-notice">
            <el-alert title="此團購場次已截止或取消，無法再新增點餐" type="warning" :closable="false" show-icon />
          </div>

          <div class="menu-list">
            <div
              v-for="item in menuItems"
              :key="item.id"
              class="menu-item-row"
            >
              <div class="item-detail">
                <div class="item-name">{{ item.name }}</div>
                <div class="item-desc" v-if="item.description">{{ item.description }}</div>
                <div class="item-price">NT$ {{ item.price }}</div>
              </div>
              <el-button
                type="primary"
                size="small"
                :disabled="session?.status !== 'Open'"
                @click="openOrderModal(item)"
              >
                + 點餐
              </el-button>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- Order List & Summary Column -->
      <el-col :span="14">
        <el-card shadow="never" class="list-card">
          <template #header>
            <div class="card-header">
              <span>📋 已點便當明細對照表</span>
            </div>
          </template>

          <el-table :data="orderItems" style="width: 100%" size="small">
            <el-table-column prop="userName" label="訂購人" width="100" />
            <el-table-column prop="menuItemName" label="便當品名" min-width="140" />
            <el-table-column prop="quantity" label="數量" width="60" align="center" />
            <el-table-column label="小計" width="90" align="right">
              <template #default="scope">
                <span style="font-weight: 600">NT$ {{ scope.row.subtotal }}</span>
              </template>
            </el-table-column>
            <el-table-column prop="note" label="備註" min-width="100" />
            <el-table-column label="操作" width="70" align="center">
              <template #default="scope">
                <el-button
                  v-if="canDelete(scope.row)"
                  type="danger"
                  link
                  size="small"
                  @click="deleteItem(scope.row.id)"
                >
                  刪除
                </el-button>
              </template>
            </el-table-column>
          </el-table>
        </el-card>
      </el-col>
    </el-row>

    <!-- Order Item Dialog -->
    <el-dialog v-model="modalVisible" :title="`點餐 - ${selectedMenuItem?.name}`" width="400px">
      <el-form label-width="80px">
        <el-form-item label="單價">
          <span style="font-weight: 600; color: #ef4444">NT$ {{ selectedMenuItem?.price }}</span>
        </el-form-item>
        <el-form-item label="數量">
          <el-input-number v-model="orderQuantity" :min="1" :max="50" style="width: 100%" />
        </el-form-item>
        <el-form-item label="備註">
          <el-input v-model="orderNote" placeholder="例如: 飯少 / 不加菜" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="modalVisible = false">取消</el-button>
        <el-button type="primary" @click="submitOrder">確認下單</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import api from '@/api';
import { useAuthStore } from '@/stores/auth';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Shop } from '@element-plus/icons-vue';

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

interface MenuItem {
  id: number;
  name: string;
  description?: string;
  price: number;
}

interface OrderItem {
  id: number;
  orderSessionId: number;
  userId: number;
  userName: string;
  menuItemId: number;
  menuItemName: string;
  quantity: number;
  unitPrice: number;
  subtotal: number;
  note?: string;
}

const route = useRoute();
const authStore = useAuthStore();
const sessionId = Number(route.params.id);

const session = ref<OrderSession | null>(null);
const menuItems = ref<MenuItem[]>([]);
const orderItems = ref<OrderItem[]>([]);
const loading = ref(true);

const modalVisible = ref(false);
const selectedMenuItem = ref<MenuItem | null>(null);
const orderQuantity = ref(1);
const orderNote = ref('');

const fetchDetail = async () => {
  loading.value = true;
  try {
    const [sessRes, itemsRes] = await Promise.all([
      api.get(`/orders/sessions/${sessionId}`),
      api.get(`/orders/sessions/${sessionId}/items`),
    ]);

    session.value = sessRes.data;
    orderItems.value = itemsRes.data;

    if (session.value) {
      const menuRes = await api.get(`/menuitems/store/${session.value.storeId}?onlyActive=true`);
      menuItems.value = menuRes.data;
    }
  } catch {
    ElMessage.error('載入點餐明細失敗');
  } finally {
    loading.value = false;
  }
};

const openOrderModal = (item: MenuItem) => {
  selectedMenuItem.value = item;
  orderQuantity.value = 1;
  orderNote.value = '';
  modalVisible.value = true;
};

const submitOrder = async () => {
  if (!selectedMenuItem.value) return;

  try {
    await api.post('/orders/items', {
      orderSessionId: sessionId,
      menuItemId: selectedMenuItem.value.id,
      quantity: orderQuantity.value,
      note: orderNote.value,
    });

    ElMessage.success('點餐成功！');
    modalVisible.value = false;
    fetchDetail();
  } catch (err: any) {
    ElMessage.error(err.response?.data?.message || '點餐失敗');
  }
};

const canDelete = (item: OrderItem) => {
  if (authStore.isAdmin) return true;
  if (session.value?.status !== 'Open') return false;
  return item.userName === authStore.user?.fullName;
};

const deleteItem = (id: number) => {
  ElMessageBox.confirm('確定要刪除該筆點餐紀錄嗎？', '提示', { type: 'warning' }).then(async () => {
    try {
      await api.delete(`/orders/items/${id}`);
      ElMessage.success('已刪除');
      fetchDetail();
    } catch {
      ElMessage.error('刪除失敗');
    }
  });
};

onMounted(() => {
  fetchDetail();
});
</script>

<style scoped>
.order-detail-container {
  display: flex;
  flex-direction: column;
}

.info-card {
  border-radius: 8px;
}

.header-banner {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.session-info h2 {
  margin: 0 0 8px 0;
  font-size: 20px;
  color: #111827;
}

.meta-tags {
  display: flex;
  align-items: center;
  gap: 12px;
}

.created-meta {
  font-size: 13px;
  color: #6b7280;
}

.summary-box {
  display: flex;
  align-items: center;
  background-color: #f9fafb;
  padding: 12px 24px;
  border-radius: 8px;
  border: 1px solid #e5e7eb;
}

.stat-item {
  display: flex;
  flex-direction: column;
  align-items: center;
}

.stat-num {
  font-size: 20px;
  font-weight: 700;
  color: #111827;
}

.stat-num.price {
  color: #ef4444;
}

.stat-lbl {
  font-size: 12px;
  color: #6b7280;
}

.stat-divider {
  width: 1px;
  height: 32px;
  background-color: #d1d5db;
  margin: 0 20px;
}

.menu-card, .list-card {
  border-radius: 8px;
}

.card-header {
  font-weight: 600;
}

.closed-notice {
  margin-bottom: 16px;
}

.menu-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.menu-item-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 12px;
  background-color: #f9fafb;
  border-radius: 6px;
  border: 1px solid #f3f4f6;
}

.item-name {
  font-weight: 600;
  color: #1f2937;
}

.item-desc {
  font-size: 12px;
  color: #6b7280;
}

.item-price {
  font-size: 14px;
  font-weight: 700;
  color: #ef4444;
  margin-top: 2px;
}
</style>
