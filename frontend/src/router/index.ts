import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '@/stores/auth';

import AdminLayout from '@/layouts/AdminLayout.vue';
import Login from '@/views/Login.vue';
import Dashboard from '@/views/Dashboard.vue';
import Stores from '@/views/Stores.vue';
import Orders from '@/views/Orders.vue';
import OrderDetail from '@/views/OrderDetail.vue';
import Users from '@/views/Users.vue';
import Roles from '@/views/Roles.vue';
import AuditLogs from '@/views/AuditLogs.vue';

const routes = [
  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: { guestOnly: true },
  },
  {
    path: '/',
    component: AdminLayout,
    meta: { requiresAuth: true },
    children: [
      {
        path: '',
        name: 'Dashboard',
        component: Dashboard,
        meta: { menuPath: '/' },
      },
      {
        path: 'orders',
        name: 'Orders',
        component: Orders,
        meta: { menuPath: '/orders' },
      },
      {
        path: 'orders/:id',
        name: 'OrderDetail',
        component: OrderDetail,
        meta: { menuPath: '/orders' },
      },
      {
        path: 'stores',
        name: 'Stores',
        component: Stores,
        meta: { menuPath: '/stores' },
      },
      {
        path: 'users',
        name: 'Users',
        component: Users,
        meta: { menuPath: '/users' },
      },
      {
        path: 'roles',
        name: 'Roles',
        component: Roles,
        meta: { menuPath: '/roles' },
      },
      {
        path: 'logs',
        name: 'AuditLogs',
        component: AuditLogs,
        meta: { menuPath: '/logs' },
      },
    ],
  },
  {
    path: '/:pathMatch(.*)*',
    redirect: '/',
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

router.beforeEach((to, _from, next) => {
  const authStore = useAuthStore();

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    next('/login');
  } else if (to.meta.guestOnly && authStore.isAuthenticated) {
    next('/');
  } else {
    // Check menu permission if route specifies a menuPath
    if (to.meta.menuPath && !authStore.hasMenuAccess(to.meta.menuPath as string)) {
      next('/');
      return;
    }
    next();
  }
});

export default router;
