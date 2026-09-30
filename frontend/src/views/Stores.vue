<template>
  <div class="stores-container">
    <el-card shadow="never" class="table-card">
      <template #header>
        <div class="card-header">
          <span class="header-title">便當店家列表</span>
          <el-button type="primary" :icon="Plus" @click="openStoreDialog()">
            新增店家
          </el-button>
        </div>
      </template>

      <el-table :data="stores" style="width: 100%" v-loading="loading">
        <el-table-column prop="id" label="ID" width="70" align="center" />
        <el-table-column prop="name" label="店家名稱" min-width="160" />
        <el-table-column prop="phone" label="電話" width="140" />
        <el-table-column prop="address" label="地址" min-width="200" />
        <el-table-column label="狀態" width="100" align="center">
          <template #default="scope">
            <el-tag :type="scope.row.isActive ? 'success' : 'danger'">
              {{ scope.row.isActive ? '提供中' : '已停用' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="260" align="center">
          <template #default="scope">
            <el-button size="small" type="info" @click="openMenuDrawer(scope.row)">
              菜單管理
            </el-button>
            <el-button size="small" type="primary" @click="openStoreDialog(scope.row)">
              編輯
            </el-button>
            <el-button size="small" type="danger" @click="deleteStore(scope.row.id)">
              刪除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>

    <!-- Store Add/Edit Dialog -->
    <el-dialog
      v-model="storeDialogVisible"
      :title="editingStoreId ? '編輯店家' : '新增店家'"
      width="500px"
    >
      <el-form :model="storeForm" label-width="90px">
        <el-form-item label="店家名稱" required>
          <el-input v-model="storeForm.name" placeholder="例如: 池上排骨飯" />
        </el-form-item>
        <el-form-item label="電話">
          <el-input v-model="storeForm.phone" placeholder="02-12345678" />
        </el-form-item>
        <el-form-item label="地址">
          <el-input v-model="storeForm.address" placeholder="店家地址" />
        </el-form-item>
        <el-form-item label="是否提供">
          <el-switch v-model="storeForm.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="storeDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveStore">儲存</el-button>
      </template>
    </el-dialog>

    <!-- Menu Items Drawer -->
    <el-drawer
      v-model="menuDrawerVisible"
      :title="`菜單管理 - ${currentStore?.name || ''}`"
      size="600px"
    >
      <div style="margin-bottom: 16px; display: flex; justify-content: flex-end">
        <el-button type="primary" size="small" :icon="Plus" @click="openMenuItemDialog()">
          新增菜單品項
        </el-button>
      </div>

      <el-table :data="menuItems" style="width: 100%" v-loading="menuLoading">
        <el-table-column prop="name" label="品名" min-width="120" />
        <el-table-column prop="description" label="說明" min-width="140" />
        <el-table-column label="單價" width="100" align="right">
          <template #default="scope">
            <span style="font-weight: 600; color: #ef4444">NT$ {{ scope.row.price }}</span>
          </template>
        </el-table-column>
        <el-table-column label="狀態" width="80" align="center">
          <template #default="scope">
            <el-tag size="small" :type="scope.row.isActive ? 'success' : 'info'">
              {{ scope.row.isActive ? '供應' : '停售' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="140" align="center">
          <template #default="scope">
            <el-button size="small" type="primary" link @click="openMenuItemDialog(scope.row)">
              編輯
            </el-button>
            <el-button size="small" type="danger" link @click="deleteMenuItem(scope.row.id)">
              刪除
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-drawer>

    <!-- Menu Item Dialog -->
    <el-dialog
      v-model="menuItemDialogVisible"
      :title="editingMenuItemId ? '編輯品項' : '新增品項'"
      width="450px"
      append-to-body
    >
      <el-form :model="menuItemForm" label-width="90px">
        <el-form-item label="品名" required>
          <el-input v-model="menuItemForm.name" placeholder="例如: 招牌排骨飯" />
        </el-form-item>
        <el-form-item label="說明">
          <el-input v-model="menuItemForm.description" placeholder="配菜說明" />
        </el-form-item>
        <el-form-item label="價格 (NT$)" required>
          <el-input-number v-model="menuItemForm.price" :min="1" :precision="0" style="width: 100%" />
        </el-form-item>
        <el-form-item label="是否供應">
          <el-switch v-model="menuItemForm.isActive" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="menuItemDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveMenuItem">儲存</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, onMounted } from 'vue';
import api from '@/api';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Plus } from '@element-plus/icons-vue';

interface Store {
  id: number;
  name: string;
  phone?: string;
  address?: string;
  isActive: boolean;
}

interface MenuItem {
  id: number;
  storeId: number;
  name: string;
  description?: string;
  price: number;
  isActive: boolean;
}

const stores = ref<Store[]>([]);
const loading = ref(false);

const storeDialogVisible = ref(false);
const editingStoreId = ref<number | null>(null);
const storeForm = reactive({
  name: '',
  phone: '',
  address: '',
  isActive: true,
});

// Menu Items Drawer State
const menuDrawerVisible = ref(false);
const currentStore = ref<Store | null>(null);
const menuItems = ref<MenuItem[]>([]);
const menuLoading = ref(false);

const menuItemDialogVisible = ref(false);
const editingMenuItemId = ref<number | null>(null);
const menuItemForm = reactive({
  name: '',
  description: '',
  price: 100,
  isActive: true,
});

const fetchStores = async () => {
  loading.value = true;
  try {
    const res = await api.get('/stores');
    stores.value = res.data;
  } catch (err) {
    ElMessage.error('載入店家失敗');
  } finally {
    loading.value = false;
  }
};

const openStoreDialog = (store?: Store) => {
  if (store) {
    editingStoreId.value = store.id;
    storeForm.name = store.name;
    storeForm.phone = store.phone || '';
    storeForm.address = store.address || '';
    storeForm.isActive = store.isActive;
  } else {
    editingStoreId.value = null;
    storeForm.name = '';
    storeForm.phone = '';
    storeForm.address = '';
    storeForm.isActive = true;
  }
  storeDialogVisible.value = true;
};

const saveStore = async () => {
  if (!storeForm.name.trim()) {
    ElMessage.warning('請輸入店家名稱');
    return;
  }
  try {
    if (editingStoreId.value) {
      await api.put(`/stores/${editingStoreId.value}`, storeForm);
      ElMessage.success('更新店家成功');
    } else {
      await api.post('/stores', storeForm);
      ElMessage.success('新增店家成功');
    }
    storeDialogVisible.value = false;
    fetchStores();
  } catch (err) {
    ElMessage.error('儲存失敗');
  }
};

const deleteStore = (id: number) => {
  ElMessageBox.confirm('確定要刪除該店家及其菜單嗎？', '警告', {
    type: 'warning',
  }).then(async () => {
    try {
      await api.delete(`/stores/${id}`);
      ElMessage.success('已刪除店家');
      fetchStores();
    } catch {
      ElMessage.error('刪除失敗');
    }
  });
};

const openMenuDrawer = async (store: Store) => {
  currentStore.value = store;
  menuDrawerVisible.value = true;
  fetchMenuItems(store.id);
};

const fetchMenuItems = async (storeId: number) => {
  menuLoading.value = true;
  try {
    const res = await api.get(`/menuitems/store/${storeId}`);
    menuItems.value = res.data;
  } catch {
    ElMessage.error('載入菜單失敗');
  } finally {
    menuLoading.value = false;
  }
};

const openMenuItemDialog = (item?: MenuItem) => {
  if (item) {
    editingMenuItemId.value = item.id;
    menuItemForm.name = item.name;
    menuItemForm.description = item.description || '';
    menuItemForm.price = item.price;
    menuItemForm.isActive = item.isActive;
  } else {
    editingMenuItemId.value = null;
    menuItemForm.name = '';
    menuItemForm.description = '';
    menuItemForm.price = 100;
    menuItemForm.isActive = true;
  }
  menuItemDialogVisible.value = true;
};

const saveMenuItem = async () => {
  if (!currentStore.value || !menuItemForm.name.trim()) return;

  const payload = {
    storeId: currentStore.value.id,
    name: menuItemForm.name,
    description: menuItemForm.description,
    price: menuItemForm.price,
    isActive: menuItemForm.isActive,
  };

  try {
    if (editingMenuItemId.value) {
      await api.put(`/menuitems/${editingMenuItemId.value}`, payload);
      ElMessage.success('更新品項成功');
    } else {
      await api.post('/menuitems', payload);
      ElMessage.success('新增品項成功');
    }
    menuItemDialogVisible.value = false;
    fetchMenuItems(currentStore.value.id);
  } catch {
    ElMessage.error('儲存品項失敗');
  }
};

const deleteMenuItem = (id: number) => {
  ElMessageBox.confirm('確定要刪除該菜單品項嗎？', '提示', { type: 'warning' }).then(async () => {
    try {
      await api.delete(`/menuitems/${id}`);
      ElMessage.success('已刪除品項');
      if (currentStore.value) fetchMenuItems(currentStore.value.id);
    } catch {
      ElMessage.error('刪除失敗');
    }
  });
};

onMounted(() => {
  fetchStores();
});
</script>

<style scoped>
.stores-container {
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
