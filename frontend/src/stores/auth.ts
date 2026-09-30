import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import api from '@/api';

export interface UserInfo {
  username: string;
  fullName: string;
  role: string;
  allowedMenus: string[];
}

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('token'));
  const user = ref<UserInfo | null>(
    localStorage.getItem('user') ? JSON.parse(localStorage.getItem('user')!) : null
  );

  const isAuthenticated = computed(() => !!token.value);
  const isAdmin = computed(() => user.value?.role === 'Admin');
  const allowedMenus = computed(() => user.value?.allowedMenus || ['/', '/orders']);

  function setAuth(newToken: string, userInfo: UserInfo) {
    token.value = newToken;
    user.value = userInfo;
    localStorage.setItem('token', newToken);
    localStorage.setItem('user', JSON.stringify(userInfo));
  }

  function logout() {
    token.value = null;
    user.value = null;
    localStorage.removeItem('token');
    localStorage.removeItem('user');
  }

  function hasMenuAccess(path: string): boolean {
    if (isAdmin.value) return true;
    return allowedMenus.value.includes(path);
  }

  async function checkAuth() {
    if (!token.value) return false;
    try {
      const res = await api.get('/auth/me');
      setAuth(res.data.token, {
        username: res.data.username,
        fullName: res.data.fullName,
        role: res.data.role,
        allowedMenus: res.data.allowedMenus || ['/', '/orders'],
      });
      return true;
    } catch {
      logout();
      return false;
    }
  }

  return {
    token,
    user,
    isAuthenticated,
    isAdmin,
    allowedMenus,
    setAuth,
    logout,
    hasMenuAccess,
    checkAuth,
  };
});
