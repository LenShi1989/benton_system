<template>
  <el-container class="admin-layout">
    <!-- Sidebar / 側邊導航欄 -->
    <el-aside :width="isCollapse ? '64px' : '240px'" class="aside">
      <div class="logo-container">
        <el-icon class="logo-icon"><Food /></el-icon>
        <span v-if="!isCollapse" class="logo-title">訂便當系統</span>
      </div>

      <el-menu
        :default-active="activeMenu"
        class="el-menu-vertical"
        :collapse="isCollapse"
        router
        background-color="#1f2937"
        text-color="#9ca3af"
        active-text-color="#ffffff"
      >
        <el-menu-item index="/" v-if="authStore.hasMenuAccess('/')">
          <el-icon><DataBoard /></el-icon>
          <template #title>系統儀表板</template>
        </el-menu-item>

        <el-menu-item index="/orders" v-if="authStore.hasMenuAccess('/orders')">
          <el-icon><ShoppingCart /></el-icon>
          <template #title>團購場次管理</template>
        </el-menu-item>

        <el-menu-item index="/stores" v-if="authStore.hasMenuAccess('/stores')">
          <el-icon><Shop /></el-icon>
          <template #title>店家與菜單管理</template>
        </el-menu-item>

        <el-sub-menu
          index="user-management"
          v-if="authStore.hasMenuAccess('/users') || authStore.hasMenuAccess('/roles')"
        >
          <template #title>
            <el-icon><User /></el-icon>
            <span>使用者與權限管理</span>
          </template>
          <el-menu-item index="/users" v-if="authStore.hasMenuAccess('/users')">
            <el-icon><UserFilled /></el-icon>
            <template #title>使用者管理</template>
          </el-menu-item>
          <el-menu-item index="/roles" v-if="authStore.hasMenuAccess('/roles')">
            <el-icon><Lock /></el-icon>
            <template #title>角色管理 (側邊欄權限)</template>
          </el-menu-item>
        </el-sub-menu>

        <el-menu-item index="/logs" v-if="authStore.hasMenuAccess('/logs')">
          <el-icon><Document /></el-icon>
          <template #title>操作紀錄</template>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <!-- Main Container -->
    <el-container class="main-container">
      <!-- Header / 頁頭 -->
      <el-header class="header">
        <div class="header-left">
          <el-button
            type="text"
            class="toggle-btn"
            @click="isCollapse = !isCollapse"
          >
            <el-icon :size="20">
              <Expand v-if="isCollapse" />
              <Fold v-else />
            </el-icon>
          </el-button>
          <span class="page-title">{{ pageTitle }}</span>
        </div>

        <div class="header-right">
          <el-tag :type="authStore.isAdmin ? 'danger' : 'info'" class="role-tag">
            {{ authStore.user?.role || '使用者' }}
          </el-tag>
          <el-dropdown trigger="click">
            <div class="user-profile">
              <el-avatar :size="32" icon="UserFilled" class="user-avatar" />
              <span class="user-name">{{ authStore.user?.fullName }}</span>
              <el-icon><CaretBottom /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item disabled>帳號：{{ authStore.user?.username }}</el-dropdown-item>
                <el-dropdown-item divided @click="handleLogout">
                  <el-icon><SwitchButton /></el-icon>登出系統
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <!-- Main Content Area -->
      <el-main class="content-body">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import { ElMessageBox, ElMessage } from 'element-plus';
import {
  Food,
  DataBoard,
  ShoppingCart,
  Shop,
  User,
  UserFilled,
  Lock,
  Document,
  Expand,
  Fold,
  CaretBottom,
  SwitchButton,
} from '@element-plus/icons-vue';

const isCollapse = ref(false);
const authStore = useAuthStore();
const route = useRoute();
const router = useRouter();

const activeMenu = computed(() => route.path);

const pageTitle = computed(() => {
  if (route.path === '/') return '系統儀表板';
  if (route.path === '/stores') return '店家與菜單管理';
  if (route.path.startsWith('/orders')) return '團購便當場次';
  if (route.path === '/users') return '使用者管理';
  if (route.path === '/roles') return '角色管理 (側邊欄權限)';
  if (route.path === '/logs') return '操作紀錄';
  return '訂便當系統';
});

const handleLogout = () => {
  ElMessageBox.confirm('確定要登出系統嗎？', '登出確認', {
    confirmButtonText: '確定',
    cancelButtonText: '取消',
    type: 'warning',
  }).then(() => {
    authStore.logout();
    ElMessage.success('已成功登出');
    router.push('/login');
  });
};
</script>

<style scoped>
.admin-layout {
  height: 100vh;
  width: 100vw;
  overflow: hidden;
}

.aside {
  background-color: #1f2937;
  color: #fff;
  transition: width 0.3s ease;
  display: flex;
  flex-direction: column;
  box-shadow: 2px 0 8px rgba(0, 0, 0, 0.15);
}

.logo-container {
  height: 60px;
  display: flex;
  align-items: center;
  padding: 0 16px;
  background-color: #111827;
  overflow: hidden;
  white-space: nowrap;
}

.logo-icon {
  font-size: 24px;
  color: #10b981;
  margin-right: 12px;
}

.logo-title {
  font-size: 18px;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: 1px;
}

.el-menu-vertical {
  border-right: none;
  flex: 1;
}

.main-container {
  display: flex;
  flex-direction: column;
  background-color: #f3f4f6;
  height: 100vh;
  overflow: hidden;
}

.header {
  height: 60px;
  background-color: #ffffff;
  border-bottom: 1px solid #e5e7eb;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.toggle-btn {
  color: #4b5563;
  padding: 4px;
}

.page-title {
  font-size: 18px;
  font-weight: 600;
  color: #111827;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 16px;
}

.user-profile {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 6px;
  transition: background-color 0.2s;
}

.user-profile:hover {
  background-color: #f3f4f6;
}

.user-avatar {
  background-color: #3b82f6;
}

.user-name {
  font-size: 14px;
  font-weight: 500;
  color: #374151;
}

.content-body {
  padding: 24px;
  overflow-y: auto;
}
</style>
