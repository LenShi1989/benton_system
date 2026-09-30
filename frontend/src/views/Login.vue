<template>
  <div class="login-container">
    <el-card class="login-card" shadow="always">
      <div class="login-header">
        <el-icon class="app-logo"><Food /></el-icon>
        <h2>訂便當系統登入</h2>
        <p class="subtitle">請輸入您的帳號密碼進行身分驗證</p>
      </div>

      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="loginRules"
        label-position="top"
        size="large"
        @keyup.enter="handleLogin"
      >
        <el-form-item label="帳號" prop="username">
          <el-input
            v-model="loginForm.username"
            placeholder="請輸入帳號 (例如: admin)"
            :prefix-icon="User"
          />
        </el-form-item>

        <el-form-item label="密碼" prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            placeholder="請輸入密碼 (例如: admin123)"
            :prefix-icon="Lock"
            show-password
          />
        </el-form-item>

        <div class="demo-accounts">
          <p>預設測試帳號：</p>
          <div class="account-tags">
            <el-tag type="danger" style="cursor: pointer" @click="fillAccount('admin', 'admin123')">
              管理員: admin / admin123
            </el-tag>
            <el-tag type="info" style="cursor: pointer" @click="fillAccount('user1', 'user123')">
              一般用戶: user1 / user123
            </el-tag>
          </div>
        </div>

        <el-form-item style="margin-top: 24px">
          <el-button
            type="primary"
            class="submit-btn"
            :loading="loading"
            @click="handleLogin"
          >
            登 入
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '@/stores/auth';
import api from '@/api';
import { ElMessage, FormInstance, FormRules } from 'element-plus';
import { Food, User, Lock } from '@element-plus/icons-vue';

const router = useRouter();
const authStore = useAuthStore();

const loginFormRef = ref<FormInstance>();
const loading = ref(false);

const loginForm = reactive({
  username: '',
  password: '',
});

const loginRules = reactive<FormRules>({
  username: [{ required: true, message: '請輸入帳號', trigger: 'blur' }],
  password: [{ required: true, message: '請輸入密碼', trigger: 'blur' }],
});

const fillAccount = (u: string, p: string) => {
  loginForm.username = u;
  loginForm.password = p;
};

const handleLogin = async () => {
  if (!loginFormRef.value) return;
  await loginFormRef.value.validate(async (valid) => {
    if (!valid) return;
    loading.value = true;
    try {
      const res = await api.post('/auth/login', loginForm);
      authStore.setAuth(res.data.token, {
        username: res.data.username,
        fullName: res.data.fullName,
        role: res.data.role,
        allowedMenus: res.data.allowedMenus || ['/', '/orders'],
      });
      ElMessage.success(`歡迎回來，${res.data.fullName}！`);
      router.push('/');
    } catch (err: any) {
      const msg = err.response?.data?.message || '登入失敗，請檢查帳號密碼';
      ElMessage.error(msg);
    } finally {
      loading.value = false;
    }
  });
};
</script>

<style scoped>
.login-container {
  height: 100vh;
  width: 100vw;
  display: flex;
  justify-content: center;
  align-items: center;
  background: linear-gradient(135deg, #1e293b 0%, #0f172a 100%);
}

.login-card {
  width: 420px;
  border-radius: 12px;
  padding: 12px 16px;
}

.login-header {
  text-align: center;
  margin-bottom: 24px;
}

.app-logo {
  font-size: 48px;
  color: #10b981;
}

.login-header h2 {
  margin: 8px 0 4px;
  color: #1e293b;
  font-size: 22px;
}

.subtitle {
  color: #64748b;
  font-size: 13px;
}

.demo-accounts {
  background-color: #f8fafc;
  padding: 12px;
  border-radius: 8px;
  font-size: 12px;
  color: #475569;
  margin-top: 12px;
}

.account-tags {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-top: 6px;
}

.submit-btn {
  width: 100%;
  font-weight: 600;
  letter-spacing: 2px;
}
</style>
